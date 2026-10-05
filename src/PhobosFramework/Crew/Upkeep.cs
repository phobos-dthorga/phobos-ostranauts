using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Persistence;

namespace Phobos.Ostranauts.Framework.Crew;

/// <summary>Crew upkeep (Framework 0.111.0; owner request, 6 October 2026): work for idle on-shift crew on long hauls.
/// Two ship-wide switches, off until chosen, are kept on the player: <b>Tune machinery</b> and <b>Inspection rounds</b>.
/// While one is on, a planner offers idle crew one session at a time at a machine a content mod has registered, through
/// the same native task, reservation and eligibility path as standing orders, and always after them. A tuned machine's
/// work counts for more (<see cref="Rate"/>), and the tune fades as the machine works; an inspected machine fades at a
/// lesser rate. Nothing else about a job changes: yields, masses and energy per job stay as authored.</summary>
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
    private static readonly List<Family> families = new();
    private static readonly Dictionary<string, Family?> byDefinition = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, UpkeepState> states = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, double> banked = new(StringComparer.Ordinal);
    private static readonly List<CondOwner> scratch = new();
    private static Discovery.WorldFamily? world;
    private static UpkeepPack? pack;
    private static Cadence cadence = new(PlanSeconds);
    private static CondOwner? switchOwner;
    private static bool tune, inspect, switchesProtected;
    public static UpkeepSettings Settings { get; set; } = new();
    public static UpkeepPack Pack => pack ??= Load();
    internal static int StateCount => states.Count + banked.Count;
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

    private static ObjectStateStore Switches(CondOwner player) => new(player.mapGUIPropMaps, SwitchRecord, FrameworkInfo.PluginId, 1);
    private static void ReadSwitches()
    {
        var player = CrewSim.coPlayer;
        if (ReferenceEquals(player, switchOwner)) return;
        switchOwner = player; tune = inspect = switchesProtected = false;
        if (player == null) return;
        var status = Switches(player).Read(out var fields);
        if (status == SavedStateStatus.Ready) { tune = fields.TryGetValue("tune", out var t) && t == "1"; inspect = fields.TryGetValue("inspect", out var i) && i == "1"; }
        else switchesProtected = status != SavedStateStatus.Missing;
    }
    public static bool Enabled(UpkeepKind kind) { ReadSwitches(); return kind == UpkeepKind.Tune ? tune : inspect; }
    public static bool AnyEnabled { get { ReadSwitches(); return tune || inspect; } }
    /// <summary>Turns a switch on or off and says what happened.</summary>
    public static string Set(UpkeepKind kind, bool on)
    {
        ReadSwitches();
        var player = CrewSim.coPlayer;
        if (player == null) return Text.Get("Upkeep.not_in_game");
        if (switchesProtected) return Text.Get("Upkeep.switch_protected");
        bool nextTune = kind == UpkeepKind.Tune ? on : tune, nextInspect = kind == UpkeepKind.Inspect ? on : inspect;
        if (!Switches(player).TryWrite(new Dictionary<string, string> { ["tune"] = nextTune ? "1" : "0", ["inspect"] = nextInspect ? "1" : "0" }))
            return Text.Get("Upkeep.switch_protected");
        tune = nextTune; inspect = nextInspect;
        // Jobs of a kind just switched off are withdrawn at once; a kind just switched on is planned on the next pass.
        foreach (var job in CrewWork.Jobs.Values.Where(j => j.Upkeep != null && !Enabled(j.Upkeep.Value)).ToArray()) CrewWork.Release(job, true);
        CrewWork.ForgetAnnouncement(AnnounceKey); cadence.Invalidate();
        return Text.Get((kind == UpkeepKind.Tune ? "Upkeep.tune_" : "Upkeep.inspect_") + (on ? "on" : "off"));
    }
    internal const string AnnounceKey = "upkeep";

    // ---- Planning in play ----

    private static bool OnShift(CondOwner actor, int? hour = null) => actor != null && !actor.bDestroyed && actor.bAlive && actor.Company != null &&
        !actor.HasCond("IsAIManual") && actor.Company.GetShift(hour ?? StarSystem.nUTCHour, actor).nID == 2;

    private static double Seconds(UpkeepKind kind) => (kind == UpkeepKind.Tune ? Settings.TuningMinutes : Settings.InspectionMinutes) * 60;

    private static CrewWorkOffer Offer(CondOwner co, Family family, UpkeepKind kind)
    {
        string name = Controls.ObjectPresentation.Name(co);
        var offer = new CrewWorkOffer(kind == UpkeepKind.Tune ? "upkeep-tune" : "upkeep-inspect",
            Text.Get(kind == UpkeepKind.Tune ? "Upkeep.tune_label" : "Upkeep.inspect_label", name), family.Role, co, Seconds(kind), family.Skill);
        string? excluded = null;
        try { excluded = family.Excluded?.Invoke(co); } catch { }
        if (!string.IsNullOrEmpty(excluded)) offer.ExcludedActor = excluded!;
        return offer;
    }

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

    /// <summary>Every ten real seconds while a switch is on: offer idle on-shift crew one session each, after any
    /// standing-order step that is still waiting for someone.</summary>
    internal static void Plan(CondOwner[] crew, Ship[] ships)
    {
        if (families.Count == 0 || !AnyEnabled || !cadence.Due()) return;
        using var measurement = Diagnostics.Performance.Measure(Diagnostics.Performance.UpkeepPlan);
        float now = UnityEngine.Time.unscaledTime;
        foreach (var stale in CrewWork.Jobs.Values.Where(j => j.Upkeep != null && j.Worker == null && now - j.Created > StaleTaskSeconds).ToArray()) CrewWork.Release(stale, true);
        if (CrewWork.Jobs.Values.Any(j => j.Upkeep == null && j.Worker == null)) return;
        int open = CrewWork.Jobs.Values.Count(j => j.Upkeep != null && j.Worker == null);
        int idle = crew.Count(c => CrewWork.Idle(c) && OnShift(c));
        int want = Math.Min(TasksPerPass, idle - open);
        if (want <= 0) return;
        var machines = Machines(ships).Where(m => !CrewWork.Jobs.Values.Any(j => j.Upkeep != null && j.Equipment == m.Machine)).ToList();
        if (machines.Count == 0) return;
        var lookup = machines.ToDictionary(m => m.Machine.strID, m => m, StringComparer.Ordinal);
        var plan = UpkeepRules.Plan(machines.Select(m => (m.Machine.strID, State(m.Machine), m.Family.Tunable && Settings.MaxTuningGain * Share(m.Family) > 0)),
            Enabled(UpkeepKind.Tune), Enabled(UpkeepKind.Inspect), StarSystem.fEpoch, Pack);
        var manager = CrewSim.objInstance.workManager;
        foreach (var candidate in plan.Take(want))
        {
            var (machine, family) = lookup[candidate.Id];
            var offer = Offer(machine, family, candidate.Kind);
            var task = new Task2 { strName = CrewWork.WorkId + ".upkeep." + machine.strID, strInteraction = CrewWork.WorkId, strTargetCOID = machine.strID, strDuty = offer.Duty, bManual = false };
            var job = new CrewWork.Job { Equipment = machine, Offer = offer, Task = task, Upkeep = candidate.Kind, Created = now };
            if (manager.AddTask(task, CrewWork.TasksPerTarget)) { CrewWork.Jobs.Add(task, job); CrewWork.AnnounceOnce(AnnounceKey, manager); }
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

    internal static void SkipStarting() => banked.Clear();

    /// <summary>During a time-skip: on-shift crew not held by a standing order bank the step, and whenever a session and
    /// the walk to it are banked, one is done. Walking is charged by tile distance at the standing orders' rate; no path
    /// is searched. The skip's own stepping and the game's repair allowance are left as they are.</summary>
    internal static void SkipStep(IEnumerable<CondOwner> crew, Ship[] ships, double step, Func<string, bool>? held)
    {
        if (families.Count == 0 || !AnyEnabled || !(step > 0)) return;
        double least = Math.Min(Enabled(UpkeepKind.Tune) ? Seconds(UpkeepKind.Tune) : double.MaxValue, Enabled(UpkeepKind.Inspect) ? Seconds(UpkeepKind.Inspect) : double.MaxValue);
        List<(CondOwner Machine, Family Family)>? machines = null;
        foreach (var actor in crew)
        {
            if (actor == null || actor.strID == null || held?.Invoke(actor.strID) == true || !OnShift(actor)) continue;
            double bank = (banked.TryGetValue(actor.strID, out var had) ? had : 0) + step;
            banked[actor.strID] = bank;
            if (bank < least * CrewBalance.SkilledDurationFraction) continue;
            machines ??= Machines(ships);
            var plan = UpkeepRules.Plan(machines.Where(m => m.Machine.ship == actor.ship).Select(m => (m.Machine.strID, State(m.Machine), m.Family.Tunable && Settings.MaxTuningGain * Share(m.Family) > 0)),
                Enabled(UpkeepKind.Tune), Enabled(UpkeepKind.Inspect), StarSystem.fEpoch, Pack);
            bool worked = false;
            foreach (var candidate in plan)
            {
                var (machine, family) = machines.First(m => m.Machine.strID == candidate.Id);
                var offer = Offer(machine, family, candidate.Kind);
                if (!CrewWork.Eligible(actor, offer, out _)) continue;
                double session = CrewBalance.Duration(offer.Seconds, CrewSpecialities.Skilled(actor, family.Skill));
                double travel = CrewWork.Reach(actor, machine) * CrewBalance.WalkSecondsPerTile;
                if (UpkeepRules.Sessions(ref bank, session, travel) == 0) break;
                if (Complete(actor, machine, candidate.Kind, out _)) CrewSpecialities.Credit(actor, family.Skill, session, false);
                worked = true; break;
            }
            // Nothing to do: do not let hours of idle time pile up into a burst of sessions later.
            banked[actor.strID] = worked ? bank : Math.Min(bank, least);
        }
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

    internal static void Reset() { states.Clear(); banked.Clear(); byDefinition.Clear(); switchOwner = null; tune = inspect = switchesProtected = false; cadence = new Cadence(PlanSeconds); }

    /// <summary>The F3 and panel report: the switches, then every machine with its tune and inspection.</summary>
    public static string Describe(Ship? ship = null)
    {
        if (CrewSim.coPlayer == null || CrewSim.system == null) return Text.Get("Upkeep.not_in_game");
        var lines = new List<string>
        {
            Text.Get("Upkeep.report_switches", Text.Get(Enabled(UpkeepKind.Tune) ? "Upkeep.on" : "Upkeep.off"), Text.Get(Enabled(UpkeepKind.Inspect) ? "Upkeep.on" : "Upkeep.off")),
            Text.Get("Upkeep.report_settings", Settings.TuningMinutes, Settings.InspectionMinutes, (Settings.MaxTuningGain * 100).ToString("0.#", CultureInfo.InvariantCulture), Settings.TuneFadeHours)
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

    /// <summary>F3: <c>phobosframework upkeep [tune|inspect on|off]</c>.</summary>
    internal static string Command(string[] words)
    {
        if (words.Length == 2) return Describe();
        if (words.Length == 4 && (words[2] == "tune" || words[2] == "inspect") && (words[3] == "on" || words[3] == "off"))
            return Set(words[2] == "tune" ? UpkeepKind.Tune : UpkeepKind.Inspect, words[3] == "on");
        return Text.Get("Upkeep.help");
    }
}
