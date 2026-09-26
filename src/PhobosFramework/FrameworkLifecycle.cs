using System;
using HarmonyLib;
using Phobos.Ostranauts.Framework.Construction;

namespace Phobos.Ostranauts.Framework;

/// <summary>Subscribe in Awake; register definitions and recipes during ContentLoading.</summary>
public static class FrameworkLifecycle
{
    public static event Action? ContentLoading;
    public static event Action? ContentLoaded;
    internal static Action<string> Log = _ => { };

    private static void Notify(Action? handlers)
    {
        if (handlers == null) return;
        foreach (Action handler in handlers.GetInvocationList())
        {
            try { handler(); }
            catch (Exception ex) { Log(Text.Get("FrameworkLifecycle.content_registration_callback_failed", ex)); }
        }
    }

    internal static void Begin()
    {
        Crew.CrewWork.Reset();
        Audio.CompletionCues.Player?.Stop();
        Diagnostics.NativePerformance.WorldChanging();
        Observations.NativeRoomAlarms.Reset();
        FrameworkPlugin.RefreshLanguage();
        ConstructionRegistry.BeginLoad();
        Trading.MarketStock.BeginLoad();
        Registration.MaintenanceSafety.Actions.Clear();
        Registration.EquipmentSaveUpgrade.BeginLoad();
        Registration.MaintenanceSafety.Repairs.Clear();
        Registration.MaintenanceSafety.LegacyFinishes.Clear();
        Notify(ContentLoading);
    }
    internal static void Complete()
    {
        ConstructionRegistry.CompleteLoad();
        Notify(ContentLoaded);
    }
}

[HarmonyPatch(typeof(DataHandler), "PostModLoadMainThread")]
internal static class FrameworkContentPatch
{
    private static void Prefix() => FrameworkLifecycle.Begin();
    private static void Postfix() => FrameworkLifecycle.Complete();
}
