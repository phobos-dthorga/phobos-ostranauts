using System;
using System.Linq;
using System.Reflection;
using BepInEx.Bootstrap;
using BepInEx.Configuration;

namespace Phobos.Ostranauts.Framework.Liquids;

/// <summary>Optional ShipsWater 0.16.1 native vessel contract. Litres equal kg ONLY for this water commodity.</summary>
public static class ShipsWaterSupply
{
    public static bool Available => Chainloader.PluginInfos.Values.Any(p => p.Instance != null && p.Instance.GetType().FullName == "ShipsWater.Plugin" && p.Metadata.Version == new Version(0, 16, 1));
    public const string VesselTrigger = "TIsWaterVesselInstalled";
    /// <summary>The game's GetCondTrigger returns its always-true Blank trigger for an unknown name, so
    /// only a rule that actually exists may select tanks.</summary>
    public static CondTrigger? Rule(string name) => Registration.NativeDefinitions.Trigger(name);
    public static double Refill(Ship ship, ILiquidReservoir destination, double requestKg, double crewReserveKg)
        => Refill(ship, destination, requestKg, crewReserveKg, null);
    public static double Refill(Ship ship, ILiquidReservoir destination, double requestKg, double crewReserveKg, LiquidTransferGuard? destinationGuard)
    {
        if (!Available || ship == null || destination.ShipId != ship.strRegID || (int)ship.LoadState < 2 || CrewSim.system?.GetShipOwner(ship.strRegID) != CrewSim.coPlayer?.strID) return 0;
        double total = 0;
        var trigger = Rule(VesselTrigger);
        if (trigger == null) return 0;
        var vessels = ship.GetCOs(null, false, false, true).Where(c => c != null && !c.bDestroyed && c.ship == ship && c.objCOParent == null &&
            c.HasCond("IsInstalled") && !c.HasCond("IsDamaged") && !c.HasCond("IsLocked") && trigger.Triggered(c) &&
            !new LiquidTransferGuard(c.mapGUIPropMaps, "WaterSupplyTransfer", FrameworkInfo.PluginId).Protected).ToArray();
        foreach (var tank in vessels)
        {
            double all = vessels.Sum(v => Math.Max(0, v.GetCondAmount("StatLiqH2O")));
            double amount = Math.Min(requestKg - total, Math.Max(0, all - crewReserveKg));
            if (amount <= 0) break;
            var guard = new LiquidTransferGuard(tank.mapGUIPropMaps, "WaterSupplyTransfer", FrameworkInfo.PluginId);
            if (guard.Protected) continue;
            if (destinationGuard != null)
                total += LiquidTransferGuard.Commit(new Tank(tank), destination, amount, guard, destinationGuard).ReceivedKg;
            else total += FiniteLiquidTransfer.Commit(new Tank(tank), destination, amount, 0).ReceivedKg;
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
    /// <summary>The waste tank's capacity exactly as Ship's Water 0.16.1 computes it: its public configuration
    /// entry for the tank's size tag, at least one litre. Null when the plugin, the tag or the entry is missing,
    /// so a deposit can never exceed the capacity its own plumbing clamps to. Litres equal kg for water only.</summary>
    public static double? WasteCapacityKg(CondOwner tank)
    {
        if (tank == null || !Available) return null;
        string? field = WasteSizes.Where(s => tank.HasCond(s.Tag)).Select(s => s.Field).FirstOrDefault();
        var plugin = Chainloader.PluginInfos.Values.Select(p => p.Instance).FirstOrDefault(i => i != null && i.GetType().FullName == "ShipsWater.Plugin");
        if (field == null || plugin == null) return null;
        return plugin.GetType().GetField(field, BindingFlags.Public | BindingFlags.Static)?.GetValue(null) is ConfigEntry<float> entry && FiniteLiquidTransfer.Finite(entry.Value) ? Math.Max(1, entry.Value) : null;
    }
    /// <summary>Deposits up to requestKg from a Phobos reservoir into installed, undamaged, unlocked Ship's Water
    /// waste tanks on the same owned ship, in ID order, each up to its declared capacity, through guarded
    /// transfers. Returns the kilograms actually moved. Nothing is drawn from the potable tanks.</summary>
    public static double DepositWaste(Ship ship, ILiquidReservoir source, double requestKg, LiquidTransferGuard sourceGuard)
    {
        if (!Available || ship == null || source == null || sourceGuard == null || source.ShipId != ship.strRegID || (int)ship.LoadState < 2 ||
            CrewSim.system?.GetShipOwner(ship.strRegID) != CrewSim.coPlayer?.strID || !FiniteLiquidTransfer.Finite(requestKg) || requestKg <= 0) return 0;
        var trigger = Rule(WasteVesselTrigger);
        if (trigger == null) return 0;
        var tanks = ship.GetCOs(null, false, false, true).Where(c => c != null && !c.bDestroyed && c.ship == ship && c.objCOParent == null &&
            c.HasCond("IsInstalled") && !c.HasCond("IsDamaged") && !c.HasCond("IsLocked") && trigger.Triggered(c)).OrderBy(c => c.strID, StringComparer.Ordinal).ToArray();
        double total = 0;
        foreach (var tank in tanks)
        {
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
