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

/// <summary>The H2 store: a passive Framework bulk vessel of hydrogen with a Leak damage policy. Commands are
/// status, owner-confirmed acceptance and an explicit vent overboard. Its hazard: a damaged store leaks to
/// space until repaired, and with oxygen in the room and an ignition source its contents deflagrate through
/// the game's own explosion machinery, consuming the room's oxygen and heating its air.</summary>
internal static class HydrogenService
{
    private sealed class Session { internal double LastTick; internal bool Leaking; }
    private static ConditionalWeakTable<CondOwner, Session> sessions = new();
    internal static void Reset() => sessions = new();
    internal static readonly double[] VentChoices = { 1, 5, 10, 24 };

    internal static string Describe(CondOwner co)
    {
        if (BulkVessel.Protected(co)) return Text.Get("Store.protected");
        var s = BulkVessel.Snapshot(co);
        string cells = string.Join(", ", Cells(co).Select(ObjectPresentation.Name));
        if (cells.Length == 0) cells = ConsoleText.Get("not_selected");
        return Text.Get("Store.status", s.ServiceKg, s.CapacityKg, cells) + (co.HasCond("IsDamaged") ? "\n" + Text.Get("Store.leaking", HydrogenRules.LeakKgPerHour) : "");
    }
    internal static IEnumerable<CondOwner> Cells(CondOwner store) => (store.ship?.GetCOs(null, false, false, true) ?? Enumerable.Empty<CondOwner>())
        .Where(c => c != null && !c.bDestroyed && ProcessorRules.IsFamily(c.strCODef) && ProcessorService.StorePeer(c) == store.strID).OrderBy(c => c.strID, StringComparer.Ordinal).ToArray();
    internal static EquipmentState State(CondOwner co) => BulkVessel.Protected(co) || co.HasCond("IsDamaged") || co.HasCond("IsLocked") ? EquipmentState.Blocked : EquipmentState.Ready;
    internal static string? MaintenanceReason(CondOwner co, bool dismantle)
    {
        if (!HydrogenRules.IsFamily(co.strCODef)) return null;
        if (BulkVessel.Protected(co)) return Text.Get("Maintenance.protected");
        return dismantle && BulkVessel.Snapshot(co).ServiceKg > 1e-8 ? Text.Get("Store.maintenance_hydrogen") : null;
    }
    internal static bool Command(CondOwner co, ConsoleBinding? binding, string action, out string message)
    {
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
            message = vented > 0 ? Text.Get("Store.vented", vented) : Text.Get("Store.nothing_vented");
            if (vented > 0) PlayerNotices.Post(co.ship, "PhobosManufacturing.vent", NoticeLevel.Info, Text.Get("Store.vent_log", co.strNameFriendly, vented));
            return vented > 0;
        }
        catch (Exception e) { Plugin.Log(e.ToString()); message = Text.Get("Store.protected"); return false; }
    }

    /// <summary>After the native damage switch (contents stayed in service under the Leak policy): deflagrate
    /// now if the room allows it, otherwise the leak begins.</summary>
    internal static void Damaged(CondOwner co)
    {
        if (co == null || co.bDestroyed || !co.HasCond("IsDamaged") || !co.HasCond("IsInstalled")) return;
        try
        {
            if (!Release(co, "Store.damaged_log")) { sessions.GetValue(co, _ => new Session()).Leaking = true; PlayerNotices.Post(co.ship, "PhobosManufacturing.leak", NoticeLevel.Caution, Text.Get("Store.leak_notice", co.strNameFriendly, HydrogenRules.LeakKgPerHour), Text.Get("Store.leak_banner")); }
        }
        catch (Exception e) { Plugin.Log(e.ToString()); }
    }
    /// <summary>Native destruction proceeds; contents still aboard face the same release rule first.</summary>
    internal static void Destroying(CondOwner co)
    {
        if (co == null || CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading || co.HasCond("IsModeSwitching", false)) return;
        try { Release(co, "Store.destroyed_log"); } catch (Exception e) { Plugin.Log(e.ToString()); }
    }
    /// <summary>Every couple of seconds while damaged: the leak advances, and any new ignition source lights
    /// what remains. Called from the plugin's passive scan for installed damaged stores.</summary>
    internal static void Tick(CondOwner co)
    {
        if (co == null || co.bDestroyed || !co.HasCond("IsDamaged") || !co.HasCond("IsInstalled") || co.ship == null || (int)co.ship.LoadState < 2 || BulkVessel.Protected(co)) return;
        var s = sessions.GetValue(co, _ => new Session { LastTick = StarSystem.fEpoch });
        double now = StarSystem.fEpoch, hours = (now - s.LastTick) / Units.SecondsPerHour; s.LastTick = now;
        if (hours <= 0 || hours > 1) return;
        try
        {
            if (Release(co, "Store.damaged_log")) return;
            double held = BulkVessel.Snapshot(co).ServiceKg;
            double leak = HydrogenRules.Spec.LeakKg(hours, held);
            if (leak > 0) BulkVessel.Drain(co, leak, Text.Get("Store.leak_reason"));
        }
        catch (Exception e) { Plugin.Log(e.ToString()); }
    }
    /// <summary>Deflagrates the contents when the room has oxygen and an ignition source; returns whether it did.</summary>
    private static bool Release(CondOwner co, string logKey)
    {
        double held = BulkVessel.Snapshot(co).ServiceKg;
        if (held <= 1e-8 || BulkVessel.Protected(co)) return false;
        var air = RoomHeat.Read(co);
        if (air == null) return false;
        double oxygenKPa = air.Room.GetCondAmount("StatGasPpO2");
        if (HydrogenRules.Fate(oxygenKPa, FireInRoom(co, air), HearthWorking(co, air), SparkingDevice(co, air)) != HydrogenFate.Deflagrate) return false;
        var burn = HydrogenRules.Burn(held, RoomGas.HeldKg(air, "O2"));
        if (burn.BurnedKg <= 0) return false;
        // The whole content leaves the record first; what did not burn is lost with the blast.
        double gone = BulkVessel.Drain(co, held, Text.Get("Store.deflagration_reason"));
        if (gone <= 0) return false;
        RoomGas.Consume(air, "O2", burn.OxygenKg);
        // Heat into the air up to the furnace room ceiling; the rest is blast energy carried by the explosion.
        double roomRise = Math.Max(0, 333.15 - (air.Kelvin + air.PendingKelvin));
        double roomKWh = Math.Min(burn.EnergyKJ / 3600, roomRise * air.Mols * RoomHeat.GasHeatCapacityJPerMolK / RoomHeat.JoulesPerKilowattHour);
        if (roomKWh > 0) RoomHeat.Deposit(air, roomKWh);
        NativeExplosions.Spawn(co.ship, co.tf.position, HydrogenRules.DeflagrationDefinition(burn));
        string log = Text.Get(logKey, co.strNameFriendly, burn.BurnedKg, burn.OxygenKg, burn.EnergyKJ / 1000, burn.LostKg);
        Plugin.Log(log);
        PlayerNotices.Post(co.ship, "PhobosManufacturing.deflagration", NoticeLevel.Caution, log, Text.Get("Store.deflagration_banner"));
        return true;
    }
    private static bool SameRoom(CondOwner c, RoomHeat.Air air) => c.ship?.GetRoomAtWorldCoords1(c.GetPos(), false)?.CO == air.Room;
    private static IEnumerable<CondOwner> Nearby(CondOwner co) => co.ship?.GetCOs(null, false, false, true).Where(c => c != null && !c.bDestroyed && c != co) ?? Enumerable.Empty<CondOwner>();
    private static bool FireInRoom(CondOwner co, RoomHeat.Air air) => Nearby(co).Any(c => c.HasCond("IsFire") && !c.HasCond("IsExtinguished") && SameRoom(c, air));
    private static bool HearthWorking(CondOwner co, RoomHeat.Air air) => Nearby(co).Any(c => RefineryRules.IsFamily(c.strCODef) && c.HasCond(ManufacturingRules.Working) && c.HasCond("IsPowered") && SameRoom(c, air));
    /// <summary>The game's own spark rule: a powered device at half its damage or more throws sparks.</summary>
    private static bool SparkingDevice(CondOwner co, RoomHeat.Air air) => Nearby(co).Any(c => c.HasCond("IsPowered") && c.GetCondAmount("StatDamageMax") > 0 &&
        c.GetCondAmount("StatDamage") >= .5 * c.GetCondAmount("StatDamageMax") && SameRoom(c, air));
}
