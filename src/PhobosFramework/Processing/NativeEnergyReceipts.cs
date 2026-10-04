using System;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace Phobos.Ostranauts.Framework.Processing;

/// <summary>Opt-in witnesses around the existing native UsePower call. No extra power ticks.</summary>
public static class NativeEnergyReceipts
{
    private static readonly ConditionalWeakTable<Powered, EnergyReceipt> Pending = new();
    // The gather hook runs for every powered object in the world; with no receipt open it returns on one integer.
    private static int pending;
    public static EnergyReceipt Begin(Powered power, CondOwner machine, double requestedKWh)
    {
        if (Pending.TryGetValue(power, out _)) throw new InvalidOperationException("Nested electrical receipt for one appliance.");
        var receipt = new EnergyReceipt(requestedKWh, machine.GetCondAmount("StatPower"));
        Pending.Add(power, receipt); pending++; return receipt;
    }
    public static double Complete(Powered power, CondOwner machine, EnergyReceipt receipt)
    {
        if (!Pending.TryGetValue(power, out var current) || !ReferenceEquals(current, receipt))
            throw new InvalidOperationException("Electrical receipt is no longer current.");
        if (Pending.Remove(power)) pending--;
        return receipt.Consume(machine.GetCondAmount("StatPower"));
    }
    public static void Forget(Powered power) { if (Pending.Remove(power)) pending--; }
    internal static void Gather(Powered power, double requested, double remaining)
    { if (pending > 0 && Pending.TryGetValue(power, out var receipt)) receipt.Gather(requested, remaining); }
}

[HarmonyPatch(typeof(Powered), "GatherPower")]
internal static class NativeEnergyGatherPatch
{
    // The game's method counts its own argument down as it gathers (Framework 0.93.0; owner report, 5 October 2026),
    // so by the postfix the argument already equals the result: the request has to be kept from before the call.
    // Reading it afterwards recorded nothing delivered, and every machine on conduit power stood still while powered.
    private static void Prefix(double __0, out double __state) => __state = __0;
    private static void Postfix(Powered __instance, double __state, double __result) => NativeEnergyReceipts.Gather(__instance, __state, __result);
}
