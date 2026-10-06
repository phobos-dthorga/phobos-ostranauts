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
    // Crew-seconds spent on Phobos jobs (with travel) and crew-seconds on work shift during the skip.
    // The game's own repair allowance is scaled by the share that stayed free for ship repairs.
    private static double workedSeconds, availableSeconds;
    internal static double RepairShare => CrewBalance.RepairShare(workedSeconds, availableSeconds);
    // Compiled accessors: a skip steps every consumer once per simulated second, so no boxing or Invoke per step.
    private static readonly AccessTools.FieldRef<Powered, double> PowerEpoch = AccessTools.FieldRefAccess<Powered, double>("fUpdateLast");
    private static readonly Action<Powered> RunPower = AccessTools.MethodDelegate<Action<Powered>>(AccessTools.Method(typeof(Powered), "Run"));
    private static readonly AccessTools.FieldRef<Powered, CondTrigger> RechargeTrigger = AccessTools.FieldRefAccess<Powered, CondTrigger>("ctRecharge");
    /// <summary>Game seconds per skip step, the player's <c>TimeSkip/StepSeconds</c> (Framework 0.112.0).</summary>
    public static double StepSeconds { get; set; } = CrewBalance.DefaultSkipStepSeconds;
    /// <summary>One powered or gas-holding object on a skipping ship, classified once per skip.</summary>
    private sealed class Consumer
    {
        public CondOwner Co = null!;
        public bool Equipment, Recharger, Room;
        public double LastStepped;
    }
    private static readonly List<Consumer> stepped = new();
    private static readonly List<CondOwner> arrivals = new();
    private static readonly Dictionary<string,double> headStart = new(StringComparer.Ordinal);
    // Crew-ordered equipment per ship, read once per skip instead of scanning every object for each crew decision.
    private static readonly Dictionary<Ship,IReadOnlyList<CondOwner>> equipment = new();
    private static Ship[] skipping = Array.Empty<Ship>();
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
    // Footprint count for performance captures (Framework 0.104.0).
    internal static int Records => busy.Count + assignments.Count + completions.Count + unavailable.Count + nativeCare.Count + budgets.Count + faulted.Count + nextDecision.Count +
        stepped.Count + headStart.Count + equipment.Count;
    /// <summary>The Time-skip estimate (Framework 0.117.0), as rows the panel groups and the F3 command prints: each
    /// crew member's coming shifts as runs, and each enabled order as what it will do, what it waits for, or that it
    /// pauses for the skip.</summary>
    public sealed class SkipPreview
    {
        public readonly List<(string Name, string Availability, string Runs)> Crew = new();
        public readonly List<(string Id, string Name, string Group, string Detail)> Machines = new();
    }
    public const string WillRun = "will_run", Waits = "waits", Paused = "paused";
    public static readonly IReadOnlyList<string> PreviewGroups = new[] { WillRun, Waits, Paused };
    private static string ShiftWord(int shift) => Controls.ConsoleText.Get(shift == 2 ? "shift_working" : shift == 0 ? "shift_resting" : "shift_free");
    internal static SkipPreview PreviewRows(Ship ship, int hours)
    {
        var preview = new SkipPreview();
        hours = Math.Max(1, Math.Min(24, hours));
        var contexts = DataHandler.GetLoot("ACTFFWDContextPayloads")?.GetAllLootNames() ?? new List<string>();
        var members = CrewRoster.Members();
        foreach (var actor in members.Where(a => a.ship == ship))
        {
            var care = contexts.Select(id => DataHandler.GetInteraction(id)).FirstOrDefault(i => i != null && i.Triggered(actor, actor));
            string availability = care?.strTitle ?? Controls.ConsoleText.Get(actor.HasCond("IsAIManual") ? "autotask_off" : "autotask_on");
            var shifts = Enumerable.Range(0, hours).Select(i => actor.Company?.GetShift((StarSystem.nUTCHour + i) % 24, actor).nID ?? 1);
            string runs = ShiftRuns.Text(ShiftRuns.Compress(shifts), ShiftWord,
                (word, h) => Controls.ConsoleText.Get("shift_first", word, h), (word, h) => Controls.ConsoleText.Get("shift_then", word, h));
            preview.Crew.Add((Controls.ObjectPresentation.Name(actor), availability, runs));
        }
        foreach (var co in CrewWork.Equipment(ship).Where(c => CrewWork.Order(c).Permission == WorkPermission.Enabled))
        {
            var provider = CrewWork.Provider(co)!;
            string group = Paused, detail = CrewWork.Message("skip_wait");
            try
            {
                if (provider is ICrewSkipProvider p && !OrderConfiguration.Fields(co).HasFlag(OrderFields.Target))
                {
                    if (!p.CanAdvance(co, out var limit)) { group = Waits; detail = limit; }
                    else
                    {
                        var offer = provider.Next(co, CrewWork.Order(co), out var blocker);
                        if (offer == null) { group = Waits; detail = blocker; }
                        else if (!members.Any(a => CrewWork.Eligible(a, offer, out _))) { group = Waits; detail = offer.Label + " · " + CrewWork.Message("crew_unavailable"); }
                        else { group = WillRun; detail = Controls.ConsoleText.Get("skip_work", offer.Label, offer.Seconds / 60); }
                    }
                }
            }
            catch { group = Waits; detail = CrewWork.Message("protected"); }
            preview.Machines.Add((co.strID, Controls.ObjectPresentation.Name(co), group, detail));
        }
        return preview;
    }
    /// <summary>F3: <c>phobosframework skip [hours]</c>, the same estimate as text.</summary>
    internal static string Preview(Ship ship, int hours)
    {
        var preview = PreviewRows(ship, hours);
        var lines = new List<string> { CrewWork.Message("skip_preview") };
        foreach (var (name, availability, runs) in preview.Crew) lines.Add(name + ": " + availability + " · " + runs);
        foreach (string group in PreviewGroups)
        {
            var rows = preview.Machines.Where(m => m.Group == group).ToList();
            if (rows.Count == 0) continue;
            lines.Add(Controls.ConsoleText.Get("skip_group_" + group) + " (" + rows.Count + ")");
            foreach (var row in rows) lines.Add("  " + row.Name + ": " + row.Detail);
        }
        return string.Join("\n", lines);
    }

    internal static void Begin(IEnumerable<GUIFFWDRow> rows)
    {
        Managed=false; workedSeconds=availableSeconds=0; assignments.Clear(); busy.Clear(); completions.Clear(); unavailable.Clear(); budgets.Clear(); faulted.Clear(); headStart.Clear();
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
        CrewWork.StartSkip(crew.Select(c=>c.ship));
    }
    // Replace only the native clock call, retaining the surrounding native risk/events/report lifecycle.
    internal static void Advance(StarSystem system,double seconds)
    {
        var ships=crew.Select(c=>c.ship).Distinct().ToArray();
        skipping=ships; equipment.Clear();
        Managed=ships.Any(s=>SkipEquipment(s).Any(c=>CrewWork.Order(c).Permission==WorkPermission.Enabled));
        var powered=DataHandler.mapCOs.Values.Where(c=>c!=null&&!c.bDestroyed&&ships.Contains(c.ship)&&(c.Pwr!=null||c.GasContainer!=null)).ToArray();
        // Owner report (5 October 2026), Framework 0.99.0: with no crew order enabled the game jumped the whole skip in
        // one step, and the next power step then asked every machine for six hours at once. No room takes six hours
        // of a machine's heat in one step and no conduit holds six hours of its power, so every running machine stood
        // still for the skip. A skip is stepped whenever a machine's Start stands (the resume mark it already keeps),
        // so its power, heat and deliveries are its ordinary steps; with nothing running the game's own jump stays.
        // Owner report (6 October 2026), Framework 0.112.0: one-second steps with crew orders on froze the game for
        // minutes; every stepped skip now takes the player's step, 30 seconds by default.
        double stepSeconds=CrewBalance.SkipStep(Managed,powered.Any(Persistence.ResumeAfterLoad.Marked),StepSeconds);
        if(stepSeconds<=0) { equipment.Clear(); skipping=Array.Empty<Ship>(); system.Update(seconds); return; }
        Classify(powered);
        Active=true; double remaining=seconds, hourLeft=0; var initial=StarSystem.fEpoch; int previewHour=StarSystem.nUTCHour;
        try
        {
            while(remaining>1e-6)
            {
                using var stepMeasurement=Diagnostics.Performance.Measure(Diagnostics.Performance.SkipStep);
                if(!Managed)
                {
                    // Machines only: no crew job is assigned, charged or completed, and the game's own crew effects,
                    // repairs and report are untouched.
                    double machineStep=Math.Min(stepSeconds,remaining);
                    system.Update(machineStep); Cadence.AdvanceSkip(machineStep);
                    TickMachines(ships,stepSeconds);
                    // Crew upkeep (0.111.0) uses on-shift time without changing how the skip steps.
                    Upkeep.SkipStep(crew,ships,machineStep,null);
                    remaining-=machineStep; continue;
                }
                if(hourLeft<=1e-6) { SnapshotHour(crew,previewHour++); hourLeft=Math.Min(3600,remaining); }
                // Shared steps retain power competition and native gas/thermal limits. A job no longer ends its step
                // early (0.112.0): it finishes with the step it ends in and its spare seconds start the next job.
                double step=Math.Min(stepSeconds,Math.Min(CrewBalance.UntilHour(StarSystem.fEpoch),Math.Min(remaining,hourLeft)));
                using(Diagnostics.Performance.Measure(Diagnostics.Performance.SkipCrew))
                {
                    foreach(var id in assignments.Keys.ToArray())
                        if(!CrewWork.Eligible(assignments[id].Worker!,assignments[id].Offer,out _) || unavailable.Contains(id)) Drop(id);
                    foreach(var actor in crew) TryAssign(actor,ships);
                }
                int workHour=StarSystem.nUTCHour;
                // Unassigned on-shift crew spend the step natively (rest, repairs, study); who repairs
                // and how much is the game's own allowance, scaled afterwards by RepairShare.
                foreach(var actor in crew.Where(a=>!unavailable.Contains(a.strID)))
                {
                    availableSeconds+=step;
                    if(!assignments.ContainsKey(actor.strID))budgets[actor.strID].TrySpend(step,false);
                }
                system.Update(step); Cadence.AdvanceSkip(step);
                TickMachines(ships,stepSeconds);
                foreach(var actor in crew)
                {
                    if(!assignments.TryGetValue(actor.strID,out var job)) continue;
                    if(!CrewWork.Eligible(actor,job.Offer,out _,hour:workHour) || unavailable.Contains(actor.strID)) { Drop(actor.strID); continue; }
                    if(!budgets[actor.strID].TrySpend(step,true)) { Drop(actor.strID); continue; }
                    double left=busy[actor.strID];
                    workedSeconds+=Math.Min(step,Math.Max(0,left)); busy[actor.strID]=left-step;
                    if(left-step>1e-6) continue;
                    headStart[actor.strID]=step-Math.Max(0,left);
                    using(Diagnostics.Performance.Measure(Diagnostics.Performance.SkipCrew))
                        if(CrewWork.Complete(job,true,workHour)) completions[actor.strID]=completions.TryGetValue(actor.strID,out var n)?n+1:1;
                    Drop(actor.strID);
                }
                Upkeep.SkipStep(crew,ships,step,id=>assignments.ContainsKey(id)||unavailable.Contains(id));
                remaining-=step; hourLeft-=step;
            }
        }
        catch(Exception error)
        {
            foreach(var ship in ships) foreach(var co in SkipEquipment(ship)) CrewWork.Fault(co,error);
        }
        finally
        {
            foreach(var id in assignments.Keys.ToArray()) Drop(id);
            Active=false;
            // Do not rewind or replay an elapsed world interval on any failure.
            if(StarSystem.fEpoch-initial<seconds-1e-6)
            {
                foreach(var ship in ships) foreach(var co in SkipEquipment(ship)) CrewWork.Provider(co)?.Suspend(co);
                system.Update(seconds-(StarSystem.fEpoch-initial));
            }
            // A fitting not stepped since its last turn is charged its remaining seconds by its next ordinary power step,
            // as the game charges every object after its own single-jump skip; nothing is charged twice.
            stepped.Clear(); arrivals.Clear(); equipment.Clear(); headStart.Clear(); skipping=Array.Empty<Ship>();
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
        // Spare seconds from the step the last job ended in start this one; unused, they lapse.
        double spare=headStart.TryGetValue(actor.strID,out var had)?had:0; headStart.Remove(actor.strID);
        var offers=new List<CrewWork.Job>();
        foreach(var co in SkipEquipment(actor.ship).Where(c=>!faulted.Contains(c.strID)))
        {
            var order=CrewWork.Order(co); var provider=CrewWork.Provider(co)!;
            if(order.Protected || order.Permission!=WorkPermission.Enabled || CrewWork.RetryPending(co) || provider is not ICrewSkipProvider support || !support.CanAdvance(co,out _)) continue;
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
            job.Seconds=CrewBalance.Duration(offer.Seconds,CrewSpecialities.Skilled(actor,offer.Skill));
            // SFF charges handling plus conservative walking time; no carried item is cloned.
            double travel=Travel(actor,offer.Target);
            // Return via the worker's abstract starting point; conservative and never a shortcut through a wall.
            if(offer.Cargo!=null) travel+=2*Travel(actor,offer.Cargo);
            if(!CrewBalance.Finite(travel)) { CrewWork.Reservations.Release(job.Lease); continue; }
            assignments[actor.strID]=job; busy[actor.strID]=CrewBalance.JobSeconds(job.Seconds+travel,spare); break;
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
    /// <summary>The game's room object (<c>Room</c> makes one from this definition); its gas takes machine heat and gas.</summary>
    private const string RoomDefinition = "Compartment";
    /// <summary>Sorts the skipping ships' powered and gas-holding objects once per skip (Framework 0.112.0).</summary>
    private static void Classify(IEnumerable<CondOwner> powered)
    {
        stepped.Clear(); arrivals.Clear();
        double now=StarSystem.fEpoch, step=CrewBalance.ClampSkipStep(StepSeconds);
        foreach(var co in powered) stepped.Add(Make(co,CrewBalance.FixtureStart(stepped.Count,now,step)));
    }
    private static Consumer Make(CondOwner co,double lastStepped) => new()
    {
        Co=co, LastStepped=lastStepped,
        Equipment=CrewWork.Provider(co)!=null,
        Recharger=co.Pwr!=null && (RechargeTrigger(co.Pwr)!=null || co.HasCond("IsRechargingContainer")),
        Room=co.Pwr==null && co.GasContainer!=null && co.strCODef==RoomDefinition
    };
    /// <summary>Crew-ordered equipment on a skipping ship, read once per skip and again after a mode switch.</summary>
    private static IEnumerable<CondOwner> SkipEquipment(Ship ship)
    {
        if(!equipment.TryGetValue(ship,out var list)) equipment[ship]=list=CrewWork.Equipment(ship).ToArray();
        return list.Where(c=>c!=null && !c.bDestroyed && c.HasCond("IsInstalled"));
    }
    /// <summary>A mode switch (damage, repair, installation) made a new object during the skip: step it from the next
    /// step and read the ship's equipment again.</summary>
    internal static void Replaced(CondOwner? co)
    {
        if(!Active || co==null || co.bDestroyed || co.ship==null || Array.IndexOf(skipping,co.ship)<0) return;
        equipment.Remove(co.ship);
        if(co.Pwr!=null || co.GasContainer!=null) arrivals.Add(co);
    }
    /// <summary>One skip step's power and gas. Phobos machines (a standing Start), crew-ordered equipment, rooms and the
    /// objects that charge batteries are stepped every step; the game's other powered fittings every fourth step, each
    /// asked for the seconds since its last turn, so they still compete for the same power.</summary>
    private static void TickMachines(Ship[] ships,double step)
    {
        foreach(var ship in ships) if(ship.Reactor!=null) ship.Reactor.GetComponent<FusionIC>()?.CatchUp();
        double now=StarSystem.fEpoch;
        if(arrivals.Count>0) { foreach(var co in arrivals) stepped.Add(Make(co,now-CrewBalance.FixtureStepMultiple*step)); arrivals.Clear(); }
        foreach(var c in stepped)
        {
            var co=c.Co;
            if(co==null || co.bDestroyed) continue;
            bool due=c.Equipment || c.Recharger || Persistence.ResumeAfterLoad.Marked(co) || CrewBalance.FixtureDue(c.LastStepped,now,step);
            if(!due) { if(c.Room) co.GasContainer.Run(); continue; }
            c.LastStepped=now;
            using var measurement=Diagnostics.Performance.Measure(Diagnostics.Performance.SkipMachineStep);
            var provider=CrewWork.Provider(co);
            bool supported=provider==null || provider is ICrewSkipProvider p && p.CanAdvance(co,out _);
            if(!supported || faulted.Contains(co.strID))
            { if(co.Pwr!=null)PowerEpoch(co.Pwr)=now; continue; }
            if(co.Pwr!=null)
            {
                double elapsed=now-PowerEpoch(co.Pwr);
                if(elapsed>=1)
                {
                    try
                    {
                        // A charger takes the step in one-second slices so batteries charge as they would in play.
                        int slices=c.Recharger?CrewBalance.RechargeSlices(elapsed):1;
                        for(int i=0;i<slices;i++) { PowerEpoch(co.Pwr)=now-elapsed/slices; RunPower(co.Pwr); }
                    }
                    catch(Exception error) { faulted.Add(co.strID); if(provider!=null)CrewWork.Fault(co,error); }
                    finally { PowerEpoch(co.Pwr)=now; }
                }
            }
            if(co.GasContainer!=null) co.GasContainer.Run();
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
// The game computes the repair allowance from its own preview (ship-duty hours, skip length and
// its 2.25 factor). Keep that formula; only the share of crew time our jobs took is deducted.
[HarmonyPatch(typeof(GUIFFWD),"UndamageParts")]
internal static class CrewSkipRepairs
{
    private static void Prefix(ref double fAmount) { if(CrewSkip.Managed) fAmount*=CrewSkip.RepairShare; }
}
