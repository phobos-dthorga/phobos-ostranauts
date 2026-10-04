using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Controls;
using PhobosMedical.Core;

namespace PhobosMedical;

[BepInPlugin(Id, "Phobos Medical", Version)]
[BepInDependency(FrameworkInfo.PluginId, MinimumFrameworkVersion)]
[BepInProcess("Ostranauts.exe")]
public sealed class Plugin : BaseUnityPlugin
{
    public const string Id = MedicalRules.Owner;
    public const string Version = "0.2.0";
    public const string MinimumFrameworkVersion = "0.84.0";
    internal static Action<string> Log = _ => { };
    private Harmony? harmony;
    private float nextScan;
    private readonly List<CondOwner> members = new();
    // Ward-3 beds come from Framework's shared world sweep, not a pass over every world object.
    private static readonly Phobos.Ostranauts.Framework.Discovery.WorldFamily beds =
        Phobos.Ostranauts.Framework.Discovery.WorldFamilies.Register(Id + ".beds", MedicalRules.IsBed);

    private void Awake()
    {
        Log = x => Logger.LogInfo(x); Text.EnsureLoaded();
        PerformanceMetrics.Initialize();
        harmony = new Harmony(Id); harmony.PatchAll(typeof(Plugin).Assembly);
        FrameworkLifecycle.ContentLoading += Load;
        EquipmentProviders.Register(new Provider());
        EquipmentProviders.RegisterGroup(Provider.Group, () => Text.Get("Group.bed"));
        Panel.Register();
        Log(Text.Get("Plugin.loaded", Version));
    }
    private static void Load() { BedService.Reset(); Content.Register(Log); }

    private void Update()
    {
        if (UnityEngine.Time.unscaledTime < nextScan) return;
        nextScan = UnityEngine.Time.unscaledTime + (float)MedicalRules.TickSeconds;
        if (!Content.Ready || CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading || DataHandler.mapCOs == null) return;
        beds.Members(members);
        foreach (var bed in members)
        {
            using var tick = Phobos.Ostranauts.Framework.Diagnostics.Performance.Measure(PerformanceMetrics.BedTick);
            try { BedService.Tick(bed); } catch (Exception ex) { Log(ex.ToString()); }
        }
        using var sweep = Phobos.Ostranauts.Framework.Diagnostics.Performance.Measure(PerformanceMetrics.CareSweep);
        try { BedService.AfterPass(); } catch (Exception ex) { Log(ex.ToString()); }
    }
    private void OnDestroy()
    {
        FrameworkLifecycle.ContentLoading -= Load;
        EquipmentProviders.Unregister(Id);
        BedService.Reset(); harmony?.UnpatchSelf();
    }
}

[HarmonyPatch]
internal static class ReloadPatch
{
    private static IEnumerable<MethodBase> TargetMethods() => typeof(CrewSim).GetMethods().Where(m => m.Name == nameof(CrewSim.LoadGame) || m.Name == nameof(CrewSim.NewGame));
    private static void Prefix() => BedService.Reset();
}

// Sleep, Rest and Lay are offered on a Ward-3 only when they make sense: never into a bed someone else lies in, Rest
// only for the injured, and Sleep only for the injured while the bed is reserved for them. This runs for every offer
// the game evaluates, so the name is checked before anything else.
[HarmonyPatch(typeof(Interaction), "TriggeredInternal")]
internal static class OfferGate
{
    private static void Postfix(Interaction __instance, CondOwner objUs, CondOwner objThem, ref bool __result)
    {
        if (!__result || objThem == null || objThem.strCODef != MedicalRules.BedInstalled) return;
        string name = __instance.strName;
        if (name != MedicalRules.Sleep && name != MedicalRules.Rest && name != MedicalRules.Lay) return;
        using var measurement = Phobos.Ostranauts.Framework.Diagnostics.Performance.Measure(PerformanceMetrics.OfferGate);
        string? reason;
        try { reason = BedService.Refusal(name, objUs, objThem); }
        catch (Exception ex) { Plugin.Log(ex.Message); reason = null; }
        if (reason != null) { __instance.AddFailReason("main", reason); __result = false; }
    }
}

[HarmonyPatch(typeof(Interaction), nameof(Interaction.ApplyEffects))]
internal static class ActionPatch
{
    private static void Postfix(Interaction __instance, bool isCancelIa)
    {
        if (isCancelIa || __instance.objThem == null || __instance.objUs != CrewSim.GetSelectedCrew() || !Content.Machine(__instance.objThem)) return;
        var actor = __instance.objUs;
        if (__instance.strName == MedicalRules.Controls) { Panel.Show(__instance.objThem); return; }
        if (__instance.strName == MedicalRules.Send)
        {
            bool on = !BedService.StateOf(__instance.objThem).SendInjured;
            bool ok = BedService.Command(__instance.objThem, null, on ? "send:on" : "send:off", out string reply);
            if (actor != null && !actor.bDestroyed && actor.HasCond("IsHuman")) actor.LogMessage(reply, ok ? "Neutral" : "Bad", "Game");
            return;
        }
        if (__instance.strName != MedicalRules.Lay) return;
        bool done;
        string message;
        try { done = BedService.Lay(__instance.objThem, actor, out message); }
        catch (Exception ex) { Plugin.Log(ex.ToString()); done = false; message = Text.Get("Bed.lay_failed", ex.GetType().Name); }
        if (actor != null && !actor.bDestroyed && actor.HasCond("IsHuman")) actor.LogMessage(message, done ? "Neutral" : "Bad", "Game");
    }
}

// Removal work is refused when it is offered, never by blocking native destruction; a refused finish still closes the
// game's task (the owner's vanilla-precedence rule).
[HarmonyPatch(typeof(Interaction), "TriggeredInternal")]
internal static class MaintenanceOffer
{
    private static void Postfix(Interaction __instance, CondOwner objUs, CondOwner objThem, ref bool __result)
    {
        if (!__result || __instance.strName == null || !Content.Machine(objUs) && !Content.Machine(objThem)) return;
        var reason = MaintenanceFinish.Reason(__instance.strName, objUs, objThem);
        if (reason != null) { __instance.AddFailReason("main", reason); __result = false; }
    }
}
[HarmonyPatch(typeof(Interaction), nameof(Interaction.ApplyEffects))]
internal static class MaintenanceFinish
{
    internal static string? Reason(string action, CondOwner? us, CondOwner? them)
    {
        var bed = Content.Machine(us) ? us : Content.Machine(them) ? them : null;
        if (bed == null || action == null) return null;
        bool removal = action.IndexOf("Dismantle", StringComparison.OrdinalIgnoreCase) >= 0 || action.IndexOf("Uninstall", StringComparison.OrdinalIgnoreCase) >= 0;
        return removal ? BedService.MaintenanceReason(bed) : null;
    }
    private static bool Prefix(Interaction __instance)
    {
        var reason = Reason(__instance.strName, __instance.objUs, __instance.objThem);
        return reason == null || Phobos.Ostranauts.Framework.Registration.NativeEffects.Refuse(__instance, reason);
    }
}

[HarmonyPatch(typeof(ConsoleResolver), nameof(ConsoleResolver.ResolveString))]
internal static class ConsolePatch
{
    private static bool Prefix(ref string strInput, ref bool __result)
    {
        var parts = strInput.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0 || !parts[0].Equals("phobosmedical", StringComparison.OrdinalIgnoreCase)) return true;
        if (parts.Length == 1 || parts[1] == "list")
        {
            strInput += "\n" + string.Join("\n", CrewSim.GetSelectedCrew()?.ship?.GetCOs(null, false, false, true).Where(Content.Machine).Select(c => c.strNameFriendly + " " + c.strID) ?? Array.Empty<string>());
            __result = true; return false;
        }
        var bed = parts.Length >= 3 ? Content.Resolve(parts[2]) : null;
        string message = Text.Get("Console.help");
        string action = parts.Length == 4 && (parts[1] == "use" || parts[1] == "send") ? parts[1] + ":" + parts[3] : parts[1];
        __result = Content.Machine(bed) && BedService.Command(bed!, null, action, out message);
        strInput += "\n" + message; return false;
    }
}
