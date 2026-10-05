using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Persistence;

namespace Phobos.Ostranauts.Framework.Crew;

/// <summary>Crew upkeep (Framework 0.111.0; owner request, 6 October 2026): work for idle on-shift crew on long hauls.
/// Ship-wide switches, off until chosen, are kept on the player: <b>Tune machinery</b> and <b>Inspection rounds</b>
/// (0.111.0), <b>Practice at machines</b> and <b>Housekeeping</b> (0.113.0). While one is on, a planner offers idle crew
/// one job at a time through the same native task, reservation and eligibility path as standing orders, and always
/// after them. A tuned machine's work counts for more (<see cref="Rate"/>), and the tune fades as the machine works; an
/// inspected machine fades at a lesser rate. Practice trains an unskilled crew member at a machine as studying at a
/// terminal does; housekeeping carries supplies lying on the deck to a store. Nothing else about a job changes: yields,
/// masses and energy per job stay as authored.</summary>
public static class Upkeep
{
    /// <summary>One registered machine family.</summary>
    public sealed class Family
    {
        public string Key { get; }
        public string Skill { get; }
        public CrewRole Role { get; }
        public bool Tunable { get; }
        internal Func<string, bool> Member { get; }
        /// <summary>What the machine is waiting for, for an inspection's crew-log line; null or empty when all is well.</summary>
        internal Func<CondOwner, string?>? Attention { get; }
        /// <summary>A crew member who must not work at this machine now (a patient in their own bed), by id.</summary>
        internal Func<CondOwner, string?>? Excluded { get; }
        internal Family(string key, Func<string, bool> member, string skill, CrewRole role, bool tunable, Func<CondOwner, string?>? attention, Func<CondOwner, string?>? excluded)
        { Key = key; Member = member; Skill = skill; Role = role; Tunable = tunable; Attention = attention; Excluded = excluded; }
    }

    public const string ModFolder = "PhobosFramework", Resource = "PhobosFramework.upkeep.json", RecordName = "upkeep", SwitchRecord = "PhobosUpkeep";
    public const double PlanSeconds = 10, StaleTaskSeconds = 60;
    public const int TasksPerPass = 2;
    /// <summary>At most this many items are planned for housekeeping in one pass over a ship (Framework 0.113.0).</summary>
    public const int TidyLimit = 16;
    /// <summary>During a time-skip, a ship found with nothing to tidy is looked at again after this many game seconds.</summary>
    public const double TidyRefreshSeconds = 3600;
    /// <summary>In play, a deck with nothing to tidy is read again after this many real seconds, not every pass.</summary>
    public const float TidyQuietSeconds = 60;
    private static float nextTidyScan;
    private const string PhobosPrefix = "Phobos";
    private static readonly List<Family> families = new();
    private static readonly Dictionary<string, Family?> byDefinition = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, UpkeepState> states = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, double> banked = new(StringComparer.Ordinal);
    private static readonly List<CondOwner> scratch = new();
    private static readonly List<Func<string, bool>> tidyStores = new();
    private static readonly List<Func<CondOwner, bool>> unavailable = new();
    // Housekeeping during a time-skip (0.113.0): each ship's planned moves, read once and used up, and items that failed.
    private static readonly Dictionary<Ship, Queue<(CondOwner Item, CondOwner Store)>> skipTidy = new();
    private static readonly Dictionary<Ship, double> skipTidyEmpty = new();
    private static readonly HashSet<string> skipFailed = new(StringComparer.Ordinal);
    private static Discovery.WorldFamily? world;
    private static UpkeepPack? pack;
    private static Cadence cadence = new(PlanSeconds);
    private static CondOwner? switchOwner;
    private static UpkeepSwitches switches = new();
    private static bool switchesProtected;
    public static UpkeepSettings Settings { get; set; } = new();
    public static UpkeepPack Pack => pack ??= Load();
    internal static int StateCount => states.Count + banked.Count + skipTidy.Count + skipTidyEmpty.Count + skipFailed.Count;
    internal static DataPackSource Source => new(FrameworkInfo.PluginId, ModFolder, UpkeepSchema.Name, typeof(Upkeep).Assembly, Resource);
    public static UpkeepPack Load() { pack = DataPacks.Load<UpkeepPack>(Source, UpkeepSchema.Validate); return pack; }

    /// <summary>Declares a family of installed machines for upkeep. Call once in Awake. <paramref name="member"/> takes
    /// a definition id; <paramref name="skill"/> is the speciality or game skill that makes a session count for more.</summary>
    public static Family Register(string key, Func<string, bool> member, string skill, CrewRole role, bool tunable,
        Func<CondOwner, string?>? attention = null, Func<CondOwner, string?>? excluded = null)
    {
        if (string.IsNullOrEmpty(key) || member == null) throw new ArgumentException("An upkeep family needs a key and a member test.");
        if (families.Any(f => f.Key == key)) throw new ArgumentException("Duplicate upkeep family: " + key);
        var family = new Family(key, member, skill ?? "", role, tunable, attention, excluded);
        families.Add(family); byDefinition.Clear();
        world ??= Discovery.WorldFamilies.Register("framework.upkeep", definition => FamilyOf(definition) != null);
        return family;
    }
    /// <summary>Names a kind of store housekeeping may fill with whatever it accepts, by definition id (Framework 0.113.0;
    /// Shipbreaker names its Rivetline Y bins). The store's own rules still decide what fits.</summary>
    public static void RegisterTidyStore(Func<string, bool> definition) { if (definition != null) tidyStores.Add(definition); }
    /// <summary>A test for crew who must take no upkeep work now, such as a patient resting in a medical bed
    /// (Framework 0.113.0). Called for each idle crew member; keep it cheap.</summary>
    public static void RegisterUnavailable(Func<CondOwner, bool> test) { if (test != null) unavailable.Add(test); }
    public static IReadOnlyList<Family> Families => families;
    internal static void ForgetDefinitions() => byDefinition.Clear();
    public static Family? FamilyOf(string? definition)
    {
        if (string.IsNullOrEmpty(definition)) return null;
        if (byDefinition.TryGetValue(definition!, out var known)) return known;
        Family? found = null;
        foreach (var family in families) if (family.Member(definition!)) { found = family; break; }
        byDefinition[definition!] = found; return found;
    }
    internal static bool IsTidyStore(string? definition)
    {
        if (string.IsNullOrEmpty(definition)) return false;
        foreach (var test in tidyStores) { try { if (test(definition!)) return true; } catch { } }
        return false;
    }
    internal static bool Unavailable(CondOwner actor)
    {
        foreach (var test in unavailable) { try { if (test(actor)) return true; } catch { } }
        return false;
    }

    // ---- The machine's record ----

    internal static UpkeepState State(CondOwner co)
    {
        if (states.TryGetValue(co.strID, out var state)) return state;
        var status = CrewWork.Store(co, RecordName).Read(out var fields);
        state = status == SavedStateStatus.Ready ? UpkeepRules.Decode(fields) : new UpkeepState { Protected = status != SavedStateStatus.Missing };
        states[co.strID] = state; return state;
    }
    private static void Save(CondOwner co, UpkeepState state)
    {
        if (state.Protected) return;
        if (CrewWork.Store(co, RecordName).TryWriteIfChanged(UpkeepRules.Encode(state))) { state.SavedLevel = state.Level; state.SavedInspected = state.Inspected; }
    }

    /// <summary>How much faster this machine works now, and the fade its work costs. Call at the one place a machine
    /// credits progress, with the seconds of work that step covers, and multiply the progress by the result. Returns
    /// exactly 1 for an untuned machine, so an untuned machine behaves as it always did.</summary>
    public static double Rate(CondOwner? co, double workedSeconds)
    {
        if (co == null || co.strID == null) return 1;
        var state = State(co);
        if (state.Protected || !(state.Level > 0)) return 1;
        var settings = Settings;
        var family = FamilyOf(co.strCODef);
        if (family == null || !family.Tunable) return 1;
        double rate = UpkeepRules.Rate(state.Level, settings.MaxTuningGain, Share(family));
        var data = Pack;
        state.Level = UpkeepRules.Fade(state.Level, workedSeconds, settings.TuneFadeHours,
            UpkeepRules.InspectionGood(state.Inspected, StarSystem.fEpoch, data.inspectionValidHours), data.inspectedFadeShare);
        if (UpkeepRules.WorthSaving(state.Level, state.SavedLevel)) Save(co, state);
        return rate;
    }
    /// <summary>For a machine that asks the game for electricity each step: a tuned machine asks for that much more, so
    /// it does more work in the step for the same electricity per job. Call it only while the machine is working, with
    /// the step's energy and its length in seconds; returns the rate (1 untuned), to scale the step's heat and any
    /// progress counted in seconds.</summary>
    public static double Draw(CondOwner? co, ref double amountKWh, double seconds)
    {
        double rate = Rate(co, seconds);
        if (rate != 1) amountKWh *= rate;
        return rate;
    }
    /// <summary>The rate without any work done: for panels.</summary>
    public static double Rate(CondOwner? co)
    {
        if (co == null || co.strID == null || FamilyOf(co.strCODef) is not Family family || !family.Tunable) return 1;
        var state = State(co);
        return state.Protected ? 1 : UpkeepRules.Rate(state.Level, Settings.MaxTuningGain, Share(family));
    }
    private static double Share(Family family) => Pack.families.TryGetValue(family.Key, out var entry) ? entry.gainShare : 1;

    /// <summary>One line for a machine's panel: its tune and whether it fades, how to get one, or nothing for a machine
    /// that is never tuned.</summary>
    public static string StatusLine(CondOwner? co)
    {
        if (co == null || FamilyOf(co.strCODef) is not Family family || !family.Tunable || !(Settings.MaxTuningGain * Share(family) > 0)) return "";
        var state = State(co);
        if (state.Protected) return Text.Get("Upkeep.status_protected");
        double percent = (UpkeepRules.Rate(state.Level, Settings.MaxTuningGain, Share(family)) - 1) * 100;
        if (percent < 0.05) return Text.Get(Enabled(UpkeepKind.Tune) ? "Upkeep.status_none_on" : "Upkeep.status_none_off");
        bool inspected = UpkeepRules.InspectionGood(state.Inspected, StarSystem.fEpoch, Pack.inspectionValidHours);
        return Text.Get(inspected ? "Upkeep.status_tuned_inspected" : "Upkeep.status_tuned", percent.ToString("0.#", CultureInfo.InvariantCulture));
    }

    // ---- The ship-wide switches, kept on the player ----

    /// <summary>The word for a kind in text keys and F3: tune, inspect, practice or tidy.</summary>
    public static string Word(UpkeepKind kind) => kind switch
    {
        UpkeepKind.Tune => "tune", UpkeepKind.Inspect => "inspect", UpkeepKind.Practice => "practice", _ => "tidy"
    };
    /// <summary>The switches in the order the panel and reports show them.</summary>
    public static readonly UpkeepKind[] Kinds = { UpkeepKind.Tune, UpkeepKind.Inspect, UpkeepKind.Practice, UpkeepKind.Housekeeping };

    private static ObjectStateStore Switches(CondOwner player) => new(player.mapGUIPropMaps, SwitchRecord, FrameworkInfo.PluginId, 1);
    private static void ReadSwitches()
    {
        var player = CrewSim.coPlayer;
        if (ReferenceEquals(player, switchOwner)) return;
        switchOwner = player; switches = new UpkeepSwitches(); switchesProtected = false;
        if (player == null) return;
        var status = Switches(player).Read(out var fields);
        if (status == SavedStateStatus.Ready) switches = UpkeepSwitches.Decode(fields);
        else switchesProtected = status != SavedStateStatus.Missing;
    }
    public static bool Enabled(UpkeepKind kind) { ReadSwitches(); return switches[kind]; }
    public static bool AnyEnabled { get { ReadSwitches(); return switches.Any; } }
    /// <summary>Turns a switch on or off and says what happened.</summary>
    public static string Set(UpkeepKind kind, bool on)
    {
        ReadSwitches();
        var player = CrewSim.coPlayer;
        if (player == null) return Text.Get("Upkeep.not_in_game");
        if (switchesProtected) return Text.Get("Upkeep.switch_protected");
        var next = switches.With(kind, on);
        if (!Switches(player).TryWrite(next.Encode())) return Text.Get("Upkeep.switch_protected");
        switches = next;
        // Jobs of a kind just switched off are withdrawn at once; a kind just switched on is planned on the next pass.
        foreach (var job in CrewWork.Jobs.Values.Where(j => j.Upkeep != null && !Enabled(j.Upkeep.Value)).ToArray()) CrewWork.Release(job, true);
        CrewWork.ForgetAnnouncement(AnnounceKey); cadence.Invalidate(); nextTidyScan = 0;
        return Text.Get("Upkeep." + Word(kind) + "_" + (on ? "on" : "off"));
    }
    internal const string AnnounceKey = "upkeep";

    // ---- Who takes upkeep work ----

    private static bool OnShift(CondOwner actor, int? hour = null) => actor != null && !actor.bDestroyed && actor.bAlive && actor.Company != null &&
        !actor.HasCond("IsAIManual") && actor.Company.GetShift(hour ?? StarSystem.nUTCHour, actor).nID == 2;

    /// <summary>Whether this crew member may take this upkeep job, beyond the standing orders' own checks: nobody a
    /// content mod has marked unavailable, and only someone not yet skilled practises.</summary>
    internal static bool Suits(UpkeepKind? kind, CrewWorkOffer offer, CondOwner actor) =>
        kind == null || !Unavailable(actor) && (kind != UpkeepKind.Practice || !CrewSpecialities.Skilled(actor, offer.Skill));
    /// <summary>Whether a skilled crew member goes first, as for standing orders. Practice is for the unskilled.</summary>
    internal static bool SkilledFirst(UpkeepKind? kind) => kind != UpkeepKind.Practice;
    private static bool Learner(CondOwner actor, Family family) => CrewSpecialities.IsSpeciality(family.Skill) &&
        !CrewSpecialities.Skilled(actor, family.Skill) && CrewSpecialities.Allowed(actor, family.Role);

    private static double Seconds(UpkeepKind kind) => kind switch
    {
        UpkeepKind.Tune => Settings.TuningMinutes * 60, UpkeepKind.Inspect => Settings.InspectionMinutes * 60,
        UpkeepKind.Practice => Pack.practiceMinutes * 60, _ => CrewBalance.HandlingSeconds
    };

    private static CrewWorkOffer Offer(CondOwner co, Family family, UpkeepKind kind)
    {
        string name = Controls.ObjectPresentation.Name(co);
        var offer = new CrewWorkOffer("upkeep-" + Word(kind), Text.Get("Upkeep." + Word(kind) + "_label", name), family.Role, co, Seconds(kind), family.Skill);
        string? excluded = null;
        try { excluded = family.Excluded?.Invoke(co); } catch { }
        if (!string.IsNullOrEmpty(excluded)) offer.ExcludedActor = excluded!;
        return offer;
    }
    /// <summary>Housekeeping is hauling: the Haul duty, any crew member allowed industrial work, and no speciality.</summary>
    private static CrewWorkOffer TidyOffer(CondOwner item, CondOwner store) =>
        new("upkeep-tidy", Text.Get("Upkeep.tidy_label", item.strNameFriendly, Controls.ObjectPresentation.Name(store)), CrewRole.Industry, store,
            CrewBalance.HandlingSeconds, duty: "Haul", cargo: item, destination: store);

    /// <summary>The machines on these ships that could take upkeep now.</summary>
    private static List<(CondOwner Machine, Family Family)> Machines(Ship[] ships)
    {
        var result = new List<(CondOwner, Family)>();
        if (world == null) return result;
        scratch.Clear(); world.Members(scratch);
        foreach (var co in scratch)
        {
            if (co == null || co.bDestroyed || co.objCOParent != null || co.ship == null || Array.IndexOf(ships, co.ship) < 0 || !co.HasCond("IsInstalled") ||
                co.HasCond("IsDamaged") || !CrewWork.CanManage(co) || FamilyOf(co.strCODef) is not Family family) continue;
            result.Add((co, family));
        }
        scratch.Clear();
        return result;
    }

    // ---- Housekeeping: what lies on the deck and where it goes ----

    /// <summary>A stack head lying loose on the deck of this ship: not installed, not in anyone's hands, not in any
    /// container or tray, and a supply (it stacks), so equipment waiting to be installed is left alone.</summary>
    private static bool TidyItem(CondOwner? co, Ship ship) => co != null && !co.bDestroyed && co.ship == ship && co.objCOParent == null && co.slotNow == null &&
        co.coStackHead == null && co.nStackLimit > 1 && !co.HasCond("IsInstalled") && !co.HasCond("IsHuman") && CrewLogistics.Loose(co);

    private sealed class TidyStore
    {
        internal CondOwner Store = null!; internal bool Tidy; internal HashSet<string> Kinds = null!;
    }
    /// <summary>The stores housekeeping may fill on a ship: the game's own unlocked containers, and Phobos stores only
    /// when a content mod named them tidy stores, so nothing is put in a machine's tray, a bed's drawer or a tank's rack.</summary>
    private static List<TidyStore> TidyStores(Ship ship) => CrewWork.Stores(ship)
        .Where(s => CrewWork.CanManage(s) && (IsTidyStore(s.strCODef) || s.strCODef?.StartsWith(PhobosPrefix, StringComparison.Ordinal) != true))
        .Select(s => new TidyStore { Store = s, Tidy = IsTidyStore(s.strCODef),
            Kinds = new HashSet<string>(s.objContainer.ContainedCOs.Where(c => c != null && c.strCODef != null).Select(c => c.strCODef), StringComparer.Ordinal) })
        .ToList();
    private static CondOwner? Destination(CondOwner item, List<TidyStore> stores)
    {
        bool phobos = item.strCODef?.StartsWith(PhobosPrefix, StringComparison.Ordinal) == true;
        var origin = item.GetPos();
        var choices = new List<TidyStoreChoice>();
        foreach (var s in stores)
        {
            bool same = item.strCODef != null && s.Kinds.Contains(item.strCODef);
            if (!s.Tidy && !(phobos && same)) continue;
            choices.Add(new TidyStoreChoice(s.Store.strID, TileUtils.TileRange(origin, s.Store.GetPos("use")), same, s.Tidy, Inventory.UnitItemTransfer.Fits(s.Store, item, out _)));
        }
        string? id = UpkeepRules.TidyDestination(phobos, choices);
        return id == null ? null : stores.First(s => s.Store.strID == id).Store;
    }
    /// <summary>Up to <paramref name="max"/> items to carry and where each goes. Only the ships' top-level objects are
    /// read; an item another job carries or holds is left out.</summary>
    private static List<(CondOwner Item, CondOwner Store)> Tidy(Ship[] ships, int max)
    {
        var result = new List<(CondOwner, CondOwner)>();
        var carried = new HashSet<string>(CrewWork.Jobs.Values.Where(j => j.Offer.Cargo != null).Select(j => j.Offer.Cargo!.strID), StringComparer.Ordinal);
        foreach (var ship in ships)
        {
            if (result.Count >= max || ship == null) break;
            List<TidyStore>? stores = null;
            foreach (var item in ship.GetCOs(null, false, false, true))
            {
                if (result.Count >= max) break;
                if (!TidyItem(item, ship) || carried.Contains(item.strID) || skipFailed.Contains(item.strID) || !CrewWork.Reservations.Available("item:" + item.strID, "")) continue;
                stores ??= TidyStores(ship);
                if (stores.Count == 0) break;
                if (Destination(item, stores) is CondOwner store) { result.Add((item, store)); carried.Add(item.strID); }
            }
        }
        return result;
    }

    // ---- Planning in play ----

    /// <summary>Every ten real seconds while a switch is on: offer idle on-shift crew one job each, after any
    /// standing-order step that is still waiting for someone. Tuning and inspection first, then housekeeping, then
    /// practice (<see cref="UpkeepRules.Priority"/>).</summary>
    internal static void Plan(CondOwner[] crew, Ship[] ships)
    {
        if (!AnyEnabled || !cadence.Due()) return;
        using var measurement = Diagnostics.Performance.Measure(Diagnostics.Performance.UpkeepPlan);
        float now = UnityEngine.Time.unscaledTime;
        foreach (var stale in CrewWork.Jobs.Values.Where(j => j.Upkeep != null && j.Worker == null && now - j.Created > StaleTaskSeconds).ToArray()) CrewWork.Release(stale, true);
        if (CrewWork.Jobs.Values.Any(j => j.Upkeep == null && j.Worker == null)) return;
        int open = CrewWork.Jobs.Values.Count(j => j.Upkeep != null && j.Worker == null);
        var idle = crew.Where(c => CrewWork.Idle(c) && OnShift(c) && !Unavailable(c)).ToArray();
        int want = Math.Min(TasksPerPass, idle.Length - open);
        if (want <= 0) return;
        var manager = CrewSim.objInstance.workManager;
        var taken = new HashSet<CondOwner>(CrewWork.Jobs.Values.Where(j => j.Upkeep != null).Select(j => j.Equipment));
        var machines = families.Count == 0 ? new List<(CondOwner Machine, Family Family)>() : Machines(ships);
        int added = 0;
        if (Enabled(UpkeepKind.Tune) || Enabled(UpkeepKind.Inspect))
        {
            var free = machines.Where(m => !taken.Contains(m.Machine)).ToList();
            var lookup = free.ToDictionary(m => m.Machine.strID, m => m, StringComparer.Ordinal);
            var plan = UpkeepRules.Plan(free.Select(m => (m.Machine.strID, State(m.Machine), m.Family.Tunable && Settings.MaxTuningGain * Share(m.Family) > 0)),
                Enabled(UpkeepKind.Tune), Enabled(UpkeepKind.Inspect), StarSystem.fEpoch, Pack);
            foreach (var candidate in plan)
            {
                if (added >= want) break;
                var (machine, family) = lookup[candidate.Id];
                if (Add(manager, machine, Offer(machine, family, candidate.Kind), candidate.Kind, now)) { added++; taken.Add(machine); }
            }
        }
        if (added < want && Enabled(UpkeepKind.Housekeeping) && now >= nextTidyScan)
        {
            var moves = Tidy(ships, want - added);
            if (moves.Count == 0) nextTidyScan = now + TidyQuietSeconds;
            foreach (var (item, store) in moves)
                if (Add(manager, store, TidyOffer(item, store), UpkeepKind.Housekeeping, now)) added++;
        }
        if (added < want && Enabled(UpkeepKind.Practice))
        {
            // One practice job per ship and speciality at a time, and only where an idle crew member could learn it.
            var practising = new HashSet<(Ship, string)>(CrewWork.Jobs.Values.Where(j => j.Upkeep == UpkeepKind.Practice && j.Equipment.ship != null)
                .Select(j => (j.Equipment.ship, j.Offer.Skill)));
            foreach (var (machine, family) in machines.OrderBy(m => m.Machine.strID, StringComparer.Ordinal))
            {
                if (added >= want) break;
                if (taken.Contains(machine) || practising.Contains((machine.ship, family.Skill)) || !idle.Any(c => c.ship == machine.ship && Learner(c, family))) continue;
                if (Add(manager, machine, Offer(machine, family, UpkeepKind.Practice), UpkeepKind.Practice, now)) { added++; taken.Add(machine); practising.Add((machine.ship, family.Skill)); }
            }
        }
    }
    private static bool Add(WorkManager manager, CondOwner equipment, CrewWorkOffer offer, UpkeepKind kind, float now)
    {
        var task = new Task2 { strName = CrewWork.WorkId + ".upkeep." + offer.Target.strID + (offer.Cargo != null ? "." + offer.Cargo.strID : ""),
            strInteraction = CrewWork.WorkId, strTargetCOID = offer.Target.strID, strDuty = offer.Duty, bManual = false };
        var job = new CrewWork.Job { Equipment = equipment, Offer = offer, Task = task, Upkeep = kind, Created = now };
        if (!manager.AddTask(task, CrewWork.TasksPerTarget)) return false;
        CrewWork.Jobs.Add(task, job); CrewWork.AnnounceOnce(AnnounceKey, manager);
        return true;
    }

    /// <summary>An upkeep job finished, in play or in a time-skip: carry the item, credit the practice, or tune or
    /// inspect the machine and credit the work.</summary>
    internal static bool Finish(CondOwner worker, CondOwner equipment, CrewWorkOffer offer, UpkeepKind kind, double seconds, bool skipping, out string reason)
    {
        reason = "";
        switch (kind)
        {
            case UpkeepKind.Housekeeping:
                return CrewLogistics.Deliver(new CrewWorkContext(worker, equipment, new StandingOrder(), skipping), offer, out reason);
            case UpkeepKind.Practice:
                if (equipment == null || equipment.bDestroyed || !equipment.HasCond("IsInstalled") || FamilyOf(equipment.strCODef) == null)
                { reason = Text.Get("Upkeep.machine_gone"); return false; }
                // Practice trains as studying at a terminal does (Framework 0.113.0, agent default).
                CrewSpecialities.Credit(worker, offer.Skill, seconds, true);
                return true;
            default:
                if (!Complete(worker, equipment, kind, out reason)) return false;
                CrewSpecialities.Credit(worker, offer.Skill, seconds, false);
                return true;
        }
    }

    /// <summary>A session finished at a machine: the tune rises, or the inspection is noted and anything the machine
    /// waits for is reported.</summary>
    internal static bool Complete(CondOwner worker, CondOwner machine, UpkeepKind kind, out string reason)
    {
        reason = "";
        if (machine == null || machine.bDestroyed || FamilyOf(machine.strCODef) is not Family family) { reason = Text.Get("Upkeep.machine_gone"); return false; }
        var state = State(machine);
        if (state.Protected) { reason = Text.Get("Upkeep.status_protected"); return false; }
        if (kind == UpkeepKind.Tune) state.Level = UpkeepRules.Tuned(state.Level, CrewSpecialities.Skilled(worker, family.Skill), Pack);
        else
        {
            state.Inspected = StarSystem.fEpoch;
            string? attention = null;
            try { attention = family.Attention?.Invoke(machine); } catch { }
            double damage = machine.GetCondAmount("StatDamage"), limit = machine.GetCondAmount("StatDamageMax");
            if (limit > 0 && damage / limit > 0.5) attention = string.IsNullOrEmpty(attention) ? Text.Get("Upkeep.inspect_damage") : attention + " " + Text.Get("Upkeep.inspect_damage");
            if (!string.IsNullOrEmpty(attention) && machine.ship != null)
                Notices.PlayerNotices.Post(machine.ship, "upkeep.inspect." + machine.strID, Notices.NoticeLevel.Info,
                    Text.Get("Upkeep.inspect_report", worker.FriendlyName, Controls.ObjectPresentation.Name(machine), attention!));
        }
        Save(machine, state);
        return true;
    }

    // ---- Time-skips ----

    internal static void SkipStarting() { banked.Clear(); skipTidy.Clear(); skipTidyEmpty.Clear(); skipFailed.Clear(); }

    private enum SkipOutcome { Nothing, Waiting, Done }

    /// <summary>During a time-skip: on-shift crew not held by a standing order bank the step, and whenever a job and
    /// the walk to it are banked, one is done, in the same order as in play. Walking is charged by tile distance at the
    /// standing orders' rate; no path is searched for the walk. The skip's own stepping and the game's repair allowance
    /// are left as they are.</summary>
    internal static void SkipStep(IEnumerable<CondOwner> crew, Ship[] ships, double step, Func<string, bool>? held)
    {
        if (!AnyEnabled || !(step > 0)) return;
        double least = Kinds.Where(Enabled).Select(Seconds).DefaultIfEmpty(double.MaxValue).Min();
        List<(CondOwner Machine, Family Family)>? machines = null;
        foreach (var actor in crew)
        {
            if (actor == null || actor.strID == null || held?.Invoke(actor.strID) == true || !OnShift(actor) || Unavailable(actor)) continue;
            double bank = (banked.TryGetValue(actor.strID, out var had) ? had : 0) + step;
            banked[actor.strID] = bank;
            if (bank < least * CrewBalance.SkilledDurationFraction) continue;
            if (families.Count > 0) machines ??= Machines(ships);
            var outcome = SkipJob(actor, machines, ref bank);
            // Nothing to do: do not let hours of idle time pile up into a burst of jobs later. A job waiting for
            // enough banked time keeps what is banked.
            banked[actor.strID] = outcome == SkipOutcome.Nothing ? Math.Min(bank, least) : bank;
        }
    }
    private static SkipOutcome SkipJob(CondOwner actor, List<(CondOwner Machine, Family Family)>? machines, ref double bank)
    {
        var mine = machines?.Where(m => m.Machine.ship == actor.ship).ToList();
        if (mine != null && mine.Count > 0 && (Enabled(UpkeepKind.Tune) || Enabled(UpkeepKind.Inspect)))
        {
            var plan = UpkeepRules.Plan(mine.Select(m => (m.Machine.strID, State(m.Machine), m.Family.Tunable && Settings.MaxTuningGain * Share(m.Family) > 0)),
                Enabled(UpkeepKind.Tune), Enabled(UpkeepKind.Inspect), StarSystem.fEpoch, Pack);
            foreach (var candidate in plan)
            {
                var (machine, family) = mine.First(m => m.Machine.strID == candidate.Id);
                var offer = Offer(machine, family, candidate.Kind);
                if (!CrewWork.Eligible(actor, offer, out _)) continue;
                return Spend(actor, machine, offer, candidate.Kind, CrewWork.Reach(actor, machine) * CrewBalance.WalkSecondsPerTile, ref bank);
            }
        }
        if (Enabled(UpkeepKind.Housekeeping) && NextTidy(actor.ship) is var (item, store))
        {
            var offer = TidyOffer(item, store);
            if (CrewWork.Eligible(actor, offer, out _))
                return Spend(actor, store, offer, UpkeepKind.Housekeeping, (CrewWork.Reach(actor, item) + CrewWork.Reach(item, store)) * CrewBalance.WalkSecondsPerTile, ref bank);
        }
        if (mine != null && Enabled(UpkeepKind.Practice))
            foreach (var (machine, family) in mine.Where(m => Learner(actor, m.Family)).OrderBy(m => m.Machine.strID, StringComparer.Ordinal))
            {
                var offer = Offer(machine, family, UpkeepKind.Practice);
                if (!CrewWork.Eligible(actor, offer, out _)) continue;
                return Spend(actor, machine, offer, UpkeepKind.Practice, CrewWork.Reach(actor, machine) * CrewBalance.WalkSecondsPerTile, ref bank);
            }
        return SkipOutcome.Nothing;
    }
    private static SkipOutcome Spend(CondOwner actor, CondOwner equipment, CrewWorkOffer offer, UpkeepKind kind, double travel, ref double bank)
    {
        double seconds = kind == UpkeepKind.Practice || kind == UpkeepKind.Housekeeping ? offer.Seconds : CrewBalance.Duration(offer.Seconds, CrewSpecialities.Skilled(actor, offer.Skill));
        if (UpkeepRules.Sessions(ref bank, seconds, travel) == 0) return SkipOutcome.Waiting;
        bool done = Finish(actor, equipment, offer, kind, seconds, true, out _);
        if (kind == UpkeepKind.Housekeeping && offer.Cargo != null)
        {
            if (!done) skipFailed.Add(offer.Cargo.strID);
            // A move leaves the rest of a stack on the deck under a new head: look again on the next job.
            else if (equipment.ship != null && skipTidy.TryGetValue(equipment.ship, out var queue) && queue.Count == 0) skipTidy.Remove(equipment.ship);
        }
        return SkipOutcome.Done;
    }
    /// <summary>The next move housekeeping makes on a ship during a skip. The deck is read once and the moves used up;
    /// a ship with nothing to tidy is read again after <see cref="TidyRefreshSeconds"/>.</summary>
    private static (CondOwner Item, CondOwner Store)? NextTidy(Ship? ship)
    {
        if (ship == null) return null;
        if (!skipTidy.TryGetValue(ship, out var queue))
        {
            if (skipTidyEmpty.TryGetValue(ship, out var at) && StarSystem.fEpoch - at < TidyRefreshSeconds) return null;
            queue = new Queue<(CondOwner, CondOwner)>(Tidy(new[] { ship }, TidyLimit));
            if (queue.Count == 0) { skipTidyEmpty[ship] = StarSystem.fEpoch; return null; }
            skipTidyEmpty.Remove(ship); skipTidy[ship] = queue;
        }
        while (queue.Count > 0)
        {
            var next = queue.Peek();
            if (TidyItem(next.Item, ship) && !next.Store.bDestroyed && !skipFailed.Contains(next.Item.strID)) return next;
            queue.Dequeue();
        }
        skipTidy.Remove(ship);
        return null;
    }

    // ---- Replacement, reset and reports ----

    /// <summary>A mode switch made a new object. A machine that is damaged or no longer installed has lost its tune.</summary>
    internal static void Replaced(CondOwner? co)
    {
        if (co == null || co.strID == null) return;
        states.Remove(co.strID);
        if (co.HasCond("IsInstalled") && !co.HasCond("IsDamaged")) return;
        var store = CrewWork.Store(co, RecordName);
        if (store.Read(out var fields) != SavedStateStatus.Ready) return;
        var state = UpkeepRules.Decode(fields);
        if (!(state.Level > 0)) return;
        state.Level = 0; store.TryWrite(UpkeepRules.Encode(state));
    }

    internal static void Reset()
    {
        states.Clear(); banked.Clear(); byDefinition.Clear(); SkipStarting();
        switchOwner = null; switches = new UpkeepSwitches(); switchesProtected = false; cadence = new Cadence(PlanSeconds); nextTidyScan = 0;
    }

    /// <summary>The F3 and panel report: the switches, then every machine with its tune and inspection.</summary>
    public static string Describe(Ship? ship = null)
    {
        if (CrewSim.coPlayer == null || CrewSim.system == null) return Text.Get("Upkeep.not_in_game");
        string Shown(UpkeepKind kind) => Text.Get(Enabled(kind) ? "Upkeep.on" : "Upkeep.off");
        var lines = new List<string>
        {
            Text.Get("Upkeep.report_switches", Shown(UpkeepKind.Tune), Shown(UpkeepKind.Inspect)) + " " +
                Text.Get("Upkeep.report_switches_more", Shown(UpkeepKind.Practice), Shown(UpkeepKind.Housekeeping)),
            Text.Get("Upkeep.report_settings", Settings.TuningMinutes, Settings.InspectionMinutes, (Settings.MaxTuningGain * 100).ToString("0.#", CultureInfo.InvariantCulture), Settings.TuneFadeHours),
            Text.Get("Upkeep.report_practice", Pack.practiceMinutes)
        };
        var ships = ship != null ? new[] { ship } : CrewRoster.Members().Select(c => c.ship).Distinct().ToArray();
        var machines = Machines(ships);
        if (machines.Count == 0) lines.Add(Text.Get("Upkeep.report_none"));
        foreach (var (machine, family) in machines.OrderBy(m => Controls.ObjectPresentation.Name(m.Machine), StringComparer.Ordinal).Take(MaxReported))
        {
            var state = State(machine);
            string tuneText = !family.Tunable ? Text.Get("Upkeep.report_not_tuned") : ((Rate(machine) - 1) * 100).ToString("0.#", CultureInfo.InvariantCulture) + "%";
            string inspected = state.Inspected <= 0 ? Text.Get("Upkeep.report_never") :
                Text.Get("Upkeep.report_hours_ago", Math.Max(0, (StarSystem.fEpoch - state.Inspected) / 3600).ToString("0.#", CultureInfo.InvariantCulture));
            lines.Add(Text.Get("Upkeep.report_line", Controls.ObjectPresentation.Name(machine), tuneText, inspected));
        }
        if (machines.Count > MaxReported) lines.Add(Text.Get("Upkeep.report_more", machines.Count - MaxReported));
        return string.Join("\n", lines);
    }
    private const int MaxReported = 40;

    /// <summary>F3: <c>phobosframework upkeep [tune|inspect|practice|tidy on|off]</c>.</summary>
    internal static string Command(string[] words)
    {
        if (words.Length == 2) return Describe();
        if (words.Length == 4 && (words[3] == "on" || words[3] == "off"))
            foreach (var kind in Kinds)
                if (Word(kind) == words[2] || kind == UpkeepKind.Housekeeping && words[2] == "housekeeping") return Set(kind, words[3] == "on");
        return Text.Get("Upkeep.help");
    }
}
