using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HarmonyLib;
using Phobos.Ostranauts.Framework.Persistence;
using UnityEngine;

namespace Phobos.Ostranauts.Framework.Crew;

/// <summary>Main-thread bridge. Native WorkManager owns scheduling and movement; providers own gameplay.</summary>
public static class CrewWork
{
    internal const string WorkId = "PhobosCrewWork";
    private static readonly Dictionary<string,ICrewWorkProvider> providers = new(StringComparer.Ordinal);
    private static readonly Dictionary<string,StandingOrder> orders = new(StringComparer.Ordinal);
    internal static readonly Dictionary<Task2,Job> Jobs = new();
    internal static readonly Dictionary<Interaction,Job> Active = new();
    private static readonly Dictionary<string,string> notices = new(StringComparer.Ordinal);
    internal static readonly WorkReservations Reservations = new();
    private static float nextScan;
    private static CrewWorkContext? executing;
    public static CondOwner? Actor => executing?.Actor;
    public static bool IsExecuting => executing != null;
    public static event Action? SkipStarting;
    public static IEnumerable<ICrewWorkProvider> Providers => providers.Values;
    public static string Message(string key, params object[] args) => Text.Get("Crew." + key, args);
    internal sealed class Job
    {
        internal CondOwner Equipment = null!;
        internal ICrewWorkProvider Provider = null!;
        internal CrewWorkOffer Offer = null!;
        internal Task2 Task = null!;
        internal CondOwner? Worker;
        internal Interaction? Interaction;
        internal Interaction? Pickup;
        internal string Lease = Guid.NewGuid().ToString("N");
        internal double Seconds;
        internal string[] Inputs = Array.Empty<string>();
    }
    public static void Register(ICrewWorkProvider provider)
    { if (providers.ContainsKey(provider.Id)) throw new ArgumentException("Duplicate crew-work provider."); providers.Add(provider.Id, provider); }
    public static void Unregister(string id) => providers.Remove(id);
    public static ICrewWorkProvider? Provider(CondOwner co) => providers.Values.FirstOrDefault(p => p.Supports(co));
    public static CondOwner? Resolve(string? id) => id != null && DataHandler.mapCOs != null && DataHandler.mapCOs.TryGetValue(id, out var c) && c != null && !c.bDestroyed ? c : null;
    internal static ObjectStateStore Store(CondOwner co, string name) => new(co.mapGUIPropMaps, name, FrameworkInfo.PluginId, 1);
    public static StandingOrder Order(CondOwner co)
    {
        if (orders.TryGetValue(co.strID, out var value)) return value;
        var status = Store(co, "crew-order").Read(out var fields);
        value = status == SavedStateStatus.Ready ? StandingOrder.Read(fields) : new StandingOrder { Protected = status != SavedStateStatus.Missing };
        value.Reload(Provider(co)?.RoutineResume(co) == true);
        orders.Add(co.strID, value); return value;
    }
    public static bool CanManage(CondOwner co) => CrewSim.coPlayer != null && co != null && !co.bDestroyed && co.ship != null &&
        CrewSim.system?.GetShipOwner(co.ship.strRegID) == CrewSim.coPlayer.strID;
    public static bool Configure(CondOwner co, Action<StandingOrder> edit)
    {
        if (!CanManage(co) || Order(co).Protected) return false;
        var next = StandingOrder.Read(Order(co).Save()); edit(next);
        next = StandingOrder.Read(next.Save());
        if (next.Protected || !Store(co, "crew-order").TryWrite(next.Save())) return false;
        Cancel(co); orders[co.strID] = next;
        if (next.Permission != WorkPermission.Enabled) Provider(co)?.Suspend(co);
        return true;
    }
    public static void SetPermission(CondOwner co, WorkPermission permission, string reason = "")
    {
        if (!Configure(co, o => {
            o.Permission = permission; o.StopReason=reason.Length>0?reason:permission==WorkPermission.Stopped?"manual":"";
            if(permission==WorkPermission.Enabled && Provider(co) is ICrewBoundProvider bound)o.Binding=bound.CaptureBinding(co,o);
        })) return;
    }
    public static void ManualStop(CondOwner co)
    { if (!IsExecuting && Provider(co) != null && Order(co).Permission != WorkPermission.Disabled && Order(co).Permission != WorkPermission.Stopped) SetPermission(co,WorkPermission.Stopped); }
    /// <summary>Consume a one-shot launch permission without stopping the admitted machine/flight.</summary>
    public static bool CompleteOnce(CondOwner co)
    {
        var next=StandingOrder.Read(Order(co).Save());
        next.Permission=WorkPermission.Suspended; next.StopReason="complete";
        next.Drain=false;
        if(next.Protected || !Store(co,"crew-order").TryWrite(next.Save()))return false;
        orders[co.strID]=next; return true;
    }
    public static string Status(CondOwner co)
    {
        var order = Order(co);
        if (order.Protected) return Message("protected");
        var active = Jobs.Values.FirstOrDefault(j => j.Equipment == co);
        var detail=notices.TryGetValue(co.strID,out var reason)?reason:Message("waiting");
        if(order.StopReason.Length>0 && order.Permission!=WorkPermission.Enabled)detail=Message("reason_"+order.StopReason)+"\n"+detail;
        return Message("order_status", Message(order.Permission.ToString()), active?.Worker?.FriendlyName ?? Message("unassigned"),
            active==null?detail:active.Offer.Label+"\n"+detail);
    }
    public static IEnumerable<CondOwner> Equipment(Ship ship) => DataHandler.mapCOs.Values.Where(c => c != null && !c.bDestroyed && c.ship == ship &&
        c.objCOParent == null && c.HasCond("IsInstalled") && Provider(c) != null).ToArray();
    public static IEnumerable<CondOwner> Stores(Ship ship) => DataHandler.mapCOs.Values.Where(c => c != null && !c.bDestroyed && c.ship == ship &&
        c.objContainer != null && !c.objContainer.Locked && !c.HasCond("IsInfiniteContainer") && !c.HasCond("IsHuman") &&
        c.objCOParent == null && Provider(c) == null).OrderBy(c => c.strID, StringComparer.Ordinal).ToArray();
    public static bool LocalAccess(CondOwner actor, CondOwner target, double range) => actor != null && target != null && actor.ship == target.ship &&
        (TileUtils.TileRange(actor.GetPos(), target.GetPos("use")) <= range || executing?.Skipping == true && executing.Actor == actor && executing.Equipment == target);
    public static bool Eligible(CondOwner actor, CrewWorkOffer offer, out string reason, bool checkRole = true, int? hour = null)
    {
        reason = Message("crew_unavailable");
        if (actor == null || actor.bDestroyed || !actor.bAlive || actor.HasCond("IsDead") || actor.HasCond("Unconscious") ||
            actor.HasCond("IsAIManual") || actor.HasCond("IsInCombat") || actor.HasCond("IsEmergencyOverride") || actor.Company == null ||
            actor.Company != CrewSim.coPlayer?.Company || actor.ship != offer.Target.ship || !actor.Company.mapRoster.TryGetValue(actor.strID, out var roster) ||
            actor.Company.GetShift(hour??StarSystem.nUTCHour, actor).nID != 2 || checkRole && !CrewSpecialities.Allowed(actor, offer.Role)) return false;
        int duty = Array.IndexOf(JsonCompanyRules.aDutiesNew, offer.Duty);
        if (duty < 0 || duty >= roster.aDutyLvls.Length || roster.aDutyLvls[duty] < JsonCompanyRules.nPriorityMin || roster.aDutyLvls[duty] > JsonCompanyRules.nPriorityMax) return false;
        if (actor.HasCond("DcPain03") || actor.HasCond("DcPain04")) { reason = Message("personal_needs"); return false; }
        foreach (var key in new[] { "TIsSleepingAny", "TIsSleepy", "TIsHungry", "TCanDrinkThirsty", "TIsSuffocatingManWalkEmerg" })
            if (DataHandler.GetCondTrigger(key)?.Triggered(actor) == true) { reason = Message("personal_needs"); return false; }
        // Exterior mission controls are onboard. Native paths retain airlock/EVA checks for actual travel.
        reason = ""; return true;
    }
    internal static bool Path(CondOwner actor, CondOwner target)
    {
        if (actor.ship != target.ship || target.HasCond("IsLocked") || target.objContainer?.Locked == true) return false;
        var ia = DataHandler.GetInteraction(WorkId);
        return ia != null && ia.Triggered(actor, target, bStats: false, bIgnoreItems: false, bCheckPath: true, bFetchItems: false);
    }
    internal static IEnumerable<string> Keys(Job j)
    {
        yield return "equipment:" + j.Equipment.strID;
        if (j.Offer.Cargo != null) yield return "item:" + j.Offer.Cargo.strID;
        if (j.Offer.Destination != null) yield return "capacity:" + j.Offer.Destination.strID;
        if (j.Offer.Origin != null) yield return "capacity:" + j.Offer.Origin.strID;
        yield return "capacity:" + j.Equipment.strID;
        foreach (var item in j.Equipment.GetCOsSafe(true)) yield return "item:" + item.strID;
    }
    internal static int DutyPriority(CondOwner actor, string duty)
    {
        int index = Array.IndexOf(JsonCompanyRules.aDutiesNew, duty);
        return index >= 0 && actor.Company?.mapRoster.TryGetValue(actor.strID, out var r) == true && index < r.aDutyLvls.Length &&
            r.aDutyLvls[index] >= JsonCompanyRules.nPriorityMin && r.aDutyLvls[index] <= JsonCompanyRules.nPriorityMax ? r.aDutyLvls[index] : int.MaxValue;
    }
    internal static bool Idle(CondOwner actor) => !actor.aQueue.Any(i => !i.bCancel && i.strName != "QuickWait" && i.strName != "Wait");
    internal static long DutyRank(CondOwner actor,string duty)=>(long)DutyPriority(actor,duty)*JsonCompanyRules.aDutiesNew.Length+Array.IndexOf(JsonCompanyRules.aDutiesNew,duty);
    internal static bool NativeWorkPrecedes(CondOwner actor,CrewWorkOffer offer)=>CrewSim.objInstance.workManager.GetAllTasks().Any(t=>
        t.strInteraction!=WorkId && (t.bManual || DutyRank(actor,t.strDuty)<DutyRank(actor,offer.Duty)) &&
        Resolve(t.strTargetCOID)?.ship==actor.ship);
    internal static void ManualTakeover(CondOwner actor, Interaction interaction)
    {
        if(!interaction.bManual || interaction.strName==WorkId)return;
        foreach(var job in Jobs.Values.Where(j=>j.Worker==actor).ToArray())Release(job,true);
    }
    internal static bool PreferredAvailable(CondOwner actor, CrewWorkOffer offer, Func<CondOwner,bool>? available = null) =>
        !CrewSpecialities.Skilled(actor, offer.Skill) && CrewSim.aCrew.Any(other => other != actor &&
            (available?.Invoke(other) ?? Idle(other)) && DutyPriority(other, offer.Duty) == DutyPriority(actor, offer.Duty) &&
            CrewSpecialities.Skilled(other, offer.Skill) && Eligible(other, offer, out _) && Path(other, offer.Target) && CrewLogistics.Prepare(other, offer));
    internal static void Notice(CondOwner co, string reason) => notices[co.strID] = reason;
    internal static void Fault(CondOwner co, Exception error)
    { Notice(co, Message("fault", error.Message)); SetPermission(co, WorkPermission.Suspended,"fault"); }
    public static void Poll()
    {
        if (CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading || CrewSim.coPlayer == null || CrewSim.Paused || CrewSkip.Active || Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + (float)CrewBalance.DiscoverySeconds;
        foreach(var actor in CrewSim.aCrew)
            foreach(var ia in actor.aQueue.Where(i=>i.strName.StartsWith(CrewSpecialities.StudyPrefix,StringComparison.Ordinal)))
                if(!CrewSpecialities.StudyReady(actor,ia.objThem))ia.bCancel=true;
        Reconcile();
        var ships = CrewSim.aCrew.Where(c => c != null && c.ship != null).Select(c => c.ship).Distinct().ToArray();
        foreach (var ship in ships) foreach (var co in Equipment(ship)) Discover(co);
    }
    private static void Discover(CondOwner co)
    {
        if (!CanManage(co) || Jobs.Values.Any(j => j.Equipment == co)) return;
        var o = Order(co); if (o.Protected || o.Permission != WorkPermission.Enabled) return;
        var provider = Provider(co)!;
        try
        {
            var offer = provider.Next(co, o, out var reason); notices[co.strID] = reason;
            if (offer == null || !CrewBalance.Finite(offer.Seconds) || offer.Seconds <= 0) return;
            var task = new Task2 { strName = WorkId + "." + co.strID, strInteraction = WorkId, strTargetCOID = offer.Target.strID,
                strDuty = offer.Duty, aOwnerIDs = new[] { CrewSim.coPlayer.strID }, bManual = false };
            var job = new Job { Equipment = co, Provider = provider, Offer = offer, Task = task };
            if (CrewSim.objInstance.workManager.AddTask(task)) Jobs.Add(task, job);
        }
        catch (Exception e) { Fault(co, e); }
    }
    internal static bool Admit(Job j, CondOwner actor, Interaction ia)
    {
        if (Order(j.Equipment).Permission != WorkPermission.Enabled || !CanManage(j.Equipment) || !Eligible(actor, j.Offer, out var reason)) return false;
        if (!Path(actor, j.Offer.Target) || !CrewLogistics.Prepare(actor, j.Offer) || PreferredAvailable(actor, j.Offer) || !Reservations.Acquire(j.Lease, Keys(j))) return false;
        j.Worker = actor; j.Interaction = ia;
        j.Inputs=j.Equipment.GetCOsSafe(true).Select(c=>c.strID).ToArray();
        j.Seconds = CrewBalance.Duration(j.Offer.Seconds, CrewSpecialities.Skilled(actor, j.Offer.Skill));
        ia.bManual=false;
        ia.fDuration = ia.fDurationOrig = j.Seconds / 3600; ia.strTitle = j.Offer.Label;
        Active[ia] = j; return true;
    }
    internal static void QueuePickup(Job j)
    {
        var item = j.Offer.Cargo;
        if (item == null || item.RootParent() == j.Worker || j.Interaction == null) return;
        var pickup = DataHandler.GetInteraction("PickupItem");
        pickup.bManual=false;
        pickup.AddDependent(j.Interaction); j.Pickup = pickup; j.Worker!.QueueInteraction(item, pickup);
    }
    internal static bool Complete(Job j, bool skipping = false, int? workHour = null)
    {
        if (j.Worker == null || Order(j.Equipment).Permission != WorkPermission.Enabled || !Eligible(j.Worker, j.Offer, out var reason,hour:workHour)) return false;
        if(j.Offer.Cargo==null && j.Inputs.Except(j.Equipment.GetCOsSafe(true).Select(c=>c.strID),StringComparer.Ordinal).Any())
        { Notice(j.Equipment,Message("cargo_blocked")); return false; }
        executing = new CrewWorkContext(j.Worker, j.Equipment, Order(j.Equipment), skipping);
        try
        {
            bool done = j.Offer.Cargo != null ? CrewLogistics.Deliver(executing, j.Offer, out reason) : j.Provider.Complete(executing, j.Offer, out reason);
            notices[j.Equipment.strID] = reason;
            if (done) CrewSpecialities.Credit(j.Worker, j.Offer.Skill, j.Seconds, false);
            return done;
        }
        catch (Exception e) { Fault(j.Equipment, e); return false; }
        finally { executing = null; }
    }
    internal static void Release(Job job, bool removeTask)
    {
        Reservations.Release(job.Lease);
        if (job.Interaction != null) { Active.Remove(job.Interaction); job.Interaction.bCancel = true; }
        if (job.Pickup != null) job.Pickup.bCancel = true;
        Jobs.Remove(job.Task);
        if (removeTask) CrewSim.objInstance?.workManager?.RemoveTask(job.Task);
    }
    private static void Reconcile()
    {
        var all = CrewSim.objInstance.workManager.GetAllTasks();
        foreach (var j in Jobs.Values.ToArray())
        {
            if (j.Equipment == null || j.Equipment.bDestroyed || Order(j.Equipment).Permission != WorkPermission.Enabled ||
                !all.Contains(j.Task) || j.Interaction != null && (j.Interaction.bCancel || j.Worker == null || !Eligible(j.Worker, j.Offer, out _))) Release(j, true);
            else if(j.Worker==null)
            {
                var eligible=CrewSim.aCrew.Where(c=>Eligible(c,j.Offer,out _)).ToArray();
                Notice(j.Equipment,eligible.Length==0?Message("crew_unavailable"):
                    !eligible.Any(c=>Path(c,j.Offer.Target)&&CrewLogistics.Prepare(c,j.Offer))?Message("access_blocked"):Message("busy"));
            }
        }
        // Generated jobs are transient. A native saved task without its live reservation is rebuilt from intent.
        foreach (var task in all.Where(t => t.strInteraction == WorkId && !Jobs.ContainsKey(t)).ToArray()) CrewSim.objInstance.workManager.RemoveTask(task);
    }
    private static void Cancel(CondOwner co) { foreach (var j in Jobs.Values.Where(j => j.Equipment == co).ToArray()) Release(j, true); }
    internal static void StartSkip()
    {
        foreach (var j in Jobs.Values.ToArray()) Release(j, true);
        SkipStarting?.Invoke();
        foreach (var p in providers.Values.OfType<ICrewSkipProvider>()) p.BeforeSkip();
        foreach(var co in DataHandler.mapCOs.Values.Where(c=>c!=null&&!c.bDestroyed&&c.ship!=null&&Provider(c)!=null).ToArray())
            if(Order(co).Permission==WorkPermission.Enabled && (Provider(co) is not ICrewSkipProvider adapter || !adapter.CanAdvance(co,out _)))
                SetPermission(co,WorkPermission.Suspended,"skip");
    }
    internal static void FinishSkip() { foreach (var p in providers.Values.OfType<ICrewSkipProvider>()) p.AfterSkip(); }
    internal static void Reset()
    { Jobs.Clear(); Active.Clear(); orders.Clear(); notices.Clear(); Reservations.Clear(); executing = null; nextScan = 0; CrewSpecialities.Reset(); }
}

[HarmonyPatch(typeof(WorkManager), "CollectTasks")]
internal static class CrewTaskFilter
{
    [HarmonyPriority(Priority.Last)]
    private static void Postfix(CondOwner co, ref List<Task2> __result)
    {
        __result.RemoveAll(t => t.strInteraction == CrewWork.WorkId && (!CrewWork.Jobs.TryGetValue(t, out var j) || !CrewWork.Eligible(co, j.Offer, out _)));
    }
}
[HarmonyPatch(typeof(WorkManager), "FinalizeTask")]
internal static class CrewTaskClaim
{
    private static bool Prefix(CondOwner co, Task2 task, Interaction iact, ref Task2? __result)
    {
        if (task.strInteraction != CrewWork.WorkId) return true;
        if (CrewWork.Jobs.TryGetValue(task, out var j) && CrewWork.Admit(j, co, iact)) return true;
        __result = null; return false;
    }
    private static void Postfix(Task2 task, Task2? __result)
    {
        if (!CrewWork.Jobs.TryGetValue(task, out var j)) return;
        if (__result == task) CrewWork.QueuePickup(j); else if (j.Worker != null) CrewWork.Release(j, false);
    }
}
[HarmonyPatch(typeof(Interaction), nameof(Interaction.ApplyEffects))]
internal static class CrewTaskFinish
{
    private static void Postfix(Interaction __instance, bool isCancelIa)
    {
        if (!CrewWork.Active.TryGetValue(__instance, out var j)) return;
        try { if (!isCancelIa && !__instance.bCancel) CrewWork.Complete(j); }
        finally { CrewWork.Release(j, false); }
    }
}
