using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Processing;
using Phobos.Ostranauts.Framework.Construction;

namespace PhobosAgriculture;

[BepInPlugin(Id, "Phobos Agriculture", Version)]
[BepInDependency(FrameworkInfo.PluginId, "0.26.0")]
[BepInDependency("com.ostranauts.shipswater", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("phobosgekko.ostranauts.shipbreaker", BepInDependency.DependencyFlags.SoftDependency)]
[BepInProcess("Ostranauts.exe")]
public sealed class Plugin : BaseUnityPlugin
{
    public const string Id = "phobosgekko.ostranauts.agriculture", Version = "0.13.0";
    internal static Action<string> Log = _ => { };
    internal static ConfigEntry<double> Pace = null!, ReserveLitres = null!;
    internal static ConfigEntry<bool> LootEnabled = null!;
    internal static ConfigEntry<double> LootMultiplier = null!;
    private Harmony? harmony;
    private float nextScan;
    private void Awake()
    {
        Log = x => Logger.LogInfo(x); Text.EnsureLoaded();
        Pace = Config.Bind("Crops", "GrowthDurationMultiplier", 1d, new ConfigDescription(Text.Get("pace_setting"), new AcceptableValueRange<double>(.5, 2)));
        ReserveLitres = Config.Bind("Irrigation", "CrewReserveLitres", 10d, new ConfigDescription(Text.Get("reserve_setting"), new AcceptableValueRange<double>(0, 100000)));
        LootEnabled = Config.Bind("Loot", "Enabled", true, Text.Get("loot_enabled_setting"));
        LootMultiplier = Config.Bind("Loot", "ChanceMultiplier", LootContent.DefaultMultiplier, new ConfigDescription(Text.Get("loot_multiplier_setting"), new AcceptableValueRange<double>(0, LootContent.MaximumMultiplier)));
        harmony = new Harmony(Id); harmony.PatchAll(typeof(Plugin).Assembly);
        Phobos.Ostranauts.Framework.Inventory.CollectorCargo.Register(Id, RecyclerCapture.Cargo);
        RecyclerCapture.Available = BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue("phobosgekko.ostranauts.shipbreaker", out var shipbreaker) && shipbreaker.Metadata.Version >= new Version(0,20,0) && Phobos.Ostranauts.Framework.Liquids.ShipsWaterRejects.Install(harmony, RecyclerCapture.Instance);
        FrameworkLifecycle.ContentLoading += Load; FrameworkLifecycle.ContentLoaded += Confirm;
        EquipmentProviders.Register(new Provider());
        Phobos.Ostranauts.Framework.Crew.CrewWork.Register(new AgricultureCrewProvider());
        Phobos.Ostranauts.Framework.Crew.CrewSpecialities.Register(new("Agriculture", Text.Get("crew_skill_agriculture"), Id, Phobos.Ostranauts.Framework.Crew.CrewRole.Agriculture));
        Phobos.Ostranauts.Framework.Crew.CrewSpecialities.Register(new("Cooking", Text.Get("crew_skill_cooking"), Id, Phobos.Ostranauts.Framework.Crew.CrewRole.Cooking));
    }
    private static void Load() { Service.Reset(); RecyclerCapture.Reset(); try { Definitions.Load(); } catch (Exception e) { Definitions.Ready = false; Log(e.ToString()); } }
    private static void Confirm() => Definitions.Ready = ConstructionRegistry.Ready(Id);
    private void Update() { if (UnityEngine.Time.unscaledTime >= nextScan) { nextScan = UnityEngine.Time.unscaledTime + 2; Service.PassiveScan(); } }
    private void OnDestroy() { Phobos.Ostranauts.Framework.Inventory.CollectorCargo.Unregister(Id); Phobos.Ostranauts.Framework.Liquids.ShipsWaterRejects.Forget(RecyclerCapture.Instance); FrameworkLifecycle.ContentLoading -= Load; FrameworkLifecycle.ContentLoaded -= Confirm; EquipmentProviders.Unregister(Id); harmony?.UnpatchSelf(); Service.Reset(); }
}

[HarmonyPatch(typeof(Powered), "Run")]
internal static class RunPatch
{
    private static void Prefix(Powered __instance) { if (Definitions.Machine(__instance.CO)) Service.BeginRun(__instance.CO); }
    private static void Finalizer(Powered __instance) { if (Definitions.Machine(__instance.CO)) Service.Tick(__instance.CO); }
}
[HarmonyPatch(typeof(Powered), "UsePower", new[] { typeof(CondOwner), typeof(double) })]
internal static class PowerPatch
{
    private static bool Prefix(Powered __instance, CondOwner __0, ref double __1, out EnergyReceipt? __state)
    {
        __state = null; if (!Definitions.Machine(__0)) return true;
        try { __1 = Service.Requested(__0, __1); if (__1 <= 0) return false; __state = NativeEnergyReceipts.Begin(__instance, __0, __1); return true; }
        catch (Exception e) { Service.Fault(__0, e); return false; }
    }
    private static void Finalizer(Powered __instance, CondOwner __0, EnergyReceipt? __state)
    {
        if (__state == null) return;
        try { var s = Service.Get(__0); s.Received += NativeEnergyReceipts.Complete(__instance, __0, __state); s.LastPower = StarSystem.fEpoch; }
        catch (Exception e) { Service.Fault(__0, e); }
        finally { NativeEnergyReceipts.Forget(__instance); }
    }
}
[HarmonyPatch(typeof(Interaction), nameof(Interaction.ApplyEffects))]
internal static class EffectsPatch
{
    private static void Prefix(Interaction __instance, bool isCancelIa)
    {
        if (isCancelIa) return;
        var food = __instance.objUs;
        if (food != null && (__instance.strName == "SeekFoodAllowDirect" || __instance.strName == "SeekFoodAllowDirectPrepared") && (food.strCODef == Definitions.Meal || food.strCODef == Definitions.Leaves))
            __instance.LootCTsThem = DataHandler.GetLoot(food.strCODef + "Effects");
    }
    private static void Postfix(Interaction __instance, bool isCancelIa)
    {
        if (!isCancelIa && __instance.strName == RecyclerCapture.Controls && RecyclerCapture.IsRecycler(__instance.objThem)) { Panel.Show(__instance.objThem); return; }
        if (isCancelIa || !Definitions.Machine(__instance.objThem)) return;
        if (__instance.strName == Definitions.Controls) { if (__instance.objUs == CrewSim.GetSelectedCrew()) Panel.Show(__instance.objThem); return; }
        foreach (string action in Definitions.Work)
            if (!__instance.bCancel && __instance.strName == Definitions.WorkId(action) && Service.Work(__instance.objThem, __instance.objUs, action))
                Phobos.Ostranauts.Framework.Crew.CrewSpecialities.CreditPractical(__instance);
    }
}
[HarmonyPatch(typeof(ConsoleResolver), nameof(ConsoleResolver.ResolveString))]
internal static class ConsolePatch
{
    private static bool Prefix(ref string strInput, ref bool __result)
    {
        var parts = strInput.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0 || !parts[0].Equals("phobosagriculture", StringComparison.OrdinalIgnoreCase)) return true;
        if (parts.Length == 1 || parts[1] == "list")
        { strInput += "\n" + string.Join("\n", CrewSim.GetSelectedCrew()?.ship?.GetCOs(null, false, false, true).Where(Definitions.Machine).Select(c => c.strNameFriendly + " " + c.strID) ?? Array.Empty<string>()); __result = true; return false; }
        var co = parts.Length >= 3 ? Service.Resolve(parts[2]) : null;
        string message = Text.Get("help");
        string action = parts[1] == "link-water" && parts.Length == 4 ? "link-water:" + parts[3] : parts[1];
        __result = Definitions.Machine(co) && Service.Command(co!, null, action, out message); strInput += "\n" + message; return false;
    }
}
[HarmonyPatch]
internal static class ReloadPatch
{
    private static IEnumerable<MethodBase> TargetMethods() => typeof(CrewSim).GetMethods().Where(m => m.Name == nameof(CrewSim.LoadGame) || m.Name == nameof(CrewSim.NewGame));
    private static void Prefix() { Service.Reset(); RecyclerCapture.Reset(); }
}

internal sealed class Provider : IEquipmentProvider, IEquipmentPanelPresentation
{
    public bool IsConfiguration(string action)=>action.StartsWith("mix-",StringComparison.Ordinal)||action.StartsWith("dose-",StringComparison.Ordinal)||action=="water-only"||action=="water-routed"||action=="water-legacy"||action=="unlink-water";
    public string ConfigurationStamp(CondOwner co)=>PanelConfiguration.Stamp(co);
    public bool ApplyConfiguration(CondOwner co,ConsoleBinding? binding,string expected,string action,out string reason)
    {
        reason=Phobos.Ostranauts.Framework.Controls.ConsoleWidgets.Text("stale");if(co.bDestroyed||expected!=PanelConfiguration.Stamp(co)||!IsConfiguration(action))return false;
        bool saved=Service.Command(co,binding,action,out reason);if(saved)Phobos.Ostranauts.Framework.Controls.ConfigurationStamp.SuspendChangedOrder(co);return saved;
    }
    public string Id => Plugin.Id;
    public IReadOnlyList<string> Definitions { get; } = Array.AsReadOnly(new[] { PhobosAgriculture.Definitions.Rack + "Installed", PhobosAgriculture.Definitions.Cooker + "Installed", IrrigationDefinitions.Supply + "Installed", WorkupDefinitions.Bench + "Installed" });
    public EquipmentSnapshot Snapshot(CondOwner co)
    {
        var s = Service.Get(co); var b = s.State;
        return new EquipmentSnapshot(co.strID, co.strNameFriendly, "agriculture", new EquipmentActivity(s.Protected || b.Health < .5 ? EquipmentState.Blocked : b.Ready ? EquipmentState.Ready : b.Running ? EquipmentState.Running : EquipmentState.Paused, Service.Describe(co)),
            Service.Actions(co).Select(a => new EquipmentAction(a, Text.Get(a))));
    }
    public bool Command(CondOwner co, ConsoleBinding? binding, string action, out string message) => Service.Command(co, binding, action, out message);
}

[HarmonyPatch(typeof(Interaction), "TriggeredInternal")]
internal static class ContentsEligibilityPatch
{
    internal static bool Blocked(Interaction action, CondOwner? us, CondOwner? them)
    {
        var co = action.strName.StartsWith("MS", StringComparison.Ordinal) ? us : them;
        bool supply = new[] { Definitions.Nutrient, WorkupDefinitions.Makeup, WorkupDefinitions.Mixture, WorkupDefinitions.Concentrate, Service.RecoveryCartridge }.Contains(co?.strCODef);
        if (supply && (action.strName.Contains("Repair") || action.strName.Contains("Restore") || action.strName.Contains("Undamage"))) return true;
        return Definitions.Machine(co) && (action.strName.Contains("Dismantle") || action.strName.Contains("Uninstall")) &&
            (Service.Get(co!).Protected || Service.WaterGuard(co!).Protected || Service.Get(co!).State.ContentsMass + Service.Get(co!).Solution.TotalKg + Service.Get(co!).Line.TotalKg > 1e-8 || Service.Get(co!).State.CookerProgress > 0 || Service.Get(co!).Workup.Mode.Length > 0);
    }
    private static void Postfix(Interaction __instance, CondOwner objUs, CondOwner objThem, ref bool __result)
    { if (__result && Blocked(__instance, objUs, objThem)) { __result = false; __instance.AddFailReason("main", Text.Get(Definitions.Machine(objUs) || Definitions.Machine(objThem) ? "unload_first" : "consumable_no_repair")); } }
}
[HarmonyPatch(typeof(Interaction), nameof(Interaction.ApplyEffects))]
internal static class ContentsCompletionPatch
{
    private static bool Prefix(Interaction __instance) => !ContentsEligibilityPatch.Blocked(__instance, __instance.objUs, __instance.objThem);
}

[HarmonyPatch(typeof(CondOwner), nameof(CondOwner.ModeSwitch))]
internal static class ModeChangePatch
{
    private static void Prefix(CondOwner __instance, CondOwner coNew, out Service.Session? __state)
    {
        __state = Definitions.Machine(__instance) && Definitions.Machine(coNew) && WorkupDefinitions.IsBench(__instance) == WorkupDefinitions.IsBench(coNew) && Definitions.IsCooker(__instance) == Definitions.IsCooker(coNew) && IrrigationDefinitions.IsSupply(__instance) == IrrigationDefinitions.IsSupply(coNew) ? Service.Get(__instance) : null;
    }
    private static void Postfix(CondOwner coNew, Service.Session? __state)
    {
        if (__state == null) return;
        try { Service.ModeChanged(coNew, __state); } catch (Exception e) { Service.Fault(coNew, e); }
    }
}
