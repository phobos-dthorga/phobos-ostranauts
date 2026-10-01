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
[BepInDependency(FrameworkInfo.PluginId, "0.71.0")]
[BepInDependency("com.ostranauts.shipswater", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("phobosgekko.ostranauts.shipbreaker", BepInDependency.DependencyFlags.SoftDependency)]
[BepInProcess("Ostranauts.exe")]
public sealed class Plugin : BaseUnityPlugin
{
    public const string Id = "phobosgekko.ostranauts.agriculture", Version = "0.38.0";
    internal static Action<string> Log = _ => { };
    internal static ConfigEntry<double> Pace = null!, ReserveLitres = null!;
    internal static ConfigEntry<bool> LootEnabled = null!;
    internal static ConfigEntry<double> LootMultiplier = null!;
    private Harmony? harmony;
    private float nextScan;
    private void Awake()
    {
        Log = x => Logger.LogInfo(x); Text.EnsureLoaded();
        PerformanceMetrics.Initialize();
        Pace = Config.Bind("Crops", "GrowthDurationMultiplier", 1d, new ConfigDescription(Text.Get("pace_setting"), new AcceptableValueRange<double>(.5, 2)));
        ReserveLitres = Config.Bind("Irrigation", "CrewReserveLitres", 10d, new ConfigDescription(Text.Get("reserve_setting"), new AcceptableValueRange<double>(0, 100000)));
        LootEnabled = Config.Bind("Loot", "Enabled", true, Text.Get("loot_enabled_setting"));
        LootMultiplier = Config.Bind("Loot", "ChanceMultiplier", LootContent.DefaultMultiplier, new ConfigDescription(Text.Get("loot_multiplier_setting"), new AcceptableValueRange<double>(0, LootContent.MaximumMultiplier)));
        harmony = new Harmony(Id); harmony.PatchAll(typeof(Plugin).Assembly);
        Phobos.Ostranauts.Framework.Inventory.CollectorCargo.Register(Id, RecyclerCapture.Cargo);
        RecyclerCapture.Available = BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue("phobosgekko.ostranauts.shipbreaker", out var shipbreaker) && shipbreaker.Metadata.Version >= new Version(0,20,0) && Phobos.Ostranauts.Framework.Liquids.ShipsWaterRejects.Install(harmony, RecyclerCapture.Instance);
        FrameworkLifecycle.ContentLoading += Load; FrameworkLifecycle.ContentLoaded += Confirm;
        EquipmentProviders.Register(new Provider());
        Phobos.Ostranauts.Framework.Trading.BulkSupplies.Register(new AgricultureBulkSupplies());
        Phobos.Ostranauts.Framework.Crew.CrewWork.Register(new AgricultureCrewProvider());
        Phobos.Ostranauts.Framework.Crew.CrewSpecialities.Register(new("Agriculture", Text.Get("crew_skill_agriculture"), Id, Phobos.Ostranauts.Framework.Crew.CrewRole.Agriculture));
        Phobos.Ostranauts.Framework.Crew.CrewSpecialities.Register(new("Cooking", Text.Get("crew_skill_cooking"), Id, Phobos.Ostranauts.Framework.Crew.CrewRole.Cooking));
    }
    private static void Load()
    {
        Service.Reset(); RecyclerCapture.Reset();
        try { Definitions.Load(); } catch (Exception e) { Definitions.Ready = false; Log(e.ToString()); }
        // Items a saved job names keep their place when a smaller inventory is fitted on load (Framework 0.70.0).
        foreach (string family in new[] { IrrigationDefinitions.Supply, WorkupDefinitions.Bench, Definitions.Cooker })
            Phobos.Ostranauts.Framework.Persistence.ContainerFit.KeepFirst(family, Service.NamedByJob);
    }
    private static void Confirm() => Definitions.Ready = ConstructionRegistry.Ready(Id);
    private void Update() { if (UnityEngine.Time.unscaledTime >= nextScan) { nextScan = UnityEngine.Time.unscaledTime + 2; Service.PassiveScan(); } }
    private void OnDestroy() { Phobos.Ostranauts.Framework.Inventory.CollectorCargo.Unregister(Id); Phobos.Ostranauts.Framework.Liquids.ShipsWaterRejects.Forget(RecyclerCapture.Instance); FrameworkLifecycle.ContentLoading -= Load; FrameworkLifecycle.ContentLoaded -= Confirm; EquipmentProviders.Unregister(Id); Phobos.Ostranauts.Framework.Trading.BulkSupplies.Unregister(Id); Phobos.Ostranauts.Framework.Liquids.BulkVessels.Unregister(Id); harmony?.UnpatchSelf(); Service.Reset(); }
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
    // Only the requested amount is ours; the game's Run always proceeds, because skipping it left IsPowered
    // unset forever (the game sets and clears it inside Run).
    private static bool Prefix(Powered __instance, CondOwner __0, ref double __1, out EnergyReceipt? __state)
    {
        __state = null; if (!Definitions.Machine(__0)) return true;
        try { __1 = Service.Requested(__0, __1); if (__1 > 0) __state = NativeEnergyReceipts.Begin(__instance, __0, __1); }
        catch (Exception e) { Service.Fault(__0, e); __1 = 0; }
        return true;
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
    // Food effects are no longer swapped in here: each food has its own eating reply in the vanilla chain (Definitions).
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

internal sealed class Provider : IEquipmentProvider, IEquipmentPanelFields
{
    public IEnumerable<EquipmentField> Fields(CondOwner co)
    {
        if(!BulkDefinitions.IsTank(co)&&!IrrigationDefinitions.IsSupply(co))yield break;
        string peer=BulkService.Peer(co);
        yield return new(Text.Get("bulk_connection"),ObjectPresentation.Name(peer),BulkService.Candidates(co).Select(c=>("bulk-link:"+c.strID,LinkChoices.Label(co,c,Phobos.Ostranauts.Framework.Liquids.LineFamilies.ProcessWater,false))).Concat(new[]{("bulk-link:none",Text.Get("bulk-link:none"))}),
            "bulk-link:"+(peer.Length==0?"none":peer),()=>BulkService.LinkNote(co));
        if(BulkDefinitions.IsTank(co)&&!BulkService.Protected(co))yield return new(Text.Get("bulk_reserve"),Text.Get("bulk_kg",BulkService.Read(co).ReserveKg),BulkDefinitions.ReserveChoices(BulkDefinitions.CapacityOf(co)).Select(n=>("bulk-reserve:"+n.ToString(System.Globalization.CultureInfo.InvariantCulture),Text.Get("bulk_kg",n))));
        if(IrrigationDefinitions.IsSupply(co))yield return new(Text.Get("bulk_target"),BulkService.TryTarget(co,out double target)?Text.Get("bulk_kg",target):Text.Get("protected"),new[]{5d,10d,15d,19.5}.Select(n=>("bulk-target:"+n.ToString(System.Globalization.CultureInfo.InvariantCulture),Text.Get("bulk_kg",n))));
    }
    public bool IsConfiguration(string action)=>action.StartsWith("mix-",StringComparison.Ordinal)||action.StartsWith("dose-",StringComparison.Ordinal)||action=="water-only"||action=="water-routed"||action=="water-legacy"||action=="unlink-water"||action.StartsWith("bulk-link:",StringComparison.Ordinal)||action.StartsWith("bulk-reserve:",StringComparison.Ordinal)||action.StartsWith("bulk-target:",StringComparison.Ordinal);
    public string ConfigurationStamp(CondOwner co)=>PanelConfiguration.Stamp(co);
    public bool ApplyConfiguration(CondOwner co,ConsoleBinding? binding,string expected,string action,out string reason)
    {
        reason=Phobos.Ostranauts.Framework.Controls.ConsoleWidgets.Text("stale");if(co.bDestroyed||expected!=PanelConfiguration.Stamp(co)||!IsConfiguration(action))return false;
        bool saved=Command(co,binding,action,out reason);if(saved)Phobos.Ostranauts.Framework.Controls.ConfigurationStamp.SuspendChangedOrder(co);return saved;
    }
    public string Id => Plugin.Id;
    public IReadOnlyList<string> Definitions { get; } = Array.AsReadOnly(new[] { PhobosAgriculture.Definitions.Rack + "Installed", PhobosAgriculture.Definitions.Cooker + "Installed", IrrigationDefinitions.Supply + "Installed", WorkupDefinitions.Bench + "Installed", BulkDefinitions.Tank + "Installed", BulkDefinitions.Tank + "InstalledDmg" }
        .Concat(BulkDefinitions.Sizes.Skip(1).SelectMany(s => new[] { s.Prefix + "Installed", s.Prefix + "InstalledDmg" }))
        .Concat(HopperDefinitions.Sizes.SelectMany(s => new[] { s.Prefix + "Installed", s.Prefix + "InstalledDmg" })).ToArray());
    public EquipmentSnapshot Snapshot(CondOwner co)
    {
        // A protected machine offers the owner-confirmed accept action in place of its ordinary controls.
        if(BulkDefinitions.IsTank(co))
        {
            bool tankProtected=BulkService.Protected(co);
            return new EquipmentSnapshot(co.strID,co.strNameFriendly,"agriculture",new EquipmentActivity(tankProtected||co.HasCond("IsDamaged")||co.HasCond("IsLocked")||BulkService.Read(co).CatchKg>0?EquipmentState.Blocked:EquipmentState.Ready,BulkService.Describe(co)),
                tankProtected?new[]{new EquipmentAction("bulk-accept",Text.Get("accept")),new EquipmentAction("pause",Text.Get("pause"))}:new[]{new EquipmentAction("pause",Text.Get("pause"))});
        }
        if(HopperDefinitions.IsHopper(co))
        {
            bool hopperProtected=HopperService.Protected(co);
            return new EquipmentSnapshot(co.strID,co.strNameFriendly,"agriculture",new EquipmentActivity(hopperProtected||co.HasCond("IsDamaged")||co.HasCond("IsLocked")||HopperService.Read(co).CatchKg>0?EquipmentState.Blocked:EquipmentState.Ready,HopperService.Describe(co)),
                hopperProtected?new[]{new EquipmentAction("bulk-accept",Text.Get("accept"))}:Array.Empty<EquipmentAction>());
        }
        var s = Service.Get(co); var b = s.State;
        return new EquipmentSnapshot(co.strID, co.strNameFriendly, "agriculture", new EquipmentActivity(s.Protected || b.Health < .5 ? EquipmentState.Blocked : b.Ready ? EquipmentState.Ready : b.Running ? EquipmentState.Running : EquipmentState.Paused, Service.Describe(co)),
            (s.Protected ? new[] { "accept" } : Service.Actions(co)).Select(a => new EquipmentAction(a, Text.Get(a))));
    }
    public bool Command(CondOwner co, ConsoleBinding? binding, string action, out string message) => BulkDefinitions.IsTank(co)?BulkService.Command(co,binding,action,out message):
        HopperDefinitions.IsHopper(co)?HopperService.Command(co,binding,action,out message):Service.Command(co, binding, action, out message);
}

[HarmonyPatch(typeof(Interaction), "TriggeredInternal")]
internal static class ContentsEligibilityPatch
{
    internal static bool Blocked(Interaction action, CondOwner? us, CondOwner? them)
        => Reason(action, us, them) != null;
    /// <summary>Offer-time check for our own work actions, so crew are not sent to fail: the same rule the work
    /// applies when it runs. Maintenance reasons are checked separately, at offer and at finish.</summary>
    // These run for every native offer and completion in the game: our objects are recognised first, by set
    // lookup, and only then are action names searched.
    internal static string? WorkReason(Interaction action, CondOwner? us, CondOwner? them)
    {
        if (action.strName == null || !Definitions.Machine(them) || !Definitions.WorkIds.TryGetValue(action.strName, out var work)) return null;
        return Service.WorkProblem(them!, us, work);
    }
    internal static string? Reason(Interaction action, CondOwner? us, CondOwner? them)
    {
        string name = action.strName;
        if (string.IsNullOrEmpty(name)) return null;
        var co = name.StartsWith("MS", StringComparison.Ordinal) ? us : them;
        if (co == null) return null;
        bool supply = Definitions.Supplies.Contains(co.strCODef);
        if (supply) return name.Contains("Repair") || name.Contains("Restore") || name.Contains("Undamage") ? Text.Get("consumable_no_repair") : null;
        if (!BulkDefinitions.IsTank(co) && !HopperDefinitions.IsHopper(co) && !Definitions.Machine(co)) return null;
        return name.Contains("Dismantle") || name.Contains("Uninstall") ? RemovalReason(co) : null;
    }
    internal static string? RemovalReason(CondOwner? co)
    {
        if (HopperDefinitions.IsHopper(co)) return HopperService.RemovalReason(co!);
        if (BulkDefinitions.IsTank(co))
        {
            if (BulkService.Protected(co!)) return Text.Get("Maintenance.protected");
            if (BulkService.Read(co!).TotalKg > 1e-8) return Text.Get("Maintenance.tank");
            return BulkService.HasLink(co!) ? Text.Get("Maintenance.link") : null;
        }
        if (!Definitions.Machine(co)) return null;
        var s = Service.Get(co!);
        if (s.Protected || Service.WaterGuard(co!).Protected) return Text.Get("Maintenance.protected");
        if (s.State.ContentsMass + s.Solution.TotalKg + s.Line.TotalKg > 1e-8) return Text.Get("Maintenance.contents");
        if (s.State.CookerProgress > 0 || s.Workup.Mode.Length > 0) return Text.Get("Maintenance.job");
        return null;
    }
    private static void Postfix(Interaction __instance, CondOwner objUs, CondOwner objThem, ref bool __result)
    { var reason = __result ? Reason(__instance, objUs, objThem) ?? WorkReason(__instance, objUs, objThem) : null; if (reason != null) { __result = false; __instance.AddFailReason("main", reason); } }
}
// A refused maintenance finish still closes the game's task for it, as native effects would.
[HarmonyPatch(typeof(Interaction), nameof(Interaction.ApplyEffects))]
internal static class ContentsCompletionPatch
{
    private static bool Prefix(Interaction __instance)
    {
        var reason = ContentsEligibilityPatch.Reason(__instance, __instance.objUs, __instance.objThem);
        return reason == null || Phobos.Ostranauts.Framework.Registration.NativeEffects.Refuse(__instance, reason);
    }
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
