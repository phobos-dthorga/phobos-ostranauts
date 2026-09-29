using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Crew;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Notices;
using Phobos.Ostranauts.Framework.Persistence;
using Phobos.Ostranauts.Framework.Processing;
using PhobosManufacturing.Core;

namespace PhobosManufacturing;

/// <summary>The A2 cabin air regulator. Every couple of seconds while switched on, installed, intact and powered, it
/// reads its room, adds oxygen from its linked oxygen store up to the set point (never past the oxygen-fraction cap),
/// then nitrogen from its linked nitrogen store up to the pressure set point, each within its valve flow limit for the
/// time that passed. Gas leaves the store's record and enters the room as the game's own species. A room below
/// 10 kPa is treated as breached and is never fed. Its switches and links are saved and it keeps working after a
/// reload, like the game's own air pumps.</summary>
internal static class RegulatorService
{
    private sealed class Session
    {
        internal RegulatorState State = new();
        internal bool Protected, BreachNoticed;
        internal double LastTick = double.NaN;
        internal string Status = Text.Get("Regulator.off");
    }
    private static ConditionalWeakTable<CondOwner, Session> sessions = new();
    internal static void Reset() => sessions = new();
    private static ObjectStateStore Store(CondOwner co) => new(co.mapGUIPropMaps, RegulatorRules.Record, Plugin.Id, 1);
    private static Session Get(CondOwner co)
    {
        if (sessions.TryGetValue(co, out var found)) return found;
        var s = new Session();
        var status = Store(co).Read(out var fields);
        if (status == SavedStateStatus.Ready) { try { s.State = RegulatorState.Read(fields); } catch (Exception e) { s.Protected = true; Plugin.Log(e.ToString()); } }
        else if (status != SavedStateStatus.Missing) s.Protected = true;
        if (s.Protected) s.Status = Text.Get("Regulator.protected");
        sessions.Add(co, s);
        return s;
    }
    private static bool Save(CondOwner co, Session s)
    {
        // A holding regulator changes nothing between ticks; its record is then left untouched.
        if (Store(co).TryWriteIfChanged(s.State.Save())) return true;
        s.Protected = true; s.Status = Text.Get("Regulator.protected"); return false;
    }
    internal static RegulatorState StateOf(CondOwner co) => Get(co).State;
    internal static bool Protected(CondOwner co) => Get(co).Protected;

    /// <summary>Oxygen or nitrogen stores of any size within one tile or along a gas line.</summary>
    internal static IEnumerable<CondOwner> Candidates(CondOwner co, string commodity) => (co.ship?.GetCOs(null, false, false, true) ?? Enumerable.Empty<CondOwner>())
        .Where(c => c != null && !c.bDestroyed && c.ship == co.ship && c.HasCond("IsInstalled") && GasStores.Holds(c.strCODef, commodity) &&
            GasLine.Connection(co, RegulatorRules.Inlet, c) != null)
        .OrderBy(c => c.strID, StringComparer.Ordinal).ToArray();
    private static CondOwner? Source(CondOwner co, string id, string commodity, out string why)
    {
        why = Text.Get("Regulator.no_store");
        if (id.Length == 0) return null;
        var store = CrewWork.Resolve(id);
        why = Text.Get("Regulator.store_missing");
        if (store == null || store.ship != co.ship || !GasStores.Holds(store.strCODef, commodity) || GasLine.Connection(co, RegulatorRules.Inlet, store) == null) return null;
        why = Text.Get("Regulator.store_not_ready");
        if (!NativeFluidRoute.EndpointReady(store) || BulkVessel.Protected(store) || CommodityReservations.Held(store.strID)) return null;
        why = ""; return store;
    }
    private static bool Running(CondOwner co) => Content.Ready && co.strCODef == RegulatorRules.Installed && co.HasCond("IsInstalled") && !co.HasCond("IsDamaged") &&
        !co.HasCond("IsLocked") && co.HasCond("IsPowered") && co.ship != null && (int)co.ship.LoadState >= 2;

    /// <summary>One regulation step, from the plugin's short timer.</summary>
    internal static void Tick(CondOwner co)
    {
        var s = Get(co);
        double now = StarSystem.fEpoch, hours = double.IsNaN(s.LastTick) ? 0 : (now - s.LastTick) / Units.SecondsPerHour;
        s.LastTick = now;
        if (s.Protected) return;
        if (!s.State.On) { s.Status = Text.Get("Regulator.off"); return; }
        if (!Running(co)) { s.Status = Text.Get(co.HasCond("IsDamaged") ? "Regulator.repair_first" : "Regulator.no_power"); return; }
        if (hours <= 0 || hours > 1) return;
        try
        {
            var air = RoomHeat.Read(co);
            if (air == null || air.PressureKPa < RegulatorRules.MinRoomKPa)
            {
                s.Status = Text.Get("Regulator.breach", RegulatorRules.MinRoomKPa);
                if (!s.BreachNoticed) { s.BreachNoticed = true; PlayerNotices.Post(co.ship, "PhobosManufacturing.regulator", NoticeLevel.Caution, Text.Get("Regulator.breach_notice", co.strNameFriendly)); }
                return;
            }
            s.BreachNoticed = false;
            var lines = new List<string>();
            double o2 = Feed(co, s, air, "O2", ManufacturingRules.Oxygen, s.State.OxygenStore,
                RegulatorRules.OxygenMoles(air.Mols, air.PressureKPa, NativeGasCanister.Moles("O2", RoomGas.HeldKg(air, "O2")), s.State.OxygenKPa), RegulatorRules.OxygenKgPerHour * hours, lines);
            s.State.AddedOxygenKg += o2;
            if (s.State.PressureKPa > 0)
            {
                // Oxygen just added is still pending in the room's gas, so it counts against the nitrogen shortfall.
                double shortfall = Math.Max(0, RegulatorRules.NitrogenMoles(air.Mols, air.PressureKPa, s.State.PressureKPa) - NativeGasCanister.Moles("O2", o2));
                double n2 = Feed(co, s, air, "N2", ManufacturingRules.Nitrogen, s.State.NitrogenStore, shortfall, RegulatorRules.NitrogenKgPerHour * hours, lines);
                s.State.AddedNitrogenKg += n2;
            }
            s.Status = lines.Count == 0 ? Text.Get("Regulator.holding") : string.Join(" ", lines);
            Save(co, s);
        }
        catch (Exception e) { Plugin.Log(e.ToString()); s.Status = Text.Get("Regulator.fault"); }
    }
    /// <summary>Moves one gas from its store into the room, bounded by the deficit, the flow limit and the store; returns kg.</summary>
    private static double Feed(CondOwner co, Session s, RoomHeat.Air air, string species, string commodity, string storeId, double deficitMoles, double limitKg, List<string> lines)
    {
        double wantKg = Math.Min(NativeGasCanister.Kilograms(species, deficitMoles), limitKg);
        if (wantKg <= 1e-6) return 0;
        var store = Source(co, storeId, commodity, out string why);
        if (store == null) { lines.Add(Text.Get("Regulator.needs", Text.Get("Filler.gas_" + species), why)); return 0; }
        BufferedDrains.Settle(store);
        double taken = BulkVessel.Drain(store, Math.Min(wantKg, BulkVessel.Snapshot(store).AvailableKg), Text.Get("Regulator.draw_reason"), false);
        if (taken <= 0) { lines.Add(Text.Get("Regulator.needs", Text.Get("Filler.gas_" + species), Text.Get("Regulator.store_empty"))); return 0; }
        RoomGas.Emit(air, species, taken);
        lines.Add(Text.Get("Regulator.feeding", Text.Get("Filler.gas_" + species)));
        return taken;
    }

    internal static EquipmentState State(CondOwner co)
    {
        var s = Get(co);
        if (s.Protected || co.HasCond("IsDamaged") || co.HasCond("IsLocked")) return EquipmentState.Blocked;
        if (!s.State.On) return EquipmentState.Paused;
        return Running(co) ? EquipmentState.Running : EquipmentState.Waiting;
    }
    internal static string Describe(CondOwner co)
    {
        var s = Get(co);
        if (s.Protected) return Text.Get("Regulator.protected");
        var air = RoomHeat.Read(co);
        string room = air == null ? Text.Get("Regulator.no_room") :
            Text.Get("Regulator.room", air.Room.GetCondAmount("StatGasPpO2"), air.PressureKPa);
        string Store(string id) => CrewWork.Resolve(id) is CondOwner c && BulkVessels.IsVessel(c) ? Text.Get("Regulator.store_line", ObjectPresentation.Name(c), BulkVessel.Snapshot(c).AvailableKg) : ObjectPresentation.Name(id);
        return string.Join("\n", new[]
        {
            s.Status, room,
            Text.Get("Regulator.targets", s.State.OxygenKPa, s.State.PressureKPa > 0 ? Text.Get("Regulator.kpa", s.State.PressureKPa) : Text.Get("Regulator.pressure_off")),
            Text.Get("Regulator.stores", Store(s.State.OxygenStore), Store(s.State.NitrogenStore)),
            Text.Get("Regulator.totals", s.State.AddedOxygenKg, s.State.AddedNitrogenKg),
            co.HasCond("IsPowered") ? Text.Get("Content.powered") : Text.Get("Content.no_power")
        });
    }
    internal static string? MaintenanceReason(CondOwner co) => RegulatorRules.IsFamily(co.strCODef) && Get(co).Protected ? Text.Get("Maintenance.protected") : null;
    internal static bool Command(CondOwner co, ConsoleBinding? binding, string action, out string message)
    {
        message = Content.Ready ? Content.Access(co, binding) ?? "" : Content.Status;
        if (message.Length > 0) return false;
        var s = Get(co);
        if (action == "status") { message = Describe(co); return true; }
        if (action == "accept")
        {
            var status = Store(co).Read(out var fields);
            RegulatorState? state = null;
            try { state = status == SavedStateStatus.Ready ? RegulatorState.Read(fields) : status == SavedStateStatus.Missing ? new RegulatorState() : null; } catch (Exception e) { Plugin.Log(e.ToString()); }
            if (state == null) { message = Text.Get("Regulator.accept_unavailable"); return false; }
            s.State = state; s.Protected = false;
            bool ok = Save(co, s); message = Text.Get(ok ? "Regulator.accept_done" : "Regulator.accept_unavailable"); return ok;
        }
        if (s.Protected) { message = Text.Get("Regulator.protected"); return false; }
        string[] parts = action.Split(new[] { ':' }, 2);
        string verb = parts[0], arg = parts.Length > 1 ? parts[1] : "";
        switch (verb)
        {
            case "on": case "start": s.State.On = true; message = Text.Get("Regulator.switched_on"); break;
            case "off": case "pause": s.State.On = false; message = Text.Get("Regulator.switched_off"); break;
            case "o2":
                if (!double.TryParse(arg, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double o2) || !RegulatorRules.OxygenTargets.Contains(o2))
                { message = Text.Get("Regulator.invalid_target"); return false; }
                s.State.OxygenKPa = o2; message = Text.Get("Regulator.target_set"); break;
            case "pressure":
                if (!double.TryParse(arg, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double p) || !RegulatorRules.PressureTargets.Contains(p))
                { message = Text.Get("Regulator.invalid_target"); return false; }
                s.State.PressureKPa = p; message = Text.Get("Regulator.target_set"); break;
            case "oxygen": case "nitrogen":
            {
                string commodity = verb == "oxygen" ? ManufacturingRules.Oxygen : ManufacturingRules.Nitrogen;
                if (arg != "none" && !Candidates(co, commodity).Any(c => c.strID == arg)) { message = Text.Get("Regulator.link_missing"); return false; }
                if (verb == "oxygen") s.State.OxygenStore = arg == "none" ? "" : arg; else s.State.NitrogenStore = arg == "none" ? "" : arg;
                message = Text.Get(arg == "none" ? "Regulator.unlinked" : "Regulator.linked"); break;
            }
            default: message = Text.Get("Content.unsupported_action"); return false;
        }
        if (!Save(co, s)) { message = Text.Get("Regulator.protected"); return false; }
        return true;
    }
}
