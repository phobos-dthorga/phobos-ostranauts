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
            machine.HasCond(StorageRules.Unloading), Plugin.Options.ReclaimerKW, ReclaimerRules.IdleKW, Plugin.Options.FeederKW);
        double seconds = amount * Units.SecondsPerHour / demand;
        double mols = 0;
        if (gas == null || !gas.mapGasMols1.TryGetValue("StatGasMolTotal", out mols) ||
            !ReclaimerRules.CoolingBudget(mols, room!.GetCondAmount("StatGasTemp"), gas.fDGasTemp,
                room.GetCondAmount("StatGasPressure"), demand, seconds, out _))
        {
            // Not a stop: no power is drawn this step, the job keeps its permission and progress, and work
            // continues by itself once the room can take the heat. The status shows the room's numbers.
            string status = WaitStatus(room, gas, mols);
            Plugin.Service.HeatWait(machine, status);
            Plugin.Collectors.HeatWait(machine, status);
            machine.ZeroCondAmount("IsPowered");
            return false;
        }
        Plugin.Service.HeatReady(machine);
        transfer = new Transfer { Gas = gas, Mols = mols,
            Receipt = NativeEnergyReceipts.Begin(power, machine, amount), WorkSeconds = seconds };
        pending.Add(power, transfer);
        return true;
    }
    /// <summary>Why the room cannot take the heat right now, with its temperature, air and pressure, so the
    /// 40 C rule can be judged in play. Without a room there is no air to warm at all.</summary>
    internal static string WaitStatus(CondOwner? room, GasContainer? gas, double mols)
    {
        double limitC = ReclaimerRules.MaxRoomKelvin - Units.CelsiusToKelvin;
        if (room == null || gas == null) return Text.Get("Reclaimer.cooling_block", ReclaimerRules.MinPressureKPa, limitC);
        return Text.Get("Reclaimer.cooling_wait", room.GetCondAmount("StatGasTemp") + gas.fDGasTemp - Units.CelsiusToKelvin, limitC,
            mols, room.GetCondAmount("StatGasPressure"), ReclaimerRules.MinPressureKPa);
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
