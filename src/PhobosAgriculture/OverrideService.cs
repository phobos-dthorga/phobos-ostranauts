using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Liquids;

namespace PhobosAgriculture;

/// <summary>Press twice to go ahead (Agriculture 0.66.0, Framework 0.125.0; owner rule, 6 October 2026). A change that
/// needs machines paused or an old link removed no longer refuses for that: the first press says what will be done,
/// the second does it and lets the paused machines carry on. Owner report: the rack's water connection kept refusing
/// with "One of these machines is already linked" and "Pause both machines and their water intake first". Only what
/// pausing cannot fix is still refused, with a reason that names the machine.</summary>
internal static partial class Service
{
    /// <summary>The steps one change needs: the machines it pauses (and how each carries on) and any other step, worded.</summary>
    internal sealed class Hold
    {
        private readonly List<(Session Session, bool KeepIntake)> machines = new();
        internal readonly List<string> Steps = new();
        /// <summary>Holds a machine for the change. <paramref name="keepIntake"/> false: the change itself decides its
        /// water intake (a rack whose water source changes), so only its work is resumed.</summary>
        internal void Add(Session x, bool keepIntake = true)
        {
            if (machines.Any(m => m.Session == x)) return;
            machines.Add((x, keepIntake));
        }
        private IEnumerable<string> Working => machines.Where(m => m.Session.State.Running || m.Session.State.Receiving).Select(m => ObjectPresentation.Name(m.Session.Object));
        internal bool NeedsAsk => Steps.Count > 0 || Working.Any();
        /// <summary>What will be done, in the order it happens: the other steps, then the pause.</summary>
        internal string Warning()
        {
            var working = Working.ToList();
            var parts = new List<string>(Steps);
            if (working.Count > 0) parts.Add(Text.Get("override_pause", ListText(working)));
            return string.Join(" ", parts);
        }
        internal PausedChange Pause()
        {
            var change = new PausedChange();
            foreach (var (x, keepIntake) in machines)
            {
                bool running = x.State.Running, receiving = x.State.Receiving;
                change.Hold(ObjectPresentation.Name(x.Object), running || receiving,
                    () => { x.State.Running = false; x.State.Receiving = false; Save(x); },
                    () => ResumeHeld(x, running, receiving, keepIntake));
            }
            return change;
        }
    }

    /// <summary>Runs a change: asks first when it needs steps, holds the machines, makes the change, lets them carry on.
    /// <paramref name="change"/> returns null when it worked, or the refusal. The message is <paramref name="success"/>'s
    /// (the machine's status by default) followed by any machine that could not carry on.</summary>
    internal static bool Go(Hold hold, bool confirmed, Func<string?> change, Func<string> success, out string message)
    {
        if (hold.NeedsAsk && !Confirmations.Ask(hold.Warning(), confirmed, out message)) return false;
        var held = hold.Pause();
        string? failure; string notes;
        try { failure = change(); }
        finally { notes = held.Release(); }
        message = failure ?? success();
        if (notes.Length > 0) message = message.TrimEnd() + "\n" + notes;
        return failure == null;
    }

    /// <summary>Lets a held machine carry on through the checks its own Start uses; the reason when it cannot.</summary>
    private static string? ResumeHeld(Session x, bool running, bool receiving, bool keepIntake)
    {
        var co = x.Object;
        if (x.Protected || WaterGuard(co).Protected) return Text.Get("held_fault");
        if (!co.HasCond("IsInstalled") || co.HasCond("IsDamaged")) return Text.Get("held_damaged");
        if (running && !x.State.Running && (!Definitions.IsCooker(co) || CookerInput(x) != null)) x.State.Running = true;
        if (keepIntake && receiving && !x.State.Receiving && CanReceive(x)) x.State.Receiving = true;
        Save(x);
        return null;
    }
    private static bool CanReceive(Session x) => !Definitions.IsCooker(x.Object) &&
        (x.Routed || ShipsWaterSupply.Available || IrrigationDefinitions.IsSupply(x.Object) && BulkService.HasSelection(x.Object));

    /// <summary>Why another machine's water link cannot change now, or null. Pausing is never a reason: that is a step.</summary>
    private static string? PeerFault(CondOwner co, CondOwner peer)
    {
        if (!Definitions.Machine(peer) || Definitions.IsCooker(peer) || WorkupDefinitions.IsBench(peer)) return Text.Get("water_wrong_kind");
        var x = Get(peer);
        if (peer.ship != co.ship || !NativeFluidRoute.EndpointReady(peer) || x.Protected || WaterGuard(peer).Protected || LineGuard(peer).Protected)
            return Text.Get("water_not_ready", ObjectPresentation.Name(peer));
        return null;
    }

    /// <summary>"A", "A and B", "A, B and C".</summary>
    internal static string ListText(IReadOnlyList<string> names) => names.Count switch
    {
        0 => "", 1 => names[0],
        _ => Text.Get("list_and", string.Join(", ", names.Take(names.Count - 1)), names[names.Count - 1])
    };
}
