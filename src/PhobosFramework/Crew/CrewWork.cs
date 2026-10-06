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
    // Equipment whose first task since Enable has been announced to the native task list; later
    // re-adds are quiet so continuing an order never interrupts other crew members' study.
    private static readonly HashSet<string> announced = new(StringComparer.Ordinal);
    private static readonly Dictionary<string,int> failures = new(StringComparer.Ordinal);
    private static readonly Dictionary<string,double> nextAttempt = new(StringComparer.Ordinal);
    private static readonly Dictionary<string,string> retryReason = new(StringComparer.Ordinal);
    // Footprint counts for performance captures (Framework 0.104.0).
    internal static int OrderCount => orders.Count;
    internal static int RetryRecords => notices.Count + announced.Count + failures.Count + nextAttempt.Count + retryReason.Count;
    private static bool worldReady;
    public static CondOwner? Actor => executing?.Actor;
    public static bool IsExecuting => executing != null;
    public static bool AllCrewAboard(Ship ship) => CrewRoster.AllAboard(ship);
    /// <summary>The player's loaded crew, from the same company roster the game's time skip uses (Framework 0.84.0).</summary>
    public static IReadOnlyList<CondOwner> Crew() => CrewRoster.Members();
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
        // Reservation keys, rebuilt once per step: the filter asks for them for every crew member.
        internal string[]? KeysNow; internal long KeysStep = long.MinValue;
        // Crew upkeep (Framework 0.111.0): a ship-wide job at this machine, allowed by the upkeep switches instead of
        // the machine's own order, with no provider. Created is real time, so a task nobody takes can be withdrawn.
        internal UpkeepKind? Upkeep; internal float Created;
    }
    /// <summary>Whether a job may still run: its machine's order is enabled, or its upkeep switch is on.</summary>
    internal static bool Allowed(Job j) => j.Upkeep != null ? Upkeep.Enabled(j.Upkeep.Value) : Order(j.Equipment).Permission == WorkPermission.Enabled;
    // Several orders may target one store (a rack feeding a tray, a tray feeding a reclaimer); the
    // native default of one task per target and action would silently drop the later ones.
    internal const int TasksPerTarget = 16;
    private static ICrewWorkProvider[] providerList = Array.Empty<ICrewWorkProvider>();
    public static void Register(ICrewWorkProvider provider)
    { if (providers.ContainsKey(provider.Id)) throw new ArgumentException("Duplicate crew-work provider."); providers.Add(provider.Id, provider); providerList = providers.Values.ToArray(); }
    public static void Unregister(string id) { providers.Remove(id); providerList = providers.Values.ToArray(); }
    /// <summary>The provider that supports an object. A plain loop: this runs for every root object of every crew
    /// ship at each discovery pass, and one provider (Auto Nav) reads a live condition, so no memo by definition.</summary>
    public static ICrewWorkProvider? Provider(CondOwner co)
    {
        var current = providerList;
        for (int i = 0; i < current.Length; i++) if (current[i].Supports(co)) return current[i];
        return null;
    }
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
        Cancel(co); orders[co.strID] = next; Forget(co.strID);
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
        var active = Jobs.Values.FirstOrDefault(j => j.Equipment == co && j.Upkeep == null);
        var detail=notices.TryGetValue(co.strID,out var reason)?reason:Message("waiting");
        if(order.StopReason.Length>0 && order.Permission!=WorkPermission.Enabled)detail=Message("reason_"+order.StopReason)+"\n"+detail;
        return Message("order_status", Message(order.Permission.ToString()), active?.Worker?.FriendlyName ?? Message("unassigned"),
            active==null?detail:active.Offer.Label+"\n"+detail);
    }
    public static OrderStatus ReadStatus(CondOwner co)
    {
        var order=Order(co);var provider=Provider(co);var job=Jobs.Values.FirstOrDefault(j=>j.Equipment==co&&j.Upkeep==null);
        string work=provider?.Recipes(co).Contains(order.Recipe)==true?provider.RecipeLabel(order.Recipe):Message("choose_work");
        string worker=job?.Worker?.FriendlyName??Message("unassigned");
        if(order.Protected)return new(OrderState.Blocked,work,worker,Message("protected"));
        if(order.Permission==WorkPermission.Stopped)return new(OrderState.Stopped,work,worker,Message("reason_"+(order.StopReason.Length>0?order.StopReason:"manual")));
        if(provider?.Recipes(co).Contains(order.Recipe)!=true)return new(OrderState.NeedsSetup,work,worker,Message("choose_work"));
        if(order.Permission==WorkPermission.Disabled)return new(OrderState.Disabled,work,worker,"");
        if(order.Permission==WorkPermission.Suspended)return new(OrderState.Stopped,work,worker,order.StopReason.Length>0?Message("reason_"+order.StopReason):Message("Suspended"));
        if(job!=null)return new(job.Worker!=null?OrderState.Running:OrderState.Waiting,job.Offer.Label,worker,job.Worker==null?Message("crew_unavailable"):"");
        string detail=notices.TryGetValue(co.strID,out var reason)?reason:"";
        OrderState state;
        try{state=provider is ICrewOrderPresentation presentation?presentation.Activity(co,order):OrderState.Waiting;}
        catch{state=OrderState.Blocked;detail=Message("protected");}
        return new(state,work,worker,detail);
    }
    public static IEnumerable<CondOwner> Equipment(Ship ship) => Controls.ShipEquipment.Read(ship,
        c => c.HasCond("IsInstalled") && Provider(c) != null);
    public static IEnumerable<CondOwner> Stores(Ship ship) => Controls.ShipEquipment.Read(ship, IsStore).OrderBy(c => c.strID, StringComparer.Ordinal).ToArray();
    /// <summary>A finite, unlocked native container that is not a person or provider-owned equipment (unless its provider
    /// says it still counts, <see cref="ICrewStoreEquipment"/>), and that the
    /// <c>stores</c> pack counts as a store (Framework 0.116.0: never a weapon's magazine, a charger or a toilet).
    /// Shared by crew hauling and machine storage routes; callers still check ship and access.</summary>
    public static bool IsStore(CondOwner? c) => c != null && !c.bDestroyed && c.objCOParent == null &&
        c.objContainer != null && !c.objContainer.Locked && !c.HasCond("IsInfiniteContainer") && !c.HasCond("IsHuman") &&
        (Provider(c) is not { } provider || provider is ICrewStoreEquipment store && store.CountsAsStore(c)) && !Inventory.StoreRules.NotAStore(c);
    public static bool LocalAccess(CondOwner actor, CondOwner target, double range) => actor != null && target != null && actor.ship == target.ship &&
        (Reach(actor, target) <= range || executing?.Skipping == true && executing.Actor == actor && executing.Equipment == target);
    /// <summary>How far a crew member stands from a machine's use point, tile to tile, exactly as the game measures an
    /// interaction's range (Framework 0.96.0; owner report, 5 October 2026). Measuring from the crew member's exact
    /// position instead could come out a tile further than the game's own figure, so a crew member the game had already
    /// judged within reach of Control Panel was refused, silently, and the panel never opened.</summary>
    public static int Reach(CondOwner actor, CondOwner target)
    {
        var from = actor.GetPos(); var to = target.GetPos("use");
        var ship = actor.ship;
        var standing = ship?.GetTileAtWorldCoords1(from.x, from.y, bAllowDocked: true);
        var use = ship?.GetTileAtWorldCoords1(to.x, to.y, bAllowDocked: true);
        return standing != null && use != null ? TileUtils.TileRange(standing, use) : TileUtils.TileRange(from, to);
    }
    public static bool Eligible(CondOwner actor, CrewWorkOffer offer, out string reason, bool checkRole = true, int? hour = null)
    {
        reason = Message("crew_unavailable");
        if (actor == null || actor.bDestroyed || actor.aQueue == null || !actor.bAlive || actor.HasCond("IsDead") || actor.HasCond("Unconscious") ||
            actor.HasCond("IsAIManual") || actor.HasCond("IsInCombat") || actor.HasCond("IsEmergencyOverride") || actor.Company == null ||
            actor.Company != CrewSim.coPlayer?.Company || actor.ship != offer.Target.ship || actor.Company.mapRoster == null ||
            !actor.Company.mapRoster.TryGetValue(actor.strID, out var roster) || roster?.aDutyLvls == null ||
            actor.Company.GetShift(hour??StarSystem.nUTCHour, actor).nID != 2 || checkRole && !CrewSpecialities.Allowed(actor, offer.Role) ||
            offer.ExcludedActor.Length > 0 && offer.ExcludedActor == actor.strID) return false;
        int duty = Array.IndexOf(JsonCompanyRules.aDutiesNew, offer.Duty);
        if (duty < 0 || duty >= roster.aDutyLvls.Length || roster.aDutyLvls[duty] < JsonCompanyRules.nPriorityMin || roster.aDutyLvls[duty] > JsonCompanyRules.nPriorityMax) return false;
        // Needs, pain and sleep are the game's business: a painted job has no such gate either, and the
        // crew AI already puts eating, drinking, rest and emergencies ahead of work through its pledges.
        // Exterior mission controls are onboard. Native paths retain airlock/EVA checks for actual travel.
        reason = ""; return true;
    }
    /// <summary>A fresh native path check (an A* search); the claim uses this directly.</summary>
    internal static bool Path(CondOwner actor, CondOwner target)
    {
        if (actor.ship != target.ship || target.HasCond("IsLocked") || target.objContainer?.Locked == true) return false;
        return Reachable(actor, target, WorkId);
    }
    /// <summary>The game's own pickup that fetches a haul's cargo, queued ahead of the delivery.</summary>
    internal const string PickupId = "PickupItem";
    /// <summary>Whether a crew member can walk to a haul's cargo at the reach of the game's own pickup: one tile from the
    /// store's use point, where our own action allows two. Checked at our action's reach, a claim was admitted that the
    /// pickup then could not make, so the crew member set off for the machine empty-handed (Framework 0.126.1).</summary>
    internal static bool CanPickUp(CondOwner actor, CondOwner item) =>
        actor.ship == item.ship && !item.HasCond("IsLocked") && Reachable(actor, item, PickupId);
    /// <summary>Whether a crew member could walk to do one of the game's actions at a target, at that action's own reach,
    /// without disturbing where they are walking now. The game's path check leaves the walk target on the checked place
    /// whenever its same-frame memory already holds the answer (Pathfinder.CheckGoal returns early without putting it
    /// back). Two waiting orders that share a store checked the same place twice each discovery pass, so the first crew
    /// member on the roster never counted as arrived at whatever they were doing and stood still (owner report, 6 October
    /// 2026). The game's own task search clears that memory around each check; this does the same and puts the walk
    /// target back (Framework 0.126.1).</summary>
    internal static bool Reachable(CondOwner actor, CondOwner target, string action)
    {
        var ia = DataHandler.GetInteraction(action);
        if (ia == null) return false;
        Diagnostics.Performance.Increment(Diagnostics.Performance.CrewPathChecks);
        var walker = actor.Pathfinder;
        if (walker == null) return ia.Triggered(actor, target, bStats: false, bIgnoreItems: false, bCheckPath: true, bFetchItems: false);
        var tile = walker.tilDest; float range = walker.fRangeGoal; var destination = walker.coDest;
        walker.ResetMemory();
        try { return ia.Triggered(actor, target, bStats: false, bIgnoreItems: false, bCheckPath: true, bFetchItems: false); }
        finally { walker.tilDest = tile; walker.fRangeGoal = range; walker.coDest = destination; walker.ResetMemory(); }
    }
    // The native task search asks about every Phobos task for every crew member on each AI turn; within one step
    // nothing moves, so a path or preparation answer is reused for that step and forgotten with it. The claim
    // itself (Admit) always checks fresh.
    private static readonly Processing.StepMemo<(CondOwner, CondOwner), bool> paths = new();
    private static readonly Processing.StepMemo<(CondOwner, CrewWorkOffer), bool> preparations = new();
    internal static bool PathThisStep(CondOwner actor, CondOwner target)
    {
        long step = Processing.NativeSteps.Frame;
        if (paths.TryGet(step, (actor, target), out bool known)) return known;
        bool result = Path(actor, target); paths.Set(step, (actor, target), result); return result;
    }
    internal static bool PreparedThisStep(CondOwner actor, CrewWorkOffer offer)
    {
        long step = Processing.NativeSteps.Frame;
        if (preparations.TryGet(step, (actor, offer), out bool known)) return known;
        bool result = CrewLogistics.Prepare(actor, offer); preparations.Set(step, (actor, offer), result); return result;
    }
    internal static IEnumerable<string> Keys(Job j)
    {
        long step = Processing.NativeSteps.Frame;
        if (j.KeysNow != null && j.KeysStep == step) return j.KeysNow;
        // An upkeep session holds only its own key: the machine's order, feed and trays stay free for standing orders.
        // Housekeeping (0.113.0) holds the item it carries and room in its store, as any haul does, and nothing else.
        if (j.Upkeep == UpkeepKind.Housekeeping)
        { j.KeysNow = new[] { "item:" + j.Offer.Cargo?.strID, "capacity:" + j.Offer.Destination?.strID }; j.KeysStep = step; return j.KeysNow; }
        if (j.Upkeep != null) { j.KeysNow = new[] { "upkeep:" + j.Equipment.strID }; j.KeysStep = step; return j.KeysNow; }
        var keys = new List<string> { "equipment:" + j.Equipment.strID };
        if (j.Offer.Cargo != null) keys.Add("item:" + j.Offer.Cargo.strID);
        if (j.Offer.Destination != null) keys.Add("capacity:" + j.Offer.Destination.strID);
        if (j.Offer.Origin != null) keys.Add("capacity:" + j.Offer.Origin.strID);
        keys.Add("capacity:" + j.Equipment.strID);
        foreach (var item in j.Equipment.GetCOsSafe(true)) keys.Add("item:" + item.strID);
        j.KeysNow = keys.ToArray(); j.KeysStep = step;
        return j.KeysNow;
    }
    internal static int DutyPriority(CondOwner actor, string duty)
    {
        int index = Array.IndexOf(JsonCompanyRules.aDutiesNew, duty);
        return index >= 0 && actor.Company?.mapRoster?.TryGetValue(actor.strID, out var r) == true && r?.aDutyLvls != null && index < r.aDutyLvls.Length &&
            r.aDutyLvls[index] >= JsonCompanyRules.nPriorityMin && r.aDutyLvls[index] <= JsonCompanyRules.nPriorityMax ? r.aDutyLvls[index] : int.MaxValue;
    }
    /// <summary>Nothing live queued; cancelled actions and the game's own Wait/QuickWait idling do not count.</summary>
    internal static bool Idle(CondOwner actor) => actor.aQueue != null && !actor.aQueue.Any(i => i == null || !i.bCancel && i.strName != "QuickWait" && i.strName != "Wait");
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
        !CrewSpecialities.Skilled(actor, offer.Skill) && CrewRoster.Members().Any(other => other != actor &&
            (available?.Invoke(other) ?? Idle(other)) && DutyPriority(other, offer.Duty) == DutyPriority(actor, offer.Duty) &&
            CrewSpecialities.Skilled(other, offer.Skill) && Eligible(other, offer, out _) && PathThisStep(other, offer.Target) && PreparedThisStep(other, offer));
    internal static void Notice(CondOwner co, string reason) => notices[co.strID] = reason;
    private static void Forget(string id) { announced.Remove(id); failures.Remove(id); nextAttempt.Remove(id); retryReason.Remove(id); }
    /// <summary>Seconds of game time before a failed step is offered again, or zero.</summary>
    public static double RetrySeconds(CondOwner co) => nextAttempt.TryGetValue(co.strID, out var at) ? Math.Max(0, at - StarSystem.fEpoch) : 0;
    internal static bool RetryPending(CondOwner co)
    {
        if (RetrySeconds(co) <= 0) return false;
        Notice(co, Message("retry", RetrySeconds(co), retryReason.TryGetValue(co.strID, out var why) ? why : ""));
        return true;
    }
    private static void RecordOutcome(CondOwner co, bool done, string reason)
    {
        if (done) { failures.Remove(co.strID); nextAttempt.Remove(co.strID); retryReason.Remove(co.strID); return; }
        int count = (failures.TryGetValue(co.strID, out var n) ? n : 0) + 1;
        failures[co.strID] = count; nextAttempt[co.strID] = StarSystem.fEpoch + CrewBalance.RetryDelay(count); retryReason[co.strID] = reason;
    }
    // The game interrupts every on-shift crew member's study whenever its task total rises. A standing
    // order is one job: announce its first task after Enable like a painted job, then keep re-adds quiet.
    private static void Announce(CondOwner co, WorkManager manager)
    {
        if (announced.Add(co.strID)) return;
        try { manager.nTotalTasks = Math.Max(manager.nTotalTasks, CrewDiagnostics.TaskCount(manager)); }
        catch (Exception e) { Notice(co, Message("fault", e.Message)); }
    }
    /// <summary>The same once-per-Enable rule for a job that belongs to no single machine (crew upkeep).</summary>
    internal static void AnnounceOnce(string key, WorkManager manager)
    {
        if (announced.Add(key)) return;
        try { manager.nTotalTasks = Math.Max(manager.nTotalTasks, CrewDiagnostics.TaskCount(manager)); } catch { }
    }
    internal static void ForgetAnnouncement(string key) => announced.Remove(key);
    /// <summary>Everything a claim would check, so an unclaimable task is withheld from the native
    /// search instead of ending it and hiding lower-priority vanilla work. Cheap facts first, the path
    /// searches last, each reused within the step.</summary>
    internal static bool Admissible(CondOwner actor, Job j) => Reservations.Available(j.Lease, Keys(j)) && Eligible(actor, j.Offer, out _) &&
        Upkeep.Suits(j.Upkeep, j.Offer, actor) && PathThisStep(actor, j.Offer.Target) && PreparedThisStep(actor, j.Offer) &&
        !(Upkeep.SkilledFirst(j.Upkeep) && PreferredAvailable(actor, j.Offer));
    internal static void Fault(CondOwner co, Exception error)
    { Notice(co, Message("fault", error.Message)); SetPermission(co, WorkPermission.Suspended,"fault"); }
    public static void Poll()
    {
        if (CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading || CrewSim.objInstance.workManager == null ||
            DataHandler.mapCOs == null || CrewSim.coPlayer == null || CrewSim.Paused || CrewSkip.Active || Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + (float)CrewBalance.DiscoverySeconds;
        using var measurement = Diagnostics.Performance.Measure(Diagnostics.Performance.CrewDiscovery);
        if (!worldReady) { worldReady = true; CrewStudy.WorldReady(CrewSpecialities.All, FrameworkLifecycle.Log); }
        var crew = CrewRoster.Members();
        Reconcile(crew);
        var ships = crew.Select(c => c.ship).Distinct().ToArray();
        // Crew upkeep (0.111.0) plans after the orders already waiting; it returns at once while its switches are off.
        try { Upkeep.Plan(crew, ships); } catch (Exception e) { FrameworkLifecycle.Log(Message("fault", e.Message)); }
        // Framework 0.106.0: the full ship scan loads saved orders, so it runs after a load and every
        // DiscoveryPlan.FullSeconds; in between only switched-on orders and just-replaced machines are visited.
        if (DiscoveryPlan.FullScanDue(Time.unscaledTime, nextFullScan))
        {
            nextFullScan = Time.unscaledTime + (float)DiscoveryPlan.FullSeconds; replaced.Clear();
            foreach (var ship in ships) foreach (var co in Equipment(ship)) Discover(co);
            return;
        }
        var ids = DiscoveryPlan.Quick(orders, replaced); replaced.Clear();
        foreach (var id in ids)
            if (Resolve(id) is CondOwner co && co.objCOParent == null && co.HasCond("IsInstalled") && ships.Contains(co.ship) && Provider(co) != null)
                Discover(co);
    }
    private static float nextFullScan;
    // Objects a mode switch produced that a provider supports, for the next quick pass (Framework 0.106.0).
    private static readonly HashSet<string> replaced = new(StringComparer.Ordinal);
    internal static void Replaced(CondOwner? co)
    {
        if (co != null && !co.bDestroyed && co.strID != null && providerList.Length > 0 && Provider(co) != null) replaced.Add(co.strID);
    }
    private static void Discover(CondOwner co)
    {
        if (!CanManage(co) || Jobs.Values.Any(j => j.Equipment == co && j.Upkeep == null)) return;
        var o = Order(co); if (o.Protected || o.Permission != WorkPermission.Enabled) return;
        if (RetryPending(co)) return;
        var provider = Provider(co)!;
        try
        {
            var offer = provider.Next(co, o, out var reason); notices[co.strID] = reason;
            if (offer == null || !CrewBalance.Finite(offer.Seconds) || offer.Seconds <= 0) return;
            // No owner list: like a painted job, any crew member the game admits may take it. An owner
            // list would forbid everyone but the named person (Task2.GetOwnership).
            var task = new Task2 { strName = WorkId + "." + co.strID, strInteraction = WorkId, strTargetCOID = offer.Target.strID,
                strDuty = offer.Duty, bManual = false };
            var job = new Job { Equipment = co, Provider = provider, Offer = offer, Task = task };
            var manager = CrewSim.objInstance.workManager;
            if (manager.AddTask(task, TasksPerTarget)) { Jobs.Add(task, job); Announce(co, manager); }
        }
        catch (Exception e) { Fault(co, e); }
    }
    internal static bool Admit(Job j, CondOwner actor, Interaction ia)
    {
        if (!Allowed(j) || !CanManage(j.Equipment) || !Eligible(actor, j.Offer, out var reason) || !Upkeep.Suits(j.Upkeep, j.Offer, actor)) return false;
        if (!Path(actor, j.Offer.Target) || !CrewLogistics.Prepare(actor, j.Offer) || Upkeep.SkilledFirst(j.Upkeep) && PreferredAvailable(actor, j.Offer) ||
            !Reservations.Acquire(j.Lease, Keys(j))) return false;
        j.Worker = actor; j.Interaction = ia;
        // Practice is for the unskilled and takes its full length (0.113.0).
        j.Seconds = CrewBalance.Duration(j.Offer.Seconds, j.Upkeep != UpkeepKind.Practice && CrewSpecialities.Skilled(actor, j.Offer.Skill));
        ia.bManual=false;
        ia.fDuration = ia.fDurationOrig = j.Seconds / 3600; ia.strTitle = j.Offer.Label;
        Active[ia] = j; return true;
    }
    /// <summary>Queues the game's pickup ahead of a haul's delivery. False when the game refuses it on the spot (no way to
    /// the cargo, or the crew member cannot walk): it drops the pickup before the delivery is queued, so the delivery
    /// would otherwise go ahead with empty hands.</summary>
    internal static bool QueuePickup(Job j)
    {
        var item = j.Offer.Cargo;
        if (item == null || item.RootParent() == j.Worker || j.Interaction == null) return true;
        var pickup = DataHandler.GetInteraction(PickupId);
        if (pickup == null) return false;
        pickup.bManual=false;
        pickup.AddDependent(j.Interaction); j.Pickup = pickup;
        return j.Worker!.QueueInteraction(item, pickup);
    }
    /// <summary>The claim is withdrawn when its pickup was refused: the order says why and backs off (Framework 0.126.1).</summary>
    internal static void PickupRefused(Job j)
    {
        if (j.Upkeep == null)
        {
            string reason = Message("pickup_unreachable", j.Worker?.FriendlyName ?? "", j.Offer.Cargo?.strNameFriendly ?? "");
            Notice(j.Equipment, reason); RecordOutcome(j.Equipment, false, reason);
        }
        Release(j, true);
    }
    /// <summary>The worker no longer has the claimed step queued, although it was neither finished nor cancelled. The game
    /// drops a step that way when the crew member cannot reach its target or when the pickup ahead of it is abandoned
    /// (which takes the delivery with it), then puts its task back on the list still marked as claimed; the order then
    /// waited on that claim until the game was reloaded (Framework 0.126.1).</summary>
    private static bool Dropped(Job j) => j.Interaction != null && j.Worker?.aQueue != null && !j.Worker.aQueue.Contains(j.Interaction);
    private static void StepDropped(Job j)
    {
        if (j.Upkeep == null)
        {
            string reason = Message("step_dropped", j.Worker?.FriendlyName ?? "", j.Offer.Label);
            Notice(j.Equipment, reason); RecordOutcome(j.Equipment, false, reason);
        }
        Release(j, true);
    }
    internal static bool Complete(Job j, bool skipping = false, int? workHour = null)
    {
        if (j.Worker == null || !Allowed(j) || !Eligible(j.Worker, j.Offer, out var reason,hour:workHour)) return false;
        if (j.Upkeep != null)
        {
            try { return Upkeep.Finish(j.Worker, j.Equipment, j.Offer, j.Upkeep.Value, j.Seconds, skipping, out _); }
            catch (Exception e) { FrameworkLifecycle.Log(Message("fault", e.Message)); return false; }
        }
        // The provider checks the equipment's actual contents when it completes; a snapshot of the
        // contents at claim time would refuse work merely because a stack was tidied meanwhile.
        executing = new CrewWorkContext(j.Worker, j.Equipment, Order(j.Equipment), skipping);
        try
        {
            bool done = j.Offer.Cargo != null ? CrewLogistics.Deliver(executing, j.Offer, out reason) : j.Provider.Complete(executing, j.Offer, out reason);
            notices[j.Equipment.strID] = reason;
            RecordOutcome(j.Equipment, done, reason);
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
    private static void Reconcile(CondOwner[] crew)
    {
        var all = CrewSim.objInstance.workManager.GetAllTasks();
        foreach (var j in Jobs.Values.ToArray())
        {
            if (j.Equipment == null || j.Equipment.bDestroyed || !Allowed(j) ||
                !all.Contains(j.Task) || j.Interaction != null && (j.Interaction.bCancel || j.Worker == null || !Eligible(j.Worker, j.Offer, out _))) Release(j, true);
            else if (Dropped(j)) StepDropped(j);
            else if(j.Worker==null && j.Upkeep==null)
            {
                var eligible=crew.Where(c=>Eligible(c,j.Offer,out _)).ToArray();
                Notice(j.Equipment,eligible.Length==0?Message("crew_unavailable"):
                    !eligible.Any(c=>Path(c,j.Offer.Target)&&CrewLogistics.Prepare(c,j.Offer))?Message("access_blocked"):Message("busy"));
            }
        }
        // Generated jobs are transient. A native saved task without its live reservation is rebuilt from intent.
        foreach (var task in all.Where(t => t.strInteraction == WorkId && !Jobs.ContainsKey(t)).ToArray()) CrewSim.objInstance.workManager.RemoveTask(task);
    }
    private static void Cancel(CondOwner co) { foreach (var j in Jobs.Values.Where(j => j.Equipment == co && j.Upkeep == null).ToArray()) Release(j, true); }
    /// <summary>Before a time-skip: release transient jobs and suspend only the orders on the skipping
    /// crew's ships that the managed skip cannot advance. Orders elsewhere are not touched.</summary>
    internal static void StartSkip(IEnumerable<Ship> ships)
    {
        foreach (var j in Jobs.Values.ToArray()) Release(j, true);
        Upkeep.SkipStarting();
        SkipStarting?.Invoke();
        foreach (var p in providers.Values.OfType<ICrewSkipProvider>()) p.BeforeSkip();
        foreach(var ship in ships.Where(s=>s!=null).Distinct().ToArray()) foreach(var co in Equipment(ship).ToArray())
            if(Order(co).Permission==WorkPermission.Enabled && (Provider(co) is not ICrewSkipProvider adapter || !adapter.CanAdvance(co,out _)))
                SetPermission(co,WorkPermission.Suspended,"skip");
    }
    internal static void FinishSkip() { foreach (var p in providers.Values.OfType<ICrewSkipProvider>()) p.AfterSkip(); }
    internal static void Reset()
    {
        Jobs.Clear(); Active.Clear(); orders.Clear(); notices.Clear(); Reservations.Clear(); executing = null; nextScan = 0;
        nextFullScan = 0; replaced.Clear();
        announced.Clear(); failures.Clear(); nextAttempt.Clear(); retryReason.Clear(); worldReady = false; CrewSpecialities.Reset();
        Upkeep.Reset();
    }
}

[HarmonyPatch(typeof(WorkManager), "CollectTasks")]
internal static class CrewTaskFilter
{
    [HarmonyPriority(Priority.Last)]
    private static void Postfix(CondOwner co, ref List<Task2> __result)
    {
        if (CrewWork.Jobs.Count == 0 || __result == null) return;
        using var measurement = Phobos.Ostranauts.Framework.Diagnostics.Performance.Measure(Phobos.Ostranauts.Framework.Diagnostics.Performance.CrewTaskFilter);
        __result.RemoveAll(t => t.strInteraction == CrewWork.WorkId && (!CrewWork.Jobs.TryGetValue(t, out var j) || !CrewWork.Admissible(co, j)));
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
    private static void Postfix(Task2 task, ref Task2? __result)
    {
        if (!CrewWork.Jobs.TryGetValue(task, out var j)) return;
        if (__result != task) { if (j.Worker != null) CrewWork.Release(j, false); return; }
        // A refused pickup ends the claim here, so the game never queues the delivery and the crew member stays free.
        if (!CrewWork.QueuePickup(j)) { CrewWork.PickupRefused(j); __result = null; }
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

/// <summary>A machine replaced by a mode switch (damage, repair, installation) is visited on the next quick discovery
/// pass rather than waiting for the full scan (Framework 0.106.0).</summary>
[HarmonyLib.HarmonyPatch(typeof(CondOwner), nameof(CondOwner.ModeSwitch))]
internal static class CrewModeSwitchPatch
{
    private static void Postfix(CondOwner coNew) { try { CrewWork.Replaced(coNew); } catch { } try { Upkeep.Replaced(coNew); } catch { } try { CrewSkip.Replaced(coNew); } catch { } }
}

