using System;
using System.Linq;
using System.Reflection;
using Phobos.Ostranauts.Framework.Crew;

internal static class CrewNativeChecks
{
    internal static void Run(Action<bool,string> check)
    {
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
        foreach(var trigger in new[]{"TIsHumanAwake","TIsSleepingAny","TIsSleepy","TIsHungry","TCanDrinkThirsty","TIsSuffocatingManWalkEmerg"})
            check(DataHandler.dictCTs.ContainsKey(trigger),"Native crew eligibility trigger: "+trigger);
        foreach(var id in new[]{"Agriculture","Cooking","IndustrialProcessing"})
            CrewSpecialities.Register(new CrewSpeciality(id,id,"test",CrewRole.Industry));
        var terminal=DataHandler.dictCOs["ItmTerminal01"];
        var original=terminal.aInteractions;
        terminal.aInteractions=original.Concat(new[]{"StudyAtTerminals_ExistingAction"}).ToArray();
        var prepared=CrewSpecialities.PrepareDefinitions();
        check(prepared.Objects["ItmTerminal01"].aInteractions.Contains("StudyAtTerminals_ExistingAction")&&
            original.All(i=>prepared.Objects["ItmTerminal01"].aInteractions.Contains(i)),"Terminal study preserves every native and optional mod action");
        check(prepared.Objects["ItmTerminal01"].aInteractions.Count(i=>i.StartsWith("PhobosCrewStudy_",StringComparison.Ordinal))==3,"Exactly three additive study actions");
        check(prepared.Conditions.Count==3&&prepared.Conditions.Values.All(c=>c.aPer.Length==0),"Specialities do not inherit unrelated engineering/yield bonuses");
        check(prepared.Interactions["PhobosCrewWork"].strDuty=="Operate"&&prepared.Interactions["PhobosCrewWork"].aInverse.Length==0,
            "Native job interaction has no inherited inventory panel/reply effects");
        check(prepared.Interactions["PhobosCrewStudy_Agriculture"].fDuration==.25&&prepared.Interactions["PhobosCrewStudy_Agriculture"].CTTestThem=="PhobosCrewStudyTerminal",
            "Study time and powered-terminal gate are explicit");
        terminal.aInteractions=original;
    }
}
