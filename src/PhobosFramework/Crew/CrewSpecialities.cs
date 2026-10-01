using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Phobos.Ostranauts.Framework.Persistence;
using Phobos.Ostranauts.Framework.Registration;

namespace Phobos.Ostranauts.Framework.Crew;

public static class CrewSpecialities
{
    private static readonly Dictionary<string,CrewSpeciality> skills = new(StringComparer.Ordinal);
    private static readonly Dictionary<string,string> practical = new(StringComparer.Ordinal);
    private static ConditionalWeakTable<Interaction,object> credited = new();
    public static void RegisterPractical(string interaction, string skill) => practical[interaction] = skill;
    public static void CreditPractical(Interaction interaction)
    {
        if(!interaction.bCancel && interaction.strName != null && practical.TryGetValue(interaction.strName,out var skill) && ClaimCredit(interaction))
            Credit(interaction.objUs,skill,interaction.fDurationOrig*3600,false);
    }
    internal static bool ClaimCredit(Interaction interaction)
    { if(credited.TryGetValue(interaction,out _))return false; credited.Add(interaction,new object()); return true; }
    internal static void Adjust(CondOwner actor,Interaction interaction)
    {
        if(interaction.strName == null || !practical.TryGetValue(interaction.strName,out var skill) || !Skilled(actor,skill) ||
            !DataHandler.dictInteractions.TryGetValue(interaction.strName,out var definition)) return;
        // A restored, already shortened interaction must not receive the bonus twice.
        if(Math.Abs(interaction.fDurationOrig-definition.fDuration)<1e-10)
        { interaction.fDuration*=CrewBalance.SkilledDurationFraction; interaction.fDurationOrig*=CrewBalance.SkilledDurationFraction; }
    }
    public static IEnumerable<CrewSpeciality> All => skills.Values;
    // Retired 15-minute terminal actions (0.25.0 to 0.34.0). Their definitions stay registered so a save
    // that queued one still loads; terminals no longer offer them. Study now follows the vanilla chain.
    internal const string StudyPrefix = "PhobosCrewStudy_";
    public const string LegacyTerminalTrigger = "PhobosCrewStudyTerminal";
    public static void Register(CrewSpeciality skill)
    { if (skills.ContainsKey(skill.Id)) throw new ArgumentException("Duplicate speciality."); skills.Add(skill.Id, skill); }
    public static string Condition(string id) => "PhobosSkill_" + id;
    public static bool Skilled(CondOwner actor, string id) => id.Length > 0 && (skills.ContainsKey(id) ? actor.HasCond(Condition(id)) || Progress(actor,id) >= 100 : actor.HasCond(id));
    public static double Progress(CondOwner actor, string id)
    {
        var status = CrewWork.Store(actor,"crew-skills").Read(out var fields);
        if (status == SavedStateStatus.Missing || status == SavedStateStatus.Ready && !fields.ContainsKey(id)) return 0;
        return status == SavedStateStatus.Ready && fields.TryGetValue(id,out var v) && double.TryParse(v,NumberStyles.Float,CultureInfo.InvariantCulture,out var n) && CrewBalance.Finite(n) && n >= 0 && n <= 100 ? n : double.NaN;
    }
    public static void Credit(CondOwner actor, string id, double seconds, bool study)
    {
        if (!skills.ContainsKey(id) || !CrewBalance.Finite(seconds) || seconds <= 0 || seconds > 21600) return;
        var store = CrewWork.Store(actor,"crew-skills"); var state = store.Read(out var fields);
        if (state != SavedStateStatus.Ready && state != SavedStateStatus.Missing) return;
        double previous = Progress(actor,id); if (!CrewBalance.Finite(previous)) return;
        double next = CrewBalance.Credit(previous,seconds,study);
        var saved = fields.ToDictionary(p=>p.Key,p=>p.Value,StringComparer.Ordinal); saved[id] = next.ToString("R",CultureInfo.InvariantCulture);
        if (store.TryWrite(saved) && next >= 100 && !actor.HasCond(Condition(id))) actor.AddCondAmount(Condition(id),1);
    }
    public static bool Allowed(CondOwner actor, CrewRole role)
    {
        var status = CrewWork.Store(actor,"crew-roles").Read(out var fields);
        if (status == SavedStateStatus.Missing) return role != CrewRole.Exterior;
        return status == SavedStateStatus.Ready && (fields.TryGetValue(role.ToString(),out var value) ? value == "1" : role != CrewRole.Exterior);
    }
    public static bool ToggleRole(CondOwner actor,CrewRole role)
    {
        if (actor.Company != CrewSim.coPlayer?.Company) return false;
        var store = CrewWork.Store(actor,"crew-roles"); var status=store.Read(out var fields);
        if(status!=SavedStateStatus.Ready && status!=SavedStateStatus.Missing) return false;
        var next=fields.ToDictionary(p=>p.Key,p=>p.Value,StringComparer.Ordinal); next[role.ToString()]=Allowed(actor,role)?"0":"1";
        return store.TryWrite(next);
    }
    public static string RoleFingerprint(CondOwner actor)
    {
        var status=CrewWork.Store(actor,"crew-roles").Read(out var fields);
        return status+":"+CrewBalance.Binding(fields.OrderBy(p=>p.Key,StringComparer.Ordinal).SelectMany(p=>new[]{p.Key,p.Value}));
    }
    public static bool ApplyRoles(CondOwner actor,string expected,IReadOnlyDictionary<CrewRole,bool> roles,out string reason)
    {
        reason=Controls.ConsoleText.Get("stale");
        if(actor.Company!=CrewSim.coPlayer?.Company||actor.bDestroyed||RoleFingerprint(actor)!=expected)return false;
        var store=CrewWork.Store(actor,"crew-roles");var status=store.Read(out var fields);
        if(status!=SavedStateStatus.Missing&&status!=SavedStateStatus.Ready)return false;
        var next=fields.ToDictionary(p=>p.Key,p=>p.Value,StringComparer.Ordinal);
        foreach(var role in roles){if(!Enum.IsDefined(typeof(CrewRole),role.Key))return false;next[role.Key.ToString()]=role.Value?"1":"0";}
        reason=Controls.ConsoleText.Get("protected");bool saved=store.TryWrite(next);if(saved)reason=Controls.ConsoleText.Get("applied");return saved;
    }
    // ContentLoaded: after every mod's data and the game's own installable generation. Nothing native is
    // republished by name; the vanilla study chain is joined in place (see CrewStudy).
    internal static void Definitions()
    {
        PrepareDefinitions().Publish();
        CrewStudy.Amend(skills.Values);
        CrewStudy.SeedTemplate(skills.Values);
    }
    /// <summary>The vanilla Operate-duty job our task action is cloned from: a plain painted job with no panel, replies or effects of its own.</summary>
    public const string WorkTemplate = "ACTTogglePower";
    public static NativeDefinitions PrepareDefinitions()
    {
        if (!DataHandler.dictInteractions.TryGetValue(WorkTemplate, out var template)) throw new InvalidOperationException("Native " + WorkTemplate + " interaction missing.");
        var d = new NativeDefinitions();
        d.Triggers[LegacyTerminalTrigger] = new CondTrigger { strName=LegacyTerminalTrigger, fChance=1, fCount=1, bAND=true,
            aReqs=new[]{"IsTerminal","IsInstalled","IsPowered"},aForbids=new[]{"IsDamaged","IsLocked","IsOverrideOff"},aTriggers=Array.Empty<string>() };
        var work = NativeDefinitions.Clone(template);
        work.strName = CrewWork.WorkId; work.strTitle = work.strDesc = work.strTooltip = CrewWork.Message("work");
        work.strRaiseUI = null; work.aInverse = Array.Empty<string>(); work.strActionGroup = "Work"; work.strDuty = "Operate";
        work.fDuration = CrewBalance.HandlingSeconds / 3600; work.fTargetPointRange = 2; work.strTargetPoint = "use";
        work.bIgnoreFeelings = false; work.bHumanOnly = false; work.bOpener = false;
        // The template's own switch effects and reply list do not apply; the provider does the work.
        work.LootCTsUs = null; work.LootCTsThem = null; work.aLootItms = Array.Empty<string>();
        work.CTTestUs = "Blank"; work.CTTestThem = "Blank";
        d.Interactions[work.strName] = work;
        foreach(var skill in skills.Values)
        {
            var c = NativeDefinitions.Clone(DataHandler.dictConds["SkillEngConstruction"]);
            c.strName = Condition(skill.Id); c.strNameFriendly = CrewWork.Message("skilled", skill.Label); c.strDesc = CrewWork.Message("skill_description",skill.Label,100*(1-CrewBalance.SkilledDurationFraction));
            c.aPer = Array.Empty<string>(); d.Conditions[c.strName] = c;
            var study = NativeDefinitions.Clone(work); study.strName = StudyPrefix + skill.Id;
            study.strTitle = study.strDesc = study.strTooltip = CrewWork.Message("study",skill.Label); study.fDuration = .25;
            // A duty would turn a direct order into a native task; study is a personal action like vanilla's.
            study.strActionGroup = "Use"; study.fWorkCancelChance = 1; study.strDuty = null; study.strAnim = "Tablet";
            study.CTTestUs="TIsHumanAwake"; study.CTTestThem=LegacyTerminalTrigger;
            d.Interactions[study.strName] = study;
        }
        CrewStudy.Prepare(d, skills.Values);
        return d;
    }
    /// <summary>The game pools and reuses Interaction objects; per-instance bookkeeping ends when one is reset.</summary>
    internal static void Forget(Interaction interaction) { credited.Remove(interaction); CrewWork.Active.Remove(interaction); }
    internal static void Reset() { credited=new(); }
}

[HarmonyPatch(typeof(Interaction),nameof(Interaction.ResetObject))]
internal static class CrewInteractionReset
{
    private static void Postfix(Interaction __instance) => CrewSpecialities.Forget(__instance);
}

[HarmonyPatch(typeof(CondOwner),nameof(CondOwner.QueueInteraction),new[]{typeof(CondOwner),typeof(Interaction),typeof(bool)})]
internal static class CrewPracticalDuration
{
    private static void Prefix(CondOwner __instance,Interaction __1)
    { if(__1!=null) { CrewWork.ManualTakeover(__instance,__1); CrewSpecialities.Adjust(__instance,__1); } }
}

[HarmonyPatch(typeof(Interaction),nameof(Interaction.ApplyEffects))]
internal static class CrewStudyFinish
{
    private static void Postfix(Interaction __instance, bool isCancelIa)
    {
        if (isCancelIa || __instance.bCancel || string.IsNullOrEmpty(__instance.strName)) return;
        if (!__instance.strName.StartsWith(CrewSpecialities.StudyPrefix,StringComparison.Ordinal)) { CrewStudy.Credit(__instance, isCancelIa); return; }
        var actor=__instance.objUs; var terminal=__instance.objThem;
        if(actor==null || !actor.bAlive || terminal==null || terminal.bDestroyed || !terminal.HasCond("IsTerminal") ||
            !CrewWork.LocalAccess(actor,terminal,2) || !CrewSpecialities.ClaimCredit(__instance)) return;
        CrewSpecialities.Credit(actor,__instance.strName.Substring(CrewSpecialities.StudyPrefix.Length),Math.Min(900,__instance.fDurationOrig*3600),true);
    }
}
