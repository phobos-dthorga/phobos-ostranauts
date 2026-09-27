using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
[HarmonyPatch(typeof(CondOwner),nameof(CondOwner.ModeSwitch))]
internal static class BulkMode
{
    internal static readonly HashSet<CondOwner> Switching=new();
    private static bool Prefix(CondOwner __instance,CondOwner coNew,out StoredCommodity? __state)
    {
        __state=null;if(!BulkDefinitions.IsTank(__instance))return true;
        if(!BulkDefinitions.IsTank(coNew)||BulkService.Protected(__instance))return false;
        __state=BulkService.Read(__instance);Switching.Add(__instance);return true;
    }
    private static void Postfix(CondOwner coNew,StoredCommodity? __state)
    {if(__state==null)return;if(coNew.HasCond("IsDamaged")){__state.Isolate();BulkService.PauseSupply(coNew);}BulkService.Save(coNew,__state);}
    private static void Finalizer(CondOwner __instance){Switching.Remove(__instance);}
}
[HarmonyPatch(typeof(CondOwner),nameof(CondOwner.Destroy))]
internal static class BulkDestroy
{
    // Live destructive commands cannot discard filled/uncertain storage. Loading cleanup
    // and the old half of a mode switch are lifecycle disposal, not material disposal.
    internal static bool Loading;
    internal static int ShipDisposal;
    internal static bool Allow(CondOwner co)
    {
        if(Loading||ShipDisposal>0||CrewSim.objInstance==null||!CrewSim.objInstance.FinishedLoading||!BulkDefinitions.IsTank(co)||BulkMode.Switching.Contains(co))return true;
        return !BulkService.Protected(co)&&BulkService.Read(co).TotalKg<=1e-8;
    }
    private static bool Prefix(CondOwner __instance)
    {
        return Allow(__instance);
    }
}
[HarmonyPatch(typeof(CondOwner),nameof(CondOwner.RemoveFromCurrentHome))]
internal static class BulkDetach
{
    private static bool Prefix(CondOwner __instance,ref Ship? __result)
    {if(BulkDestroy.Allow(__instance))return true;__result=null;return false;}
}
[HarmonyPatch(typeof(CrewSim),nameof(CrewSim.ScheduleCODestruction))]
internal static class BulkScheduleDestroy
{
    private static bool Prefix(CondOwner coToDestroy)=>BulkDestroy.Allow(coToDestroy);
}
// Native despawn/unload and whole-ship destruction retain their own semantics.
// Preventing them would strand unloaded Unity objects and corrupt subsequent loads.
[HarmonyPatch(typeof(Ship),nameof(Ship.Destroy))]
internal static class BulkShipDisposal
{
    private static void Prefix(){BulkDestroy.ShipDisposal++;}
    private static void Finalizer(){BulkDestroy.ShipDisposal--;}
}
[HarmonyPatch]
internal static class BulkLoadLifecycle
{
    private static IEnumerable<MethodBase> TargetMethods()=>typeof(CrewSim).GetMethods().Where(m=>m.Name==nameof(CrewSim.LoadGame)||m.Name==nameof(CrewSim.NewGame));
    private static void Prefix(){BulkDestroy.Loading=true;}
}
