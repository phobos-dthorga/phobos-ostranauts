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
    /// <summary>Whether an object is going because its whole ship is being unloaded (a reload, a return to the menu, a
    /// despawn) rather than destroyed in play (Framework 0.73.0). The save keeps what such objects held, so a destroy hook
    /// must not release, vent or announce it as lost. The game's <c>Ship.Destroy</c> marks the ship destroyed, then
    /// takes each object off the ship before destroying it, so by then the object has no ship to ask (owner report,
    /// Framework 0.101.0: every reload announced the stores' contents as lost). The ship's unload is therefore tracked
    /// itself, while <c>Ship.Destroy</c> runs.</summary>
    public static bool Unloading(CondOwner? co) => co != null && IsUnloading(co.ship != null && co.ship.bDestroyed, shipUnloads);
    /// <summary>The rule, pure: the object's own ship is marked destroyed, or a ship's unload is under way.</summary>
    public static bool IsUnloading(bool shipDestroyed, int shipsUnloading) => shipDestroyed || shipsUnloading > 0;
    private static int shipUnloads;
    internal static void ShipUnloadStarted() => shipUnloads++;
    internal static void ShipUnloadFinished() { if (shipUnloads > 0) shipUnloads--; }

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
        Persistence.LegacyItemConversions.Reset();
        Registration.EquipmentInventory.Reset();
        Persistence.ContainerFit.Reset();
        Liquids.VesselContentsDisplay.Reset();
        Construction.NativeFloors.Reset();
        Inventory.StackUnits.Reset();
        Registration.LineJoints.Reset();
        Inventory.BeltCarriers.ClearAll();
        Registration.LineJobFilter.Reset();
        Registration.ItemHandling.BeginLoad();
        Registration.LootCarveRegistry.Reset();
        Data.DataPacks.Reset();
        Registration.MaintenanceSafety.LegacyFinishes.Clear();
        Liquids.GasNetworkSafety.Reset();
        // Framework's own items first, so content mods may add ports to them and name them in stock and conversions.
        Items.FrameworkItems.Register(Log);
        // Ship's Water tanks gain a process-water port once the water line's joint is published (Framework 0.59.0).
        try { Liquids.ShipsWaterPorts.Apply(); }
        catch (Exception e) { Log(Text.Get("ShipsWaterPorts.failed", e.Message)); }
        Notify(ContentLoading);
    }
    internal static void Complete()
    {
        ConstructionRegistry.CompleteLoad();
        Notify(ContentLoaded);
        // Family predicates read content-owned definition tables; remembered answers start again with them.
        Discovery.WorldFamilies.Reset();
        // Upkeep families are judged by definition too (Framework 0.111.0).
        Crew.Upkeep.ForgetDefinitions();
    }
}

[HarmonyPatch(typeof(DataHandler), "PostModLoadMainThread")]
internal static class FrameworkContentPatch
{
    private static void Prefix() => FrameworkLifecycle.Begin();
    private static void Postfix() => FrameworkLifecycle.Complete();
}

/// <summary>Marks a ship's unload for <see cref="FrameworkLifecycle.Unloading"/> while the game's own Ship.Destroy runs,
/// and always clears the mark afterwards, whatever happens inside.</summary>
[HarmonyPatch(typeof(Ship), nameof(Ship.Destroy))]
internal static class ShipUnloadPatch
{
    private static void Prefix() => FrameworkLifecycle.ShipUnloadStarted();
    private static void Finalizer() => FrameworkLifecycle.ShipUnloadFinished();
}
