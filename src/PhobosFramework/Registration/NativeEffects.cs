using System;

namespace Phobos.Ostranauts.Framework.Registration;

/// <summary>Refusing an action at its effects step. The game's ApplyEffects closes the task that
/// queued the action before applying anything; a Harmony prefix that returns false skips that and
/// leaves a painted or standing task listed forever. Close the task here, tell the actor why, and
/// let the caller return the false this returns.</summary>
public static class NativeEffects
{
    public static bool Refuse(Interaction action, string reason)
    {
        try
        {
            var manager = CrewSim.objInstance?.workManager;
            if (manager != null && action?.objUs != null && action.objThem != null)
                manager.CompleteTask(action.strChainStart ?? action.strName, action.objUs.strID, action.objThem.strID);
            if (!string.IsNullOrEmpty(reason))
            {
                FrameworkLifecycle.Log(reason);
                var actor = action?.objUs;
                if (actor != null && !actor.bDestroyed && actor.HasCond("IsHuman")) actor.LogMessage(reason, "Badish", "Game");
            }
        }
        catch (Exception e) { FrameworkLifecycle.Log(Text.Get("NativeEffects.refusal_failed", action?.strName ?? "", e.Message)); }
        return false;
    }
}
