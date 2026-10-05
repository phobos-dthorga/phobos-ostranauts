using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Liquids;
using PhobosAgriculture.Core;

namespace PhobosAgriculture;

/// <summary>The one thing a rack or W2 is waiting for, in the order a player would fix it (Agriculture 0.54.0; owner
/// report, 5 October 2026: the rack's buttons seemed to do nothing, because a rack that was switched on correctly still
/// said nothing about the paused pump, the missing nutrients or the room that kept it from working). Read-only: the
/// panel, the console status and the crew order's reason show it; nothing here changes a machine.</summary>
internal static partial class Service
{
    /// <summary>The least water a rack needs before a crew order plants in it: one ration's worth.</summary>
    internal const double PlantWaterKg = .25;
    /// <summary>"Next: ..." for a rack or W2, or empty when it has what it needs (or is another kind of machine).</summary>
    internal static string Advice(CondOwner co)
    {
        if (!Definitions.Machine(co) || Definitions.IsCooker(co) || WorkupDefinitions.IsBench(co)) return "";
        var s = Get(co);
        if (s.Protected || WaterGuard(co).Protected) return "";
        string? next = !co.HasCond("IsInstalled") || co.HasCond("IsDamaged") ? Text.Get("repair") :
            IrrigationDefinitions.IsSupply(co) ? SupplyNeed(s) : RackNeed(s);
        return next == null ? "" : Text.Get("advice", next);
    }
    /// <summary>What a rack needs first, or null. Room, power, water, nutrients, a crop, then Start.</summary>
    internal static string? RackNeed(Session s)
    {
        var b = s.State;
        return RoomNeed(s) ?? (b.Ready ? Text.Get("advice_harvest") : b.CropId.Length > 0 && b.Health <= 0 ? Text.Get("advice_dead") :
            SuppliesNeed(s) ?? (b.CropId.Length == 0 ? Text.Get("advice_plant") : b.Running ? null : Text.Get("advice_start")));
    }
    /// <summary>Why nothing should be planted in a rack yet, or null: its room, its power, its water, its nutrients.</summary>
    internal static string? PlantBlock(Session s) => RoomNeed(s) ?? SuppliesNeed(s);
    private static string? RoomNeed(Session s)
    {
        var co = s.Object; var room = Room(co); var gas = room?.GasContainer;
        if (gas == null || Moles(gas, "StatGasMolTotal") < 1) return Text.Get("advice_no_air");
        double temp = room!.GetCondAmount("StatGasTemp") + gas.fDGasTemp, kPa = room.GetCondAmount("StatGasPressure");
        if (!GrowthRoom.Suits(temp, kPa))
            return Text.Get("advice_room", temp - 273.15, kPa, GrowthRoom.MinK - 273.15, GrowthRoom.MaxK - 273.15, GrowthRoom.MinKPa, GrowthRoom.MaxKPa);
        return co.HasCond("IsPowered") ? null : Text.Get("advice_power");
    }
    private static string? SuppliesNeed(Session s) => s.State.Water + s.Solution.Quantity.CarrierKg < PlantWaterKg ? WaterNeed(s) :
        !s.Solution.Enabled && s.State.Nutrients < .001 ? Text.Get("advice_nutrients") : null;
    /// <summary>Why a rack has no water yet: its own switch first, then each thing its W2 needs.</summary>
    private static string WaterNeed(Session s)
    {
        if (!s.Routed) return Text.Get("advice_water_manual");
        var peer = WaterPeer(s.Object);
        if (peer == null || !Definitions.Machine(peer) || !IrrigationDefinitions.IsSupply(peer)) return Text.Get("advice_link");
        if (!s.State.Receiving) return Text.Get("advice_intake");
        string name = ObjectPresentation.Name(peer); var w2 = Get(peer);
        if (w2.Protected || !NativeFluidRoute.EndpointReady(peer)) return Text.Get("advice_w2_fault", name);
        if (!Piped(peer, s.Object)) return Text.Get("water_no_conduit");
        if (!CompatibleSolution(w2, s)) return Text.Get("advice_w2_feed", name);
        if (w2.State.Water + w2.Solution.TotalKg < .01) return Text.Get("advice_w2_empty", name);
        if (!w2.State.Running) return Text.Get("advice_w2_start", name);
        if (!peer.HasCond("IsPowered")) return Text.Get("advice_w2_power", name);
        return FeedConflict(w2) ? Text.Get("water_feed_conflict") : Text.Get("advice_w2_wait", name);
    }
    /// <summary>What a W2 needs first to feed its racks, or null.</summary>
    internal static string? SupplyNeed(Session s)
    {
        var co = s.Object;
        if (!co.HasCond("IsPowered")) return Text.Get("advice_power");
        if (!System.Linq.Enumerable.Any(PanelConfiguration.WaterPeers(co))) return Text.Get("advice_supply_link");
        if (!System.Linq.Enumerable.Any(Destinations(s, true))) return Text.Get("advice_supply_intake");
        if (s.State.Water + s.Solution.TotalKg < .01) return Text.Get("advice_supply_empty");
        if (s.Solution.Enabled && s.Solution.Quantity.SoluteKg < .001 && s.State.Nutrients < .001 && !DoseReady(s)) return Text.Get("advice_supply_dose");
        return s.State.Running ? null : Text.Get("advice_supply_start");
    }
    /// <summary>Starts a paused W2's pump because a rack it feeds was just switched to take its water. The player's
    /// one action on the rack ("take water from the pipes") is the whole intent; the pump can still be paused at the
    /// W2. Returns whether it started now.</summary>
    internal static bool StartPumpFor(CondOwner rack)
    {
        var peer = WaterPeer(rack);
        if (peer == null || !Definitions.Machine(peer) || !IrrigationDefinitions.IsSupply(peer) || !peer.HasCond("IsInstalled") || peer.HasCond("IsDamaged")) return false;
        var w2 = Get(peer);
        if (w2.Protected || WaterGuard(peer).Protected || w2.State.Running || w2.RecoveryInput.Length > 0) return false;
        w2.State.Running = true; w2.Notice = Text.Get("pump_started_for_rack", ObjectPresentation.Name(rack)); Save(w2);
        return true;
    }
}
