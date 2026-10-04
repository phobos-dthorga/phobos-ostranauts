using System;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Registration;

namespace Phobos.Ostranauts.Framework.Persistence;

/// <summary>Work that was running when the game was saved carries on after the reload (Framework 0.95.0; owner
/// decision, 5 October 2026, widening the 30 September decision for belt routes to processing machines, the mining
/// laser, G4 work and Auto Nav docking; the F6 furnace keeps its explicit Resume). A machine keeps one saved mark on
/// itself while the player's Start stands; after a load each marked machine is offered once, when the game has
/// finished loading and its ship is loaded, and its owner then starts it through its ordinary Start with every check.
/// A machine whose checks fail stays stopped with the reason, as it would have before. The mark grants nothing by
/// itself: it never replays work, power or deliveries, and a player may turn the whole behaviour off.</summary>
public static class ResumeAfterLoad
{
    /// <summary>The saved mark, a hidden condition on the machine.</summary>
    public const string Condition = "PhobosResumeAfterLoad";
    /// <summary>The player setting (Persistence/ResumeAfterLoad): off puts back the wait for a manual Resume.</summary>
    public static bool Enabled { get; set; } = true;
    private static readonly HashSet<string> offered = new(StringComparer.Ordinal);
    private static bool defaultReady() => CrewSim.objInstance != null && CrewSim.objInstance.FinishedLoading;
    /// <summary>Whether the world is ready for a resume; replaced by checks that run without the game.</summary>
    internal static Func<bool> Ready = defaultReady;

    internal static void Add(NativeDefinitions d) =>
        d.Conditions[Condition] = new JsonCond { strName = Condition, strNameFriendly = Condition, strColor = "Neutral", nDisplaySelf = 2, nDisplayOther = 2 };

    /// <summary>Keeps the mark equal to whether the player's Start still stands. Written only when it changes, so a
    /// machine may call it on every step.</summary>
    public static void Mark(CondOwner? machine, bool running)
    {
        if (machine == null || machine.bDestroyed) return;
        // A machine started in this session needs no offer: it is already running.
        if (running) offered.Add(machine.strID);
        if (machine.HasCond(Condition) != running) machine.SetCondAmount(Condition, running ? 1 : 0);
    }
    /// <summary>For a machine that follows its own state each step rather than marking at Start and Stop: sets the mark
    /// while it runs, and clears it when it does not, except while its offer after a load is still to come.</summary>
    public static void Sync(CondOwner? machine, bool running)
    {
        if (machine == null || machine.bDestroyed) return;
        if (running) Mark(machine, true);
        else if (machine.HasCond(Condition) && offered.Contains(machine.strID)) Mark(machine, false);
    }
    /// <summary>For a machine with more than one thing to carry on (a grow rack grows and takes in water): the mark holds
    /// a small set of flags the owner defines, zero meaning nothing was running. Same rule as <see cref="Sync(CondOwner, bool)"/>.</summary>
    public static void Sync(CondOwner? machine, int flags)
    {
        if (machine == null || machine.bDestroyed || flags < 0) return;
        if (flags == 0) { Sync(machine, false); return; }
        offered.Add(machine.strID);
        if ((int)Math.Round(machine.GetCondAmount(Condition)) != flags) machine.SetCondAmount(Condition, flags);
    }
    /// <summary>The flags a marked machine was saved with; 1 for a machine that only marks running.</summary>
    public static int Flags(CondOwner? machine) => machine == null || machine.bDestroyed ? 0 : Math.Max(0, (int)Math.Round(machine.GetCondAmount(Condition)));
    public static bool Marked(CondOwner? machine) => machine != null && !machine.bDestroyed && machine.HasCond(Condition);

    /// <summary>True once for a marked machine after a load: the caller starts it now. False for an unmarked machine
    /// (one condition probe), while the game or the machine's ship is still loading, and ever after the one offer.
    /// With the setting off the mark is cleared instead, so a stale mark never starts anything later.</summary>
    public static bool Due(CondOwner? machine)
    {
        if (machine == null || machine.bDestroyed || !machine.HasCond(Condition)) return false;
        if (!Ready() || machine.ship == null || (int)machine.ship.LoadState < 2) return false;
        if (!Enabled) { Mark(machine, false); return false; }
        return Decide(offered, machine.strID);
    }
    /// <summary>The one-offer rule, pure: an id is offered once until the next load.</summary>
    public static bool Decide(HashSet<string> seen, string? id) => !string.IsNullOrEmpty(id) && seen.Add(id!);
    /// <summary>A new load: every marked machine may be offered again.</summary>
    internal static void Reset() => offered.Clear();
}

[HarmonyLib.HarmonyPatch]
internal static class ResumeAfterLoadResetPatch
{
    private static IEnumerable<System.Reflection.MethodBase> TargetMethods() =>
        System.Linq.Enumerable.Where(typeof(CrewSim).GetMethods(), m => m.Name == nameof(CrewSim.LoadGame) || m.Name == nameof(CrewSim.NewGame));
    private static void Prefix() => ResumeAfterLoad.Reset();
}
