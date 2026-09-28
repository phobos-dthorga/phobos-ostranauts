// Adapter doubles for production assembly/recovery logic, not a simulated Unity session.
using System;
using System.Collections.Generic;
using System.Linq;
public class Ship { public int LoadState=2; }
public class CondOwner {
 public string strCODef="", strID="",strPersistentCO="";public bool bDestroyed;
 public Ship? ship=new();public CondOwner? objCOParent,coStackHead;public Container? objContainer;
 public List<CondOwner> aStack=new(),lot=new(),children=new();public Placeholder? marker;public Interaction? active;
 public Dictionary<string,double> conditions=new();
 public double GetCondAmount(string k)=>conditions.TryGetValue(k,out var x)?x:0;
 public void SetCondAmount(string k,double v)=>conditions[k]=v;
 public void ZeroCondAmount(string k)=>conditions[k]=0;
 public bool HasCond(string k)=>GetCondAmount(k)>0;
 public List<CondOwner> GetCOsSafe(bool deep)=>children.Concat(aStack).ToList();
 public List<CondOwner> GetLotCOs(bool deep)=>lot;
 public T? GetComponent<T>() where T:class=>marker as T;
 public Interaction? GetInteractionCurrent()=>active;
 public void ModeSwitch(CondOwner coNew){}
 public void LogMessage(string msg,string kind,string id){}
}
public class Container { public CondOwner CO;public List<CondOwner> ContainedCOs=new();public Container(CondOwner co){CO=co;co.objContainer=this;} public bool AllowedCO(CondOwner coIn)=>false; }
public class Placeholder { public string strInstallIA="",strInstalledCO="";public CondOwner owner=null!;public T GetComponent<T>() where T:class=> (owner as T)!; }
public class Interaction { public string strName="",strTitle="";public CondOwner objUs=null!,objThem=null!;public void ApplyEffects(bool isCancelIa=false){}public void ResetObject(){} }
public class CondTrigger { public string strName="";public float fChance,fCount;public bool bAND;public string[] aReqs=Array.Empty<string>(),aForbids=Array.Empty<string>(),aTriggers=Array.Empty<string>();public bool Triggered(CondOwner co,string action,bool stats)=>true; }
public class JsonCondOwner {public string strName="";public string[] aStartingConds=Array.Empty<string>(),aInteractions=Array.Empty<string>();}
public class JsonInteraction {public string strName="",strTitle="",strDesc="",strTooltip="";public string? strRaiseUI,CTTestThem;public float fTargetPointRange;}
public class JsonInstallable {
 public string strName="",strActionCO="",strActionGroup="",strJobType="",strInteractionName="",strInteractionTemplate="",strStartInstall="",strBuildType="",CTThem="";
 public string[] aInputs=Array.Empty<string>(),aToolCTsUse=Array.Empty<string>(),aLootCOs=Array.Empty<string>();public float fDuration,fTargetPointRange;
 public string strAllowLootCTsUs="",strAllowLootCTsThem="",strProgressStat="",strCTThemMultCondUs="",strCTThemMultCondTools="";
}
public static class DataHandler {
 public static Dictionary<string,JsonInstallable> dictInstallables=new();public static Dictionary<string,JsonCondOwner> dictCOs=new();public static Dictionary<string,JsonInteraction> dictInteractions=new();public static void GetCOPlaceholder(){}
}
public static class Installables {public static Dictionary<string,Dictionary<string,JsonInstallable>> dictJobBuildOptions=new(),dictJobBuildOptionsListed=new();}
public static class CrewSim {public static CondOwner actor=new();public static CondOwner GetSelectedCrew()=>actor;public static InventoryGUI? inventoryGUI=new();}
public class InventoryGUI {public CondOwner? opened;public void SpawnInventoryWindow(CondOwner co,object kind,object? arg){opened=co;}}
namespace Ostranauts.Inventory {public enum InventoryWindowType {Container}}
namespace HarmonyLib {[AttributeUsage(AttributeTargets.Class)]public class HarmonyPatch:Attribute {public HarmonyPatch(Type t,string name){} public HarmonyPatch(Type t,string name,Type[] args){}}}
namespace Phobos.Ostranauts.Framework {
 internal static class Text {internal static string Get(string key,params object[] args)=>key;}
 public static class Units {public const double MassToleranceKg=.000001;}
}
namespace Phobos.Ostranauts.Framework.Registration {
 public class NativeDefinitions {public Dictionary<string,JsonCondOwner> Objects=new();public Dictionary<string,JsonInstallable> Installables=new();public Dictionary<string,CondTrigger> Triggers=new();public Dictionary<string,JsonInteraction> Interactions=new();public static JsonInteraction Clone(JsonInteraction x)=>new();}
 public static class EquipmentSaveUpgrade {public static double Amount(IEnumerable<string> values,string key)=>double.Parse(values.Single(s=>s.StartsWith(key+"=")).Split('x').Last(),System.Globalization.CultureInfo.InvariantCulture);}
 public static class MaintenanceDefinitions {public static void SetStat(JsonCondOwner co,string k,double v)=>co.aStartingConds=co.aStartingConds.Where(s=>!s.StartsWith(k+"=")).Append(k+"=1x"+v.ToString(System.Globalization.CultureInfo.InvariantCulture)).ToArray();}
 public static class InstallMenu {public const string Appliances="APPS";}
}
namespace Phobos.Ostranauts.Framework.Construction {
 public static class ConstructionRegistry {public static CondTrigger Trigger(string id,string[] req,string[] forbid)=>new(){strName=id,aReqs=req,aForbids=forbid};}
 internal static class ConstructionHooks {internal static bool HasNoContents(CondOwner co)=>co.GetCOsSafe(true).All(co.aStack.Contains);}
}
namespace PhobosShipbreaker {
 internal static class Text {internal static string Get(string k,params object[] args)=>k;}
 internal static class ProcessingService {internal static string? Problem;internal static string? AccessProblem(CondOwner co)=>Problem;}
 internal static class FurnaceService {internal static string? Problem;internal static string? MaintenanceReason(CondOwner co,bool repair)=>Problem;}
}
namespace PhobosShipbreaker.Core {
 internal static class FurnaceRules {internal static bool Cooling(string id)=>id.StartsWith("Port")||id.StartsWith("Radiator");internal static bool Underside(string id)=>id.StartsWith("Port");}
}
