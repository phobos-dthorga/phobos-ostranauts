using System;
using System.Linq;
using System.Reflection;
using Phobos.Ostranauts.Framework.Crew;

internal static class CrewNativeChecks
{
    internal static void Run(Action<bool,string> check)
    {
        var nativeRoster=typeof(JsonCompany).GetMethod(nameof(JsonCompany.GetCrewMembers))!;
        check(PlaceholderLoadChecks.Calls(typeof(CrewRoster).GetMethod("Members",BindingFlags.Static|BindingFlags.NonPublic,null,new[]{typeof(JsonCompany)},null)!)
            .Any(m=>m==nativeRoster),"Shared resolver calls the native company crew API");
        var flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
        var clock=typeof(GUIFFWD).GetMethod("FFWD",flags)!;
        check(PlaceholderLoadChecks.Calls(clock).Count(m=>m.DeclaringType==typeof(StarSystem)&&m.Name=="Update")==1,
            "Native time-skip has exactly one replaceable world-clock call");
        check(typeof(Powered).GetField("fUpdateLast",flags)?.FieldType==typeof(double)&&typeof(Powered).GetMethod("Run",flags)!=null,
            "Measured power adapter can advance and settle the native epoch");
        check(typeof(GUIFFWD).GetField("tfCrew",flags)!=null&&typeof(GUIFFWD).GetMethod("UndamageParts",flags)?.GetParameters().Single().ParameterType==typeof(double),
            "Native crew scope and repair allowance resolve");
        check(typeof(GUIFFWDRow).GetField("dictPayloads",flags)?.FieldType==typeof(System.Collections.Generic.Dictionary<string,int>),
            "Time budgets reserve the exact native preview's frozen care payloads");
        check(typeof(WorkManager).GetMethod("FinalizeTask",flags)?.GetParameters().Select(p=>p.Name).SequenceEqual(new[]{"co","task","iact","coThem"})==true,
            "Claim adapter uses actual native worker and interaction");
        check(typeof(CondOwner).GetMethod("QueueInteraction",flags)?.GetParameters()[1].ParameterType==typeof(Interaction),
            "Practical duration patch resolves by parameter index");
        check(DataHandler.dictInteractions.ContainsKey("PickupItem")&&DataHandler.dictInteractions["PickupItem"].aInverse.Contains("PickupItemAllow"),
            "Hauling retains native single-item pickup chain");
        check(DataHandler.dictCTs.ContainsKey("TIsHumanAwake"),"Native awake trigger for the retired study action");
        // Vanilla precedence (0.36.0): a task without an owner list is open to every crew member the game
        // admits; an owner list forbids everyone else. Standing orders therefore name no owner.
        var open=new Task2{strName="t",strInteraction="PhobosCrewWork",strTargetCOID="x",strDuty="Operate"};
        var owned=new Task2{strName="t",strInteraction="PhobosCrewWork",strTargetCOID="x",strDuty="Operate",aOwnerIDs=new[]{"player"}};
        check(open.GetOwnership("anyone")==Task2.Allowed.Allowed&&owned.GetOwnership("anyone")==Task2.Allowed.Forbidden&&owned.GetOwnership("player")==Task2.Allowed.Owned,
            "Native ownership: no owner list admits any crew member; an owner list forbids everyone else");
        var addTask=typeof(WorkManager).GetMethod("AddTask");
        check(addTask?.GetParameters().Length==2&&addTask.GetParameters()[1].Name=="nMax"&&((int)addTask.GetParameters()[1].DefaultValue!)==1,
            "Native AddTask keeps one task per target and action unless told otherwise");
        check(typeof(Interaction).GetMethod("ResetObject")!=null,"Pooled interactions are reset for reuse; per-instance bookkeeping ends there");
        check(typeof(WorkManager).GetMethod("CompleteTask",new[]{typeof(string),typeof(string),typeof(string)})!=null&&
            PlaceholderLoadChecks.Calls(typeof(Interaction).GetMethod("ApplyEffects")!).Any(m=>m.DeclaringType==typeof(WorkManager)&&m.Name=="CompleteTask"),
            "Native effects close the queued task; a refusing prefix must do the same");
        check(typeof(GUIData).GetMethod("RegisterOpenWindow")?.GetParameters().Single().ParameterType==typeof(Ostranauts.ShipGUIs.Interfaces.IDataWindow)&&
            PlaceholderLoadChecks.Calls(typeof(CrewSim).GetMethod("CloseGUIData",flags)!).Any(m=>m.DeclaringType==typeof(GUIData)&&m.Name=="CloseOutermostWindow"),
            "Escape closes the outermost registered window before the panel is lowered");
        check(typeof(Loot).GetProperty("aLoots")?.SetMethod!=null,"Assigning a loot table's list re-parses it in place");
        check(DataHandler.dictInteractions.TryGetValue(CrewSpecialities.WorkTemplate,out var workTemplate)&&workTemplate.strDuty=="Operate",
            "The task action is cloned from a vanilla Operate-duty job");
        // The game interrupts every on-shift crew member's study whenever its task total rises; quiet
        // re-adds keep that total in step through the public counter.
        check(typeof(WorkManager).GetField("nTotalTasks")?.FieldType==typeof(int),"Native task counter is public");
        check(typeof(WorkManager).GetField("dictTasks2ByCOID",flags)?.FieldType==typeof(System.Collections.Generic.Dictionary<string,System.Collections.Generic.List<Task2>>),
            "Task total can be recounted from the native task lists");
        check(PlaceholderLoadChecks.Calls(typeof(WorkManager).GetMethod("Update",flags)!).Any(m=>m.DeclaringType==typeof(CondOwner)&&m.Name=="InterruptForWork"),
            "Native task-count rise still interrupts crew work");
        check(PlaceholderLoadChecks.Calls(typeof(DataHandler).GetMethod("PostModLoadMainThread")!).Any(m=>m.DeclaringType==typeof(Installables)&&m.Name=="Create"),
            "Native installable generation runs inside the load step our ContentLoaded follows");
        check(typeof(JsonCondOwner).GetField("mapJobActions",flags)!=null&&typeof(JsonCondOwner).GetMethod("GetJobActions")!=null,
            "Object definitions carry private job actions a clone cannot copy");
        check(typeof(CondOwner).GetField("mapIAHist")?.FieldType==typeof(System.Collections.Generic.Dictionary<string,Ostranauts.Condowner.CondHistory>)&&
            typeof(Ostranauts.Condowner.CondHistory).GetMethod("Clone")!=null&&typeof(Ostranauts.Condowner.InteractionHistory).GetProperty("strName")?.CanWrite==true,
            "Crew AI history can receive copied study entries");
        check(typeof(CondOwner).GetMethod("CheckInteractionFlag")!=null&&typeof(CondOwner).GetField("aInteractions")?.FieldType==typeof(System.Collections.Generic.List<string>),
            "Live objects can gain actions in place");
        check(typeof(JsonAIPersonality).GetProperty("mapIAHist2")!=null,"New-crew AI template is reachable");

        foreach(var id in new[]{"Agriculture","Cooking","IndustrialProcessing"})
            CrewSpecialities.Register(new CrewSpeciality(id,id,"test",CrewRole.Industry));
        check(CrewStudy.Available,"Installed game supplies the vanilla study chain: "+string.Join(", ",new[]{CrewStudy.VanillaOpener,CrewStudy.VanillaAllow,CrewStudy.VanillaDenyAlready,CrewStudy.VanillaTick,CrewStudy.ContinueChooser}.Where(n=>!DataHandler.dictInteractions.ContainsKey(n))));
        foreach(var name in new[]{"ACTStudySkillDenyMeaning","ACTStudySkillDenyContact","ACTStudySkillDenyInUse","ACTStudySkillDeny","ACTStudySkillStop"}.Concat(CrewStudy.Continuations))
            check(DataHandler.dictInteractions.ContainsKey(name),"Vanilla study reply exists: "+name);
        check(DataHandler.dictLoot.ContainsKey(CrewStudy.MoodTick)&&DataHandler.dictLoot.ContainsKey("CONDACTStudySkillStartThem"),"Vanilla study mood and material-use loot exist");
        check(DataHandler.dictInteractions[CrewStudy.VanillaOpener].bOpener&&DataHandler.dictInteractions[CrewStudy.VanillaOpener].strDuty==null,
            "Vanilla per-skill study opener is an AI opener without a duty");
        var terminal=DataHandler.dictCOs["ItmTerminal01"];
        var original=terminal.aInteractions??Array.Empty<string>();
        terminal.aInteractions=original.Concat(new[]{"StudyAtTerminals_ExistingAction"}).ToArray();
        terminal.AddJobAction("repair","RepairTerminalForTest");
        var prepared=CrewSpecialities.PrepareDefinitions();
        check(prepared.Objects.Count==0,"No native object definition is republished");
        check(prepared.Conditions.Count==6&&new[]{"Agriculture","Cooking","IndustrialProcessing"}.All(id=>prepared.Conditions[CrewSpecialities.Condition(id)].aPer.Length==0),
            "Specialities do not inherit unrelated engineering/yield bonuses and gain a studying mark");
        var work=prepared.Interactions["PhobosCrewWork"];
        check(work.strDuty=="Operate"&&work.aInverse.Length==0&&work.strRaiseUI==null&&!work.bOpener&&work.LootCTsUs==null&&work.aLootItms.Length==0&&
            work.strAnim==DataHandler.dictInteractions[CrewSpecialities.WorkTemplate].strAnim,
            "Native job interaction keeps the vanilla job's duty and animation with no inherited panel, replies, switch effects or AI opener flag");
        check(prepared.Interactions["PhobosCrewStudy_Agriculture"].strDuty==null&&prepared.Triggers[CrewSpecialities.LegacyTerminalTrigger].fChance==1,
            "Retired study action keeps loading without a duty and with a passable trigger");
        foreach(var id in new[]{"Agriculture","Cooking","IndustrialProcessing"})
        {
            var opener=prepared.Interactions[CrewStudy.Opener(id)]; var allow=prepared.Interactions[CrewStudy.Allow(id)]; var resume=prepared.Interactions[CrewStudy.Continue(id)];
            check(opener.bOpener&&opener.strDuty==null&&opener.CTTestUs==CrewStudy.CanStudy&&opener.CTTestThem==CrewStudy.MaterialTrigger(id)&&
                opener.aInverse.Select(Phobos.Ostranauts.Framework.Registration.DefinitionAmendments.ReplyName).SequenceEqual(new[]{CrewStudy.DenyAlready(id),"ACTStudySkillDenyMeaning","ACTStudySkillDenyContact","ACTStudySkillDenyInUse",CrewStudy.Allow(id),"ACTStudySkillDeny"}),
                "Study opener mirrors the vanilla per-skill opener: "+id);
            check(allow.LootCondsUs==CrewStudy.StartLoot(id)&&allow.LootCTsUs==null&&allow.strCancelInteraction=="ACTStudySkillStop"&&allow.LootCondsThem=="CONDACTStudySkillStartThem"&&
                allow.aInverse.Select(Phobos.Ostranauts.Framework.Registration.DefinitionAmendments.ReplyName).Skip(3).SequenceEqual(CrewStudy.Continuations),
                "Study chooser marks the session and continues through the vanilla steps: "+id);
            check(resume.CTTestUs==CrewStudy.StudyingTrigger(id)&&prepared.Triggers[CrewStudy.StudyingTrigger(id)].aReqs.Contains(CrewStudy.Studying(id)),
                "Continuing a session stays with the speciality being studied: "+id);
            check(prepared.Interactions[CrewStudy.DenyAlready(id)].CTTestUs==CrewStudy.SkilledTrigger(id)&&prepared.Interactions[CrewStudy.Tick(id)].CTTestUs==CrewStudy.TickTrigger(id),
                "Refusal once skilled and time-skip tick are gated on our conditions: "+id);
            foreach(var trigger in new[]{CrewStudy.MaterialTrigger(id),CrewStudy.SkilledTrigger(id),CrewStudy.StudyingTrigger(id),CrewStudy.TickTrigger(id)})
                check(prepared.Triggers[trigger].fChance==1&&prepared.Triggers[trigger].aTriggers!=null,"Runtime trigger can pass: "+trigger);
            check(prepared.Triggers[CrewStudy.MaterialTrigger(id)].aReqs.SequenceEqual(new[]{"IsTerminal"})&&prepared.Triggers[CrewStudy.MaterialTrigger(id)].aTriggers.SequenceEqual(new[]{CrewStudy.MaterialUnused}),
                "Study material is any terminal the vanilla rule admits, so existing saves qualify: "+id);
        }
        var fresh=new CondTrigger{strName="Fresh"};
        check(fresh.fChance==1&&fresh.aReqs!=null&&fresh.aTriggers!=null&&fresh.aLowerConds!=null,"A code-built trigger starts passable with empty lists, like a JSON trigger");
        bool rejected=false;
        try { Phobos.Ostranauts.Framework.Registration.NativeDefinitions.Validate(new CondTrigger{strName="ZeroChance",fChance=0,aReqs=new[]{"IsTerminal"}}); } catch(ArgumentException) { rejected=true; }
        check(rejected,"Publishing an explicit zero-chance trigger is refused");
        var filled=new CondTrigger{strName="Filled",fChance=1,aTriggers=null,aLowerConds=null}; Phobos.Ostranauts.Framework.Registration.NativeDefinitions.Validate(filled);
        check(filled.aTriggers!=null&&filled.aLowerConds!=null,"Publishing restores lists a consumer nulled");

        var chooser=DataHandler.dictInteractions[CrewStudy.ContinueChooser]; var chooserBefore=chooser.aInverse.ToArray();
        var stop=DataHandler.dictLoot[CrewStudy.StopLoot]; var stopBefore=stop.aCOs.ToArray();
        var payloads=DataHandler.dictLoot[CrewStudy.SkipPayloads]; var payloadsBefore=payloads.aCOs.ToArray();
        CrewStudy.Amend(CrewSpecialities.All); CrewStudy.Amend(CrewSpecialities.All);
        check(ReferenceEquals(DataHandler.dictCOs["ItmTerminal01"],terminal)&&terminal.GetJobActions("repair").Contains("RepairTerminalForTest"),
            "Terminal keeps its identity and private job actions");
        check(terminal.aInteractions.Contains("StudyAtTerminals_ExistingAction")&&original.All(i=>terminal.aInteractions.Contains(i)),"Terminal keeps every native and optional mod action");
        check(terminal.aInteractions.Count(i=>i.StartsWith("PhobosStudySkill_",StringComparison.Ordinal))==3&&!terminal.aInteractions.Any(i=>i.StartsWith("PhobosCrewStudy_",StringComparison.Ordinal)),
            "Terminal offers exactly one vanilla-style opener per speciality and no retired action");
        check(!DataHandler.dictCOs["ItmTerminal01Off"].aInteractions.Any(i=>i.StartsWith("PhobosStudySkill_",StringComparison.Ordinal)),"Unpowered terminal is not study material");
        int inserted=chooser.aInverse.Length-chooserBefore.Length;
        check(inserted==3&&chooser.aInverse.Take(2).SequenceEqual(chooserBefore.Take(2))&&
            Array.IndexOf(chooser.aInverse,CrewStudy.Continue("Agriculture")+",[us],[them]")<Array.IndexOf(chooser.aInverse,"ACTStudySkillAdminAllow,[us],[them]")&&
            chooserBefore.All(e=>chooser.aInverse.Contains(e)),"Continuation choices are inserted once, before the vanilla skills, keeping every vanilla reply");
        check(stop.aCOs.Length==stopBefore.Length+3&&stop.aCOs.Contains("-"+CrewStudy.Studying("Cooking")+"=1.0x1")&&stopBefore.All(e=>stop.aCOs.Contains(e)),"Stopping study clears our marks too");
        check(payloads.aCOs.Length==payloadsBefore.Length+3&&payloads.aCOs.Contains(CrewStudy.Tick("Agriculture")+"=1.0x1"),"Time-skip continues Phobos study like vanilla study");
        var abner=new JsonAIPersonality{mapIAHist2=new System.Collections.Generic.Dictionary<string,Ostranauts.Condowner.CondHistory>()};
        var privacy=new Ostranauts.Condowner.CondHistory("StatPrivacy"); privacy.AddInteractionScore(CrewStudy.TemplateOpener,-3,bNew:true);
        var meaning=new Ostranauts.Condowner.CondHistory("StatMeaning"); meaning.AddInteractionScore("ACTStudySkill",2,bNew:true);
        abner.mapIAHist2["StatPrivacy"]=privacy; abner.mapIAHist2["StatMeaning"]=meaning;
        DataHandler.dictAIPersonalities=new System.Collections.Generic.Dictionary<string,JsonAIPersonality>{["Abner"]=abner};
        check(CrewStudy.SeedTemplate(CrewSpecialities.All)==3&&CrewStudy.SeedTemplate(CrewSpecialities.All)==0,"Template history gains one entry per speciality, once");
        check(privacy.mapInteractions[CrewStudy.Opener("Cooking")].fAverage==privacy.mapInteractions[CrewStudy.TemplateOpener].fAverage&&
            privacy.mapInteractions[CrewStudy.Opener("Cooking")].strName==CrewStudy.Opener("Cooking")&&!meaning.mapInteractions.ContainsKey(CrewStudy.Opener("Cooking")),
            "Seeded entries copy the vanilla per-skill values only where that template exists");
        terminal.aInteractions=original;
    }
}
