using System;
using HarmonyLib;

namespace PhobosAgriculture;
[HarmonyPatch(typeof(Interaction),nameof(Interaction.ApplyEffects))]
internal static class BulkEffects
{
    private static void Postfix(Interaction __instance,bool isCancelIa)
    {
        if(isCancelIa||__instance.bCancel)return;
        if(HopperDefinitions.IsHopper(__instance.objThem))
        {
            if(__instance.strName==BulkDefinitions.Controls){if(__instance.objUs==CrewSim.GetSelectedCrew())Panel.Show(__instance.objThem);return;}
            bool done=__instance.strName==HopperDefinitions.WorkId(HopperDefinitions.RecoverWork)?HopperService.Recover(__instance.objThem,__instance.objUs):
                __instance.strName==HopperDefinitions.WorkId(HopperDefinitions.BagWork)&&Service.BagFromHopper(__instance.objThem,__instance.objUs);
            if(done)Phobos.Ostranauts.Framework.Crew.CrewSpecialities.CreditPractical(__instance);
            else RackFull(__instance.objThem,__instance.objUs);
            return;
        }
        // The reservoir crew work applies to Framework's water tanks too (Agriculture 0.31.0); their panel is Framework's.
        if(!BulkDefinitions.IsWaterTank(__instance.objThem))return;
        if(__instance.strName==BulkDefinitions.Controls&&BulkDefinitions.IsTank(__instance.objThem)){if(__instance.objUs==CrewSim.GetSelectedCrew())Panel.Show(__instance.objThem);return;}
        foreach(var action in BulkDefinitions.Work)
        {
            if(__instance.strName!=BulkDefinitions.WorkId(action))continue;
            if(BulkService.Work(__instance.objThem,__instance.objUs,action))Phobos.Ostranauts.Framework.Crew.CrewSpecialities.CreditPractical(__instance);
            else if(action=="bulk-drain")RackFull(__instance.objThem,__instance.objUs);
        }
    }
    // A service rack is four cells (Agriculture 0.36.0): when work that puts an item in it finds no free cell, say so.
    private static void RackFull(CondOwner? vessel,CondOwner? actor)
    {
        if(vessel?.objContainer==null||actor==null||actor.bDestroyed||!actor.HasCond("IsHuman"))return;
        if(vessel.objContainer.gridLayout.FindFirstUnoccupiedTile(1,1).IsValid())return;
        actor.LogMessage(Text.Get("rack_full",Phobos.Ostranauts.Framework.Controls.ObjectPresentation.Name(vessel)),"Bad","Game");
    }
}
// Framework BulkVessel carries the water into a tank successor (isolating it when the successor is damaged)
// and logs water lost with a destroyed tank. Agriculture only pauses the paired W2 intake on damage.
[HarmonyPatch(typeof(CondOwner),nameof(CondOwner.ModeSwitch))]
internal static class BulkMode
{
    private static void Postfix(CondOwner coNew)
    {
        if(!BulkDefinitions.IsWaterTank(coNew)||!coNew.HasCond("IsDamaged"))return;
        try{BulkService.PauseSupply(coNew);}catch(Exception e){Plugin.Log(e.ToString());}
    }
}
