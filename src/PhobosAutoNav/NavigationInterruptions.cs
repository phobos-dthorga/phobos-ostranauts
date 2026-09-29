using System;
using System.Diagnostics;
using PhobosAutoNav.Core;

namespace PhobosAutoNav;

// Owner request (30 September 2026): the game's own StarSystem.Update sometimes throws while it spawns an NPC ship,
// and Auto Nav used to stop the flight every time. A failure that did not come from Auto Nav's code now clears Auto
// Nav's thrust for that step and guidance carries on next frame; repeated failures suspend with the intent kept; a
// failure inside Auto Nav's own code still stops, because then the fault is ours.
internal sealed partial class NavigationService
{
    private readonly InterruptionBudget interruptions = new();
    internal int PhysicsHolds { get; private set; }

    /// <summary>Called by the StarSystem.Update finalizer with the exception the game's update threw.</summary>
    internal void PhysicsInterrupted(Exception error)
    {
        if (ThrownByAutoNav(error)) { Disengage(Text.Get("Plugin.physics_interrupted")); return; }
        if (!AutoNavCore.Engaged && industrial == null && departure == null) return;
        if (!interruptions.TryHold(RealClock()))
        {
            interruptions.Reset();
            SuspendForInterruption();
            return;
        }
        PhysicsHolds++;
        string why = Text.Get("Plugin.physics_held");
        try
        {
            var own = industrial?.Carrier ?? departure?.Console?.ship ?? AutoNavCore.EngagedPlayer;
            if (own != null && !own.bDestroyed) HoldThrust(own, why); else { Torch.Cut(); status = why; }
        }
        catch (Exception ex) { log(ex.ToString()); Disengage(Text.Get("Plugin.physics_interrupted")); return; }
        log(why);
    }

    /// <summary>True when any frame of the exception's stack is Auto Nav's own code.</summary>
    internal static bool ThrownByAutoNav(Exception error)
    {
        try
        {
            foreach (var frame in new StackTrace(error, false).GetFrames() ?? Array.Empty<StackFrame>())
            {
                var space = frame.GetMethod()?.DeclaringType?.Namespace;
                if (space != null && (space == nameof(PhobosAutoNav) || space.StartsWith(nameof(PhobosAutoNav) + ".", StringComparison.Ordinal))) return true;
            }
            return false;
        }
        catch { return true; } // An unreadable stack is treated as ours: stop, as before.
    }

    // Keeps the destination, assigned ports and elapsed budget for Resume, like a lost contact.
    private void SuspendForInterruption()
    {
        string reason = Text.Get("Plugin.physics_repeated", InterruptionBudget.MaximumHolds, InterruptionBudget.WindowSeconds);
        StopExtended(reason); EndIndustrial(reason);
        CeaseFire();
        bool persisted = FinishSavedFlight(savedFlight?.SuspendedMode ?? SavedFlightMode.Suspended);
        issuing = true;
        try { if (AutoNavCore.Engaged) AutoNavCore.EndFlight(AutoNavCore.EngagedPlayer, "ABORTED"); }
        catch (Exception ex) { log(ex.ToString()); }
        finally
        {
            AutoNavCore.ResetStatics(); DropCombat(); issuing = false;
            status = persisted ? reason : Text.Get("Persistence.write_failed");
        }
        log(status);
    }
}
