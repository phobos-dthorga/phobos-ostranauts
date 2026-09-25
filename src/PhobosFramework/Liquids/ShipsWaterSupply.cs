using System;
using System.Linq;
using BepInEx.Bootstrap;

namespace Phobos.Ostranauts.Framework.Liquids;

/// <summary>Optional ShipsWater 0.16.1 native vessel contract. Litres equal kg ONLY for this water commodity.</summary>
public static class ShipsWaterSupply
{
    public static bool Available => Chainloader.PluginInfos.Values.Any(p => p.Instance != null && p.Instance.GetType().FullName == "ShipsWater.Plugin" && p.Metadata.Version == new Version(0, 16, 1));
    public static double Refill(Ship ship, ILiquidReservoir destination, double requestKg, double crewReserveKg)
        => Refill(ship, destination, requestKg, crewReserveKg, null);
    public static double Refill(Ship ship, ILiquidReservoir destination, double requestKg, double crewReserveKg, LiquidTransferGuard? destinationGuard)
    {
        if (!Available || ship == null || destination.ShipId != ship.strRegID || (int)ship.LoadState < 2 || CrewSim.system?.GetShipOwner(ship.strRegID) != CrewSim.coPlayer?.strID) return 0;
        double total = 0;
        var trigger = DataHandler.GetCondTrigger("TIsWaterVesselInstalled");
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
}
