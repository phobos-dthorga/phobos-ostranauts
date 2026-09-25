using System;
using System.Linq;
using System.Collections.Generic;
using PhobosShipbreaker;
using PhobosShipbreaker.Core;
using Phobos.Ostranauts.Framework.Controls;
namespace UnityEngine { internal struct Vector3 { internal float x {get;set;}internal float y {get;set;}internal float z {get;set;} } }
internal sealed class Transform { internal UnityEngine.Vector3 position;internal UnityEngine.Vector3 eulerAngles=default; }
internal sealed class Item { internal Transform TF=new(); }
internal readonly struct PairXY { }
internal sealed class Container
{
    internal CondOwner Owner=null!;
    internal bool Locked=false,Full=false,FailAfterPlacement=false;
    internal List<CondOwner> ContainedCOs=new();
    internal bool Contains(CondOwner item)=>ContainedCOs.Contains(item);
    internal bool AllowedCO(CondOwner item)=>true;
    internal bool CanAddSimple(CondOwner item,out PairXY cell) {cell=default;return !Full&&ContainedCOs.Count==0;}
    internal void AddCOSimple(CondOwner item,PairXY cell) {ContainedCOs.Add(item);item.ship=Owner.ship;item.objCOParent=Owner;if(FailAfterPlacement)throw new InvalidOperationException("after placement");}
    internal void Redraw() { }
}
internal sealed class CondOwner
{
    internal string strID="",strCODef="";
    internal bool bDestroyed=false,Reach=true,ValidWall=true;
    internal Ship ship=null!;
    internal CondOwner? objCOParent=null;
    internal Item Item=new();
    internal Transform tf=new();
    internal Container objContainer;
    internal Dictionary<string,Dictionary<string,string>> mapGUIPropMaps=new();
    internal HashSet<string> Conditions=new(){"IsInstalled","IsPowered"};
    internal CondOwner() { objContainer=new() {Owner=this}; }
    internal bool HasCond(string key)=>Conditions.Contains(key);
    internal void ZeroCondAmount(string key)=>Conditions.Remove(key);
    internal void SetCondAmount(string key,double value)
    {
        if(key=="StatUninstallProgress")
        {
            World.UninstallCalls++;
            if(World.InterruptUninstall) throw new InvalidOperationException("native uninstall");
            var replacement=new CondOwner {strID=strID,strCODef="ItmWall1x1Loose",ship=ship,Reach=!World.Relocate};replacement.Conditions.Remove("IsInstalled");
            ship.Items.Remove(this);ship.Items.Add(replacement);bDestroyed=true;
        }
        else if(value==0)Conditions.Remove(key);else Conditions.Add(key);
    }
    internal double GetCondAmount(string key)=>120;
    internal UnityEngine.Vector3 GetPos()=>tf.position;
    internal T GetComponent<T>() where T:new()=>new();
    internal Ship RemoveFromCurrentHome() {var previous=ship;ship.Items.Remove(this);ship=null!;return previous;}
}
internal sealed class Destructable { internal void ScheduleDamageCheck(){} }
internal sealed class ShipSitu {internal double vPosx=0,vPosy=0;internal float fRot=0;}
internal sealed class JsonItem {internal string strName="";}
internal sealed class Ship
{
    internal enum Loaded { Shallow,Edit,Full }
    internal Loaded LoadState=Loaded.Full;
    internal string strRegID="";
    internal bool Attached=true,bCheckRooms=false;
    internal int nGridRotation=0;
    internal List<CondOwner> Items=new();
    internal ShipSitu objSS=new();
    internal bool IsDocked()=>Attached;
    internal bool IsMoored()=>Attached;
    internal bool IsDockedWith(Ship target)=>Attached;
    internal bool TowBraceSecured(string target)=>false;
    internal Dictionary<string,Ship> GetDockedShipsAndPortIDs()=>Attached?new(){{"port",this}}:new();
    internal IEnumerable<CondOwner> GetCOs(object? filter,bool sub,bool docked,bool locked)=>Items;
    internal void AddCO(CondOwner item,bool bTiles) {Items.Add(item);item.ship=this;}
}
internal sealed class StarSystem
{
    internal static double fEpoch;
    internal Dictionary<string,Ship> Ships=new();
    internal Ship GetShipByRegID(string id)=>Ships[id];
}
internal sealed class CrewSim
{
    internal static CrewSim objInstance=new();
    internal bool FinishedLoading=true;
    internal static bool Paused=false;
    internal static StarSystem system=new();
}
internal static class World
{
    internal static CaptureRecord Capture=new();
    internal static CondOwner Grabber=null!;
    internal static bool InterruptUninstall,Relocate,CaptureFit=true;
    internal static int UninstallCalls,ReleaseCalls,CaptureCalls;
    internal static Ship Target=>CrewSim.system.GetShipByRegID("target");
}
namespace PhobosAutoNav
{
    public enum IndustrialMove { CaptureApproach,Egress,Transit }
    internal static class IndustrialNavigation
    {
        internal static string? Permission;
        internal static bool Visible=true,Ready=true;
        internal static List<IndustrialMove> Moves=new();
        internal static bool CanTrack(Ship own,string id)=>Visible;
        internal static bool Observe(string permission,out bool ready,out string message) {ready=Ready;message="nav";return Permission==permission;}
        internal static void Release(string permission) {if(Permission==permission)Permission=null;}
        internal static bool RouteCost(CondOwner co,string module,string target,double bearing,double gap,out double cost) {cost=1;return true;}
        internal static bool RequestMove(string permission,CondOwner co,string module,string target,IndustrialMove move,double bearing,double gap,double facing,Func<string?> binding,out string message)
        {Permission=permission;Moves.Add(move);message="nav";return binding()==null;}
    }
}
namespace PhobosShipbreaker
{
    internal static class Text {internal static string Get(string key,params object[] args)=>key;}
    internal static class Plugin {internal static Action<string> Log=_=>{};internal static ProcessingService Service=new();}
    internal sealed class ProcessingService
    {
        internal bool Armed,PendingFeed;
        internal bool MissionPendingFeed(CondOwner g,string processor,out bool capacity) {capacity=false;return PendingFeed;}
        internal static string? AccessProblem(CondOwner g,ConsoleBinding? binding)=>null;
        internal bool MissionIntakeActive(CondOwner g,string processor)=>Armed;
        internal bool ArmMission(CondOwner g,string processor,out string message) {message="intake";Armed=true;return true;}
        internal void StopMission(CondOwner g,string processor)=>Armed=false;
        internal static bool ValidPanel(CondOwner item)=>item.strCODef=="ItmWall1x1Loose"&&!item.bDestroyed;
    }
    internal static class CollectorService {internal static CondOwner Resolve(string id)=>new(){strID=id,ship=World.Grabber.ship};}
    internal static class CaptureService
    {
        internal static bool Read(CondOwner g,out CaptureRecord r) {r=World.Capture;return true;}
        internal static string? BindingProblem(CondOwner g,CaptureRecord r,bool attached)=>null;
        internal static bool MissionReconcile(CondOwner g,out string message) {message="reconcile";return true;}
        internal static string Describe(CondOwner g)=>"capture";
        internal static bool SelectWork(CondOwner g,string wall,int outward,out string message)
        {World.CaptureCalls++;g.ship.Attached=World.Target.Attached=true;World.Capture.Phase=CapturePhase.Captured;World.Target.Items.First(c=>c.strID==wall).Reach=true;message="capture";return World.CaptureFit;}
        internal static bool MissionRelease(CondOwner g,out string message)
        {World.ReleaseCalls++;g.ship.Attached=World.Target.Attached=false;World.Capture.Phase=CapturePhase.Released;message="release";return true;}
    }
    internal static class CaptureGeometry
    {
        internal static bool ExactAttachment(Ship own,Ship target,CaptureRecord r)=>own.Attached&&target.Attached;
        internal static string? TargetProblem(Ship own,Ship target)=>null;
        internal static JsonItem[] Parts(Ship target)=>target.Items.Select(c=>new JsonItem {strName=c.strCODef}).ToArray();
        internal static IEnumerable<(string Id,int Outward)> Windows(Ship target)=>target.Items.Where(c=>c.strCODef=="ItmWall1x1").Select(c=>(c.strID,0));
        internal static bool TryPlan(CondOwner g,Ship target,out object? plan,string wall,int outward,bool working) {plan=null;return World.CaptureFit;}
    }
    internal static class ReclamationGeometry
    {
        internal static bool CanReserve(CondOwner g,CondOwner wall)=>g.objContainer.CanAddSimple(wall,out _);
        internal static CondOwner? Resolve(Ship target,string id)=>target.Items.FirstOrDefault(c=>c.strID==id);
        internal static string? TargetProblem(CondOwner g,Ship target)=>null;
        internal static bool Support(Ship target,CaptureRecord record)=>record["support"]=="retained-support";
        internal static bool Wall(CondOwner g,Ship target,CondOwner wall,CaptureRecord? capture=null,bool started=false)=>wall.strCODef=="ItmWall1x1"&&wall.ValidWall;
        internal static bool Reach(CondOwner g,CondOwner wall)=>wall.Reach;
    }
}
