using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
// The production policy is linked; no Unity slot physics or input is executed.
public class CondOwner { public string? slotNow; public Dictionary<string,JsonSlotEffects> mapSlotEffects=new(); public void SetData(){} }
public class JsonSlotEffects { public string strSlotPrimary=""; }
public class Slots { public CondOwner? UnSlotItem(string slot,CondOwner item,bool force)=>null; }
public class JsonCondOwner { public string strName="",strType="Item"; public string[] aStartingConds=Array.Empty<string>(),aInteractions=Array.Empty<string>(),mapSlotEffects=Array.Empty<string>();public int nStackLimit=1; }
public class JsonCondOwnerSave { public string strCODef="",strID="",strSlotName="";public string[]? aConds,aCondZeroes;public int[]? aCondReveals; }
public static class DataHandler { public static JsonSlotEffects? GetSlotEffect(string id)=>new(); }
namespace HarmonyLib { [AttributeUsage(AttributeTargets.Class)] public sealed class HarmonyPatch:Attribute { public HarmonyPatch(Type type,string method){} public HarmonyPatch(Type type,string method,Type[] args){} } }
namespace Phobos.Ostranauts.Framework.Registration {
 public class NativeDefinitions { public Dictionary<string,JsonCondOwner> Objects=new(); public static T Clone<T>(T value)=>JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value,new JsonSerializerOptions{IncludeFields=true}),new JsonSerializerOptions{IncludeFields=true})!; }
 public static class MaintenanceDefinitions { public static void SetStat(JsonCondOwner co,string name,double value)=>co.aStartingConds=co.aStartingConds.Concat(new[]{name+"=1x"+value}).ToArray(); }
 public static class EquipmentSaveUpgrade { public static double Amount(IEnumerable<string> terms,string name)=>terms.Any(s=>s==name+"=1x1")?1:0; }
}
