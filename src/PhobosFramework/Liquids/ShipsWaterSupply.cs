using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Bootstrap;
using BepInEx.Configuration;

namespace Phobos.Ostranauts.Framework.Liquids;

/// <summary>Optional ShipsWater 0.16.1 native vessel contract. Litres equal kg ONLY for this water commodity.</summary>
public static class ShipsWaterSupply
{
    private static bool? available;
    private static object? plugin;
    /// <summary>Whether the pinned Ship's Water is loaded. Plugins cannot appear after the chainloader finishes, so
    /// the answer is remembered once a world has loaded; before that it is read fresh.</summary>
    public static bool Available
    {
        get
        {
            if (available != null) return available.Value;
            plugin = Chainloader.PluginInfos.Values.Select(p => p.Instance).FirstOrDefault(i => i != null && i.GetType().FullName == "ShipsWater.Plugin" &&
                Chainloader.PluginInfos.Values.Any(p => p.Instance == i && p.Metadata.Version == new Version(0, 16, 1)));
            bool found = plugin != null;
            if (CrewSim.objInstance != null && CrewSim.objInstance.FinishedLoading) available = found;
            return found;
        }
    }
    /// <summary>Forgets the plugin answer, the tank lists and the reflected fields (content reload).</summary>
    internal static void Reset() { available = null; plugin = null; tanks.Clear(); capacityFields.Clear(); }
    public const string VesselTrigger = "TIsWaterVesselInstalled";
    /// <summary>Requests at or below this take nothing and cost no scan.</summary>
    public const double MinimumRequestKg = 0;
    /// <summary>The game's GetCondTrigger returns its always-true Blank trigger for an unknown name, so
    /// only a rule that actually exists may select tanks.</summary>
    public static CondTrigger? Rule(string name) => Registration.NativeDefinitions.Trigger(name);

    // Installed tank lists per ship and rule, reread every couple of real seconds; contents are always read fresh.
    private sealed class TankList { internal CondOwner[] Tanks = Array.Empty<CondOwner>(); internal readonly Cadence Cadence = new(FluidRouteCache.RecheckSeconds); }
    private static readonly Dictionary<(Ship, string), TankList> tanks = new();
    private static CondOwner[] InstalledTanks(Ship ship, string rule, CondTrigger trigger)
    {
        if (!tanks.TryGetValue((ship, rule), out var list)) tanks[(ship, rule)] = list = new TankList();
        if (list.Cadence.Due())
            list.Tanks = ship.GetCOs(null, false, false, true).Where(c => c != null && !c.bDestroyed && c.ship == ship && c.objCOParent == null &&
                c.HasCond("IsInstalled") && trigger.Triggered(c)).OrderBy(c => c.strID, StringComparer.Ordinal).ToArray();
        return list.Tanks;
    }
    private static bool Usable(CondOwner tank, Ship ship) => tank != null && !tank.bDestroyed && tank.ship == ship && tank.objCOParent == null &&
        tank.HasCond("IsInstalled") && !tank.HasCond("IsDamaged") && !tank.HasCond("IsLocked");

    /// <summary>Whether <paramref name="via"/> reaches <paramref name="tank"/> by the owner's link rule (30 September
    /// 2026): touching, or on one process-water network (the tanks' ports come from <see cref="ShipsWaterPorts"/>).</summary>
    public static bool Reaches(CondOwner via, CondOwner tank) => LineReach.Of(via, tank, LineFamilies.ProcessWater) != LineReachKind.None;
    /// <summary>The usable drinking-water (or, with <paramref name="waste"/>, waste) tanks <paramref name="via"/> reaches,
    /// in ID order; empty when the pinned Ship's Water is not loaded.</summary>
    public static IReadOnlyList<CondOwner> ReachableTanks(CondOwner via, bool waste = false)
    {
        var ship = via?.ship;
        string rule = waste ? WasteVesselTrigger : VesselTrigger;
        if (via == null || ship == null || !Available || Rule(rule) is not CondTrigger trigger) return Array.Empty<CondOwner>();
        return InstalledTanks(ship, rule, trigger).Where(t => Usable(t, ship) && Reaches(via, t)).ToArray();
    }

    /// <summary>Draws drinking water for <paramref name="via"/> (the Phobos machine or silo that receives it) from the
    /// Ship's Water tanks it reaches, keeping <paramref name="crewReserveKg"/> across every tank aboard, since the crew
    /// drink from all of them (Framework 0.59.0).</summary>
    public static double Refill(CondOwner via, ILiquidReservoir destination, double requestKg, double crewReserveKg, LiquidTransferGuard? destinationGuard)
        => via?.ship == null ? 0 : Refill(via.ship, via, destination, requestKg, crewReserveKg, destinationGuard);
    [Obsolete("Takes tanks anywhere aboard, across open floor. Use Refill(via, ...), which applies the link rule.")]
    public static double Refill(Ship ship, ILiquidReservoir destination, double requestKg, double crewReserveKg)
        => Refill(ship, null, destination, requestKg, crewReserveKg, null);
    [Obsolete("Takes tanks anywhere aboard, across open floor. Use Refill(via, ...), which applies the link rule.")]
    public static double Refill(Ship ship, ILiquidReservoir destination, double requestKg, double crewReserveKg, LiquidTransferGuard? destinationGuard)
        => Refill(ship, null, destination, requestKg, crewReserveKg, destinationGuard);
    private static double Refill(Ship ship, CondOwner? via, ILiquidReservoir destination, double requestKg, double crewReserveKg, LiquidTransferGuard? destinationGuard)
    {
        if (!FiniteLiquidTransfer.Finite(requestKg) || requestKg <= MinimumRequestKg || !Available || ship == null || destination.ShipId != ship.strRegID ||
            (int)ship.LoadState < 2 || CrewSim.system?.GetShipOwner(ship.strRegID) != CrewSim.coPlayer?.strID) return 0;
        using var measurement = Diagnostics.Performance.Measure(Diagnostics.Performance.WaterRefill);
        double total = 0;
        var trigger = Rule(VesselTrigger);
        if (trigger == null) return 0;
        var vessels = InstalledTanks(ship, VesselTrigger, trigger);
        // The pool is summed once, over every tank aboard; each transfer then reads its own tank fresh.
        double all = 0;
        foreach (var v in vessels) if (Usable(v, ship)) all += Math.Max(0, v.GetCondAmount("StatLiqH2O"));
        foreach (var tank in vessels)
        {
            if (!Usable(tank, ship) || (via != null && !Reaches(via, tank))) continue;
            double amount = Math.Min(requestKg - total, Math.Max(0, all - crewReserveKg));
            if (amount <= 0) break;
            var guard = new LiquidTransferGuard(tank.mapGUIPropMaps, "WaterSupplyTransfer", FrameworkInfo.PluginId);
            if (guard.Protected) continue;
            double moved = destinationGuard != null
                ? LiquidTransferGuard.Commit(new Tank(tank), destination, amount, guard, destinationGuard).ReceivedKg
                : FiniteLiquidTransfer.Commit(new Tank(tank), destination, amount, 0).ReceivedKg;
            total += moved; all -= moved;
        }
        return total;
    }
    /// <summary>Whether an object is a usable Ship's Water drinking tank (installed, undamaged, unlocked), when the pinned
    /// Ship's Water is loaded.</summary>
    public static bool IsDrinkingTank(CondOwner? co) =>
        co?.ship != null && Available && Rule(VesselTrigger) is CondTrigger trigger && trigger.Triggered(co) && Usable(co, co.ship);
    /// <summary>What the given drinking tanks (those on one water line, Framework 0.65.0) can give without taking the
    /// ship's drinking water below <paramref name="crewReserveKg"/>, which is kept across every tank aboard.</summary>
    public static double LineAvailableKg(Ship ship, IEnumerable<CondOwner> tanksOnLine, double crewReserveKg)
    {
        if (ship == null || !Available || Rule(VesselTrigger) is not CondTrigger trigger) return 0;
        double all = 0;
        foreach (var v in InstalledTanks(ship, VesselTrigger, trigger)) if (Usable(v, ship)) all += Math.Max(0, v.GetCondAmount("StatLiqH2O"));
        double here = tanksOnLine.Where(t => Usable(t, ship)).Sum(t => Math.Max(0, t.GetCondAmount("StatLiqH2O")));
        return Math.Max(0, Math.Min(here, all - Math.Max(0, crewReserveKg)));
    }
    /// <summary>Draws up to <paramref name="requestKg"/> of drinking water from the given tanks into a line's hold-up
    /// (Framework 0.65.0; owner decision, 1 October 2026: Ship's Water tanks fill the water lines they join), in ID
    /// order, within <see cref="LineAvailableKg"/>. A tank with an interrupted transfer is skipped. Returns the kilograms
    /// taken; litres equal kilograms for water only.</summary>
    public static double DrawForLine(Ship ship, IEnumerable<CondOwner> tanksOnLine, double requestKg, double crewReserveKg)
    {
        var list = tanksOnLine.Where(t => Usable(t, ship)).OrderBy(t => t.strID, StringComparer.Ordinal).ToArray();
        double allowed = Math.Min(requestKg, LineAvailableKg(ship, list, crewReserveKg)), total = 0;
        foreach (var tank in list)
        {
            if (allowed - total <= 1e-12) break;
            if (new LiquidTransferGuard(tank.mapGUIPropMaps, "WaterSupplyTransfer", FrameworkInfo.PluginId).Protected) continue;
            var reservoir = new Tank(tank);
            double take = Math.Min(allowed - total, Math.Max(0, reservoir.QuantityKg));
            if (take <= 0) continue;
            reservoir.SetQuantity(reservoir.QuantityKg - take);
            total += take;
        }
        return total;
    }
    private sealed class Tank : ILiquidReservoir
    {
        private readonly CondOwner co;
        internal Tank(CondOwner co) { this.co = co; }
        public string Identity => co.strID;
        public string ShipId => co.ship.strRegID;
        public string Commodity => "water";
        public double QuantityKg => co.GetCondAmount("StatLiqH2O");
        public double CapacityKg => QuantityKg; // Source-only adapter; provider retains its capacity/quality semantics.
        public void SetQuantity(double kg)
        {
            if (!FiniteLiquidTransfer.Finite(kg) || kg < 0) throw new ArgumentException("Invalid vessel quantity.");
            co.SetCondAmount("StatLiqH2O", kg);
        }
    }

    // Reclaim, do not join (owner direction, 29 September 2026): process water goes into Ship's Water's waste
    // tanks, where its own recycler decides what returns as potable water with its own loss. A Phobos vessel
    // never joins the potable pool and never takes over the kiosk row.
    public const string WasteVesselTrigger = "TIsWasteVesselInstalled", WasteStat = "StatLiqH2OWaste";
    private static readonly (string Tag, string Field)[] WasteSizes = { ("IsWaterTankWasteSmall", "SmallWasteTankLitres"), ("IsWaterTankWasteMedium", "MediumWasteTankLitres"), ("IsWaterTankWaste", "WasteTankLitres") };
    private static readonly Dictionary<string, FieldInfo?> capacityFields = new(StringComparer.Ordinal);
    /// <summary>The waste tank's capacity exactly as Ship's Water 0.16.1 computes it: its public configuration
    /// entry for the tank's size tag, at least one litre. Null when the plugin, the tag or the entry is missing,
    /// so a deposit can never exceed the capacity its own plumbing clamps to. Litres equal kg for water only.</summary>
    public static double? WasteCapacityKg(CondOwner tank)
    {
        if (tank == null || !Available || plugin == null) return null;
        string? field = null;
        foreach (var size in WasteSizes) if (tank.HasCond(size.Tag)) { field = size.Field; break; }
        if (field == null) return null;
        if (!capacityFields.TryGetValue(field, out var info)) capacityFields[field] = info = plugin.GetType().GetField(field, BindingFlags.Public | BindingFlags.Static);
        return info?.GetValue(null) is ConfigEntry<float> entry && FiniteLiquidTransfer.Finite(entry.Value) ? Math.Max(1, entry.Value) : null;
    }
    /// <summary>Deposits up to requestKg from a Phobos reservoir into installed, undamaged, unlocked Ship's Water
    /// waste tanks on the same owned ship, in ID order, each up to its declared capacity, through guarded
    /// transfers. Returns the kilograms actually moved. Nothing is drawn from the potable tanks.</summary>
    /// <summary>Deposits from <paramref name="via"/> (the Phobos vessel sending it) into the waste tanks it reaches:
    /// touching, or on its process-water network (Framework 0.59.0).</summary>
    public static double DepositWaste(CondOwner via, ILiquidReservoir source, double requestKg, LiquidTransferGuard sourceGuard)
        => via?.ship == null ? 0 : DepositWaste(via.ship, via, source, requestKg, sourceGuard);
    [Obsolete("Fills waste tanks anywhere aboard, across open floor. Use DepositWaste(via, ...), which applies the link rule.")]
    public static double DepositWaste(Ship ship, ILiquidReservoir source, double requestKg, LiquidTransferGuard sourceGuard)
        => DepositWaste(ship, null, source, requestKg, sourceGuard);
    private static double DepositWaste(Ship ship, CondOwner? via, ILiquidReservoir source, double requestKg, LiquidTransferGuard sourceGuard)
    {
        if (!FiniteLiquidTransfer.Finite(requestKg) || requestKg <= MinimumRequestKg || !Available || ship == null || source == null || sourceGuard == null ||
            source.ShipId != ship.strRegID || (int)ship.LoadState < 2 || CrewSim.system?.GetShipOwner(ship.strRegID) != CrewSim.coPlayer?.strID) return 0;
        var trigger = Rule(WasteVesselTrigger);
        if (trigger == null) return 0;
        var candidates = InstalledTanks(ship, WasteVesselTrigger, trigger);
        double total = 0;
        foreach (var tank in candidates)
        {
            if (!Usable(tank, ship) || (via != null && !Reaches(via, tank))) continue;
            double? capacity = WasteCapacityKg(tank);
            if (capacity == null) continue;
            var guard = new LiquidTransferGuard(tank.mapGUIPropMaps, "WaterWasteTransfer", FrameworkInfo.PluginId);
            if (guard.Protected) continue;
            double room = capacity.Value - Math.Max(0, tank.GetCondAmount(WasteStat));
            double amount = Math.Min(requestKg - total, room);
            if (amount <= 0) continue;
            total += LiquidTransferGuard.Commit(source, new WasteTank(tank, capacity.Value), amount, sourceGuard, guard).ReceivedKg;
            if (requestKg - total <= 0) break;
        }
        return total;
    }
    private sealed class WasteTank : ILiquidReservoir
    {
        private readonly CondOwner co; private readonly double capacity;
        internal WasteTank(CondOwner co, double capacity) { this.co = co; this.capacity = capacity; }
        public string Identity => co.strID;
        public string ShipId => co.ship.strRegID;
        public string Commodity => "water";
        public double QuantityKg => Math.Max(0, co.GetCondAmount(WasteStat));
        public double CapacityKg => Math.Max(capacity, QuantityKg); // Never report a tank already over its configured capacity as invalid; it takes nothing more.
        public void SetQuantity(double kg)
        {
            if (!FiniteLiquidTransfer.Finite(kg) || kg < 0) throw new ArgumentException("Invalid vessel quantity.");
            co.SetCondAmount(WasteStat, kg);
        }
    }
}
