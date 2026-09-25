using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Phobos.Ostranauts.Framework;
using PhobosShipbreaker.Core;
using Phobos.Ostranauts.Framework.Processing;

namespace PhobosShipbreaker;

// One concrete appliance, using the native room heat accumulator. No parallel
// temperature simulation or disappearing heat in vacuum. GatherPower reports the
// remaining demand, allowing partially supplied brownouts to retain their heat too.
internal static class ReclaimerHeat
{
    internal sealed class Transfer
    {
        internal GasContainer Gas = null!;
        internal double Mols, WorkSeconds;
        internal EnergyReceipt Receipt = null!;
    }
    private static readonly ConditionalWeakTable<Powered, Transfer> pending = new ConditionalWeakTable<Powered, Transfer>();
    internal static bool Begin(Powered power, CondOwner machine, double amount, out Transfer? transfer)
    {
        transfer = null;
        if (!ReclaimerRules.IsFamily(machine.strCODef)) return true;
        pending.Remove(power);
        var room = machine.ship?.GetRoomAtWorldCoords1(machine.GetPos("use"), false)?.CO;
        var gas = room?.GasContainer;
        double demand = RoutingRules.DemandKW(machine.HasCond(ProcessRules.Working), machine.HasCond(RoutingRules.Feeding),
            Plugin.Options.ReclaimerKW, Plugin.Options.FeederKW);
        double seconds = amount * Units.SecondsPerHour / demand;
        double mols = 0;
        if (gas == null || !gas.mapGasMols1.TryGetValue("StatGasMolTotal", out mols) ||
            !ReclaimerRules.CoolingBudget(mols, room!.GetCondAmount("StatGasTemp"), gas.fDGasTemp,
                room.GetCondAmount("StatGasPressure"), demand, seconds, out _))
        {
            Plugin.Service.Block(machine, Text.Get("Reclaimer.cooling_block", ReclaimerRules.MinPressureKPa, ReclaimerRules.MaxRoomKelvin - Phobos.Ostranauts.Framework.Units.CelsiusToKelvin));
            Plugin.Collectors.Block(machine, Text.Get("Reclaimer.cooling_block", ReclaimerRules.MinPressureKPa, ReclaimerRules.MaxRoomKelvin - Phobos.Ostranauts.Framework.Units.CelsiusToKelvin));
            machine.ZeroCondAmount("IsPowered");
            return false;
        }
        transfer = new Transfer { Gas = gas, Mols = mols,
            Receipt = NativeEnergyReceipts.Begin(power, machine, amount), WorkSeconds = seconds };
        pending.Add(power, transfer);
        return true;
    }
    internal static void Finish(Powered power, CondOwner machine, Transfer? transfer)
    {
        pending.Remove(power);
        if (transfer == null) return;
        double supplied = NativeEnergyReceipts.Complete(power, machine, transfer.Receipt);
        if (double.IsNaN(supplied) || double.IsInfinity(supplied)) throw new InvalidOperationException("Invalid reclaimer energy receipt.");
        // Native Heater and gas simulation own later mixing, cooling and persistence.
        transfer.Gas.fDGasTemp += supplied * Units.SecondsPerHour * ReclaimerRules.JoulesPerKilojoule /
            (transfer.Mols * ReclaimerRules.GasHeatCapacity);
    }
    internal static void Forget(Powered power) { pending.Remove(power); NativeEnergyReceipts.Forget(power); }
}
