using System;
using HarmonyLib;

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
// Framework BulkVessel carries the water into a tank successor (isolating it when the successor is damaged)
// and logs water lost with a destroyed tank. Agriculture only pauses the paired W2 intake on damage.
[HarmonyPatch(typeof(CondOwner),nameof(CondOwner.ModeSwitch))]
internal static class BulkMode
{
    private static void Postfix(CondOwner coNew)
    {
        if(!BulkDefinitions.IsTank(coNew)||!coNew.HasCond("IsDamaged"))return;
        try{BulkService.PauseSupply(coNew);}catch(Exception e){Plugin.Log(e.ToString());}
    }
}
