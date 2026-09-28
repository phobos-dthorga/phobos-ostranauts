using System;
using HarmonyLib;
using Phobos.Ostranauts.Framework.Liquids;

namespace PhobosAgriculture;
[HarmonyPatch(typeof(Interaction),nameof(Interaction.ApplyEffects))]
internal static class BulkEffects
{
    private static void Postfix(Interaction __instance,bool isCancelIa)
    {
        if(isCancelIa||__instance.bCancel||!BulkDefinitions.IsTank(__instance.objThem))return;
        if(__instance.strName==BulkDefinitions.Controls){if(__instance.objUs==CrewSim.GetSelectedCrew())Panel.Show(__instance.objThem);return;}
        foreach(var action in BulkDefinitions.Work)if(__instance.strName==BulkDefinitions.WorkId(action)&&BulkService.Work(__instance.objThem,__instance.objUs,action))Phobos.Ostranauts.Framework.Crew.CrewSpecialities.CreditPractical(__instance);
    }
}
// The game creates the replacement under the old ID before it calls ModeSwitch, so blocking here would
// orphan that replacement. Contents follow into a tank successor; a damaged successor isolates them.
// Owner decision (28 September 2026): vanilla destructibility, with refusals only when work is offered.
[HarmonyPatch(typeof(CondOwner),nameof(CondOwner.ModeSwitch))]
internal static class BulkMode
{
    private static void Prefix(CondOwner __instance,CondOwner coNew,out StoredCommodity? __state)
    {
        __state=null;if(!BulkDefinitions.IsTank(__instance)||!BulkDefinitions.IsTank(coNew))return;
        try{__state=BulkService.Read(__instance);}catch(Exception e){Plugin.Log(e.ToString());}
    }
    private static void Postfix(CondOwner coNew,StoredCommodity? __state)
    {
        if(__state==null)return;
        try{if(coNew.HasCond("IsDamaged")){__state.Isolate();BulkService.PauseSupply(coNew);}BulkService.Save(coNew,__state);}
        catch(Exception e){Plugin.Log(e.ToString());}
    }
}
// Native destruction proceeds. Water that vanishes with a destroyed tank is logged, never blocked; the old
// half of a mode switch and load-time cleanup are lifecycle disposal, not lost material.
[HarmonyPatch(typeof(CondOwner),nameof(CondOwner.Destroy))]
internal static class BulkDestroy
{
    private static void Prefix(CondOwner __instance)
    {
        if(CrewSim.objInstance==null||!CrewSim.objInstance.FinishedLoading||!BulkDefinitions.IsTank(__instance)||__instance.HasCond("IsModeSwitching",false))return;
        try{var s=BulkService.Read(__instance);if(s.TotalKg>1e-8)Plugin.Log(Text.Get("bulk_lost",__instance.strID,s.TotalKg));}catch{}
    }
}
