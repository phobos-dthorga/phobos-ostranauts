using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Phobos.Ostranauts.Framework.Persistence;
using Phobos.Ostranauts.Framework.Registration;

namespace PhobosShipbreaker.Core;

/// <summary>What the laser may work on: asteroid rock, a moored hull's ordinary wall panels, or both.</summary>
public enum LaserFilter { Rock, Walls, Both }

/// <summary>The Ablatine ML-2 mining laser: a 2 x 2 hull-mounted head that works the one ship moored to ours (a
/// tethered asteroid, or a hull the G4 has captured) inside a fixed arc. Rock takes the game's own damage, one
/// native damage stage per paid job, so the game's mining tables decide what falls out; a hull's ordinary wall
/// panel is freed by the game's own uninstall, as the G4 frees one, and left where it drops. Every figure here is
/// authored balance: the game's damage points are not joules, and nothing below is a measured laser.</summary>
public static class LaserRules
{
    public const string Prefix = "PhobosMiningLaser", Installed = Prefix + "Installed";
    /// <summary>Set while a paid job wants power; the power info draws the working amount under it.</summary>
    public const string Working = "PhobosMiningLaserWorking";
    public const int Footprint = 2;
    public const double MachineKg = 120;

    // Electrical demand. The working draw is the largest the air-cooled rule lets run for more than a few seconds:
    // the heat share below into about 800 mol of room air is roughly 0.9 K a second.
    public const double WorkingKW = 24;
    public const double IdleKW = 0.1;
    /// <summary>The share of delivered electricity that warms the room behind the mount; the rest is taken to leave
    /// with the vapour and debris. Authored for play: no real laser was measured for it.</summary>
    public const double WasteHeatFraction = 0.6;

    /// <summary>Native damage points bought by one second at the working draw. A crew member with the game's
    /// handheld mining laser manages roughly 0.15 a second.</summary>
    public const double PointsPerSecond = 1;
    /// <summary>Seconds at the working draw to free one wall panel: half the G4's time at twice its power.</summary>
    public const double WallSeconds = 60;
    /// <summary>The largest damage stage the laser takes on: rock and ice walls (10 to 30 points a stage) and intact
    /// cores (29, 58 and 115 points) are within it. Rock that can only leave gangue is refused separately.</summary>
    public const double MaximumPoints = 150;

    // The arc, in deck tiles (0.32 m each) from the emitter on the head's outer edge.
    public const double RangeTiles = 24;
    public const double ArcDegrees = 60;
    /// <summary>Line-of-sight sampling step, and how near the beam's path a person stops it.</summary>
    public const double StepTiles = 0.5, CrewClearanceTiles = 1.0;
    /// <summary>How long a started native change may take before the job is called uncertain (the G4's window).</summary>
    public const double PendingTimeoutSeconds = 30;
    /// <summary>Nothing within this many tiles of a mooring anchor is cut.</summary>
    public const double AnchorClearanceTiles = 1.5;

    // The firing sheet: four columns by two rows of footprint-sized cells, at a fixed rate.
    public const int SheetFrames = 8, SheetColumns = 4, SheetRows = 2, PixelsPerTile = 16;
    public const string SheetFrameRate = "12";
    /// <summary>The beam's origin on the head, in pixels from its centre: the middle of the outer edge.</summary>
    public const double EmitterPixelsX = 0, EmitterPixelsY = 16;
    public const double BeamThicknessTiles = 0.25;

    public const string Rock = "rock", Wall = "wall";

    // The radiator link (Shipbreaker 0.61.0). A head touching one of the F6's cooling assemblies may be paired with
    // it; its heat then goes to that assembly's finite store, not the room, and the high setting becomes available.
    /// <summary>The head's side of the link; the cooling assembly keeps its own single port, so one assembly serves
    /// one furnace or one laser, never both.</summary>
    public const string CoolingPort = "PhobosShipbreaker.LaserCooling";
    /// <summary>The high setting's draw. Its heat share is within what one cooling assembly sheds below its limit.</summary>
    public const double HighKW = 48;
    public const string PowerStandard = "standard", PowerHigh = "high";
    // Crew jobs (Shipbreaker 0.71.0): two saved switches beside the filter, off when absent, and how far around a
    // finished cut the laser looks for what it dropped (the game drops extra outputs within two tiles).
    public const string HaulJobsKey = "haul", DepositJobsKey = "deposits", SwitchOn = "on", SwitchOff = "off";
    /// <summary>The action prefix of every setting the laser's panel offers. Its provider accepts exactly these as
    /// configuration; a setting left out is refused on Apply for ever (the two job switches, until Shipbreaker 0.72.0).</summary>
    public static readonly string[] SettingPrefixes = { "filter:", "cooling:", "power:", HaulJobsKey + ":", DepositJobsKey + ":" };
    public const double JobSearchTiles = 2;
    public static bool ParseSwitch(string? text, out bool on)
    {
        on = text == SwitchOn;
        return on || text == SwitchOff;
    }
    public static bool ParsePower(string? text, out bool high)
    {
        high = text == PowerHigh;
        return high || text == PowerStandard;
    }
    /// <summary>The draw a new job captures: the high setting only counts while a paired assembly is ready.</summary>
    public static double JobKW(bool high, bool radiatorReady) => high && radiatorReady ? HighKW : WorkingKW;
    /// <summary>A job is a fixed amount of energy, so a higher draw finishes it sooner.</summary>
    public static double JobSeconds(double secondsAtWorkingDraw, double kw) => secondsAtWorkingDraw * WorkingKW / kw;
    /// <summary>Whether this step's heat goes to the paired assembly: it must be ready and have room for all of it.</summary>
    public static bool HeatToRadiator(bool radiatorReady, double headroomKJ, double stepHeatKJ) =>
        radiatorReady && Finite(headroomKJ) && Finite(stepHeatKJ) && stepHeatKJ >= 0 && headroomKJ >= stepHeatKJ;
    /// <summary>Two installed footprints, as centres and sizes in tiles, that touch or stand one tile apart and do
    /// not overlap: the same rule as the shared equipment links, for footprints that are not square.</summary>
    public static bool Touching(double ax, double ay, double aw, double ah, double bx, double by, double bw, double bh)
    {
        foreach (double v in new[] { ax, ay, aw, ah, bx, by, bw, bh }) if (!Finite(v)) return false;
        if (aw <= 0 || ah <= 0 || bw <= 0 || bh <= 0) return false;
        double gap = Math.Max(Math.Abs(ax - bx) - (aw + bw) / 2, Math.Abs(ay - by) - (ah + bh) / 2);
        return gap >= -1e-6 && gap <= 1 + 1e-6;
    }

    public static bool IsFamily(string? id) => EquipmentIdentity.IsFamily(id, Prefix);
    public static bool Finite(double n) => !double.IsNaN(n) && !double.IsInfinity(n);
    public static bool AdmitRock(double pointsLeft) => Finite(pointsLeft) && pointsLeft > 0 && pointsLeft <= MaximumPoints;
    public static double RockSeconds(double points) => points / PointsPerSecond;
    /// <summary>The heat share of a draw, rounded so that 24 kW reads as 14.4 on every runtime.</summary>
    public static double HeatKW(double kw) => Math.Round(kw * WasteHeatFraction, 6);
    public static bool ValidWork(double progress, double seconds, double kw) => Finite(progress) && Finite(seconds) && Finite(kw) &&
        progress >= 0 && seconds > 0 && seconds <= 3600 && progress <= seconds && kw > 0 && kw <= 1000;

    public static string FilterId(LaserFilter filter) => filter.ToString().ToLowerInvariant();
    public static bool ParseFilter(string? text, out LaserFilter filter)
    {
        filter = LaserFilter.Rock;
        foreach (LaserFilter candidate in Enum.GetValues(typeof(LaserFilter)))
            if (FilterId(candidate) == text) { filter = candidate; return true; }
        return false;
    }
    /// <summary>Asteroids are rock only whatever is selected; a hull offers what the filter asks for.</summary>
    public static bool Takes(LaserFilter filter, bool asteroid, string kind) =>
        kind == Rock ? asteroid || filter != LaserFilter.Walls : kind == Wall && !asteroid && filter != LaserFilter.Rock;

    /// <summary>The head's place on its ship, so a moved or turned laser never continues an old sweep.</summary>
    public static string Mount(double x, double y, double degrees) =>
        string.Join("/", new[] { x, y, degrees }.Select(v => v.ToString("F3", CultureInfo.InvariantCulture)));
}

internal enum LaserPhase { Seeking, Working, DamagePending, UninstallPending, Exhausted }

/// <summary>The laser's saved sweep: which moored ship it is bound to, how far round the arc it has come, what it has
/// finished and, while one is in hand, the paid job on one object. A pending phase is written before the single
/// native change it announces, so a reload can only observe what happened, never repeat it.</summary>
internal sealed class LaserRecord
{
    internal const string StoreName = "Shipbreaker.Laser";
    private static readonly string[] Binding = { "ship", "laser", "mount", "target", "port", "phase" };
    private static readonly string[] JobKeys = { "object", "kind", "stage", "points", "seconds", "kw", "progress", "bearing" };
    internal readonly Dictionary<string, string> Fields = new(StringComparer.Ordinal);
    internal string this[string key] { get => Fields.TryGetValue(key, out var v) ? v : ""; set => Fields[key] = value; }
    internal LaserPhase Phase { get => Enum.Parse<LaserPhase>(this["phase"]); set => this["phase"] = value.ToString(); }
    internal double Number(string key) => double.TryParse(this[key], NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? n : double.NaN;
    internal void Number(string key, double n) => this[key] = n.ToString("R", CultureInfo.InvariantCulture);
    internal bool HasJob => Fields.ContainsKey("object");
    internal bool Paid => HasJob && Number("progress") >= Number("seconds");
    private static bool Count(double n) => LaserRules.Finite(n) && n >= 0 && n % 1 == 0;
    internal bool Valid
    {
        get
        {
            if (!Binding.All(k => ObjectStateStore.SafeValue(this[k])) || this["ship"] == this["target"]) return false;
            if (!Enum.TryParse<LaserPhase>(this["phase"], out var phase) || !Enum.IsDefined(typeof(LaserPhase), phase) || phase.ToString() != this["phase"]) return false;
            if (!Count(Number("rock")) || !Count(Number("walls")) || !LaserRules.Finite(Number("cursor")) || Math.Abs(Number("cursor")) > 180) return false;
            bool job = HasJob;
            if (job != (phase == LaserPhase.Working || phase == LaserPhase.DamagePending || phase == LaserPhase.UninstallPending)) return false;
            if (!job) return JobKeys.All(k => !Fields.ContainsKey(k));
            if (!ObjectStateStore.SafeValue(this["object"]) || !ObjectStateStore.SafeValue(this["stage"])) return false;
            if (this["kind"] != LaserRules.Rock && this["kind"] != LaserRules.Wall) return false;
            if (!LaserRules.ValidWork(Number("progress"), Number("seconds"), Number("kw")) || !LaserRules.Finite(Number("bearing")) || Math.Abs(Number("bearing")) > 180) return false;
            if (!LaserRules.Finite(Number("points")) || Number("points") < 0) return false;
            if (phase == LaserPhase.DamagePending && this["kind"] != LaserRules.Rock || phase == LaserPhase.UninstallPending && this["kind"] != LaserRules.Wall) return false;
            return phase == LaserPhase.Working || Paid;
        }
    }
    internal static bool Read(IReadOnlyDictionary<string, string> fields, out LaserRecord r)
    { r = new LaserRecord(); foreach (var p in fields) r.Fields[p.Key] = p.Value; return r.Valid; }

    /// <summary>A fresh sweep of a newly bound ship, starting at the arc's near edge.</summary>
    internal void Bind(string ship, string laser, string mount, string target, string port)
    {
        foreach (string key in JobKeys) Fields.Remove(key);
        this["ship"] = ship; this["laser"] = laser; this["mount"] = mount; this["target"] = target; this["port"] = port;
        Number("cursor", -LaserRules.ArcDegrees / 2);
        if (!Fields.ContainsKey("rock")) Number("rock", 0);
        if (!Fields.ContainsKey("walls")) Number("walls", 0);
        Phase = LaserPhase.Seeking;
    }
    /// <summary>One paid job on one object, at the figures in force when it starts.</summary>
    internal void Begin(string id, string kind, string stage, double points, double bearing, double kw = LaserRules.WorkingKW)
    {
        if (HasJob) throw new InvalidOperationException("A laser job is already in hand.");
        if (!LaserRules.Finite(kw) || kw <= 0) throw new ArgumentOutOfRangeException(nameof(kw));
        this["object"] = id; this["kind"] = kind; this["stage"] = stage; Number("points", points);
        Number("seconds", LaserRules.JobSeconds(kind == LaserRules.Rock ? LaserRules.RockSeconds(points) : LaserRules.WallSeconds, kw));
        Number("kw", kw); Number("progress", 0); Number("bearing", bearing);
        Phase = LaserPhase.Working;
    }
    internal void Credit(double suppliedKWh)
    {
        if (Phase != LaserPhase.Working || !Valid || !LaserRules.Finite(suppliedKWh) || suppliedKWh < 0) throw new InvalidOperationException("Invalid laser receipt.");
        Number("progress", Math.Min(Number("seconds"), Number("progress") + suppliedKWh * 3600 / Number("kw")));
    }
    /// <summary>The write-ahead step: a paid job announces the one native change about to be made.</summary>
    internal void Commit()
    {
        if (Phase != LaserPhase.Working || !Paid) throw new InvalidOperationException("Only a paid laser job may be committed.");
        Phase = this["kind"] == LaserRules.Rock ? LaserPhase.DamagePending : LaserPhase.UninstallPending;
    }
    /// <summary>The native change was seen: the sweep moves on from this bearing. <paramref name="finished"/> counts
    /// a rock that is gone or a panel that is loose; a rock that only reached its damaged form is not counted.</summary>
    internal void Settle(bool finished)
    {
        if (Phase != LaserPhase.DamagePending && Phase != LaserPhase.UninstallPending) throw new InvalidOperationException("No pending laser change.");
        string counter = this["kind"] == LaserRules.Rock ? "rock" : "walls";
        if (finished) Number(counter, Number(counter) + 1);
        Number("cursor", Number("bearing"));
        foreach (string key in JobKeys) Fields.Remove(key);
        Phase = LaserPhase.Seeking;
    }
    /// <summary>Gives up the job in hand without counting it; work already paid on it is lost.</summary>
    internal void Drop()
    {
        foreach (string key in JobKeys) Fields.Remove(key);
        Phase = LaserPhase.Seeking;
    }
}
