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

    public static double Refill(Ship ship, ILiquidReservoir destination, double requestKg, double crewReserveKg)
        => Refill(ship, destination, requestKg, crewReserveKg, null);
    public static double Refill(Ship ship, ILiquidReservoir destination, double requestKg, double crewReserveKg, LiquidTransferGuard? destinationGuard)
    {
        if (!FiniteLiquidTransfer.Finite(requestKg) || requestKg <= MinimumRequestKg || !Available || ship == null || destination.ShipId != ship.strRegID ||
            (int)ship.LoadState < 2 || CrewSim.system?.GetShipOwner(ship.strRegID) != CrewSim.coPlayer?.strID) return 0;
        using var measurement = Diagnostics.Performance.Measure(Diagnostics.Performance.WaterRefill);
        double total = 0;
        var trigger = Rule(VesselTrigger);
        if (trigger == null) return 0;
        var vessels = InstalledTanks(ship, VesselTrigger, trigger);
        // The pool is summed once; each transfer then reads its own tank fresh.
        double all = 0;
        foreach (var v in vessels) if (Usable(v, ship)) all += Math.Max(0, v.GetCondAmount("StatLiqH2O"));
        foreach (var tank in vessels)
        {
            if (!Usable(tank, ship)) continue;
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
    public static double DepositWaste(Ship ship, ILiquidReservoir source, double requestKg, LiquidTransferGuard sourceGuard)
    {
        if (!FiniteLiquidTransfer.Finite(requestKg) || requestKg <= MinimumRequestKg || !Available || ship == null || source == null || sourceGuard == null ||
            source.ShipId != ship.strRegID || (int)ship.LoadState < 2 || CrewSim.system?.GetShipOwner(ship.strRegID) != CrewSim.coPlayer?.strID) return 0;
        var trigger = Rule(WasteVesselTrigger);
        if (trigger == null) return 0;
        var candidates = InstalledTanks(ship, WasteVesselTrigger, trigger);
        double total = 0;
        foreach (var tank in candidates)
        {
            if (!Usable(tank, ship)) continue;
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
