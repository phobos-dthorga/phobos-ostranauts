using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Phobos.Ostranauts.Framework;
using PhobosShipbreaker.Core;
using Phobos.Ostranauts.Framework.Processing;

namespace PhobosShipbreaker;

// One concrete appliance, using the native room heat accumulator. No parallel temperature simulation; it does not
// work in vacuum (Framework RoomHeat.Check). GatherPower reports the remaining demand, allowing partially supplied
// brownouts to retain their heat too.
internal static class ReclaimerHeat
{
    internal sealed class Transfer
    {
        internal RoomHeat.Air Air = null!;
        internal double WorkSeconds;
        internal EnergyReceipt Receipt = null!;
    }
    private static readonly ConditionalWeakTable<Powered, Transfer> pending = new ConditionalWeakTable<Powered, Transfer>();
    internal static bool Begin(Powered power, CondOwner machine, double amount, out Transfer? transfer)
    {
        transfer = null;
        if (!ReclaimerRules.IsFamily(machine.strCODef)) return true;
        pending.Remove(power);
        var air = RoomHeat.Read(machine, "use");
        double demand = RoutingRules.DemandKW(machine.HasCond(ProcessRules.Working), machine.HasCond(RoutingRules.Feeding),
            machine.HasCond(StorageRules.Unloading), Plugin.Options.ReclaimerKW, ReclaimerRules.IdleKW, Plugin.Options.FeederKW);
        double seconds = amount * Units.SecondsPerHour / demand;
        var heat = RoomHeat.Check(air, demand, seconds);
        if (!heat.Admitted)
        {
            // Not a stop: no power is drawn this step, the job keeps its permission and progress, and work
            // continues by itself once the heat can go. The status gives the numbers.
            string status = RoomHeat.Describe(heat);
            Plugin.Service.HeatWait(machine, status);
            Plugin.Collectors.HeatWait(machine, status);
            machine.ZeroCondAmount("IsPowered");
            return false;
        }
        Plugin.Service.HeatReady(machine);
        transfer = new Transfer { Air = air!,
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
        RoomHeat.Deposit(transfer.Air, supplied);
    }
    internal static void Forget(Powered power) { pending.Remove(power); NativeEnergyReceipts.Forget(power); }
}
