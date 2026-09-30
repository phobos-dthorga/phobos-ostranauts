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
    /// <summary>Routine bookkeeping lines (settled draws) at BepInEx's Debug level, which its disk log leaves out by default.</summary>
    internal static Action<string> LogDebug = _ => { };

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
        Construction.SectionAssembly.Reset();
        Liquids.FluidRouteCache.InvalidateAll();
        Liquids.ShipsWaterSupply.Reset();
        Controls.ItemInformation.Reset();
        Trading.MarketStock.BeginLoad();
        Registration.MaintenanceSafety.Actions.Clear();
        Registration.EquipmentSaveUpgrade.BeginLoad();
        Persistence.DefinitionMigrations.Reset();
        Registration.LineJoints.Reset();
        Registration.LineJobFilter.Reset();
        Registration.ItemHandling.BeginLoad();
        Registration.LootCarveRegistry.Reset();
        Data.DataPacks.Reset();
        Registration.MaintenanceSafety.Repairs.Clear();
        Registration.MaintenanceSafety.LegacyFinishes.Clear();
        Notify(ContentLoading);
    }
    internal static void Complete()
    {
        ConstructionRegistry.CompleteLoad();
        Notify(ContentLoaded);
        // Family predicates read content-owned definition tables; remembered answers start again with them.
        Discovery.WorldFamilies.Reset();
    }
}

[HarmonyPatch(typeof(DataHandler), "PostModLoadMainThread")]
internal static class FrameworkContentPatch
{
    private static void Prefix() => FrameworkLifecycle.Begin();
    private static void Postfix() => FrameworkLifecycle.Complete();
}
