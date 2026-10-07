using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace Phobos.Ostranauts.Framework.Discovery;

/// <summary>A registered family of world objects (every Agriculture machine, every furnace-family part...).</summary>
public sealed class WorldFamily
{
    internal readonly int Id;
    public string Key { get; }
    internal WorldFamily(int id, string key) { Id = id; Key = key; }
    /// <summary>Fills <paramref name="into"/> with the family's objects that are alive and registered in the world now.</summary>
    public void Members(List<CondOwner> into) => WorldFamilies.Members(this, into);
    /// <summary>Whether a definition id belongs to this family.</summary>
    public bool Contains(string? definition) => WorldFamilies.Contains(this, definition);
    /// <summary>Adds an object the caller already knows about (a mode-switch replacement) without waiting for the sweep.</summary>
    public void Offer(CondOwner? co) => WorldFamilies.Offer(co);
}

/// <summary>One shared discovery of our objects in the whole world, replacing a full pass per mod every two seconds
/// (29 September 2026 fast-forward pass, stage 8). One sweep over the world's object map runs every
/// <see cref="CycleSeconds"/> of real time, spread across frames so no single frame pays for it; the first sweep after
/// a world or content load runs in one go. A member is read back only while it is alive and still registered in the
/// world under its current id, so removal is exact.
///
/// Framework 0.133.0 (L104; owner choice, 8 October 2026): an object that joins a ship (installed, bought, spawned,
/// dropped, arriving with a ship that loads, or the new form a mode switch adds, such as a damaged or repaired machine)
/// is offered to the families at once, and the sweep that re-examines
/// all of the world's objects (about 75,000 on the owner's save, 7.6 ms a real second at two seconds) runs every ten
/// seconds as a safety net for anything that never joins a ship, such as a canister put straight into a container,
/// which is then found within one to two cycles.</summary>
public static class WorldFamilies
{
    public const double CycleSeconds = 10;
    /// <summary>The fewest objects examined in a frame while a sweep is in progress.</summary>
    public const int MinimumSlice = 64;
    private static readonly WorldIndex<CondOwner> index = new(co => co.strCODef, Live);
    private static readonly Dictionary<string, WorldFamily> families = new(StringComparer.Ordinal);
    private static double cycleStart = double.NegativeInfinity;
    private static int cycleTotal;

    public static WorldFamily Register(string key, Func<string, bool> member)
    {
        int id = index.Register(key, member);
        if (!families.TryGetValue(key, out var family)) families[key] = family = new WorldFamily(id, key);
        return family;
    }
    public static void Unregister(string key) => index.Unregister(key);

    internal static bool Live(CondOwner co) => co != null && !co.bDestroyed && co.strID != null && DataHandler.mapCOs != null &&
        DataHandler.mapCOs.TryGetValue(co.strID, out var registered) && ReferenceEquals(registered, co);
    private static bool Ready => CrewSim.objInstance != null && CrewSim.objInstance.FinishedLoading && DataHandler.mapCOs != null;

    internal static bool Contains(WorldFamily family, string? definition) => index.Belongs(family.Id, definition);
    internal static void Offer(CondOwner? co) { if (co != null && !co.bDestroyed) index.Offer(co); }

    internal static void Members(WorldFamily family, List<CondOwner> into)
    {
        if (!Ready) { into.Clear(); return; }
        if (!index.Primed) Prime();
        index.Members(family.Id, into);
    }

    /// <summary>World or content change: members, remembered definition answers and the sweep start again.</summary>
    internal static void Reset() { index.Reset(); cycleStart = double.NegativeInfinity; cycleTotal = 0; }

    private static void Prime()
    {
        using var measurement = Diagnostics.Performance.Measure(Diagnostics.Performance.WorldSweep);
        index.Begin(DataHandler.mapCOs.Values);
        Diagnostics.Performance.Increment(Diagnostics.Performance.WorldSweepObjects, index.Advance(int.MaxValue));
        cycleStart = Cadence.RealTime; cycleTotal = 0;
    }

    /// <summary>Framework Update: advances the spread sweep by the share of the cycle that real time has reached.</summary>
    internal static void Poll()
    {
        if (!Ready || index.FamilyCount == 0) return;
        if (!index.Primed) { Prime(); return; }
        double now = Cadence.RealTime;
        if (!index.Sweeping)
        {
            if (now - cycleStart < CycleSeconds) return;
            cycleStart = now;
            using var snapshot = Diagnostics.Performance.Measure(Diagnostics.Performance.WorldSweep);
            index.Begin(DataHandler.mapCOs.Values); cycleTotal = index.Pending;
            return;
        }
        double share = Math.Min(1, Math.Max(0, (now - cycleStart) / CycleSeconds));
        int due = (int)Math.Ceiling(cycleTotal * share) - (cycleTotal - index.Pending);
        using var measurement = Diagnostics.Performance.Measure(Diagnostics.Performance.WorldSweep);
        Diagnostics.Performance.Increment(Diagnostics.Performance.WorldSweepObjects, index.Advance(Math.Max(MinimumSlice, due)));
    }

    // An object joining a ship is offered at once: one definition lookup, no scan. Ship.AddCO has two overloads, each named
    // by its parameter types (PatchResolutionChecks in the native suite resolves every patch); the object is the first
    // argument of each.
    [HarmonyPatch(typeof(Ship), nameof(Ship.AddCO), typeof(CondOwner), typeof(bool))]
    private static class AddPatch { private static void Postfix(CondOwner __0) { if (index.FamilyCount > 0) Offer(__0); } }
    [HarmonyPatch(typeof(Ship), nameof(Ship.AddCO), typeof(CondOwner), typeof(bool), typeof(bool))]
    private static class AddSkipPatch { private static void Postfix(CondOwner __0) { if (index.FamilyCount > 0) Offer(__0); } }

    [HarmonyPatch]
    private static class ReloadPatch
    {
        private static IEnumerable<System.Reflection.MethodBase> TargetMethods() =>
            typeof(CrewSim).GetMethods().Where(m => m.Name == nameof(CrewSim.LoadGame) || m.Name == nameof(CrewSim.NewGame));
        private static void Prefix() => Reset();
    }
}
