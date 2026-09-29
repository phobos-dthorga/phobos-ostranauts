using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace Phobos.Ostranauts.Framework.Liquids;

/// <summary>Pure bookkeeping for many tiny draws against one vessel: a snapshot of what it held, what has been
/// taken since, and nothing taken beyond what it held. Settlement then removes the owed mass in one write.</summary>
public sealed class DrawLedger
{
    public double HeldKg { get; private set; }
    public double OwedKg { get; private set; }
    public double AvailableKg => Math.Max(0, HeldKg - OwedKg);
    public DrawLedger(double heldKg) { if (!BulkVesselSpec.Finite(heldKg) || heldKg < 0) throw new ArgumentException("Invalid held mass."); HeldKg = heldKg; }
    /// <summary>Takes up to <paramref name="kg"/> and returns what was taken.</summary>
    public double Take(double kg)
    {
        if (!BulkVesselSpec.Finite(kg) || kg < 0) throw new ArgumentException("Invalid draw.");
        double taken = Math.Min(kg, AvailableKg);
        OwedKg += taken;
        return taken;
    }
    /// <summary>After settlement: the vessel now holds <paramref name="heldKg"/> and nothing is owed.</summary>
    public void Settled(double heldKg) { HeldKg = Math.Max(0, heldKg); OwedKg = 0; }
}

/// <summary>Buffered draws on bulk vessels for consumers that take a little every frame (RCS remass). Each vessel's
/// draws accumulate in memory and settle to its saved record every couple of seconds, before the game saves the
/// ship, and when asked. A reload drops unsettled draws with the session they belong to. What is lost on a crash
/// is at most the last couple of seconds of draws. A protected vessel offers nothing.</summary>
public static class BufferedDrains
{
    public const double SettleSeconds = 2;
    private sealed class Entry { internal CondOwner Vessel = null!; internal DrawLedger Ledger = null!; internal string Reason = ""; internal float Refreshed; }
    private static readonly Dictionary<CondOwner, Entry> entries = new();
    private static float nextSettle;

    private static Entry? For(CondOwner vessel)
    {
        if (vessel == null || vessel.bDestroyed || BulkVessels.Of(vessel) == null) return null;
        if (entries.TryGetValue(vessel, out var e)) return e;
        if (BulkVessel.Protected(vessel)) return null;
        e = new Entry { Vessel = vessel, Ledger = new DrawLedger(BulkVessel.Snapshot(vessel).AvailableKg), Refreshed = UnityEngine.Time.unscaledTime };
        entries[vessel] = e;
        return e;
    }
    /// <summary>What can still be drawn now, net of unsettled draws.</summary>
    public static double AvailableKg(CondOwner vessel) => For(vessel)?.Ledger.AvailableKg ?? 0;
    /// <summary>Draws up to <paramref name="kg"/> and returns what was taken; it leaves the record at the next settlement.</summary>
    public static double Take(CondOwner vessel, double kg, string reason)
    {
        var e = For(vessel);
        if (e == null) return 0;
        e.Reason = reason;
        return e.Ledger.Take(kg);
    }
    /// <summary>Writes every owed draw to its vessel's record (one write and one log line each).</summary>
    public static void SettleAll(Ship? ship = null)
    {
        foreach (var e in entries.Values.ToArray())
        {
            if (ship != null && e.Vessel.ship != ship) continue;
            Settle(e);
        }
    }
    /// <summary>Settles one vessel now: call before changing its record directly, so earlier buffered draws land
    /// first and the next draw starts from a fresh reading.</summary>
    public static void Settle(CondOwner vessel)
    {
        if (vessel != null && entries.TryGetValue(vessel, out var e)) Settle(e);
    }
    private static void Settle(Entry e)
    {
        try
        {
            if (e.Vessel == null || e.Vessel.bDestroyed) { entries.Remove(e.Vessel!); return; }
            double owed = e.Ledger.OwedKg;
            if (owed > 1e-9)
            {
                double removed = BulkVessel.Drain(e.Vessel, owed, e.Reason, false);
                FrameworkLifecycle.Log(Text.Get("BulkVessel.drained", e.Vessel.strID, BulkVessels.Of(e.Vessel)?.Commodity ?? "", removed, e.Reason));
            }
            // A fresh reading picks up anything else that changed the vessel (a transfer in, a leak, a vent).
            if (BulkVessel.Protected(e.Vessel)) { entries.Remove(e.Vessel); return; }
            e.Ledger.Settled(BulkVessel.Snapshot(e.Vessel).AvailableKg);
            e.Refreshed = UnityEngine.Time.unscaledTime;
        }
        catch (Exception ex) { entries.Remove(e.Vessel); FrameworkLifecycle.Log(ex.ToString()); }
    }
    internal static void Poll()
    {
        if (entries.Count == 0 || UnityEngine.Time.unscaledTime < nextSettle) return;
        nextSettle = UnityEngine.Time.unscaledTime + (float)SettleSeconds;
        SettleAll();
    }
    internal static void Reset() => entries.Clear();

    // Settle before the game serialises a ship, so the saved records carry every draw already made.
    [HarmonyPatch(typeof(Ship), nameof(Ship.GetJSON))]
    private static class SavePatch
    {
        private static void Prefix(Ship __instance, bool bSaveGame) { if (bSaveGame && entries.Count > 0) SettleAll(__instance); }
    }
    [HarmonyPatch]
    private static class ReloadPatch
    {
        private static IEnumerable<System.Reflection.MethodBase> TargetMethods() =>
            typeof(CrewSim).GetMethods().Where(m => m.Name == nameof(CrewSim.LoadGame) || m.Name == nameof(CrewSim.NewGame));
        private static void Prefix() => Reset();
    }
}
