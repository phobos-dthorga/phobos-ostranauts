namespace Phobos.Ostranauts.Framework.Liquids;

/// <summary>Why a machine does not reach a store or another machine (Framework 0.69.0), in the order a player would
/// fix it: the store's own state first, then the pipe at the store, the pipe at the machine, and last a break
/// between the two.</summary>
public enum ReachProblem
{
    None, MachineNotReady, NotInstalled, Damaged, Locked, NotReady, TouchOnly, LayoutTooLarge,
    NoPipeAtStore, DrainedAtStore, NoPipeAtMachine, DrainedAtMachine, SeparateRuns
}

/// <summary>What <see cref="LinkDiagnosis.Classify"/> needs to know, with no game types so offline checks can build it.</summary>
public struct ReachFacts
{
    /// <summary>The two already reach each other (touching, or on one network).</summary>
    public bool Reached;
    public bool MachineReady;
    public bool Installed, Damaged, Locked;
    /// <summary>The store passes the endpoint checks (installed, intact, unlocked, on the player's own loaded ship).</summary>
    public bool Ready;
    /// <summary>A line family carries this cargo and both ends take part in it; false means touching is the only way.</summary>
    public bool Network;
    public bool Overflow;
    /// <summary>An open, working segment lies under or beside the store or the machine.</summary>
    public bool StoreOpenPipe, MachineOpenPipe;
    /// <summary>A drained (closed) segment lies under or beside the store or the machine.</summary>
    public bool StoreClosedPipe, MachineClosedPipe;
}

/// <summary>Turns the facts about one machine and one store into the single reason a link picker shows. Pure.</summary>
public static class LinkDiagnosis
{
    public static ReachProblem Classify(in ReachFacts f)
    {
        if (f.Reached && f.MachineReady && f.Ready) return ReachProblem.None;
        if (!f.MachineReady) return ReachProblem.MachineNotReady;
        if (!f.Installed) return ReachProblem.NotInstalled;
        if (f.Damaged) return ReachProblem.Damaged;
        if (f.Locked) return ReachProblem.Locked;
        if (!f.Ready) return ReachProblem.NotReady;
        if (f.Reached) return ReachProblem.None;
        if (!f.Network) return ReachProblem.TouchOnly;
        if (f.Overflow) return ReachProblem.LayoutTooLarge;
        if (!f.StoreOpenPipe) return f.StoreClosedPipe ? ReachProblem.DrainedAtStore : ReachProblem.NoPipeAtStore;
        if (!f.MachineOpenPipe) return f.MachineClosedPipe ? ReachProblem.DrainedAtMachine : ReachProblem.NoPipeAtMachine;
        return ReachProblem.SeparateRuns;
    }
}
