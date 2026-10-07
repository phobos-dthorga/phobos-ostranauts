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
public static partial class StoryArcs
{
    /// <summary>The hidden condition every story goal test requires and no one ever has.</summary>
    public const string Never = "IsPhobosStoryGoalOpen";
    private static Cadence cadence = new(30);
    private static CondOwner? player;
    private static StoryRecord record = new();
    private static bool removing;
    private static readonly List<(string Id, int Weight)> broadcastPool = new(), advertPool = new();
    private static Dictionary<string, List<StoryLine>> chatterPools = new(StringComparer.Ordinal);
    // The places the player was at or in at the last check (Framework 0.114.0), for placed small talk said by crew.
    private static HashSet<string> nearPlaces = new(StringComparer.Ordinal);
    private static string? lastRegionId;
    internal static Func<double> Roll = () => UnityEngine.Random.value;

    internal static StoryRecord Record => record;

    /// <summary>The hidden condition and one goal test per step that has a goal.</summary>
    internal static void AddDefinitions(NativeDefinitions d, StoryLibrary library)
    {
        d.Conditions[Never] = new JsonCond { strName = Never, strNameFriendly = Never, strColor = "Neutral", nDisplaySelf = 2, nDisplayOther = 2 };
        // Framework's story data file (0.110.0), the game's own data file under our name.
        StoryFiles.AddDefinition(d);
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
        player = null; broadcastPool.Clear(); advertPool.Clear(); chatterPools = new(StringComparer.Ordinal); nearPlaces = new(StringComparer.Ordinal); lastRegionId = null;
        StoryChatter.Reset();
    }

    private static bool Ready => CrewSim.objInstance != null && CrewSim.objInstance.FinishedLoading && CrewSim.coPlayer != null && !CrewSim.coPlayer.bDestroyed &&
        CrewSim.system != null && MonoSingleton<ObjectiveTracker>.Instance != null;

    public static void Poll()
    {
        if (StoryContent.Library.Count == 0) return;
        // Arriving in a new region (Framework 0.114.0): local news and arcs are offered at once, not up to a check later.
        string? regionId = RegionId;
        bool arrived = !string.Equals(regionId, lastRegionId, StringComparison.Ordinal);
        if (arrived) lastRegionId = regionId;
        if (!cadence.Due() && !(arrived && regionId != null)) return;
        try { Check(); }
        catch (Exception ex) { FrameworkLifecycle.Log(Text.Get("Story.check_failed", ex.Message)); }
    }
    /// <summary>The game's own notion of where the player is: the nearest regional station's id, as its traffic control
    /// keeps it; the last region entered when none is current.</summary>
    internal static string? RegionId => CollisionManager.strATCClosest ?? AIShipManager.strATCLast;

    /// <summary>One story check; F3 <c>story check</c> runs it at once.</summary>
    internal static void Check()
    {
        if (!Ready) return;
        using var measurement = Diagnostics.Performance.Measure(Diagnostics.Performance.StoryCheck);
        var library = StoryContent.Library;
        if (!ReferenceEquals(player, CrewSim.coPlayer)) Attach(CrewSim.coPlayer);
        var facts = new GameFacts(player!);
        var gates = new Gates(facts);
        bool changed = false;
        foreach (var pair in record.Arcs.Where(a => a.Value.State == ArcState.Active).ToArray())
        {
            if (!library.Arcs.TryGetValue(pair.Key, out var arc)) continue;
            int index = StoryRules.Resolve(arc.Value, pair.Value);
            if (index < 0) continue;
            var step = arc.Value.steps[index];
            if (StoryRules.Outcome(step, facts, pair.Value.StepStart) is int outcome && Finish(arc, pair.Value, index, outcome, facts)) changed = true;
            // A goal the game did not take (it refuses one titled like a goal shown in the last ten seconds) is offered again.
            else if (pair.Value.State == ArcState.Active && step.objective != null && Open(StoryRules.GoalTest(arc.Id, step.id)).Count == 0) Show(arc, step);
        }
        // Local arcs first (Framework 0.114.0): an arc with a place starts by itself only while the player is there.
        if (record.ActiveCount(library.Arcs.ContainsKey) < library.Settings.maxActiveArcs)
            foreach (var arc in library.Arcs.Values.Where(a => a.Value.chance > 0 && Available(a, facts, gates)).OrderBy(a => PlaceOf(a.Value.thread, a.Value.place) == null ? 1 : 0).ThenBy(_ => Roll()).ToArray())
                if (Roll() < arc.Value.chance) { Begin(arc, facts); changed = true; break; }
        Pools(facts, gates);
        if (changed) Save();
    }

    private static bool Available(StoryEntry<StoryArc> arc, IStoryFacts facts, Gates gates) =>
        (!record.Arcs.TryGetValue(arc.Id, out var p) || arc.Value.repeatable && p.State == ArcState.Done && StoryRules.CooledDown(p.StepStart, facts.Epoch, arc.Value.cooldownDays)) &&
        gates.Blocked(arc.Value.requires, arc.Value.thread) == null &&
        (PlaceOf(arc.Value.thread, arc.Value.place) is not string place || facts.Near(place));

    private static string? PlaceOf(string? thread, string? place) => StoryContent.Library.PlaceOf(thread, place);

    /// <summary>An entry's own requirements and its thread's, the thread's checked once per check.</summary>
    internal sealed class Gates
    {
        private readonly IStoryFacts facts;
        private readonly Dictionary<string, string?> threads = new(StringComparer.Ordinal);
        public Gates(IStoryFacts facts) { this.facts = facts; }
        public string? Blocked(StoryRequires? own, string? thread)
        {
            string? problem = StoryRules.Blocked(own, facts, record);
            if (problem != null || thread == null) return problem;
            if (!threads.TryGetValue(thread, out var cached)) threads[thread] = cached = StoryRules.Blocked(StoryContent.Library.ThreadRequires(thread), facts, record);
            return cached;
        }
    }

    /// <summary>Reads the player's record and puts the goals of active arcs back where a save lost them.</summary>
    private static void Attach(CondOwner co)
    {
        player = co;
        var store = Store(co);
        record = store.Read(out var fields) == SavedStateStatus.Ready ? StoryRecord.Decode(fields) : new StoryRecord();
        var library = StoryContent.Library;
        bool changed = false;
        // The player's story time starts with the record (Framework 0.109.0); an older record starts it now.
        if (record.Began == null) { record.Began = StarSystem.fEpoch; changed = true; }
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
            // A goal kept by the save has lost its portrait (the game does not save it).
            else
            {
                int index = arc.Value.steps.FindIndex(s => s.id == stepId);
                if (string.IsNullOrEmpty(objective.strPortraitOverride)) Portrait(objective, arc, index);
                // A goal shown before Framework 0.121.0 gains its From line once.
                if (FromLine(arc, index) is string from && (objective.strDisplayDesc ?? "").IndexOf(from, StringComparison.Ordinal) < 0)
                    objective.strDisplayDesc = string.IsNullOrEmpty(objective.strDisplayDesc) ? from : objective.strDisplayDesc + "\n" + from;
            }
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
        // A new run of the arc starts a new correspondence in the Letters window.
        record.Letters.Remove(arc.Id);
        Enter(arc, progress, 0, facts);
    }

    private static void Enter(StoryEntry<StoryArc> arc, ArcProgress progress, int index, IStoryFacts facts)
    {
        var step = arc.Value.steps[index];
        progress.Step = index; progress.StepId = step.id; progress.StepStart = facts.Epoch;
        if (step.delivery?.message is StoryMessage message)
        {
            Log(Sender(arc.Owner, arc.Id + "." + step.id + ".from", message), StoryContent.Words(arc.Owner, arc.Id + "." + step.id + ".message", message.text), PlaceOf(arc.Value.thread, arc.Value.place));
            record.AddLetter(arc.Id, new StoryLetter(step.id, StoryLetter.Opening, null, facts.Epoch));
        }
        // A letter waiting for the player's reply says where to answer it (Framework 0.122.0).
        if (step.choices != null) Log(null, Text.Get("Story.reply_hint"));
        if (step.delivery?.bulletin is string bulletin) record.Enqueue(bulletin);
        if (step.objective != null) Show(arc, step);
    }

    /// <summary>Finishes a step by its own tests (<paramref name="outcome"/> -1) or by a branch.</summary>
    private static bool Finish(StoryEntry<StoryArc> arc, ArcProgress progress, int index, int outcome, IStoryFacts facts)
    {
        var step = arc.Value.steps[index];
        var branch = outcome >= 0 ? step.branches![outcome] : null;
        return Finish(arc, progress, index, branch?.tests ?? step.tests, branch == null ? step.onComplete : branch.onComplete, branch?.next ?? step.next,
            arc.Id + "." + step.id + (branch == null ? "" : ".b" + outcome), branch == null ? StoryLetter.Completion : "b" + outcome, null, facts);
    }

    /// <summary>Finishes a step by its own tests, a branch or the player's reply (<paramref name="choice"/>): takes what
    /// those tests consume, closes the goal through the game, gives the rewards and moves to the next step, the named one
    /// or the end. Returns false and changes nothing when what is consumed could not all be taken.</summary>
    private static bool Finish(StoryEntry<StoryArc> arc, ArcProgress progress, int index, List<StoryTest> tests, StoryOutcome? result, string? nextId,
        string key, string letterKind, string? choice, IStoryFacts facts)
    {
        var step = arc.Value.steps[index];
        var pay = tests.Where(t => t.kind == StorySchema.Credits && t.consume).Sum(t => t.amount);
        if (pay > 0 && facts.Credits < pay) return false;
        foreach (var test in tests.Where(t => t.kind == StorySchema.HaveItem && t.consume))
            if (!Take(test.item!, test.count)) return false;
        // Who pays or is paid: the sender of this outcome's message, else of the arc's first message, else a contact.
        string? place = PlaceOf(arc.Value.thread, arc.Value.place);
        string from = Fill(result?.message is StoryMessage said ? Sender(arc.Owner, key + ".doneFrom", said)
            : arc.Value.steps.Select(s => s.delivery?.message).FirstOrDefault(m => m != null) is StoryMessage first
                ? Sender(arc.Owner, arc.Id + "." + arc.Value.steps.First(s => s.delivery?.message == first).id + ".from", first)
                : Text.Get("Story.ledger_contact"), place);
        if (pay > 0) Pay(-pay, from, arc.Id);
        foreach (var objective in Open(StoryRules.GoalTest(arc.Id, step.id))) Remove(objective, completed: true);
        // The Letters window keeps the reply and every letter that arrives (Framework 0.122.0).
        if (choice != null || result?.message != null) record.AddLetter(arc.Id, new StoryLetter(step.id, letterKind, choice, facts.Epoch));
        if (result?.message is StoryMessage message)
            Log(Sender(arc.Owner, key + ".doneFrom", message), StoryContent.Words(arc.Owner, key + ".done", message.text), place);
        foreach (var reward in result?.items ?? new List<StoryReward>()) Give(reward.item, reward.count);
        if (result != null && result.credits > 0) Pay(result.credits, from, arc.Id);
        if (result != null && result.files.Count > 0) GiveFiles(result.files);
        // Story flags (Framework 0.114.0): what other entries may now require.
        // Framework 0.131.0: setting a flag that is set already renews its time, so recurring news can follow it.
        if (result != null) { foreach (var flag in result.setFlags) record.RenewFlag(flag, facts.Epoch); foreach (var flag in result.clearFlags) record.ClearFlag(flag); }
        // Standing (Framework 0.115.0; owner choice: small changes): through the game's own faction scores.
        if (result != null) foreach (var change in result.standing) Stand(change.faction, change.change);
        int next = StoryRules.NextStep(arc.Value, index, nextId);
        if (next >= 0) Enter(arc, progress, next, facts);
        // A finished arc keeps its finishing time in StepStart (Framework 0.131.0), for a repeatable arc's cooldown.
        else { progress.State = ArcState.Done; progress.Completions++; progress.StepStart = facts.Epoch; }
        return true;
    }

    /// <summary>Changes a faction's view of the player by the game's own reputation call, the one its debug command
    /// makes, and says so in the crew log. A faction the game does not have is logged and skipped.</summary>
    private static void Stand(string faction, double change)
    {
        if (player == null || change == 0) return;
        var f = CrewSim.system?.GetFaction(faction);
        if (f == null) { FrameworkLifecycle.Log(Text.Get("Story.unknown_faction", faction)); return; }
        f.ApplyFactionRep(player.strID, (float)change);
        Log(null, Text.Get(change > 0 ? "Story.standing_up" : "Story.standing_down", f.strNameFriendly ?? faction, Math.Abs(change).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture),
            Text.Get("Story.tier_" + StoryRules.Tier(new GameFacts(player).Standing(faction) ?? 0))));
    }
    /// <summary>F3 <c>story standing &lt;faction&gt; &lt;change&gt;</c>: the owner's in-play check that a change shows in the game's FACTIONS app.</summary>
    internal static string StandingCommand(string faction, string amount)
    {
        if (!Ready) return Text.Get("Story.not_in_game");
        if (!ReferenceEquals(player, CrewSim.coPlayer)) Attach(CrewSim.coPlayer);
        if (!double.TryParse(amount, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double change) || change == 0 || Math.Abs(change) > StorySchema.MaxStandingChange)
            return Text.Get("Story.standing_amount", StorySchema.MaxStandingChange);
        if (CrewSim.system?.GetFaction(faction) == null) return Text.Get("Story.unknown_faction", faction);
        Stand(faction, change);
        return Text.Get("Story.standing_command", faction, new GameFacts(player!).Standing(faction)?.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) ?? "?");
    }

    /// <summary>Pays the player (a positive amount) or takes from them, with a line in the game's ledger, as the
    /// station kiosk's bulk sales do.</summary>
    private static void Pay(double amount, string sender, string arc)
    {
        if (player == null || amount == 0) return;
        player.AddCondAmount("StatUSD", amount);
        var line = amount > 0
            ? new LedgerLI(player.strID, sender, (float)amount, Text.Get("Story.ledger_paid", sender), StoryRules.TestPrefix + arc) { fTimePaid = StarSystem.fEpoch }
            : new LedgerLI(sender, player.strID, (float)-amount, Text.Get("Story.ledger_charged", sender), StoryRules.TestPrefix + arc) { fTimePaid = StarSystem.fEpoch };
        if (line.Paid) Ledger.AddLI(line);
        Log(null, Text.Get(amount > 0 ? "Story.credits_paid" : "Story.credits_charged", Math.Abs(amount).ToString("0", System.Globalization.CultureInfo.InvariantCulture), sender));
    }

    private static void Show(StoryEntry<StoryArc> arc, StoryStep step)
    {
        string test = StoryRules.GoalTest(arc.Id, step.id);
        // The game keeps finished goals in its list and refuses a goal equal to one there, so a repeated arc's old
        // finished goal is taken out first.
        Tracker.AllObjectives.RemoveAll(o => o.Finished && o.strCT == test);
        string? place = PlaceOf(arc.Value.thread, arc.Value.place);
        string description = Fill(StoryContent.Words(arc.Owner, arc.Id + "." + step.id + ".description", step.objective!.description), place);
        // Who it is from (Framework 0.121.0): the goal says so under its description, so the player knows where it came from.
        string? from = FromLine(arc, arc.Value.steps.IndexOf(step));
        if (from != null) description = description.Length == 0 ? from : description + "\n" + from;
        var objective = new Objective(player, Fill(StoryContent.Words(arc.Owner, arc.Id + "." + step.id + ".title", step.objective.title), place), test)
        { strDisplayDesc = description };
        Portrait(objective, arc, arc.Value.steps.IndexOf(step));
        Tracker.AddObjective(objective);
    }

    /// <summary>"From Name, role (home)." for a goal, or null when it is from no one in particular.</summary>
    private static string? FromLine(StoryEntry<StoryArc> arc, int index)
    {
        var (person, free) = StoryRules.GoalSender(arc.Value, index);
        var library = StoryContent.Library;
        if (person != null && library.PersonName(person, StoryContent.Words) is string name)
            return library.People.TryGetValue(person, out var p) && library.Places.Name(p.Value.home) is string home
                ? Text.Get("Story.objective_from_place", name, home) : Text.Get("Story.objective_from", name);
        if (free?.from != null) return Text.Get("Story.objective_from", Fill(free.from));
        return null;
    }

    /// <summary>The goal's portrait (Framework 0.121.0): the face of who it is from, rolled once by the game's own face
    /// roll and kept in the story record, else the game's own wrist PDA picture. The game does not save a goal's
    /// portrait, so a goal restored from a save is given it again.</summary>
    private static void Portrait(Objective objective, StoryEntry<StoryArc> arc, int index)
    {
        var (person, _) = StoryRules.GoalSender(arc.Value, index);
        objective.strPortraitOverride = (person != null ? Face(person) : null) ?? Social.Portraits.PdaImage;
    }

    /// <summary>A person's face picture name, rolling and keeping their face the first time it is needed.</summary>
    private static string? Face(string person)
    {
        if (!record.Faces.TryGetValue(person, out var parts))
        {
            string? look = StoryContent.Library.People.TryGetValue(person, out var p) ? p.Value.face : null;
            if (Social.Portraits.RandomFace(look) is not string[] rolled) return null;
            record.Faces[person] = parts = rolled;
            Save();
        }
        return Social.Portraits.Register(parts);
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

    private static void Log(string? from, string text, string? place = null)
    {
        if (player == null) return;
        player.LogMessage(Fill(from == null ? text : Text.Get("Story.message", from, text), place), "Neutral", "Game");
    }

    /// <summary>Who a message is from: the person named (Framework 0.114.0), else the free text.</summary>
    private static string Sender(string owner, string key, StoryMessage message) =>
        message.from != null ? StoryContent.Words(owner, key, message.from) : StoryContent.Library.PersonName(message.person, StoryContent.Words) ?? Text.Get("Story.ledger_contact");

    /// <summary>Fills the placeholders for the player as they are now; <paramref name="place"/> is the entry's own
    /// place, which [place], [region] and [body] name before the player's whereabouts (Framework 0.114.0).</summary>
    internal static string Fill(string text, string? place = null)
    {
        if (player == null || text.IndexOf('[') < 0) return text;
        var library = StoryContent.Library;
        string name = player.FriendlyName ?? player.strName ?? Text.Get("Story.someone");
        string? here = null; bool looked = false; var who = player;
        string? Here() { if (!looked) { looked = true; var facts = new GameFacts(who); here = facts.DockedPlace ?? facts.Region; } return here; }
        return StorySchema.Fill(text, token => token switch
        {
            "player" => name,
            "player-first" => player.FirstName ?? name,
            "ship" => player.ship?.publicName ?? Text.Get("Story.your_ship"),
            "place" => library.Places.Name(place ?? Here()) ?? StationName() ?? Text.Get("Story.the_station"),
            "region" => library.Places.Region(place ?? Here()) ?? Text.Get("Story.these_parts"),
            "station" => StationName() ?? library.Places.Name(Here()) ?? Text.Get("Story.the_station"),
            "body" => library.Places.Body(place ?? Here()) ?? Text.Get("Story.the_rock"),
            "date" => Text.Get("Story.date", MathUtils.GetYearFromS(StarSystem.fEpoch), MathUtils.GetMonthFromS(StarSystem.fEpoch).ToString("00"), MathUtils.GetDayOfMonthFromS(StarSystem.fEpoch).ToString("00")),
            "crew" => CrewName() ?? Text.Get("Story.one_of_crew"),
            _ => token.StartsWith("person:", StringComparison.Ordinal) ? library.PersonName(token.Substring(7), StoryContent.Words)?.Split(',')[0] : null
        });
    }
    /// <summary>One of the player's crew other than the player, picked at random for [crew] (Framework 0.115.0).</summary>
    private static string? CrewName()
    {
        var others = Phobos.Ostranauts.Framework.Crew.CrewRoster.Members().Where(c => c != player).ToArray();
        if (others.Length == 0) return null;
        return others[Math.Min(others.Length - 1, (int)(Roll() * others.Length))].FriendlyName;
    }
    /// <summary>The public name of the station the player is docked at or aboard, or null.</summary>
    private static string? StationName()
    {
        var ship = player?.ship;
        if (ship == null) return null;
        if (ship.IsStation(true)) return ship.publicName;
        return ship.GetAllDockedShips(null)?.FirstOrDefault(s => s != null && s.IsStation(true))?.publicName;
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

    /// <summary>A data card holding these story files, given like any reward item (Framework 0.110.0).</summary>
    private static void GiveFiles(IReadOnlyList<string> ids)
    {
        if (player == null) return;
        var card = StoryFiles.MakeCard(ids, out var problem);
        if (problem != null) FrameworkLifecycle.Log(problem);
        if (card == null) return;
        var rest = player.AddCO(card, bEquip: false, bOverflow: false, bIgnoreLocks: false);
        if (rest != null && player.ship != null) LegacyItemConversions.Drop(player.ship, rest, player.tf.position);
        Log(null, Text.Get("Story.file_given", card.FriendlyName));
    }

    /// <summary>A story file was opened on a computer: remembered for <c>filesRead</c>, and its arc may start.</summary>
    internal static void FileOpened(string id, string? startsArc)
    {
        if (!Ready) return;
        if (!ReferenceEquals(player, CrewSim.coPlayer)) Attach(CrewSim.coPlayer);
        bool changed = record.Read.Add(id);
        if (startsArc != null && StoryContent.Library.Arcs.TryGetValue(startsArc, out var arc) && !record.Started(startsArc))
        {
            var facts = new GameFacts(player!);
            if (StoryRules.Blocked(arc.Value.requires, facts, record) == null) { Begin(arc, facts); changed = true; }
        }
        if (changed) Save();
    }

    internal static string FileCommand(string id)
    {
        if (!Ready) return Text.Get("Story.not_in_game");
        if (!ReferenceEquals(player, CrewSim.coPlayer)) Attach(CrewSim.coPlayer);
        if (!StoryContent.Library.Files.ContainsKey(id)) return Text.Get("Story.unknown_file_command", id);
        GiveFiles(new[] { id });
        return Text.Get("Story.file_command", id);
    }

    /// <summary>The news and adverts the TVs may pick from until the next check, weighted towards the player's place
    /// (Framework 0.114.0), and the small talk that may be said.</summary>
    private static void Pools(IStoryFacts facts, Gates gates)
    {
        var library = StoryContent.Library;
        broadcastPool.Clear(); advertPool.Clear();
        nearPlaces = new HashSet<string>(library.Places.Keys.Where(facts.Near), StringComparer.Ordinal);
        foreach (var b in library.Broadcasts.Values)
        {
            if (b.Value.once && record.Seen.Contains(b.Id) || b.Value.onceEach && !record.DueAgain(b.Id, b.Value.requires!.flags) || gates.Blocked(b.Value.requires, b.Value.thread) != null) continue;
            string? place = library.PlaceOf(b.Value.thread, b.Value.place);
            int weight = StoryRules.PlaceWeight(b.Value.weight, place, place != null && nearPlaces.Contains(place), library.Settings);
            if (weight > 0) broadcastPool.Add((b.Id, weight));
        }
        foreach (var a in library.Adverts.Values)
        {
            if (a.Value.once && record.Seen.Contains(a.Id) || a.Value.onceEach && !record.DueAgain(a.Id, a.Value.requires!.flags) || gates.Blocked(a.Value.requires, a.Value.thread) != null) continue;
            string? place = library.PlaceOf(a.Value.thread, a.Value.place);
            int weight = StoryRules.PlaceWeight(a.Value.weight, place, place != null && nearPlaces.Contains(place), library.Settings);
            if (weight > 0) advertPool.Add((a.Id, weight));
        }
        // A mention is said only for a while after its news was shown; a stale one is not.
        chatterPools = StoryRules.ChatterPools(library.Lines.Where(l => l.Broadcast == null || StoryRules.MentionFresh(record.SeenEpoch(l.Broadcast), facts.Epoch, library.Settings.mentionDays)),
            r => StoryRules.Blocked(r, facts, record) == null);
        foreach (var pool in chatterPools.Values) pool.RemoveAll(l => gates.Blocked(null, l.Thread) != null);
    }

    /// <summary>The small-talk lines eligible at the last check, by moment (Framework 0.108.0).</summary>
    internal static IReadOnlyDictionary<string, List<StoryLine>> ChatterPools => chatterPools;
    /// <summary>Whether the player was at or in this place at the last check (Framework 0.114.0).</summary>
    internal static bool NearPlace(string place) => nearPlaces.Contains(place);
    /// <summary>Whether someone stands at this place now: the place of their ship or station.</summary>
    internal static bool SpeakerAt(CondOwner? speaker, string place) => speaker?.ship != null && StoryContent.Library.Places.Covers(place, speaker.ship.strRegID);
    /// <summary>Whether someone is aboard one of the player's ships (the player counts).</summary>
    internal static bool Crew(CondOwner? speaker) =>
        speaker != null && CrewSim.coPlayer != null && speaker.ship != null && CrewSim.system?.GetShipOwner(speaker.ship.strRegID) == CrewSim.coPlayer.strID;

    /// <summary>A pick from a pool was shown: remembered with the time (Framework 0.114.0), and a once-only entry
    /// leaves the pool.</summary>
    internal static void Shown(string id, bool once, List<(string Id, int Weight)>? pool)
    {
        if (player == null || !ReferenceEquals(player, CrewSim.coPlayer)) return;
        record.MarkSeen(id, StarSystem.fEpoch);
        if (once) pool?.RemoveAll(e => e.Id == id);
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
            // A once-only bulletin already shown is not shown again (Framework 0.114.0).
            if (StoryContent.Library.Broadcasts.TryGetValue(next, out var b) && !(b.Value.once && record.Seen.Contains(next)) &&
                !(b.Value.onceEach && !record.DueAgain(next, b.Value.requires!.flags))) id = next;
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
        var lines = new List<string> { Text.Get("Story.docked_report", facts.DockedIds()), Where(facts) };
        foreach (var arc in StoryContent.Library.Arcs.Values.OrderBy(a => a.Id, StringComparer.Ordinal))
        {
            if (record.Arcs.TryGetValue(arc.Id, out var p) && p.State == ArcState.Active)
            {
                int index = StoryRules.Resolve(arc.Value, p);
                lines.Add(Text.Get("Story.arc_active", arc.Id, arc.Value.title, index < 0 ? p.StepId : arc.Value.steps[index].id));
                if (index >= 0)
                {
                    foreach (var test in arc.Value.steps[index].tests) lines.Add("    " + StoryRules.Describe(test, facts, p.StepStart));
                    var branches = arc.Value.steps[index].branches ?? new List<StoryBranch>();
                    for (int b = 0; b < branches.Count; b++)
                        lines.Add("    " + Text.Get("Story.branch_line", b + 1, branches[b].next, string.Join("; ", branches[b].tests.Select(t => StoryRules.Describe(t, facts, p.StepStart)))));
                    lines.AddRange(ChoiceLines(arc, index, p, facts));
                }
                continue;
            }
            string state = p == null ? Text.Get("Story.state_new") : p.State == ArcState.Done ? Text.Get("Story.state_done", p.Completions) : Text.Get("Story.state_abandoned");
            string? blocked = p != null && !(arc.Value.repeatable && p.State == ArcState.Done) ? Text.Get("Story.not_again") :
                StoryRules.Blocked(arc.Value.requires, StoryContent.Library.ThreadRequires(arc.Value.thread), facts, record) ??
                (PlaceOf(arc.Value.thread, arc.Value.place) is string place && !facts.Near(place) ? Text.Get("Story.needs_place", place) : null);
            lines.Add(Text.Get("Story.arc_line", arc.Id, arc.Value.title, state, blocked ?? (arc.Value.chance > 0 ? Text.Get("Story.may_start", arc.Value.chance) : Text.Get("Story.f3_only"))));
        }
        lines.Add(Text.Get("Story.pools", broadcastPool.Count, StoryContent.Library.Broadcasts.Count, advertPool.Count, StoryContent.Library.Adverts.Count, record.Queue.Count));
        lines.Add(Text.Get("Story.chatter_pools", chatterPools.Values.Sum(p => p.Count), StoryContent.Library.Lines.Count, StoryContent.Library.Tips.Count,
            StoryLore.ShownArticles, StoryContent.Library.Articles.Count));
        return string.Join("\n", lines);
    }

    /// <summary>F3 <c>story where</c> (Framework 0.114.0): the facts the place and thread gates read now.</summary>
    internal static string WhereCommand()
    {
        if (!Ready) return Text.Get("Story.not_in_game");
        if (!ReferenceEquals(player, CrewSim.coPlayer)) Attach(CrewSim.coPlayer);
        var facts = new GameFacts(player!);
        var library = StoryContent.Library;
        var lines = new List<string> { Where(facts), Text.Get("Story.docked_report", facts.DockedIds()) };
        lines.Add(Text.Get("Story.where_date", Fill("[date]"), StarSystem.nUTCHour));
        lines.Add(record.Flags.Count == 0 ? Text.Get("Story.where_no_flags") : Text.Get("Story.where_flags", string.Join(", ", record.Flags.Keys.OrderBy(f => f, StringComparer.Ordinal))));
        lines.Add(Text.Get("Story.where_crew", facts.CrewCount, facts.Month, facts.Hour));
        // Standing with every faction a loaded place or person names (Framework 0.115.0).
        var named = library.PlaceEntries.Values.SelectMany(p => p.Value.factions).Concat(library.People.Values.Select(p => p.Value.faction).OfType<string>()).Distinct().OrderBy(f => f, StringComparer.Ordinal);
        foreach (string faction in named)
            if (facts.Standing(faction) is double score) lines.Add(Text.Get("Story.where_standing", faction, score.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture), Text.Get("Story.tier_" + StoryRules.Tier(score))));
        var gates = new Gates(facts);
        foreach (var thread in library.Threads.Values.OrderBy(t => t.Id, StringComparer.Ordinal))
            lines.Add(Text.Get("Story.where_thread", thread.Id, thread.Value.title, gates.Blocked(null, thread.Id) ?? Text.Get("Story.open")));
        return string.Join("\n", lines);
    }
    private static string Where(GameFacts facts)
    {
        var places = StoryContent.Library.Places;
        string? region = facts.Region;
        return Text.Get("Story.where_report", RegionId ?? Text.Get("Story.nowhere"), region == null ? Text.Get("Story.no_place_known") : region + " (" + (places.Name(region) ?? region) + ")",
            facts.DockedPlace == null ? Text.Get("Story.nowhere") : facts.DockedPlace + " (" + (places.Name(facts.DockedPlace) ?? facts.DockedPlace) + ")");
    }
    /// <summary>F3 <c>story thread &lt;id&gt;</c>: the thread's members and what blocks each now.</summary>
    internal static string ThreadCommand(string id)
    {
        if (!Ready) return Text.Get("Story.not_in_game");
        if (!ReferenceEquals(player, CrewSim.coPlayer)) Attach(CrewSim.coPlayer);
        var library = StoryContent.Library;
        if (!library.Threads.TryGetValue(id, out var thread)) return Text.Get("Story.unknown_thread", id);
        var facts = new GameFacts(player!);
        var gates = new Gates(facts);
        var lines = new List<string> { Text.Get("Story.thread_report", id, thread.Value.title, thread.Value.place ?? Text.Get("Story.nowhere"),
            thread.Value.people.Count == 0 ? Text.Get("Story.none") : string.Join(", ", thread.Value.people), gates.Blocked(null, id) ?? Text.Get("Story.open")) };
        foreach (var (table, member) in library.Members(id))
        {
            StoryRequires? own = table switch
            {
                "broadcasts" => library.Broadcasts[member].Value.requires, "adverts" => library.Adverts[member].Value.requires, "arcs" => library.Arcs[member].Value.requires,
                "chatter" => library.Chatter[member].Value.requires, _ => null
            };
            string state = table == "arcs" && record.Arcs.TryGetValue(member, out var p) ? (p.State == ArcState.Active ? Text.Get("Story.arc_active_short", p.StepId) : p.State == ArcState.Done ? Text.Get("Story.state_done", p.Completions) : Text.Get("Story.state_abandoned"))
                : table == "broadcasts" && record.Seen.Contains(member) ? Text.Get("Story.shown") : "";
            lines.Add(Text.Get("Story.member_line", table, member, state, StoryRules.Blocked(own, facts, record) ?? Text.Get("Story.open")));
        }
        return string.Join("\n", lines);
    }
    /// <summary>F3 <c>story flag &lt;id&gt; [clear]</c>: sets or clears a story flag, for testing content.</summary>
    internal static string FlagCommand(string id, bool clear)
    {
        if (!Ready) return Text.Get("Story.not_in_game");
        if (!ReferenceEquals(player, CrewSim.coPlayer)) Attach(CrewSim.coPlayer);
        if (!StorySchema.IsId(id)) return Text.Get("Story.bad_flag", id);
        if (clear) record.ClearFlag(id); else record.SetFlag(id, StarSystem.fEpoch);
        Save(); cadence.Invalidate();
        return Text.Get(clear ? "Story.flag_cleared" : "Story.flag_set", id);
    }
    /// <summary>F3 <c>story places</c> and <c>story people</c>: what the loaded packs know.</summary>
    internal static string PlacesCommand()
    {
        var library = StoryContent.Library;
        var lines = new List<string> { Text.Get("Story.places_title", library.PlaceEntries.Count) };
        foreach (var p in library.PlaceEntries.Values.OrderBy(p => library.Places.Root(p.Id), StringComparer.Ordinal).ThenBy(p => p.Id, StringComparer.Ordinal))
            lines.Add("  " + Text.Get("Story.place_line", p.Id, p.Value.station, p.Value.name, library.Places.Region(p.Id) ?? "", p.Value.within ?? ""));
        return string.Join("\n", lines);
    }
    internal static string PeopleCommand()
    {
        var library = StoryContent.Library;
        var lines = new List<string> { Text.Get("Story.people_title", library.People.Count) };
        foreach (var p in library.People.Values.OrderBy(p => p.Id, StringComparer.Ordinal))
            lines.Add("  " + Text.Get("Story.person_line", p.Id, library.PersonName(p.Id, StoryContent.Words) ?? p.Value.name, p.Value.home, p.Value.faction ?? ""));
        return string.Join("\n", lines);
    }

    /// <summary>The game, as the story rules see it. Lookups over a ship are made once per check, on first use.</summary>
    internal sealed class GameFacts : IStoryFacts
    {
        private readonly CondOwner player;
        private Dictionary<string, int>? installed, carried;
        private HashSet<string>? dockedPlaces;
        private string? region, dockedPlace; private bool placed;
        private Dictionary<string, int>? running;
        private CondOwner[]? crew;
        private CondOwner[] Crew() => crew ??= Phobos.Ostranauts.Framework.Crew.CrewRoster.Members().Where(c => c != player).ToArray();
        /// <summary>As the game's FACTIONS app sums it: the faction's score for each of the player's own factions.</summary>
        public double? Standing(string faction)
        {
            var f = CrewSim.system?.GetFaction(faction);
            if (f == null) return null;
            double score = 0;
            foreach (var mine in player.GetAllFactions()) score += f.GetFactionScore(mine);
            return score;
        }
        public bool CrewWith(string condition) => Crew().Any(c => c.HasCond(condition));
        public int CrewCount => Crew().Length;
        public int Running(string item) { Installed(item); return running!.TryGetValue(item, out int n) ? n : 0; }
        public int Month => MathUtils.GetMonthFromS(StarSystem.fEpoch);
        public int Hour => StarSystem.nUTCHour;
        public GameFacts(CondOwner player) { this.player = player; }
        public double Epoch => StarSystem.fEpoch;
        public double Credits => player.GetCondAmount("StatUSD");
        /// <summary>The regional place the player is in (Framework 0.114.0), from the game's nearest regional station.</summary>
        public string? Region { get { Locate(); return region; } }
        /// <summary>The place the player is docked at or aboard, or null.</summary>
        public string? DockedPlace { get { Locate(); return dockedPlace; } }
        public bool Near(string place) { Locate(); return dockedPlaces!.Contains(place) || region == place; }
        private void Locate()
        {
            if (placed) return;
            placed = true;
            var places = StoryContent.Library.Places;
            dockedPlaces = new HashSet<string>(StringComparer.Ordinal);
            var ship = player.ship;
            if (ship != null)
                foreach (var id in new[] { ship.strRegID }.Concat(ship.GetAllDockedShips(null)?.Where(s => s != null).Select(s => s.strRegID) ?? Enumerable.Empty<string>()))
                    if (places.Find(id) is string found) { dockedPlace ??= found; dockedPlaces.Add(found); dockedPlaces.Add(places.Root(found)); }
            if (places.Find(RegionId) is string current) region = places.Root(current);
        }
        public bool ModInstalled(string mod) => StoryContent.ModInstalled(mod);
        public bool PlayerHas(string condition) => player.HasCond(condition);
        public int Installed(string item)
        {
            if (installed == null)
            {
                installed = new Dictionary<string, int>(StringComparer.Ordinal); running = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (var ship in CrewSim.system.dictShips.Values.ToArray())
                {
                    if (ship == null || ship.bDestroyed || (int)ship.LoadState < 2 || CrewSim.system.GetShipOwner(ship.strRegID) != player.strID) continue;
                    foreach (var co in ship.GetCOs(null, false, false, true))
                    {
                        if (co == null || co.bDestroyed || co.ship != ship) continue;
                        installed[co.strCODef] = installed.TryGetValue(co.strCODef, out int n) ? n + 1 : 1;
                        // A Phobos machine whose Start stands is running (Framework 0.115.0).
                        if (ResumeAfterLoad.Marked(co)) running[co.strCODef] = running.TryGetValue(co.strCODef, out int r) ? r + 1 : 1;
                    }
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
        private static bool Part(string? regId, string station) => StoryPlaces.Part(regId, station);
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
