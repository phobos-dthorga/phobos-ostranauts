// Adapter doubles for production assembly/recovery logic, not a simulated Unity session.
using System;
using System.Collections.Generic;
using System.Linq;
public class Ship {
 public int LoadState=2;public bool bDestroyed;public string strRegID="";public List<CondOwner> objects=new();
 public int dropsBeforeFailure=-1;public bool overflow;public List<CondOwner> forced=new();
 public List<CondOwner> GetCOs(CondTrigger? trigger,bool bSubObjects,bool bAllowDocked,bool bAllowLocked)=>objects.ToList();
 // Native nearby drop: returns what did not fit; the caller adds it where it stands.
 public CondOwner? DropCO(CondOwner co,UnityEngine.Vector2 at){
  if(dropsBeforeFailure==0)throw new InvalidOperationException("drop failed");if(dropsBeforeFailure>0)dropsBeforeFailure--;
  co.tf.position=new UnityEngine.Vector3(at.x,at.y,0);if(overflow)return co;Place(co);return null;}
 public void AddCO(CondOwner co,bool bTiles){forced.Add(co);Place(co);}
 private void Place(CondOwner co){co.ship=this;co.objCOParent=null;if(!objects.Contains(co))objects.Add(co);}
}
public class CondOwner {
 public Item? Item; private UnityEngine.GameObject go=new();public UnityEngine.Transform tf=new();
 public UnityEngine.GameObject gameObject {get {go.owner=this;return go;}}
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
 public Ship? RemoveFromCurrentHome(bool bForce){var home=ship;home?.objects.Remove(this);objCOParent?.children.Remove(this);objCOParent=null;ship=null;return home;}
 public void Destroy(){bDestroyed=true;ship?.objects.Remove(this);}
 public void LogMessage(string msg,string kind,string id){}
}
public class Container { public CondOwner CO;public List<CondOwner> ContainedCOs=new();public Container(CondOwner co){CO=co;co.objContainer=this;} public bool AllowedCO(CondOwner coIn)=>false; }
public class Placeholder { public string strInstallIA="",strInstalledCO="";public CondOwner owner=null!;
 // The game's cancellation: delivered lot parts go to the deck beside the site, then the marker is destroyed.
 public void Cancel(CondOwner? coOwner){var ship=owner.ship!;foreach(var part in owner.lot.ToArray()){owner.lot.Remove(part);part.objCOParent=null;part.ship=ship;part.tf.position=owner.tf.position;if(!ship.objects.Contains(part))ship.objects.Add(part);}owner.Destroy();}public UnityEngine.GameObject gameObject=>owner.gameObject;public T? GetComponent<T>() where T:class=>owner as T ?? gameObject.GetComponent<T>(); }
public class Item { public bool bPlaceholder=true;public UnityEngine.Renderer rend=new(); }
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
 public static Dictionary<string,UnityEngine.Texture2D> textures=new(); public static int loads;
 public static UnityEngine.Texture2D? LoadPNG(string path,bool bNorm){loads++;return textures.TryGetValue(path,out var t)?t:null;}
 public static Func<string,CondOwner?> factory=_=>null;public static CondOwner? GetCondOwner(string id)=>factory(id);
 public static Dictionary<string,JsonInstallable> dictInstallables=new();public static Dictionary<string,JsonCondOwner> dictCOs=new();public static Dictionary<string,JsonInteraction> dictInteractions=new();public static void GetCOPlaceholder(){}
}
public static class Installables {public static Dictionary<string,Dictionary<string,JsonInstallable>> dictJobBuildOptions=new(),dictJobBuildOptionsListed=new();}
public class CrewSimInstance {public bool FinishedLoading=true;}
public class SystemDouble {public Dictionary<string,Ship> dictShips=new();public string? owner;public string? GetShipOwner(string id)=>owner;}
public static class CrewSim {public static CondOwner actor=new();public static CrewSimInstance? objInstance;public static SystemDouble? system;public static CondOwner? coPlayer;public static CondOwner GetSelectedCrew()=>actor;public static InventoryGUI? inventoryGUI=new();}
public class InventoryGUI {public CondOwner? opened;public void SpawnInventoryWindow(CondOwner co,object kind,object? arg){opened=co;}}
namespace Ostranauts.Inventory {public enum InventoryWindowType {Container}}
namespace HarmonyLib {[AttributeUsage(AttributeTargets.Class)]public class HarmonyPatch:Attribute {public HarmonyPatch(Type t,string name){} public HarmonyPatch(Type t,string name,Type[] args){}}}
namespace Phobos.Ostranauts.Framework {
 public static class FrameworkLifecycle {internal static Action<string> Log=_=>{};}
 internal static class Text {internal static string Get(string key,params object[] args)=>key;}
 public static class Units {public const double MassToleranceKg=.000001;}
}
namespace Phobos.Ostranauts.Framework.Notices {
 public enum NoticeLevel {Info,Caution}
 public static class PlayerNotices {public static List<string> posted=new();public static bool Post(Ship ship,string key,NoticeLevel level,string logText,string? bannerText=null){posted.Add(logText);return true;}}
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
