using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Ostranauts.Core;
using Ostranauts.Objectives;
using Phobos.Ostranauts.Framework.Persistence;
using Phobos.Ostranauts.Framework.Registration;

namespace Phobos.Ostranauts.Framework.Story;

/// <summary>Runs story arcs in play (Framework 0.107.0). Every check (30 real seconds by default) it finishes the steps
/// whose tests pass, may start one eligible arc, and refreshes the story news the TVs pick from. A step's goal is an
/// ordinary game objective whose completion test is <c>PhobosStory.&lt;arc&gt;.&lt;step&gt;</c>: Framework registers it on
/// every load, requiring a hidden condition that is never set, and finishes the goal itself through the game's own
/// RemoveObjective. If the pack is gone, the game reads the unknown test as its always-true Blank and the goal finishes
/// by itself, so nothing is left broken.</summary>
public static class StoryArcs
{
    /// <summary>The hidden condition every story goal test requires and no one ever has.</summary>
    public const string Never = "IsPhobosStoryGoalOpen";
    private static Cadence cadence = new(30);
    private static CondOwner? player;
    private static StoryRecord record = new();
    private static bool removing;
    private static readonly List<(string Id, int Weight)> broadcastPool = new(), advertPool = new();
    internal static Func<double> Roll = () => UnityEngine.Random.value;

    internal static StoryRecord Record => record;

    /// <summary>The hidden condition and one goal test per step that has a goal.</summary>
    internal static void AddDefinitions(NativeDefinitions d, StoryLibrary library)
    {
        d.Conditions[Never] = new JsonCond { strName = Never, strNameFriendly = Never, strColor = "Neutral", nDisplaySelf = 2, nDisplayOther = 2 };
        foreach (var arc in library.Arcs.Values)
            foreach (var step in arc.Value.steps.Where(s => s.objective != null))
            {
                string name = StoryRules.GoalTest(arc.Id, step.id);
                d.Triggers[name] = new CondTrigger { strName = name, fChance = 1, fCount = 1, bAND = true, aReqs = new[] { Never }, aForbids = Array.Empty<string>(), aTriggers = Array.Empty<string>() };
            }
    }

    /// <summary>A new library: the next check starts from the player's saved record again.</summary>
    internal static void LibraryChanged()
    {
        cadence = new Cadence(StoryContent.Library.Settings.checkSeconds);
        player = null; broadcastPool.Clear(); advertPool.Clear();
    }

    private static bool Ready => CrewSim.objInstance != null && CrewSim.objInstance.FinishedLoading && CrewSim.coPlayer != null && !CrewSim.coPlayer.bDestroyed &&
        CrewSim.system != null && MonoSingleton<ObjectiveTracker>.Instance != null;

    public static void Poll()
    {
        if (StoryContent.Library.Count == 0 || !cadence.Due()) return;
        try { Check(); }
        catch (Exception ex) { FrameworkLifecycle.Log(Text.Get("Story.check_failed", ex.Message)); }
    }

    /// <summary>One story check; F3 <c>story check</c> runs it at once.</summary>
    internal static void Check()
    {
        if (!Ready) return;
        using var measurement = Diagnostics.Performance.Measure(Diagnostics.Performance.StoryCheck);
        var library = StoryContent.Library;
        if (!ReferenceEquals(player, CrewSim.coPlayer)) Attach(CrewSim.coPlayer);
        var facts = new GameFacts(player!);
        bool changed = false;
        foreach (var pair in record.Arcs.Where(a => a.Value.State == ArcState.Active).ToArray())
        {
            if (!library.Arcs.TryGetValue(pair.Key, out var arc)) continue;
            int index = StoryRules.Resolve(arc.Value, pair.Value);
            if (index < 0) continue;
            var step = arc.Value.steps[index];
            if (StoryRules.Passed(step, facts, pair.Value.StepStart) && Finish(arc, pair.Value, index, facts)) changed = true;
            // A goal the game did not take (it refuses one titled like a goal shown in the last ten seconds) is offered again.
            else if (pair.Value.State == ArcState.Active && step.objective != null && Open(StoryRules.GoalTest(arc.Id, step.id)).Count == 0) Show(arc, step);
        }
        if (record.ActiveCount(library.Arcs.ContainsKey) < library.Settings.maxActiveArcs)
            foreach (var arc in library.Arcs.Values.Where(a => a.Value.chance > 0 && Available(a, facts)).OrderBy(_ => Roll()).ToArray())
                if (Roll() < arc.Value.chance) { Begin(arc, facts); changed = true; break; }
        Pools(facts);
        if (changed) Save();
    }

    private static bool Available(StoryEntry<StoryArc> arc, IStoryFacts facts) =>
        (!record.Arcs.TryGetValue(arc.Id, out var p) || arc.Value.repeatable && p.State == ArcState.Done) && StoryRules.Blocked(arc.Value.requires, facts, record) == null;

    /// <summary>Reads the player's record and puts the goals of active arcs back where a save lost them.</summary>
    private static void Attach(CondOwner co)
    {
        player = co;
        var store = Store(co);
        record = store.Read(out var fields) == SavedStateStatus.Ready ? StoryRecord.Decode(fields) : new StoryRecord();
        var library = StoryContent.Library;
        bool changed = false;
        foreach (var pair in record.Arcs.Where(a => a.Value.State == ArcState.Active).ToArray())
        {
            if (!library.Arcs.TryGetValue(pair.Key, out var arc)) continue;
            int index = StoryRules.Resolve(arc.Value, pair.Value);
            if (index < 0)
            {
                pair.Value.State = ArcState.Abandoned; changed = true;
                FrameworkLifecycle.Log(Text.Get("Story.step_lost", pair.Key, pair.Value.StepId));
                continue;
            }
            if (pair.Value.StepId != arc.Value.steps[index].id || pair.Value.Step != index) { pair.Value.Step = index; pair.Value.StepId = arc.Value.steps[index].id; changed = true; }
            var step = arc.Value.steps[index];
            if (step.objective != null && Open(StoryRules.GoalTest(arc.Id, step.id)).Count == 0) Show(arc, step);
        }
        // A goal of ours whose arc is no longer at that step (reset, or the record lost) is taken away quietly.
        foreach (var objective in Tracker.AllObjectives.Where(o => !o.Finished).ToArray())
        {
            if (!StoryRules.TryGoal(objective.strCT, out var arcId, out var stepId) || !library.Arcs.TryGetValue(arcId, out var arc)) continue;
            if (!record.Arcs.TryGetValue(arcId, out var p) || p.State != ArcState.Active || p.StepId != stepId) Remove(objective, completed: false);
        }
        if (changed) Save();
    }

    private static ObjectiveTracker Tracker => MonoSingleton<ObjectiveTracker>.Instance;
    private static ObjectStateStore Store(CondOwner co) => new(co.mapGUIPropMaps, StoryRecord.Name, FrameworkInfo.PluginId, StoryRecord.Version);
    internal static void Save()
    {
        if (player == null || player.bDestroyed) return;
        if (!Store(player).TryWriteIfChanged(record.Encode())) FrameworkLifecycle.Log(Text.Get("Story.record_refused"));
    }

    private static List<Objective> Open(string test) => Tracker.AllObjectives.Where(o => !o.Finished && o.strCT == test).ToList();

    /// <summary>Starts an arc at its first step. F3 starts ignore chance and requirements.</summary>
    private static void Begin(StoryEntry<StoryArc> arc, IStoryFacts facts)
    {
        int completions = record.Arcs.TryGetValue(arc.Id, out var old) ? old.Completions : 0;
        var progress = new ArcProgress { State = ArcState.Active, Completions = completions };
        record.Arcs[arc.Id] = progress;
        Enter(arc, progress, 0, facts);
    }

    private static void Enter(StoryEntry<StoryArc> arc, ArcProgress progress, int index, IStoryFacts facts)
    {
        var step = arc.Value.steps[index];
        progress.Step = index; progress.StepId = step.id; progress.StepStart = facts.Epoch;
        if (step.delivery?.message is StoryMessage message)
            Log(StoryContent.Words(arc.Owner, arc.Id + "." + step.id + ".from", message.from), StoryContent.Words(arc.Owner, arc.Id + "." + step.id + ".message", message.text));
        if (step.delivery?.bulletin is string bulletin) record.Enqueue(bulletin);
        if (step.objective != null) Show(arc, step);
    }

    /// <summary>Finishes a step whose tests passed: takes what it consumes, closes its goal through the game, gives the
    /// rewards and moves on. Returns false and changes nothing when the consumed items could not all be taken.</summary>
    private static bool Finish(StoryEntry<StoryArc> arc, ArcProgress progress, int index, IStoryFacts facts)
    {
        var step = arc.Value.steps[index];
        foreach (var test in step.tests.Where(t => t.kind == StorySchema.HaveItem && t.consume))
            if (!Take(test.item!, test.count)) return false;
        foreach (var objective in Open(StoryRules.GoalTest(arc.Id, step.id))) Remove(objective, completed: true);
        if (step.onComplete?.message is StoryMessage message)
            Log(StoryContent.Words(arc.Owner, arc.Id + "." + step.id + ".doneFrom", message.from), StoryContent.Words(arc.Owner, arc.Id + "." + step.id + ".done", message.text));
        foreach (var reward in step.onComplete?.items ?? new List<StoryReward>()) Give(reward.item, reward.count);
        if (index + 1 < arc.Value.steps.Count) Enter(arc, progress, index + 1, facts);
        else { progress.State = ArcState.Done; progress.Completions++; }
        return true;
    }

    private static void Show(StoryEntry<StoryArc> arc, StoryStep step)
    {
        string test = StoryRules.GoalTest(arc.Id, step.id);
        // The game keeps finished goals in its list and refuses a goal equal to one there, so a repeated arc's old
        // finished goal is taken out first.
        Tracker.AllObjectives.RemoveAll(o => o.Finished && o.strCT == test);
        var objective = new Objective(player, Fill(StoryContent.Words(arc.Owner, arc.Id + "." + step.id + ".title", step.objective!.title)), test)
        { strDisplayDesc = Fill(StoryContent.Words(arc.Owner, arc.Id + "." + step.id + ".description", step.objective.description)) };
        Tracker.AddObjective(objective);
    }

    private static void Remove(Objective objective, bool completed)
    {
        removing = true;
        try { Tracker.RemoveObjective(objective, completed ? ObjectiveTracker.REASON_COMPLETED : ObjectiveTracker.REASON_DISMISSED, completed); }
        finally { removing = false; }
    }

    /// <summary>The player dismissed one of our goals in the GOALS list: that arc is set aside for good (F3 reset
    /// brings it back).</summary>
    internal static void Dismissed(Objective? objective, string reason)
    {
        if (removing || objective == null || reason != ObjectiveTracker.REASON_DISMISSED || !StoryRules.TryGoal(objective.strCT, out var arcId, out _)) return;
        if (player == null || !ReferenceEquals(player, CrewSim.coPlayer) || !record.Arcs.TryGetValue(arcId, out var progress) || progress.State != ArcState.Active) return;
        progress.State = ArcState.Abandoned;
        Save();
        Log(null, Text.Get("Story.dismissed", objective.strDisplayName));
    }

    private static void Log(string? from, string text)
    {
        if (player == null) return;
        player.LogMessage(Fill(from == null ? text : Text.Get("Story.message", from, text)), "Neutral", "Game");
    }

    internal static string Fill(string text)
    {
        if (player == null) return text;
        string name = player.FriendlyName ?? player.strName ?? Text.Get("Story.someone");
        string first = player.FirstName ?? name;
        string ship = player.ship?.publicName ?? Text.Get("Story.your_ship");
        return StorySchema.Fill(text, name, first, ship);
    }

    private static bool Take(string item, int count)
    {
        var units = GameFacts.CarriedUnits(player!).Where(u => u.strCODef == item).Take(count).ToList();
        if (units.Count < count) return false;
        foreach (var unit in units) { Inventory.StackUnits.Detach(unit); unit.Destroy(); }
        return true;
    }

    private static void Give(string item, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var co = DataHandler.GetCondOwner(item);
            if (co == null) { FrameworkLifecycle.Log(Text.Get("Story.reward_missing", item)); return; }
            var rest = player!.AddCO(co, bEquip: false, bOverflow: false, bIgnoreLocks: false);
            if (rest != null && player.ship != null) LegacyItemConversions.Drop(player.ship, rest, player.tf.position);
        }
    }

    /// <summary>The news and adverts the TVs may pick from until the next check.</summary>
    private static void Pools(IStoryFacts facts)
    {
        var library = StoryContent.Library;
        broadcastPool.Clear(); advertPool.Clear();
        foreach (var b in library.Broadcasts.Values)
            if (!(b.Value.once && record.Seen.Contains(b.Id)) && StoryRules.Blocked(b.Value.requires, facts, record) == null) broadcastPool.Add((b.Id, b.Value.weight));
        foreach (var a in library.Adverts.Values)
            if (!(a.Value.once && record.Seen.Contains(a.Id)) && StoryRules.Blocked(a.Value.requires, facts, record) == null) advertPool.Add((a.Id, a.Value.weight));
    }

    /// <summary>A pick from a pool was shown: a once-only entry leaves the pool and is remembered.</summary>
    internal static void Shown(string id, bool once, List<(string Id, int Weight)>? pool)
    {
        if (!once || player == null || !ReferenceEquals(player, CrewSim.coPlayer)) return;
        record.Seen.Add(id);
        pool?.RemoveAll(e => e.Id == id);
        Save();
    }
    internal static List<(string Id, int Weight)> BroadcastPool => broadcastPool;
    internal static List<(string Id, int Weight)> AdvertPool => advertPool;

    /// <summary>The next queued bulletin that is still a known broadcast, taken off the queue.</summary>
    internal static string? NextBulletin()
    {
        if (player == null || !ReferenceEquals(player, CrewSim.coPlayer) || record.Queue.Count == 0) return null;
        string? id = null;
        while (record.Queue.Count > 0 && id == null)
        {
            string next = record.Queue[0]; record.Queue.RemoveAt(0);
            if (StoryContent.Library.Broadcasts.ContainsKey(next)) id = next;
        }
        Save();
        return id;
    }

    // F3 commands, through the same paths as the automatic check.
    internal static string StartCommand(string id)
    {
        if (!Ready) return Text.Get("Story.not_in_game");
        if (!ReferenceEquals(player, CrewSim.coPlayer)) Attach(CrewSim.coPlayer);
        if (!StoryContent.Library.Arcs.TryGetValue(id, out var arc)) return Text.Get("Story.unknown_arc_command", id);
        if (record.Arcs.TryGetValue(id, out var p) && p.State == ArcState.Active) return Text.Get("Story.already_active", id);
        Begin(arc, new GameFacts(player!));
        Save();
        return Text.Get("Story.started", id);
    }
    internal static string ResetCommand(string id)
    {
        if (!Ready) return Text.Get("Story.not_in_game");
        if (!ReferenceEquals(player, CrewSim.coPlayer)) Attach(CrewSim.coPlayer);
        if (!record.Arcs.Remove(id)) return Text.Get("Story.not_started", id);
        foreach (var objective in Tracker.AllObjectives.Where(o => !o.Finished && StoryRules.TryGoal(o.strCT, out var a, out _) && a == id).ToArray()) Remove(objective, completed: false);
        Save();
        return Text.Get("Story.reset", id);
    }
    internal static string NewsCommand(string id)
    {
        if (!Ready) return Text.Get("Story.not_in_game");
        if (!ReferenceEquals(player, CrewSim.coPlayer)) Attach(CrewSim.coPlayer);
        if (!StoryContent.Library.Broadcasts.ContainsKey(id)) return Text.Get("Story.unknown_broadcast", id);
        record.Enqueue(id); Save();
        return Text.Get("Story.queued", id);
    }

    /// <summary>The F3 report of the player's arcs and why each other arc is or is not available.</summary>
    internal static string Describe()
    {
        if (!Ready) return Text.Get("Story.not_in_game");
        if (!ReferenceEquals(player, CrewSim.coPlayer)) Attach(CrewSim.coPlayer);
        var facts = new GameFacts(player!);
        var lines = new List<string> { Text.Get("Story.docked_report", facts.DockedIds()) };
        foreach (var arc in StoryContent.Library.Arcs.Values.OrderBy(a => a.Id, StringComparer.Ordinal))
        {
            if (record.Arcs.TryGetValue(arc.Id, out var p) && p.State == ArcState.Active)
            {
                int index = StoryRules.Resolve(arc.Value, p);
                lines.Add(Text.Get("Story.arc_active", arc.Id, arc.Value.title, index < 0 ? p.StepId : arc.Value.steps[index].id));
                if (index >= 0) foreach (var test in arc.Value.steps[index].tests) lines.Add("    " + StoryRules.Describe(test, facts, p.StepStart));
                continue;
            }
            string state = p == null ? Text.Get("Story.state_new") : p.State == ArcState.Done ? Text.Get("Story.state_done", p.Completions) : Text.Get("Story.state_abandoned");
            string? blocked = p != null && !(arc.Value.repeatable && p.State == ArcState.Done) ? Text.Get("Story.not_again") : StoryRules.Blocked(arc.Value.requires, facts, record);
            lines.Add(Text.Get("Story.arc_line", arc.Id, arc.Value.title, state, blocked ?? (arc.Value.chance > 0 ? Text.Get("Story.may_start", arc.Value.chance) : Text.Get("Story.f3_only"))));
        }
        lines.Add(Text.Get("Story.pools", broadcastPool.Count, StoryContent.Library.Broadcasts.Count, advertPool.Count, StoryContent.Library.Adverts.Count, record.Queue.Count));
        return string.Join("\n", lines);
    }

    /// <summary>The game, as the story rules see it. Lookups over a ship are made once per check, on first use.</summary>
    private sealed class GameFacts : IStoryFacts
    {
        private readonly CondOwner player;
        private Dictionary<string, int>? installed, carried;
        public GameFacts(CondOwner player) { this.player = player; }
        public double Epoch => StarSystem.fEpoch;
        public bool ModInstalled(string mod) => StoryContent.ModInstalled(mod);
        public bool PlayerHas(string condition) => player.HasCond(condition);
        public int Installed(string item)
        {
            if (installed == null)
            {
                installed = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (var ship in CrewSim.system.dictShips.Values.ToArray())
                {
                    if (ship == null || ship.bDestroyed || (int)ship.LoadState < 2 || CrewSim.system.GetShipOwner(ship.strRegID) != player.strID) continue;
                    foreach (var co in ship.GetCOs(null, false, false, true))
                        if (co != null && !co.bDestroyed && co.ship == ship) installed[co.strCODef] = installed.TryGetValue(co.strCODef, out int n) ? n + 1 : 1;
                }
            }
            return installed.TryGetValue(item, out int count) ? count : 0;
        }
        public int Carried(string item)
        {
            carried ??= CarriedUnits(player).GroupBy(u => u.strCODef, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
            return carried.TryGetValue(item, out int count) ? count : 0;
        }
        public bool DockedAt(string station)
        {
            var ship = player.ship;
            if (ship == null) return false;
            if (station == StorySchema.DockedAnywhere) return ship.IsStation(true) || ship.IsDockedWithAStation();
            if (Part(ship.strRegID, station)) return true;
            return ship.GetAllDockedShips(null)?.Any(s => s != null && Part(s.strRegID, station)) == true;
        }
        /// <summary>A station's parts carry its id with a suffix (VORB_HAB, VORB|Aux).</summary>
        private static bool Part(string? regId, string station) => regId != null &&
            (regId == station || regId.StartsWith(station + "_", StringComparison.Ordinal) || regId.StartsWith(station + "|", StringComparison.Ordinal));
        public string DockedIds()
        {
            var ship = player.ship;
            if (ship == null) return Text.Get("Story.nowhere");
            var ids = new[] { ship.strRegID }.Concat(ship.GetAllDockedShips(null)?.Where(s => s != null).Select(s => s.strRegID) ?? Enumerable.Empty<string>());
            return string.Join(", ", ids.Distinct());
        }
        /// <summary>Every unit the player carries, stack members counted one by one.</summary>
        internal static List<CondOwner> CarriedUnits(CondOwner player)
        {
            var units = new HashSet<CondOwner>();
            foreach (var co in player.GetCOs(true, null) ?? new List<CondOwner>())
            {
                if (co == null || co.bDestroyed) continue;
                units.Add(co);
                if (co.aStack != null) foreach (var member in co.aStack) if (member != null && !member.bDestroyed) units.Add(member);
            }
            return units.OrderBy(u => u.coStackHead == null ? 1 : 0).ToList();
        }
    }
}

[HarmonyPatch(typeof(ObjectiveTracker), nameof(ObjectiveTracker.RemoveObjective))]
internal static class StoryDismissPatch
{
    private static void Postfix(Objective objective, string strReason)
    {
        try { StoryArcs.Dismissed(objective, strReason); }
        catch (Exception ex) { FrameworkLifecycle.Log(Text.Get("Story.check_failed", ex.Message)); }
    }
}
