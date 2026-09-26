using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Phobos.Ostranauts.Framework.Crew;

/// <summary>One chronological clock owner inside native SFF. Normal play never calls these adapters.</summary>
public static class CrewSkip
{
    public static bool Active { get; private set; }
    internal static bool Managed;
    internal static double RepairHours;
    private static readonly FieldInfo PowerEpoch = AccessTools.Field(typeof(Powered), "fUpdateLast");
    private static readonly MethodInfo RunPower = AccessTools.Method(typeof(Powered), "Run");
    private static readonly Dictionary<string,double> busy = new(StringComparer.Ordinal);
    private static readonly Dictionary<string,CrewWork.Job> assignments = new(StringComparer.Ordinal);
    private static readonly Dictionary<string,int> completions = new(StringComparer.Ordinal);
    private static readonly HashSet<string> unavailable = new(StringComparer.Ordinal);
    private static readonly HashSet<string> nativeCare = new(StringComparer.Ordinal);
    private static readonly FieldInfo PreviewPayloads = AccessTools.Field(typeof(GUIFFWDRow), "dictPayloads");
    private static readonly Dictionary<string,CrewTimeBudget> budgets = new(StringComparer.Ordinal);
    private static CondOwner[] crew = Array.Empty<CondOwner>();
    private static readonly HashSet<string> faulted = new(StringComparer.Ordinal);
    private static readonly Dictionary<string,double> nextDecision = new(StringComparer.Ordinal);
    private static CondOwner[] consumers = Array.Empty<CondOwner>();
    internal static string Preview(Ship ship)
    {
        var lines=new List<string>{CrewWork.Message("skip_preview")};
        var contexts=DataHandler.GetLoot("ACTFFWDContextPayloads").GetAllLootNames();
        var members=CrewRoster.Members();
        foreach(var actor in members.Where(a=>a.ship==ship))
        {
            var care=contexts.Select(id=>DataHandler.GetInteraction(id)).FirstOrDefault(i=>i!=null&&i.Triggered(actor,actor));
            string state=care?.strTitle??CrewWork.Message(actor.HasCond("IsAIManual")?"preview_manual":
                actor.Company?.GetShift(StarSystem.nUTCHour,actor).nID!=2?"preview_rest":!CrewWork.Idle(actor)?"busy":"preview_available");
            lines.Add(CrewWork.Message("preview_actor",actor.FriendlyName,state));
        }
        foreach(var co in CrewWork.Equipment(ship).Where(c=>CrewWork.Order(c).Permission==WorkPermission.Enabled))
        {
            var provider=CrewWork.Provider(co)!; string detail=CrewWork.Message("skip_wait");
            try
            {
                if(provider is ICrewSkipProvider p && p.CanAdvance(co,out _))
                {
                    var offer=provider.Next(co,CrewWork.Order(co),out var reason); detail=offer?.Label??reason;
                    if(offer!=null)
                    {
                        var available=members.Where(a=>CrewWork.Eligible(a,offer,out _)).ToArray();
                        if(available.Length==0)detail+=" — "+CrewWork.Message("crew_unavailable");
                        else if(!available.Any(a=>CrewWork.Path(a,offer.Target)&&CrewLogistics.Prepare(a,offer)))
                            detail+=" — "+CrewWork.Message("access_blocked");
                    }
                }
            }
            catch { detail=CrewWork.Message("protected"); }
            lines.Add(co.strNameFriendly+": "+detail);
        }
        return string.Join("\n",lines);
    }
    internal static void Begin(IEnumerable<GUIFFWDRow> rows)
    {
        Managed=false; RepairHours=0; assignments.Clear(); busy.Clear(); completions.Clear(); unavailable.Clear(); budgets.Clear(); faulted.Clear();
        var participants=rows.ToArray(); nativeCare.Clear();
        var contexts=DataHandler.GetLoot("ACTFFWDContextPayloads").GetAllLootNames();
        foreach(var row in participants)
        {
            // Native ApplyEffects uses this frozen preview for the whole skip. Reserve the same
            // minutes even if a condition changes during machine stepping; never count them twice.
            var payloads=PreviewPayloads.GetValue(row) as Dictionary<string,int>;
            if(row.CO!=null && (payloads==null || contexts.Any(id=>payloads.TryGetValue(id,out var hours)&&hours>0)))
                nativeCare.Add(row.CO.strID);
        }
        var available=CrewRoster.Members();
        crew=participants.Select(r=>r.CO).Where(c=>available.Contains(c)).Distinct().ToArray(); nextDecision.Clear();
        CrewWork.StartSkip();
    }
    // Replace only the native clock call, retaining the surrounding native risk/events/report lifecycle.
    internal static void Advance(StarSystem system,double seconds)
    {
        var ships=crew.Select(c=>c.ship).Distinct().ToArray();
        Managed=ships.Any(s=>CrewWork.Equipment(s).Any(c=>CrewWork.Order(c).Permission==WorkPermission.Enabled));
        if(!Managed) { system.Update(seconds); return; }
        consumers=DataHandler.mapCOs.Values.Where(c=>c!=null&&!c.bDestroyed&&ships.Contains(c.ship)&&(c.Pwr!=null||c.GasContainer!=null)).ToArray();
        Active=true; double remaining=seconds, hourLeft=0; var initial=StarSystem.fEpoch; int previewHour=StarSystem.nUTCHour;
        try
        {
            while(remaining>1e-6)
            {
                if(hourLeft<=1e-6) { SnapshotHour(crew,previewHour++); hourLeft=Math.Min(3600,remaining); }
                // Small shared steps retain power competition and native gas/thermal limits.
                double step=Math.Min(CrewBalance.SkipStepSeconds,Math.Min(CrewBalance.UntilHour(StarSystem.fEpoch),Math.Min(remaining,hourLeft)));
                foreach(var id in assignments.Keys.ToArray())
                    if(!CrewWork.Eligible(assignments[id].Worker!,assignments[id].Offer,out _) || unavailable.Contains(id)) Drop(id);
                foreach(var actor in crew) TryAssign(actor,ships);
                foreach(var value in busy.Values) if(value>1e-6) step=Math.Min(step,value);
                int workHour=StarSystem.nUTCHour;
                var repair=crew.Where(a=>!assignments.ContainsKey(a.strID)&&!unavailable.Contains(a.strID)&&CrewWork.Idle(a)&&
                    a.OwnsShip(a.ship.strRegID)&&CrewWork.Eligible(a,new CrewWorkOffer("repair","",CrewRole.Industry,a,1,duty:"Repair"),out _,false)).ToArray();
                foreach(var actor in crew.Where(a=>!assignments.ContainsKey(a.strID)&&!unavailable.Contains(a.strID)))
                    if(budgets[actor.strID].TrySpend(step,false)&&repair.Contains(actor))RepairHours+=step/3600;
                system.Update(step);
                TickMachines(ships);
                foreach(var actor in crew)
                    foreach(var ia in (actor.aQueue??Enumerable.Empty<Interaction>()).Where(i=>i?.strName?.StartsWith(CrewSpecialities.StudyPrefix,StringComparison.Ordinal)==true))
                        if(!CrewSpecialities.StudyReady(actor,ia.objThem))ia.bCancel=true;
                foreach(var actor in crew)
                {
                    if(!assignments.TryGetValue(actor.strID,out var job)) continue;
                    if(!CrewWork.Eligible(actor,job.Offer,out _,hour:workHour) || unavailable.Contains(actor.strID)) { Drop(actor.strID); continue; }
                    if(!budgets[actor.strID].TrySpend(step,true)) { Drop(actor.strID); continue; }
                    busy[actor.strID]-=step;
                    if(busy[actor.strID]>1e-6) continue;
                    if(CrewWork.Complete(job,true,workHour)) completions[actor.strID]=completions.TryGetValue(actor.strID,out var n)?n+1:1;
                    Drop(actor.strID);
                }
                remaining-=step; hourLeft-=step;
            }
        }
        catch(Exception error)
        {
            foreach(var ship in ships) foreach(var co in CrewWork.Equipment(ship)) CrewWork.Fault(co,error);
        }
        finally
        {
            foreach(var id in assignments.Keys.ToArray()) Drop(id);
            Active=false;
            // Do not rewind or replay an elapsed world interval on any failure.
            if(StarSystem.fEpoch-initial<seconds-1e-6)
            {
                foreach(var ship in ships) foreach(var co in CrewWork.Equipment(ship)) CrewWork.Provider(co)?.Suspend(co);
                system.Update(seconds-(StarSystem.fEpoch-initial));
            }
        }
    }
    private static void SnapshotHour(IEnumerable<CondOwner> crew,int previewHour)
    {
        unavailable.Clear(); budgets.Clear();
        foreach(var actor in crew)
        {
            budgets[actor.strID]=new CrewTimeBudget(3600);
            // Native preview allocates whole hours from the starting hour. Intersect its work
            // allowance with the actual roster checked each step, preserving native rest minutes.
            if(nativeCare.Contains(actor.strID) || actor.Company?.GetShift(previewHour,actor).nID!=2)
            { unavailable.Add(actor.strID); budgets[actor.strID].TrySpend(3600,false); }
        }
    }
    private static void TryAssign(CondOwner actor,Ship[] ships)
    {
        if(assignments.ContainsKey(actor.strID) || unavailable.Contains(actor.strID) || !CrewWork.Idle(actor) ||
            nextDecision.TryGetValue(actor.strID,out var next)&&StarSystem.fEpoch<next)return;
        nextDecision[actor.strID]=StarSystem.fEpoch+CrewBalance.DiscoverySeconds;
        var offers=new List<CrewWork.Job>();
        foreach(var co in CrewWork.Equipment(actor.ship).Where(c=>!faulted.Contains(c.strID)))
        {
            var order=CrewWork.Order(co); var provider=CrewWork.Provider(co)!;
            if(order.Protected || order.Permission!=WorkPermission.Enabled || provider is not ICrewSkipProvider support || !support.CanAdvance(co,out _)) continue;
            CrewWorkOffer? offer;
            try { offer=provider.Next(co,order,out var reason); CrewWork.Notice(co,reason); }
            catch(Exception error) { faulted.Add(co.strID); CrewWork.Fault(co,error); continue; }
            if(offer==null || !offer.SkipSupported || offer.Role==CrewRole.Exterior || !CrewWork.Eligible(actor,offer,out _) ||
                CrewWork.NativeWorkPrecedes(actor,offer) || !CrewWork.Path(actor,offer.Target) || !CrewLogistics.Prepare(actor,offer)) continue;
            offers.Add(new CrewWork.Job{Equipment=co,Provider=provider,Offer=offer,Worker=actor});
        }
        foreach(var job in offers.OrderBy(j=>CrewWork.DutyRank(actor,j.Offer.Duty)).ThenBy(j=>j.Equipment.strID,StringComparer.Ordinal))
        {
            var offer=job.Offer;
            if(CrewWork.PreferredAvailable(actor,offer,c=>CrewWork.Idle(c)&&!assignments.ContainsKey(c.strID)&&!unavailable.Contains(c.strID))) continue;
            if(!CrewWork.Reservations.Acquire(job.Lease,CrewWork.Keys(job))) continue;
            job.Inputs=job.Equipment.GetCOsSafe(true).Select(c=>c.strID).ToArray();
            job.Seconds=CrewBalance.Duration(offer.Seconds,CrewSpecialities.Skilled(actor,offer.Skill));
            // SFF charges handling plus conservative walking time; no carried item is cloned.
            double travel=Travel(actor,offer.Target);
            // Return via the worker's abstract starting point; conservative and never a shortcut through a wall.
            if(offer.Cargo!=null) travel+=2*Travel(actor,offer.Cargo);
            if(!CrewBalance.Finite(travel)) { CrewWork.Reservations.Release(job.Lease); continue; }
            assignments[actor.strID]=job; busy[actor.strID]=job.Seconds+travel; break;
        }
    }
    private static void Drop(string id)
    { if(assignments.TryGetValue(id,out var j)) CrewWork.Reservations.Release(j.Lease); assignments.Remove(id); busy.Remove(id); }
    private static double Travel(CondOwner actor,CondOwner target)
    {
        var pathfinder=actor.Pathfinder;
        if(pathfinder==null)return double.PositiveInfinity;
        var position=target.GetPos("use");
        var tile=actor.ship.GetTileAtWorldCoords1(position.x,position.y,bAllowDocked:false);
        var saved=pathfinder.coDest; var savedTile=pathfinder.tilDest; var savedRange=pathfinder.fRangeGoal;
        try
        {
            var route=pathfinder.CheckGoal(tile,2,target,actor.HasAirlockPermission(false));
            return route.HasPath?route.PathLength*CrewBalance.WalkSecondsPerTile:double.PositiveInfinity;
        }
        finally { pathfinder.coDest=saved; pathfinder.tilDest=savedTile; pathfinder.fRangeGoal=savedRange; }
    }
    private static void TickMachines(Ship[] ships)
    {
        foreach(var ship in ships)
        {
            if(ship.Reactor!=null) ship.Reactor.GetComponent<FusionIC>()?.CatchUp();
            foreach(var co in consumers.Where(c=>c!=null && !c.bDestroyed && c.ship==ship))
            {
                var provider=CrewWork.Provider(co);
                bool supported=provider==null || provider is ICrewSkipProvider p && p.CanAdvance(co,out _);
                if(!supported || faulted.Contains(co.strID))
                { if(co.Pwr!=null)PowerEpoch.SetValue(co.Pwr,StarSystem.fEpoch); continue; }
                if(co.Pwr!=null)
                {
                    var last=(double)PowerEpoch.GetValue(co.Pwr);
                    if(StarSystem.fEpoch-last>=1)
                    {
                        try { RunPower.Invoke(co.Pwr,null); }
                        catch(Exception error) { faulted.Add(co.strID); if(provider!=null)CrewWork.Fault(co,error); }
                        finally { PowerEpoch.SetValue(co.Pwr,StarSystem.fEpoch); }
                    }
                }
                if(co.GasContainer!=null) co.GasContainer.Run();
            }
        }
    }
    internal static string Report(CondOwner actor) => completions.TryGetValue(actor.strID,out var count)?CrewWork.Message("skip_report",count):CrewWork.Message("skip_no_work");
    internal static void End() { Active=false; CrewWork.FinishSkip(); Managed=false; }
}

[HarmonyPatch(typeof(GUIFFWD),"FFWD")]
internal static class CrewSkipPatch
{
    private static void Prefix(UnityEngine.Transform ___tfCrew)=>CrewSkip.Begin(___tfCrew.GetComponentsInChildren<GUIFFWDRow>());
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)
    {
        var native=AccessTools.Method(typeof(StarSystem),nameof(StarSystem.Update),new[]{typeof(double)});
        var adapter=AccessTools.Method(typeof(CrewSkip),"Advance"); int replaced=0;
        foreach(var i in code) { if(i.Calls(native)) { i.opcode=OpCodes.Call; i.operand=adapter; replaced++; } yield return i; }
        if(replaced!=1) throw new InvalidOperationException("Native SFF clock boundary changed.");
    }
    private static void Finalizer()=>CrewSkip.End();
}
[HarmonyPatch(typeof(GUIFFWDRow),nameof(GUIFFWDRow.ApplyEffects))]
internal static class CrewSkipEffects
{
    private static void Postfix(GUIFFWDRow __instance,ref string __result) { if(CrewSkip.Managed) __result+="\n"+CrewSkip.Report(__instance.CO); }
}
[HarmonyPatch(typeof(GUIFFWD),"UndamageParts")]
internal static class CrewSkipRepairs
{
    private static void Prefix(ref double fAmount) { if(CrewSkip.Managed) fAmount=CrewSkip.RepairHours*2.25; }
}
