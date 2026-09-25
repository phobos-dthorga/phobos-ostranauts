using System;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace Phobos.Ostranauts.Framework.Processing;

/// <summary>Opt-in witnesses around the existing native UsePower call. No extra power ticks.</summary>
public static class NativeEnergyReceipts
{
    private static readonly ConditionalWeakTable<Powered, EnergyReceipt> Pending = new();
    public static EnergyReceipt Begin(Powered power, CondOwner machine, double requestedKWh)
    {
        if (Pending.TryGetValue(power, out _)) throw new InvalidOperationException("Nested electrical receipt for one appliance.");
        var receipt = new EnergyReceipt(requestedKWh, machine.GetCondAmount("StatPower"));
        Pending.Add(power, receipt); return receipt;
    }
    public static double Complete(Powered power, CondOwner machine, EnergyReceipt receipt)
    {
        if (!Pending.TryGetValue(power, out var current) || !ReferenceEquals(current, receipt))
            throw new InvalidOperationException("Electrical receipt is no longer current.");
        Pending.Remove(power);
        return receipt.Consume(machine.GetCondAmount("StatPower"));
    }
    public static void Forget(Powered power) => Pending.Remove(power);
    internal static void Gather(Powered power, double requested, double remaining)
    { if (Pending.TryGetValue(power, out var receipt)) receipt.Gather(requested, remaining); }
}

[HarmonyPatch(typeof(Powered), "GatherPower")]
internal static class NativeEnergyGatherPatch
{
    private static void Postfix(Powered __instance, double __0, double __result) => NativeEnergyReceipts.Gather(__instance, __0, __result);
}
