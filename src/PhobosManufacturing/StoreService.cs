using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Hazards;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Notices;
using Phobos.Ostranauts.Framework.Processing;
using PhobosManufacturing.Core;

namespace PhobosManufacturing;

/// <summary>The H2 hydrogen and M2 methane stores: passive Framework bulk vessels with a Leak damage policy.
/// Commands are status, owner-confirmed acceptance and an explicit vent overboard. Their hazard: a damaged store
/// leaks (hydrogen to space, methane into the room as the game's own CH4) until repaired, and with oxygen in the
/// room and an ignition source its contents burn through the game's own explosion machinery, consuming the
/// room's oxygen, heating its air and, for methane, adding the carbon dioxide the burn makes.</summary>
internal static class StoreService
{
    private sealed class Session { internal double LastTick; }
    private static ConditionalWeakTable<CondOwner, Session> sessions = new();
    internal static void Reset() => sessions = new();
    internal static double[] VentChoices(FuelStore fuel) => fuel == FuelStores.Methane ? new double[] { 5, 20, 50, 160 } : new double[] { 1, 5, 10, 24 };
    private static FuelStore Fuel(CondOwner co) => FuelStores.For(co.strCODef) ?? throw new InvalidOperationException("Not a fuel store: " + co.strCODef);
    private static string T(FuelStore fuel, string key, params object[] args) => Text.Get(fuel.TextPrefix + "." + key, args);

    internal static string Describe(CondOwner co)
    {
        var fuel = Fuel(co);
        if (BulkVessel.Protected(co)) return Text.Get("Store.protected");
        var s = BulkVessel.Snapshot(co);
        string Names(IEnumerable<CondOwner> list) { var n = string.Join(", ", list.Select(ObjectPresentation.Name)); return n.Length == 0 ? ConsoleText.Get("not_selected") : n; }
        var feeding = fuel == FuelStores.Hydrogen ? Linked(co, c => ProcessorRules.IsFamily(c.strCODef) && ProcessorService.StorePeer(c) == co.strID)
            : Linked(co, c => SabatierRules.IsFamily(c.strCODef) && SabatierService.MethanePeer(c) == co.strID);
        string text = T(fuel, "status", s.ServiceKg, s.CapacityKg, Names(feeding));
        if (fuel == FuelStores.Hydrogen)
            text += "\n" + Text.Get("Store.drawing", Names(Linked(co, c => SabatierRules.IsFamily(c.strCODef) && SabatierService.HydrogenPeer(c) == co.strID)));
        return text + (co.HasCond("IsDamaged") ? "\n" + T(fuel, "leaking", fuel.LeakKgPerHour) : "");
    }
    private static IEnumerable<CondOwner> Linked(CondOwner store, Func<CondOwner, bool> match) => (store.ship?.GetCOs(null, false, false, true) ?? Enumerable.Empty<CondOwner>())
        .Where(c => c != null && !c.bDestroyed && match(c)).OrderBy(c => c.strID, StringComparer.Ordinal).ToArray();
    internal static EquipmentState State(CondOwner co) => BulkVessel.Protected(co) || co.HasCond("IsDamaged") || co.HasCond("IsLocked") ? EquipmentState.Blocked : EquipmentState.Ready;
    internal static string? MaintenanceReason(CondOwner co, bool dismantle)
    {
        var fuel = FuelStores.For(co.strCODef);
        if (fuel == null) return null;
        if (BulkVessel.Protected(co)) return Text.Get("Maintenance.protected");
        return dismantle && BulkVessel.Snapshot(co).ServiceKg > 1e-8 ? T(fuel, "maintenance_" + fuel.Commodity) : null;
    }
    internal static bool Command(CondOwner co, ConsoleBinding? binding, string action, out string message)
    {
        var fuel = Fuel(co);
        message = Text.Get("Store.protected");
        if (!Content.Ready) { message = Content.Status; return false; }
        message = Content.Access(co, binding) ?? "";
        if (message.Length > 0) return false;
        if (action == "accept") { bool ok = BulkVessel.Accept(co, Plugin.Log); message = Text.Get(ok ? "Store.accept_done" : "Store.accept_unavailable"); return ok; }
        if (action == "pause") { message = Text.Get("Store.nothing_to_pause"); return true; }
        if (action == "status") { message = Describe(co); return true; }
        if (BulkVessel.Protected(co)) { message = Text.Get("Store.protected"); return false; }
        if (!action.StartsWith("vent:", StringComparison.Ordinal)) { message = Text.Get("Content.unsupported_action"); return false; }
        if (!double.TryParse(action.Substring(5), NumberStyles.Float, CultureInfo.InvariantCulture, out double kg) || !ManufacturingRules.Finite(kg) || kg <= 0) { message = Text.Get("Store.invalid_amount"); return false; }
        try
        {
            double vented = BulkVessel.Drain(co, kg, Text.Get("Store.vent_reason"));
            message = vented > 0 ? T(fuel, "vented", vented) : Text.Get("Store.nothing_vented");
            if (vented > 0) PlayerNotices.Post(co.ship, "PhobosManufacturing.vent", NoticeLevel.Info, T(fuel, "vent_log", co.strNameFriendly, vented));
            return vented > 0;
        }
        catch (Exception e) { Plugin.Log(e.ToString()); message = Text.Get("Store.protected"); return false; }
    }

    /// <summary>After the native damage switch (contents stayed in service under the Leak policy): burn now if the
    /// room allows it, otherwise the leak begins.</summary>
    internal static void Damaged(CondOwner co)
    {
        if (co == null || co.bDestroyed || !co.HasCond("IsDamaged") || !co.HasCond("IsInstalled")) return;
        try
        {
            var fuel = Fuel(co);
            if (!Release(co, fuel, "damaged_log"))
                PlayerNotices.Post(co.ship, "PhobosManufacturing.leak", NoticeLevel.Caution, T(fuel, "leak_notice", co.strNameFriendly, fuel.LeakKgPerHour), T(fuel, "leak_banner"));
        }
        catch (Exception e) { Plugin.Log(e.ToString()); }
    }
    /// <summary>Native destruction proceeds; contents still aboard face the same release rule first.</summary>
    internal static void Destroying(CondOwner co)
    {
        if (co == null || CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading || co.HasCond("IsModeSwitching", false)) return;
        try { Release(co, Fuel(co), "destroyed_log"); } catch (Exception e) { Plugin.Log(e.ToString()); }
    }
    /// <summary>Every couple of seconds while damaged: the leak advances (to space, or into the room as a native
    /// species), and any new ignition source lights what remains.</summary>
    internal static void Tick(CondOwner co)
    {
        if (co == null || co.bDestroyed || !co.HasCond("IsDamaged") || !co.HasCond("IsInstalled") || co.ship == null || (int)co.ship.LoadState < 2 || BulkVessel.Protected(co)) return;
        var s = sessions.GetValue(co, _ => new Session { LastTick = StarSystem.fEpoch });
        double now = StarSystem.fEpoch, hours = (now - s.LastTick) / Units.SecondsPerHour; s.LastTick = now;
        if (hours <= 0 || hours > 1) return;
        try
        {
            var fuel = Fuel(co);
            if (Release(co, fuel, "damaged_log")) return;
            double leak = fuel.Spec.LeakKg(hours, BulkVessel.Snapshot(co).ServiceKg);
            if (leak <= 0) return;
            var air = fuel.LeakSpecies == null ? null : RoomHeat.Read(co);
            // Hydrogen always leaks to space; methane into the room, or to space where there is no air.
            double drained = BulkVessel.Drain(co, leak, T(fuel, fuel.LeakSpecies != null && air == null ? "leak_reason_space" : "leak_reason"));
            if (air != null && drained > 0) RoomGas.Emit(air, fuel.LeakSpecies!, drained);
        }
        catch (Exception e) { Plugin.Log(e.ToString()); }
    }
    /// <summary>Burns the whole content when the room has oxygen and an ignition source; returns whether it did.</summary>
    private static bool Release(CondOwner co, FuelStore fuel, string logKey)
    {
        double held = BulkVessel.Snapshot(co).ServiceKg;
        if (held <= 1e-8 || BulkVessel.Protected(co) || !Ignites(co, out var air)) return false;
        var burn = fuel.Burn(held, RoomGas.HeldKg(air!, "O2"));
        if (burn.BurnedKg <= 0) return false;
        // The whole content leaves the record first; what did not burn is lost with the blast.
        if (BulkVessel.Drain(co, held, Text.Get("Store.deflagration_reason")) <= 0) return false;
        Blast(co, air!, burn);
        string log = T(fuel, logKey, co.strNameFriendly, burn.BurnedKg, burn.OxygenKg, burn.EnergyKJ / 1000, burn.LostKg);
        Plugin.Log(log);
        PlayerNotices.Post(co.ship, "PhobosManufacturing.deflagration", NoticeLevel.Caution, log, T(fuel, "deflagration_banner"));
        return true;
    }
    /// <summary>Fuel already out of its vessel (a damaged reactor's hydrogen): burns when the room allows it and
    /// returns whether it did; otherwise the caller lets it escape.</summary>
    internal static bool Ignite(CondOwner source, FuelStore fuel, double kg, string logKey)
    {
        if (kg <= 1e-9 || !Ignites(source, out var air)) return false;
        var burn = fuel.Burn(kg, RoomGas.HeldKg(air!, "O2"));
        if (burn.BurnedKg <= 0) return false;
        Blast(source, air!, burn);
        Plugin.Log(Text.Get(logKey, source.strNameFriendly, burn.BurnedKg, burn.OxygenKg, burn.EnergyKJ / 1000, burn.LostKg));
        return true;
    }
    private static bool Ignites(CondOwner co, out RoomHeat.Air? air)
    {
        air = RoomHeat.Read(co);
        return air != null && FuelStores.Fate(air.Room.GetCondAmount("StatGasPpO2"), FireInRoom(co, air), HearthWorking(co, air), SparkingDevice(co, air)) == HydrogenFate.Deflagrate;
    }
    /// <summary>The room loses the burned oxygen and gains the burn's native products; heat goes into its air up to
    /// the industrial ceiling and the rest is carried by the game's own explosion.</summary>
    private static void Blast(CondOwner co, RoomHeat.Air air, Deflagration burn)
    {
        RoomGas.Consume(air, "O2", burn.OxygenKg);
        foreach (var product in burn.RoomProductsKg) if (product.Value > 0) RoomGas.Emit(air, product.Key, product.Value);
        double roomRise = Math.Max(0, 333.15 - (air.Kelvin + air.PendingKelvin));
        double roomKWh = Math.Min(burn.EnergyKJ / 3600, roomRise * air.Mols * RoomHeat.GasHeatCapacityJPerMolK / RoomHeat.JoulesPerKilowattHour);
        if (roomKWh > 0) RoomHeat.Deposit(air, roomKWh);
        NativeExplosions.Spawn(co.ship, co.tf.position, FuelStores.DeflagrationDefinition(burn));
    }
    private static bool SameRoom(CondOwner c, RoomHeat.Air air) => c.ship?.GetRoomAtWorldCoords1(c.GetPos(), false)?.CO == air.Room;
    private static IEnumerable<CondOwner> Nearby(CondOwner co) => co.ship?.GetCOs(null, false, false, true).Where(c => c != null && !c.bDestroyed && c != co) ?? Enumerable.Empty<CondOwner>();
    private static bool FireInRoom(CondOwner co, RoomHeat.Air air) => Nearby(co).Any(c => c.HasCond("IsFire") && !c.HasCond("IsExtinguished") && SameRoom(c, air));
    private static bool HearthWorking(CondOwner co, RoomHeat.Air air) => Nearby(co).Any(c => RefineryRules.IsFamily(c.strCODef) && c.HasCond(ManufacturingRules.Working) && c.HasCond("IsPowered") && SameRoom(c, air));
    /// <summary>The game's own spark rule: a powered device at half its damage or more throws sparks.</summary>
    private static bool SparkingDevice(CondOwner co, RoomHeat.Air air) => Nearby(co).Any(c => c.HasCond("IsPowered") && c.GetCondAmount("StatDamageMax") > 0 &&
        c.GetCondAmount("StatDamage") >= .5 * c.GetCondAmount("StatDamageMax") && SameRoom(c, air));
}
