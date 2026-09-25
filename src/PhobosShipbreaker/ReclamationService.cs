using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Inventory;
using Phobos.Ostranauts.Framework.Persistence;
using Phobos.Ostranauts.Framework.Processing;
using PhobosAutoNav;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

// Mission authority is transient. Only intent, paid work and mutation evidence survive a load.
internal static partial class ReclamationService
{
    private sealed class Session
    {
        internal CondOwner Grabber=null!;
        internal ReclamationRecord Record=null!;
        internal bool Authorized, Demand;
        internal double NextUpdate, PendingSince;
        internal readonly HashSet<string> RejectedWindows=new(StringComparer.Ordinal);
        internal string WindowLayout="",BestWindow="";
        internal int WindowCursor,BestOutward;
        internal double BestCost=double.PositiveInfinity,BestBearing;
        internal string Notice="";
    }
    private static readonly Dictionary<string,Session> sessions=new(StringComparer.Ordinal);
    private static ObjectStateStore Store(CondOwner g)=>new(g.mapGUIPropMaps,ReclamationRecord.StoreName,g.strID,1);
    private static bool Save(Session s)=>s.Record.Valid&&Store(s.Grabber).TryWrite(s.Record.Fields);
    private static bool Read(CondOwner g,out ReclamationRecord r)
    { r=null!; return Store(g).Read(out var fields)==SavedStateStatus.Ready&&ReclamationRecord.Read(fields,out r)&&r["g4"]==g.strID; }
    internal static string Describe(CondOwner g)
    {
        if(!Read(g,out var r)) return Text.Get("Reclamation.help");
        sessions.TryGetValue(g.strID,out var s);
        var target=CrewSim.system?.GetShipByRegID(r["target"]);
        int remnants=target!=null&&IndustrialNavigation.CanTrack(g.ship,r["target"])&&target.LoadState>=Ship.Loaded.Edit ?
            target.GetCOs(null,false,false,true).Count(c=>c.HasCond("IsWall")&&c.HasCond("IsInstalled")):-1;
        return Text.Get("Reclamation.status",Text.Get("Reclamation.phase_"+r.Phase),r.Number("completed"),remnants<0?Text.Get("Reclamation.unknown"):remnants.ToString(CultureInfo.InvariantCulture),
            s!=null?s.Notice:Text.Get("Reclamation.suspended")) +
            (r.Fields.ContainsKey("wall")?"\n"+Text.Get("Reclamation.work",r.Number("progress"),r.Number("seconds"),r.Number("kw")):"");
    }
    internal static bool Command(ConsoleBinding? binding,CondOwner g,string action,out string message)
    {
        message=ProcessingService.AccessProblem(g,binding)??"";
        if(message.Length!=0) return false;
        try
        {
            if(action=="reclaim-status") { message=Describe(g);return true; }
            if(action=="reclaim-pause"||action=="reclaim-stop")
            {
                if(sessions.TryGetValue(g.strID,out var active)) Suspend(active,Text.Get("Reclamation.suspended"));
                message=Describe(g);return true;
            }
            if(action!="reclaim-start"&&action!="reclaim-resume") return false;
            if(!CaptureService.Read(g,out var capture)) { message=Text.Get("Capture.unbound");return false; }
            if(sessions.TryGetValue(g.strID,out var running)&&running.Authorized) { message=Describe(g);return true; }
            ReclamationRecord r=new();
            bool fresh=Store(g).Read(out _)==SavedStateStatus.Missing;
            if(!fresh)
            {
                if(!Read(g,out r)) { message=Text.Get("Capture.save");return false; }
                fresh=action=="reclaim-start"&&!r.Fields.ContainsKey("wall")&&(r.Phase==ReclamationPhase.Exhausted||r.Phase==ReclamationPhase.Remnants);
                if(!fresh&&action!="reclaim-resume") { message=Text.Get("Reclamation.resume");return false; }
            }
            if(fresh)
            {
                if(action!="reclaim-start") { message=Text.Get("Reclamation.help");return false; }
                r=new ReclamationRecord();
                foreach(var key in new[]{"owner","ship","target","console","module","g4","chute","processor","permission"}) r[key]=capture[key];
                r.Phase=ReclamationPhase.Seeking;r.Number("completed",0);
            }
            var s=new Session { Grabber=g,Record=r };
            string? problem=Problem(s,false);
            if(problem!=null) { message=problem;return false; }
            if(capture.Phase==CapturePhase.CapturePending||capture.Phase==CapturePhase.ReleasePending)
            {
                if(!CaptureService.MissionReconcile(g,out message)||!CaptureService.Read(g,out capture)) return false;
            }
            if(g.ship.IsDocked()||g.ship.IsMoored())
            {
                if(!Attached(s,capture)) { message=Text.Get("Reclamation.recapture");return false; }
                if(r.Phase==ReclamationPhase.Approaching||r.Phase==ReclamationPhase.Seeking) r.Phase=ReclamationPhase.Seeking;
            }
            else if(r.Phase==ReclamationPhase.Cutting||r.Phase==ReclamationPhase.UninstallPending||r.Phase==ReclamationPhase.TransferPending||r.Phase==ReclamationPhase.Feeding)
            { message=Text.Get("Reclamation.uncertain");return false; }
            // An interrupted native uninstall is never repeated. Physical evidence is resolved below.
            s.Authorized=true;sessions[g.strID]=s;
            if(!Save(s)||!Plugin.Service.ArmMission(g,r["processor"],out message)) { Suspend(s,Text.Get("Reclamation.intake"));return false; }
            if(!g.ship.IsDocked()&&!g.ship.IsMoored()&&(r.Phase==ReclamationPhase.Egress||r.Phase==ReclamationPhase.Transit||r.Phase==ReclamationPhase.Approaching))
                r.Phase=ReclamationPhase.Seeking;
            Save(s); Step(s);message=Describe(g);return s.Authorized;
        }
        catch(Exception ex) { Fault(g,ex);message=Text.Get("Reclamation.uncertain");return false; }
    }
    private static string? Problem(Session s,bool requireIntake=true)
    {
        var g=s.Grabber;var r=s.Record;
        if(!CaptureService.Read(g,out var capture)||new[]{"owner","ship","target","console","module","g4","chute","processor","permission"}.Any(k=>capture[k]!=r[k])) return Text.Get("Capture.changed");
        string? problem=CaptureService.BindingProblem(g,capture,g.ship.IsDocked()||g.ship.IsMoored());
        if(problem!=null) return problem;
        if(!IndustrialNavigation.CanTrack(g.ship,r["target"])) return Text.Get("Reclamation.contact");
        var target=CrewSim.system.GetShipByRegID(r["target"]);
        problem=g.ship.IsDockedWith(target)?ReclamationGeometry.TargetProblem(g,target):CaptureGeometry.TargetProblem(g.ship,target);
        if(problem!=null) return problem;
        if(r.Fields.ContainsKey("wall")&&new[]{"ownPort","targetPort","support","floor"}.Any(k=>r[k]!=capture[k])) return Text.Get("Reclamation.recapture");
        if(requireIntake&&!Plugin.Service.MissionIntakeActive(g,r["processor"])) return Text.Get("Reclamation.intake");
        if(g.ship.IsDocked()||g.ship.IsMoored()) return Attached(s,capture)?null:Text.Get("Reclamation.recapture");
        if(target.IsDocked()||target.IsMoored()) return Text.Get("Capture.attached");
        return null;
    }
    private static bool Attached(Session s,CaptureRecord capture)
    {
        var own=s.Grabber.ship;var target=CrewSim.system.GetShipByRegID(s.Record["target"]);
        return capture.Phase==CapturePhase.Captured&&CaptureGeometry.ExactAttachment(own,target,capture)&&
            own.GetDockedShipsAndPortIDs().Count==1&&target.GetDockedShipsAndPortIDs().Count==1&&!own.TowBraceSecured(target.strRegID)&&ReclamationGeometry.Support(target,capture);
    }
    internal static void Update()
    {
        if(CrewSim.objInstance==null||!CrewSim.objInstance.FinishedLoading||CrewSim.Paused) return;
        foreach(var s in sessions.Values.ToArray())
        {
            if(!s.Authorized||StarSystem.fEpoch<s.NextUpdate) continue;
            s.NextUpdate=StarSystem.fEpoch+1;
            try { Step(s); } catch(Exception ex) { Fault(s.Grabber,ex); }
        }
    }
    private static void Step(Session s)
    {
        var g=s.Grabber;var r=s.Record;
        string? problem=Problem(s);
        if(problem!=null) { Suspend(s,problem);return; }
        var target=CrewSim.system.GetShipByRegID(r["target"]);CaptureService.Read(g,out var capture);
        if(g.ship.IsDockedWith(target)&&target.bCheckRooms) {s.Notice=Text.Get("Reclamation.geometry");return;}
        if(r.Phase==ReclamationPhase.Exhausted||r.Phase==ReclamationPhase.Remnants) { Suspend(s,Text.Get("Reclamation.phase_"+r.Phase));return; }
        if(r.Phase==ReclamationPhase.UninstallPending||r.Phase==ReclamationPhase.TransferPending) { Settle(s,target,capture);return; }
        if(r.Phase==ReclamationPhase.Cutting)
        {
            var wall=ReclamationGeometry.Resolve(target,r["wall"]);
            if(wall==null||!ReclamationGeometry.Wall(g,target,wall,capture,true)||!ReclamationGeometry.Reach(g,wall)) { Suspend(s,Text.Get("Reclamation.wall_changed"));return; }
            if(!Reserve(g,wall)) { s.Notice=Text.Get("Reclamation.capacity");return; }
            if(!r.Paid) return;
            // Persist before triggering the native destructable/uninstall transition exactly once.
            r.Phase=ReclamationPhase.UninstallPending;s.PendingSince=StarSystem.fEpoch;
            if(!Save(s)) { Suspend(s,Text.Get("Capture.save"));return; }
            wall.SetCondAmount("StatUninstallProgress",wall.GetCondAmount("StatUninstallProgressMax"));
            wall.GetComponent<Destructable>().ScheduleDamageCheck();
            s.Notice=Text.Get("Reclamation.native_wait");return;
        }
        if(r.Phase==ReclamationPhase.Feeding)
        {
            if(g.objContainer.ContainedCOs.Any(c=>c.strID==r["wall"])) { s.Notice=Text.Get("Reclamation.capacity");return; }
            // The transfer journal was committed before existing intake could run.
            foreach(var key in new[]{"wall","progress","seconds","kw","ownPort","targetPort","support","floor"}) r.Fields.Remove(key);
            s.RejectedWindows.Clear();r.Phase=ReclamationPhase.Seeking;if(!Save(s)) { Suspend(s,Text.Get("Capture.save"));return; }
        }
        if(r.Phase==ReclamationPhase.Approaching)
        {
            if(capture.Phase==CapturePhase.Captured) { r.Phase=ReclamationPhase.Seeking;Save(s); }
            else if(capture.Phase==CapturePhase.Suspended) { Suspend(s,Text.Get("Reclamation.navigation"));return; }
            else { s.Notice=CaptureService.Describe(g);return; }
        }
        if(r.Phase==ReclamationPhase.Egress||r.Phase==ReclamationPhase.Transit)
        {
            if(!IndustrialNavigation.Observe(r["permission"],out bool ready,out var message)) { Suspend(s,message);return; }
            s.Notice=message;if(!ready) return;
            IndustrialNavigation.Release(r["permission"]);
            if(r.Phase==ReclamationPhase.Egress) { BeginMove(s,IndustrialMove.Transit,r.Number("bearing"));return; }
            r.Phase=ReclamationPhase.Approaching;
            if(!Save(s)) { Suspend(s,Text.Get("Capture.save"));return; }
            if(!CaptureService.SelectWork(g,r["window"],(int)r.Number("outward"),out message))
            { s.RejectedWindows.Add(r["window"]+"/"+r["outward"]);r.Phase=ReclamationPhase.Seeking;Save(s);s.Notice=message; }
            return;
        }
        if(r.Phase!=ReclamationPhase.Seeking) return;
        if(g.ship.IsDockedWith(target))
        {
            var wall=target.GetCOs(null,false,false,true).Where(c=>ReclamationGeometry.Reach(g,c)&&ReclamationGeometry.Wall(g,target,c,capture))
                .OrderBy(c=>c.strID,StringComparer.Ordinal).FirstOrDefault();
            if(wall!=null)
            {
                if(!Reserve(g,wall)) { s.Notice=Text.Get("Reclamation.capacity");return; }
                foreach(var key in new[]{"ownPort","targetPort","support","floor"}) r[key]=capture[key];
                r.Begin(wall.strID);s.Notice=Text.Get("Reclamation.cutting");if(!Save(s)) Suspend(s,Text.Get("Capture.save"));return;
            }
        }
        SelectWindow(s,target);
    }
    private static bool Reserve(CondOwner g,CondOwner wall) => g.objContainer!=null&&!g.objContainer.Locked&&
        !g.HasCond("IsInfiniteContainer")&&g.objContainer.ContainedCOs.Count==0&&ReclamationGeometry.CanReserve(g,wall);
    private static void Settle(Session s,Ship target,CaptureRecord capture)
    {
        var r=s.Record;var g=s.Grabber;
        var cargo=g.objContainer.ContainedCOs.FirstOrDefault(c=>c.strID==r["wall"]);
        if(r.Phase==ReclamationPhase.TransferPending&&cargo!=null&&ProcessingService.ValidPanel(cargo))
        { r.CompleteTransfer();if(!Save(s)) Suspend(s,Text.Get("Capture.save"));return; }
        var wall=ReclamationGeometry.Resolve(target,r["wall"]);
        if(wall!=null&&wall.strCODef=="ItmWall1x1"&&r.Phase==ReclamationPhase.UninstallPending)
        { if(s.PendingSince==0) s.PendingSince=StarSystem.fEpoch;
          if(StarSystem.fEpoch-s.PendingSince>30) Suspend(s,Text.Get("Reclamation.uncertain")); else s.Notice=Text.Get("Reclamation.native_wait");return; }
        if(wall==null||!ProcessingService.ValidPanel(wall)||!ReclamationGeometry.Reach(g,wall)||!Attached(s,capture))
        { Suspend(s,Text.Get("Reclamation.uncertain"));return; }
        if(!Reserve(g,wall)) { s.Notice=Text.Get("Reclamation.capacity");return; }
        r.Phase=ReclamationPhase.TransferPending;if(!Save(s)) { Suspend(s,Text.Get("Capture.save"));return; }
        var move=new TargetWallTransfer(target,g,wall,()=>s.Authorized&&Problem(s)==null&&CaptureService.Read(g,out var fresh)&&Attached(s,fresh));
        if(!PhysicalTransfer.Commit(move)) { Suspend(s,Text.Get("Reclamation.uncertain"));return; }
        r.CompleteTransfer();if(!Save(s)) { Suspend(s,Text.Get("Capture.save"));return; }
        g.objContainer.Redraw();s.Notice=Text.Get("Reclamation.feeding");
    }
    private static void SelectWindow(Session s,Ship target)
    {
        var g=s.Grabber;var r=s.Record;
        var windows=CaptureGeometry.Windows(target).Where(w=>!s.RejectedWindows.Contains(w.Id+"/"+w.Outward)).ToArray();
        if(windows.Length==0) { Finish(s,target);return; }
        string layout=string.Join(";",windows.Select(w=>w.Id+"/"+w.Outward));
        if(layout!=s.WindowLayout) {s.WindowLayout=layout;s.WindowCursor=0;s.BestCost=double.PositiveInfinity;s.BestWindow="";}
        double best=s.BestCost,bearing=s.BestBearing;string? selected=s.BestWindow.Length==0?null:s.BestWindow;int outward=s.BestOutward;
        var nav=CollectorService.Resolve(r["console"]);
        foreach(var window in windows.Skip(s.WindowCursor).Take(ReclamationRules.MaximumWindows))
        {
            int angle=window.Outward;
            if(target.LoadState>=Ship.Loaded.Edit)
            {
                var wall=ReclamationGeometry.Resolve(target,window.Id);
                if(wall==null||!ReclamationGeometry.Wall(g,target,wall)) continue;
            }
            double candidate=angle+90+target.nGridRotation+target.objSS.fRot*180/Math.PI;
            if(!IndustrialNavigation.RouteCost(nav!,r["module"],r["target"],candidate,ReclamationRules.StagingHullGapM,out double cost)||cost>=best) continue;
            if(!g.ship.IsDockedWith(target)&&!CaptureGeometry.TryPlan(g,target,out _,window.Id,angle,true)) continue;
            best=cost;selected=window.Id;outward=angle;bearing=candidate;
        }
        s.WindowCursor+=ReclamationRules.MaximumWindows;s.BestCost=best;s.BestWindow=selected??"";s.BestBearing=bearing;s.BestOutward=outward;
        if(s.WindowCursor<windows.Length) {s.Notice=Text.Get("Reclamation.bound");return;}
        s.WindowLayout="";
        if(selected==null) { Finish(s,target);return; }
        r["window"]=selected;r.Number("outward",outward);r.Number("bearing",bearing);
        if(g.ship.IsDockedWith(target))
        {
            r.Phase=ReclamationPhase.Egress;if(!Save(s)) { Suspend(s,Text.Get("Capture.save"));return; }
            if(!CaptureService.MissionRelease(g,out var message)) { Suspend(s,message);return; }
            var refreshed=CaptureGeometry.Windows(target).Where(w=>w.Id==r["window"]).ToArray();
            if(refreshed.Length==0) { Suspend(s,Text.Get("Reclamation.geometry"));return; }
            r.Number("outward",refreshed[0].Outward);r.Number("bearing",refreshed[0].Outward+90+target.nGridRotation+target.objSS.fRot*180/Math.PI);
            double exit=Math.Atan2(g.ship.objSS.vPosy-target.objSS.vPosy,g.ship.objSS.vPosx-target.objSS.vPosx)*180/Math.PI;
            BeginMove(s,IndustrialMove.Egress,exit);
        }
        else BeginMove(s,IndustrialMove.Transit,bearing);
    }
    private static void BeginMove(Session s,IndustrialMove move,double bearing)
    {
        var r=s.Record;r.Phase=move==IndustrialMove.Egress?ReclamationPhase.Egress:ReclamationPhase.Transit;
        if(!Save(s)) { Suspend(s,Text.Get("Capture.save"));return; }
        if(!IndustrialNavigation.RequestMove(r["permission"],CollectorService.Resolve(r["console"])!,r["module"],r["target"],move,bearing,
            ReclamationRules.StagingHullGapM,s.Grabber.Item.TF.eulerAngles.z+s.Grabber.ship.nGridRotation,()=>Problem(s),out var message)) Suspend(s,message);
        else s.Notice=message;
    }
    private static void Finish(Session s,Ship target)
    {
        // Let the last acquired physical panels finish their existing D4 jobs before ending the queue.
        if(Plugin.Service.MissionPendingFeed(s.Grabber,s.Record["processor"],out bool capacity))
        {s.Notice=Text.Get(capacity?"Reclamation.capacity":"Reclamation.feeding");return;}
        CaptureService.Read(s.Grabber,out var capture);
        bool remaining=target.LoadState>=Ship.Loaded.Edit?target.GetCOs(null,false,false,true).Any(c=>c.strCODef=="ItmWall1x1"&&c.strID!=capture["support"]):CaptureGeometry.Parts(target).Any(c=>c.strName=="ItmWall1x1");
        s.Record.Phase=remaining?ReclamationPhase.Remnants:ReclamationPhase.Exhausted;
        Save(s);Suspend(s,Text.Get("Reclamation.phase_"+s.Record.Phase));
    }
    private static void Suspend(Session s,string reason)
    {
        s.Authorized=s.Demand=false;s.Notice=reason;
        IndustrialNavigation.Release(s.Record["permission"]);s.Grabber.ZeroCondAmount(IntakeRules.Working);
        Plugin.Service.StopMission(s.Grabber,s.Record["processor"]);Save(s);
    }
    internal static void ManualTakeover(Ship own)
    { foreach(var s in sessions.Values.Where(s=>s.Authorized&&s.Grabber.ship==own).ToArray()) Suspend(s,Text.Get("Reclamation.manual")); }
    internal static void Fault(CondOwner g,Exception ex)
    { Plugin.Log(ex.ToString());if(sessions.TryGetValue(g.strID,out var s)) Suspend(s,Text.Get("Reclamation.uncertain")); }
    internal static void Reset() { foreach(var s in sessions.Values.ToArray()) if(s.Authorized) Suspend(s,Text.Get("Reclamation.suspended"));sessions.Clear(); }
    internal static bool PreparePower(CondOwner g)
    {
        if(!sessions.TryGetValue(g.strID,out var s)) return false;
        s.Demand=false;
        if(!s.Authorized||s.Record.Phase!=ReclamationPhase.Cutting||s.Record.Paid) return false;
        var target=CrewSim.system.GetShipByRegID(s.Record["target"]);var wall=ReclamationGeometry.Resolve(target,s.Record["wall"]);
        var problem=Problem(s);
        if(problem!=null) { Suspend(s,problem);return false; }
        if(target.bCheckRooms) {s.Notice=Text.Get("Reclamation.geometry");return false;}
        CaptureService.Read(g,out var capture);
        if(wall==null||!ReclamationGeometry.Wall(g,target,wall,capture,true)||!ReclamationGeometry.Reach(g,wall)) { Suspend(s,Text.Get("Reclamation.wall_changed"));return false; }
        if(!Reserve(g,wall)) { s.Notice=Text.Get("Reclamation.capacity");return false; }
        s.Demand=true;g.SetCondAmount(IntakeRules.Working,1);return true;
    }
}
