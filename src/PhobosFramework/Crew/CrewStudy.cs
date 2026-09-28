using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Condowner;
using Phobos.Ostranauts.Framework.Registration;

namespace Phobos.Ostranauts.Framework.Crew;

/// <summary>Speciality study through the game's own study chain, so the player menu, the idle AI,
/// work-shift interruption, time-skip and cancelling behave exactly as for vanilla skills.
/// Framework owns the pattern; content mods only register specialities.</summary>
public static class CrewStudy
{
    // Vanilla 1.0.1.5 sources the chain is cloned from, and the vanilla entry the AI history is seeded from.
    public const string VanillaOpener = "ACTStudySkillEngSoftware", VanillaAllow = "ACTStudySkillEngSoftwareAllow",
        VanillaDenyAlready = "ACTStudySkillDenyEngSoftwareAlready", VanillaTick = "Tick1HourStudyEngSoftware",
        VanillaStudying = "IsSkillStudyingEngSoftware", TemplateOpener = "ACTStudySkillEngConstruction";
    public const string ContinueChooser = "ACTStudySkillAllowCont", StopLoot = "CONDACTStudySkillStop", SkipPayloads = "ACTFFWDContextPayloads",
        MaterialUnused = "TIsStudyMaterialUnused", CanStudy = "TCanStudy", MoodTick = "CTTick1HourStudyMoodsUs", Personality = "Abner";
    public static readonly string[] Continuations = { "ACTStudySkillCont", "ACTStudySkillContSide", "ACTStudySkillContChallenge" };
    private const double SkipHourSeconds = 3600;

    public static string Opener(string id) => "PhobosStudySkill_" + id;
    public static string Allow(string id) => Opener(id) + "Allow";
    public static string Continue(string id) => Opener(id) + "Continue";
    public static string DenyAlready(string id) => "PhobosStudySkillDeny_" + id + "Already";
    public static string Tick(string id) => "PhobosTick1HourStudy_" + id;
    public static string Studying(string id) => "PhobosStudying_" + id;
    public static string MaterialTrigger(string id) => "PhobosTIsStudyMaterial_" + id;
    public static string SkilledTrigger(string id) => "PhobosTIsSkilled_" + id;
    public static string StudyingTrigger(string id) => "PhobosTIsStudying_" + id;
    public static string TickTrigger(string id) => "PhobosTIsValidTick1HourStudy_" + id;
    public static string StartLoot(string id) => "PhobosCONDStudyStart_" + id;
    private static string Reply(string name) => name + ",[us],[them]";

    /// <summary>True when the installed game supplies the vanilla study chain this pattern clones.</summary>
    public static bool Available =>
        new[] { VanillaOpener, VanillaAllow, VanillaDenyAlready, VanillaTick, ContinueChooser }.Concat(Continuations).All(DataHandler.dictInteractions.ContainsKey) &&
        DataHandler.dictConds.ContainsKey(VanillaStudying) && DataHandler.dictCTs.ContainsKey(MaterialUnused) && DataHandler.dictCTs.ContainsKey(CanStudy) &&
        DataHandler.dictLoot.ContainsKey(StopLoot) && DataHandler.dictLoot.ContainsKey(SkipPayloads);

    /// <summary>Cloned per speciality from the vanilla software-engineering study family: an opener the idle AI
    /// can pick, a chooser that marks the session, a continuation chooser gated on that mark, a refusal once
    /// skilled, and the time-skip hourly tick. Names carry the Phobos prefix; texts come from the catalog.</summary>
    public static void Prepare(NativeDefinitions d, IEnumerable<CrewSpeciality> skills)
    {
        if (!Available) return;
        foreach (var skill in skills)
        {
            string id = skill.Id, label = skill.Label;
            var studying = NativeDefinitions.Clone(DataHandler.dictConds[VanillaStudying]);
            studying.strName = Studying(id); studying.strNameFriendly = CrewWork.Message("studying", label); studying.strDesc = CrewWork.Message("studying_description", label);
            d.Conditions[studying.strName] = studying;
            d.Triggers[MaterialTrigger(id)] = Trigger(MaterialTrigger(id), new[] { "IsTerminal" }, Array.Empty<string>(), new[] { MaterialUnused });
            d.Triggers[SkilledTrigger(id)] = Trigger(SkilledTrigger(id), new[] { CrewSpecialities.Condition(id) }, Array.Empty<string>(), Array.Empty<string>());
            d.Triggers[StudyingTrigger(id)] = Trigger(StudyingTrigger(id), new[] { Studying(id) }, Array.Empty<string>(), new[] { CanStudy });
            d.Triggers[TickTrigger(id)] = Trigger(TickTrigger(id), new[] { Studying(id) }, new[] { "IsRobot" }, new[] { "TIsHumanAwake" });
            d.Loot[StartLoot(id)] = new Loot { strName = StartLoot(id), strType = "condition", aCOs = new[] { Studying(id) + "=1.0x1" }, aLoots = Array.Empty<string>() };

            var deny = Clone(VanillaDenyAlready); deny.strName = DenyAlready(id);
            deny.strTitle = CrewWork.Message("study_already_title"); deny.strDesc = CrewWork.Message("study_already", label);
            deny.CTTestUs = SkilledTrigger(id); deny.strDuty = null;
            d.Interactions[deny.strName] = deny;

            var opener = Clone(VanillaOpener); opener.strName = Opener(id);
            opener.strTitle = CrewWork.Message("study_title", label); opener.strDesc = CrewWork.Message("study_description", label); opener.strTooltip = CrewWork.Message("study_tooltip", label);
            opener.CTTestUs = CanStudy; opener.CTTestThem = MaterialTrigger(id); opener.strDuty = null; opener.bOpener = true;
            opener.aInverse = new[] { DenyAlready(id), "ACTStudySkillDenyMeaning", "ACTStudySkillDenyContact", "ACTStudySkillDenyInUse", Allow(id), "ACTStudySkillDeny" }.Select(Reply).ToArray();
            d.Interactions[opener.strName] = opener;

            // Progress is credited by Framework on each completed study step; native training stats stay untouched.
            var allow = Clone(VanillaAllow); allow.strName = Allow(id);
            allow.strTitle = CrewWork.Message("study_stage_title"); allow.strDesc = CrewWork.Message("study_stage", label);
            allow.CTTestUs = CanStudy; allow.CTTestThem = MaterialTrigger(id); allow.strDuty = null;
            allow.LootCondsUs = StartLoot(id); allow.LootCTsUs = null;
            allow.aInverse = new[] { DenyAlready(id), "ACTStudySkillDenyMeaning", "ACTStudySkillDenyContact" }.Concat(Continuations).Select(Reply).ToArray();
            d.Interactions[allow.strName] = allow;

            // A session continues with this speciality only while its mark is present.
            var resume = NativeDefinitions.Clone(allow); resume.strName = Continue(id); resume.CTTestUs = StudyingTrigger(id);
            d.Interactions[resume.strName] = resume;

            var tick = Clone(VanillaTick); tick.strName = Tick(id);
            tick.strTitle = CrewWork.Message("study_skip_title", label); tick.strDesc = CrewWork.Message("study_skip", label);
            tick.CTTestUs = TickTrigger(id); tick.LootCTsUs = DataHandler.dictLoot.ContainsKey(MoodTick) ? MoodTick : null;
            d.Interactions[tick.strName] = tick;
        }
    }

    /// <summary>Joins the vanilla chain in place, after every mod's data has loaded: continuation choices,
    /// the shared stop list, the time-skip payload list and every study terminal's action list.</summary>
    public static void Amend(IEnumerable<CrewSpeciality> skills)
    {
        var ids = skills.Select(s => s.Id).ToArray();
        if (!Available || ids.Length == 0) return;
        var chooser = DataHandler.dictInteractions[ContinueChooser];
        foreach (string id in ids)
            DefinitionAmendments.InsertInverse(chooser, Reply(Continue(id)),
                name => name.StartsWith("ACTStudySkill", StringComparison.Ordinal) && name.EndsWith("Allow", StringComparison.Ordinal));
        DefinitionAmendments.AppendLoot(DataHandler.dictLoot[StopLoot], ids.Select(id => "-" + Studying(id) + "=1.0x1").ToArray());
        DefinitionAmendments.AppendLoot(DataHandler.dictLoot[SkipPayloads], ids.Select(id => Tick(id) + "=1.0x1").ToArray());
        var openers = ids.Select(Opener).ToArray();
        foreach (var terminal in DataHandler.dictCOs.Values.Where(IsStudyTerminal).ToArray())
            DefinitionAmendments.AppendInteractions(terminal, openers);
    }

    internal static bool IsStudyTerminal(JsonCondOwner co) => co.aStartingConds != null &&
        co.aStartingConds.Any(c => c.StartsWith("IsTerminal=", StringComparison.Ordinal)) &&
        co.aStartingConds.Any(c => c.StartsWith("IsStudyMaterial=", StringComparison.Ordinal));

    /// <summary>After a world has loaded: live study terminals get the openers in their own action list
    /// (the game copies the list at creation), and the player's crew get the AI-history entries.</summary>
    internal static void WorldReady(IEnumerable<CrewSpeciality> skills, Action<string> log)
    {
        var openers = skills.Select(s => Opener(s.Id)).ToArray();
        if (!Available || openers.Length == 0 || DataHandler.mapCOs == null) return;
        try
        {
            foreach (var co in DataHandler.mapCOs.Values.Where(c => c != null && !c.bDestroyed && c.HasCond("IsTerminal") && c.HasCond("IsStudyMaterial")).ToArray())
                DefinitionAmendments.AppendInteractions(co, openers);
            int added = 0;
            foreach (var member in CrewRoster.Members()) if (member.mapIAHist != null) added += Seed(member.mapIAHist, openers);
            if (added > 0) log(CrewWork.Message("study_seeded", added));
        }
        catch (Exception ex) { log(CrewWork.Message("study_seed_failed", ex.Message)); }
    }

    /// <summary>New crew copy the game's personality template, so it receives the same entries.</summary>
    public static int SeedTemplate(IEnumerable<CrewSpeciality> skills)
    {
        var openers = skills.Select(s => Opener(s.Id)).ToArray();
        if (openers.Length == 0 || DataHandler.dictAIPersonalities == null ||
            !DataHandler.dictAIPersonalities.TryGetValue(Personality, out var personality) || personality?.mapIAHist2 == null) return 0;
        return Seed(personality.mapIAHist2, openers);
    }

    // Copies the vanilla per-skill study entry for each need where it exists and ours is absent; nothing else changes.
    private static int Seed(Dictionary<string, CondHistory> histories, string[] openers)
    {
        var plan = StudyHistorySeed.Plan(histories.Select(p => (p.Key, (IReadOnlyCollection<string>)(p.Value?.mapInteractions?.Keys.ToArray() ?? Array.Empty<string>()))),
            TemplateOpener, openers);
        foreach (var (need, opener) in plan)
        {
            var entry = histories[need].mapInteractions[TemplateOpener].Clone();
            entry.strName = opener;
            histories[need].mapInteractions[opener] = entry;
        }
        return plan.Count;
    }

    /// <summary>Completed study steps credit the speciality being studied; each skipped hour credits an hour.</summary>
    internal static void Credit(Interaction ia, bool cancelled)
    {
        if (cancelled || ia.bCancel || ia.objUs == null || string.IsNullOrEmpty(ia.strName)) return;
        var actor = ia.objUs;
        foreach (var skill in CrewSpecialities.All)
        {
            if (ia.strName == Tick(skill.Id)) { CrewSpecialities.Credit(actor, skill.Id, SkipHourSeconds, true); return; }
            if (Array.IndexOf(Continuations, ia.strName) >= 0 && actor.HasCond(Studying(skill.Id)))
            {
                if (CrewSpecialities.ClaimCredit(ia)) CrewSpecialities.Credit(actor, skill.Id, ia.fDurationOrig * 3600, true);
                return;
            }
        }
    }

    private static JsonInteraction Clone(string name) => NativeDefinitions.Clone(DataHandler.dictInteractions[name]);
    private static CondTrigger Trigger(string id, string[] require, string[] forbid, string[] triggers) => new CondTrigger {
        strName = id, fChance = 1, fCount = 1, bAND = true, aReqs = require, aForbids = forbid, aTriggers = triggers,
        aTriggersForbid = Array.Empty<string>(), aLowerConds = Array.Empty<string>()
    };
}
