using System;
using HarmonyLib;

namespace Phobos.Ostranauts.Framework.Persistence;

/// <summary>One hook before the game serialises a ship for a save. Services that settle their records on a real-time
/// cadence (buffered draws, furnace heat, a flight in progress) subscribe here so every native save carries their
/// latest state; a failing handler is logged and never stops the save or the other handlers.</summary>
public static class SaveBoundary
{
    public static event Action<Ship>? BeforeShipSave;
    internal static void Raise(Ship ship)
    {
        var handlers = BeforeShipSave;
        if (handlers == null || ship == null) return;
        foreach (Action<Ship> handler in handlers.GetInvocationList())
        {
            try { handler(ship); }
            catch (Exception ex) { FrameworkLifecycle.Log(ex.ToString()); }
        }
    }
    [HarmonyPatch(typeof(Ship), nameof(Ship.GetJSON))]
    private static class SavePatch
    {
        [HarmonyPriority(Priority.High)]
        private static void Prefix(Ship __instance, bool bSaveGame) { if (bSaveGame) Raise(__instance); }
    }
}
