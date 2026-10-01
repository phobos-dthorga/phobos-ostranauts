using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Phobos.Ostranauts.Framework.Notices;
using Phobos.Ostranauts.Framework.Persistence;
using Phobos.Ostranauts.Framework.Processing;
using Phobos.Ostranauts.Framework.Registration;

namespace Phobos.Ostranauts.Framework.Liquids;

/// <summary>A line family whose segments hold contents (Framework 0.63.0): its segment family, its definition prefix
/// (the shared four forms) and the commodities it holds. A liquid family holds one liquid; the gas family holds any mix
/// of its gases. A network family is topped up from its stores by Framework; a family without ports (a pumped circuit
/// such as irrigation or furnace coolant, Framework 0.64.0) is filled by its content mod through
/// <see cref="LineContents.Top"/>.</summary>
public sealed class LineHoldUpFamily
{
    public FluidSegmentFamily Family { get; }
    public string Prefix { get; }
    private readonly Dictionary<string, LineCommodity> commodities = new(StringComparer.Ordinal);
    public IReadOnlyCollection<LineCommodity> Commodities => commodities.Values;
    public bool Gas => commodities.Values.All(c => c.Gas);
    /// <summary>Whether Framework tops this family up from its stores (a network family) or its content mod fills it.</summary>
    public bool StoreFilled => Family.IsNetwork;
    public LineHoldUpFamily(FluidSegmentFamily family, string prefix, IEnumerable<LineCommodity> held)
    {
        if (family == null || string.IsNullOrEmpty(prefix)) throw new ArgumentException("A holding line family needs a segment family and a prefix.");
        Family = family; Prefix = prefix;
        foreach (var c in held) Add(c);
        if (commodities.Count == 0) throw new ArgumentException("A holding line family holds at least one commodity.");
    }
    internal void Add(LineCommodity c)
    {
        if (commodities.Count > 0 && commodities.Values.First().Gas != c.Gas) throw new ArgumentException("A line family holds liquids or gases, not both.");
        commodities[c.Name] = c;
    }
    public LineCommodity? Of(string? name) => name != null && commodities.TryGetValue(name, out var c) ? c : null;
    // The four forms of the family, by the shared identity rule (no closure: destroy and offer hooks ask this often).
    public bool IsForm(string? definition) => EquipmentIdentity.IsFamily(definition, Prefix);
}

/// <summary>Lines that hold their contents until drained (Framework 0.63.0; owner decisions, 1 October 2026). Each
/// installed segment of a holding family keeps a saved record of what it holds, and its native mass is its dry mass
/// plus that. While a run is open, the stores on its network top it up to its hold-up every two real seconds (a new
/// line fills as soon as its store has something above its reserve); transfers between machines and stores are
/// otherwise unchanged. Crew drain a liquid run into a drain canister, vent a gas run, and return a drained run to
/// service; a drained run is closed, so its store no longer refills it and no link reaches through it. A segment that
/// holds anything may not be uninstalled or dismantled (refused when the work is offered). Damage keeps a liquid in its
/// segment (a declared mist reaches the room) and vents a gas; destruction releases everything, logged.</summary>
public static class LineContents
{
    public const string Record = "LineContents";
    public const string DrainAction = "PhobosLineDrain", VentAction = "PhobosLineVent", ReopenAction = "PhobosLineReopen";
    public static readonly IReadOnlyList<string> Actions = new[] { DrainAction, VentAction, ReopenAction };
    private const string MapKey = "PhobosState." + Record;
    private static readonly List<LineHoldUpFamily> families = new();
    private static readonly Cadence cadence = new(FluidRouteCache.RecheckSeconds);

    /// <summary>Declares (or replaces) a holding family. Content declares its own lines when it prepares definitions.</summary>
    public static LineHoldUpFamily Declare(FluidSegmentFamily family, string prefix, IEnumerable<LineCommodity> commodities)
    {
        var declared = new LineHoldUpFamily(family, prefix, commodities);
        families.RemoveAll(f => f.Family.Id == family.Id);
        families.Add(declared);
        return declared;
    }
    /// <summary>Adds a commodity to a declared family (a gas another mod carries on the shared gas line).</summary>
    public static void AddCommodity(string familyId, LineCommodity commodity) =>
        (families.FirstOrDefault(f => f.Family.Id == familyId) ?? throw new ArgumentException("No holding line family " + familyId + ".")).Add(commodity);
    public static IReadOnlyList<LineHoldUpFamily> Families => families;
    public static LineHoldUpFamily? FamilyOf(CondOwner? co)
    {
        if (co == null) return null;
        foreach (var f in families) if (f.IsForm(co.strCODef)) return f;
        return null;
    }
    public static bool IsSegment(CondOwner? co) => FamilyOf(co) != null;

    // Records -----------------------------------------------------------------------------------------------------
    private static ObjectStateStore Store(CondOwner co) => new(co.mapGUIPropMaps, Record, FrameworkInfo.PluginId, 1);
    /// <summary>Whether a segment's run is closed: one dictionary probe, for the topology scan.</summary>
    public static bool IsClosed(CondOwner co) =>
        co.mapGUIPropMaps != null && co.mapGUIPropMaps.TryGetValue(MapKey, out var map) && map != null && map.TryGetValue("data.state", out var state) && state == "closed";
    /// <summary>What a segment holds (an empty open segment when it has no record), or null when the record is unreadable.</summary>
    public static LineMixture? Read(CondOwner co)
    {
        var status = Store(co).Read(out var fields);
        if (status == SavedStateStatus.Missing) return new LineMixture();
        if (status != SavedStateStatus.Ready) return null;
        try { return LineMixture.Read(fields); } catch { return null; }
    }
    /// <summary>Saves what a segment holds and sets its native mass to its definition's dry mass plus the contents.</summary>
    public static bool Write(CondOwner co, LineMixture mixture)
    {
        if (!Store(co).TryWriteIfChanged(mixture.Save())) return false;
        SetMass(co, mixture);
        return true;
    }
    private static void SetMass(CondOwner co, LineMixture mixture)
    {
        double dry = Items.FrameworkItems.NativeMass(co.strCODef) ?? co.GetCondAmount("StatMass");
        double delta = dry + mixture.TotalKg - co.GetCondAmount("StatMass");
        if (Math.Abs(delta) > 1e-9) co.AddMass(delta, true);
    }
    private static void Forget(CondOwner co)
    {
        co.mapGUIPropMaps?.Remove(MapKey);
        SetMass(co, new LineMixture());
    }

    // The maintainer ------------------------------------------------------------------------------------------------
    internal static void Poll()
    {
        if (families.Count == 0 || CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading || CrewSim.system?.dictShips == null || !cadence.Due()) return;
        using var measurement = Diagnostics.Performance.Measure(Diagnostics.Performance.LineContentsMaintain);
        // Canisters come from the shared world sweep, once for every ship (Framework 0.72.0), instead of a walk over
        // every object of every owned ship every two seconds.
        var canisters = DrainCanisters.Stowed();
        foreach (var ship in CrewSim.system.dictShips.Values.ToArray())
        {
            if (ship == null || (int)ship.LoadState < 2 || CrewSim.system.GetShipOwner(ship.strRegID) != CrewSim.coPlayer?.strID) continue;
            try
            {
                foreach (var family in families) Maintain(ship, family);
                if (canisters.Count > 0) DrainCanisters.Pour(ship, canisters);
            }
            catch (Exception e) { FrameworkLifecycle.Log(Text.Get("LineContents.failed", ship.strRegID, e.Message)); }
        }
    }
    /// <summary>Tops up every open run on a ship from the stores on its network.</summary>
    internal static void Maintain(Ship ship, LineHoldUpFamily family)
    {
        if (!family.StoreFilled || ship.nCols < 1 || ship.nRows < 1) return;
        // A ship already known to have no working segment of this line is not read again until something changes it.
        if (FluidRouteCache.KnownEmpty(ship, family.Family)) return;
        var topology = FluidRouteCache.Topology(ship, family.Family);
        if (topology.Overflow) return;
        var segments = FluidRouteCache.Segments(ship, family.Family);
        if (segments.Count == 0) return;
        var participants = FluidRouteCache.Participants(ship, family.Family);
        var sources = new Dictionary<int, List<CondOwner>>();
        // Ship's Water drinking tanks on a water line fill it too (Framework 0.65.0; owner decision, 1 October 2026),
        // above the crew reserve, after the Phobos stores on the same run.
        bool drinkingWater = family.Of(LineFamilies.Water) != null && ShipsWaterSupply.Available;
        for (int k = 0; k < participants.Count; k++)
        {
            var co = participants[k];
            var spec = BulkVessels.Of(co);
            bool store = spec != null && family.Of(spec.Commodity) != null && co.HasCond("IsInstalled") && !co.HasCond("IsDamaged");
            if (!store && !(drinkingWater && spec == null && ShipsWaterSupply.IsDrinkingTank(co))) continue;
            int component = topology.ParticipantComponentOf(k);
            if (component < 0) continue;
            if (!sources.TryGetValue(component, out var list)) sources[component] = list = new List<CondOwner>();
            list.Add(co);
        }
        if (sources.Count == 0) return;
        foreach (var group in segments.Where(p => sources.ContainsKey(topology.ComponentOf(p.Key))).GroupBy(p => topology.ComponentOf(p.Key)))
        {
            var run = group.OrderBy(p => p.Key).Select(p => p.Value).ToArray();
            var mixtures = new LineMixture?[run.Length];
            bool wanting = false;
            for (int i = 0; i < run.Length; i++)
            {
                mixtures[i] = Read(run[i]);
                if (mixtures[i] != null && !mixtures[i]!.Closed && mixtures[i]!.Fraction(family.Of) < 1 - 1e-6) wanting = true;
            }
            if (!wanting) continue;
            Fill(family, sources[group.Key], run, mixtures);
        }
    }
    private static void Fill(LineHoldUpFamily family, List<CondOwner> sources, CondOwner[] run, LineMixture?[] mixtures)
    {
        var usable = new List<CondOwner>();
        var available = new Dictionary<string, double>(StringComparer.Ordinal);
        var drinkingTanks = sources.Where(v => BulkVessels.Of(v) == null).ToArray();
        var ship = run[0].ship;
        double drinking = drinkingTanks.Length == 0 ? 0 : ShipsWaterSupply.LineAvailableKg(ship, drinkingTanks, Items.WaterTankService.CrewReserveKg);
        if (drinking > LineMixture.Tolerance) available[LineFamilies.Water] = drinking;
        foreach (var v in sources.Where(v => BulkVessels.Of(v) != null).OrderBy(v => v.strID, StringComparer.Ordinal))
        {
            if (CommodityReservations.Held(v.strID)) continue;
            BufferedDrains.Settle(v);
            var snapshot = BulkVessel.Snapshot(v);
            if (snapshot.Protected || snapshot.AvailableKg <= LineMixture.Tolerance) continue;
            usable.Add(v);
            available[snapshot.Commodity] = (available.TryGetValue(snapshot.Commodity, out var a) ? a : 0) + snapshot.AvailableKg;
        }
        if (usable.Count == 0 && drinking <= LineMixture.Tolerance) return;
        var open = new List<LineMixture>(); var openObjects = new List<CondOwner>();
        for (int i = 0; i < run.Length; i++) if (mixtures[i] is { Closed: false } m) { open.Add(m); openObjects.Add(run[i]); }
        var drawn = LinePlanner.Fill(open, available, family.Of);
        if (drawn.Count == 0) return;
        // Debit the stores first; a segment is only credited with what a store actually gave.
        var given = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var pair in drawn)
        {
            double owed = pair.Value;
            foreach (var v in usable)
            {
                if (owed <= LineMixture.Tolerance) break;
                var spec = BulkVessel.Spec(v);
                if (spec.Commodity != pair.Key) continue;
                var s = BulkVessel.Read(v, spec);
                double take = Math.Min(owed, s.AvailableKg);
                if (take <= 0) continue;
                s.SetService(s.ServiceKg - take);
                BulkVessel.Save(v, spec, s);
                owed -= take;
            }
            if (owed > LineMixture.Tolerance && pair.Key == LineFamilies.Water && drinkingTanks.Length > 0)
                owed -= ShipsWaterSupply.DrawForLine(ship, drinkingTanks, owed, Items.WaterTankService.CrewReserveKg);
            given[pair.Key] = pair.Value - owed;
        }
        foreach (var pair in drawn)
            if (given[pair.Key] < pair.Value - 1e-9) Rebalance(open, pair.Key, pair.Value - given[pair.Key]);
        for (int i = 0; i < open.Count; i++) Write(openObjects[i], open[i]);
    }
    // A store gave less than planned (a draw between the snapshot and the debit): take the shortfall back from the
    // segments last filled, so no segment holds what no store gave.
    private static void Rebalance(List<LineMixture> open, string commodity, double shortfall)
    {
        for (int i = open.Count - 1; i >= 0 && shortfall > LineMixture.Tolerance; i--) shortfall -= open[i].Take(commodity, shortfall);
    }

    // Content-filled circuits (Framework 0.64.0) ----------------------------------------------------------------------
    /// <summary>The open, intact segments carrying fluid on the connected runs through any of <paramref name="cells"/>
    /// (a pumped circuit's path), in cell order, from the cached snapshot; none when the layout overflows.</summary>
    public static IReadOnlyList<CondOwner> Circuit(Ship? ship, LineHoldUpFamily family, IEnumerable<int> cells)
    {
        if (ship == null || ship.nCols < 1 || ship.nRows < 1 || cells == null) return Array.Empty<CondOwner>();
        var topology = FluidRouteCache.Topology(ship, family.Family);
        if (topology.Overflow) return Array.Empty<CondOwner>();
        var components = new HashSet<int>();
        foreach (int cell in cells) { int id = topology.ComponentOf(cell); if (id >= 0) components.Add(id); }
        if (components.Count == 0) return Array.Empty<CondOwner>();
        return FluidRouteCache.Segments(ship, family.Family).Where(p => components.Contains(topology.ComponentOf(p.Key))).OrderBy(p => p.Key).Select(p => p.Value).ToArray();
    }
    /// <summary>The kilograms of a commodity the segments still have room for (unreadable or closed segments count as full).</summary>
    public static double Room(IEnumerable<CondOwner> segments, LineHoldUpFamily family, string commodity)
    {
        var c = family.Of(commodity);
        if (c == null) return 0;
        double room = 0;
        foreach (var co in segments) if (Read(co) is { Closed: false } m) room += m.Room(c, family.Of);
        return room;
    }
    /// <summary>Whether every segment is open, readable and full to within the tolerance of its hold-up.</summary>
    public static bool Full(IEnumerable<CondOwner> segments, LineHoldUpFamily family)
    {
        foreach (var co in segments) if (Read(co) is not { Closed: false } m || m.Fraction(family.Of) < 1 - 1e-6) return false;
        return true;
    }
    /// <summary>What the segments hold of a commodity in total.</summary>
    public static double Holding(IEnumerable<CondOwner> segments, string commodity) => segments.Sum(co => Read(co)?.Of(commodity) ?? 0);
    /// <summary>Fills the segments in order with up to <paramref name="availableKg"/> of a commodity a content mod has
    /// already taken from its own store, and returns the kilograms used; the caller keeps the rest. Each segment is
    /// written with its mass.</summary>
    public static double Top(IEnumerable<CondOwner> segments, LineHoldUpFamily family, string commodity, double availableKg)
    {
        var c = family.Of(commodity);
        if (c == null || !LineGeometry.Finite(availableKg) || availableKg <= LineMixture.Tolerance) return 0;
        double used = 0;
        foreach (var co in segments)
        {
            if (availableKg - used <= LineMixture.Tolerance) break;
            if (Read(co) is not { Closed: false } m) continue;
            double amount = Math.Min(m.Room(c, family.Of), availableKg - used);
            if (amount <= LineMixture.Tolerance) continue;
            m.Add(commodity, amount);
            if (Write(co, m)) used += amount;
        }
        return used;
    }

    // Runs ----------------------------------------------------------------------------------------------------------
    /// <summary>The installed segments (intact or damaged, open or closed) of a family joined to <paramref name="start"/>
    /// through neighbouring tiles, nearest first. A drain or reopen always acts on this whole physical run.</summary>
    public static IReadOnlyList<CondOwner> Run(CondOwner start, LineHoldUpFamily family)
    {
        var ship = start?.ship;
        if (ship == null || ship.nCols < 1 || ship.nRows < 1) return Array.Empty<CondOwner>();
        var byCell = new Dictionary<int, CondOwner>();
        foreach (var co in ship.GetCOs(null, false, false, true))
        {
            if (co == null || co.bDestroyed || co.ship != ship || !co.HasCond("IsInstalled") || !family.IsForm(co.strCODef)) continue;
            int cell = NativeFluidRoute.CellAt(ship, co.GetPos());
            if (cell >= 0 && !byCell.ContainsKey(cell)) byCell[cell] = co;
        }
        int first = NativeFluidRoute.CellAt(ship, start!.GetPos());
        if (first < 0 || !byCell.ContainsKey(first)) return Array.Empty<CondOwner>();
        var order = new List<CondOwner>(); var seen = new HashSet<int> { first }; var queue = new Queue<int>(); queue.Enqueue(first);
        int count = ship.nCols * ship.nRows;
        while (queue.Count > 0)
        {
            int cell = queue.Dequeue(); order.Add(byCell[cell]);
            int column = cell % ship.nCols;
            foreach (int next in new[] { column > 0 ? cell - 1 : -1, column + 1 < ship.nCols ? cell + 1 : -1, cell - ship.nCols, cell + ship.nCols })
                if (next >= 0 && next < count && byCell.ContainsKey(next) && seen.Add(next)) queue.Enqueue(next);
        }
        return order;
    }
    /// <summary>What a run holds in total, by commodity.</summary>
    public static Dictionary<string, double> Held(IEnumerable<CondOwner> run)
    {
        var total = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var co in run)
            foreach (var pair in Read(co)?.Kilograms ?? new Dictionary<string, double>())
                total[pair.Key] = (total.TryGetValue(pair.Key, out var t) ? t : 0) + pair.Value;
        return total;
    }
    private static bool CloseRun(IReadOnlyList<CondOwner> run, bool closed)
    {
        bool changed = false;
        foreach (var co in run)
        {
            var m = Read(co);
            if (m == null || m.Closed == closed) continue;
            m.Closed = closed;
            if (!closed && m.Empty) { Forget(co); changed = true; continue; }
            changed |= Write(co, m);
        }
        if (changed) FluidRouteCache.Invalidate(run[0].ship);
        return changed;
    }

    // Crew actions ----------------------------------------------------------------------------------------------------
    /// <summary>Why an action may not be offered on a segment now, or null.</summary>
    public static string? Refusal(string action, CondOwner? actor, CondOwner? segment)
    {
        var family = FamilyOf(segment);
        if (family == null || segment == null || !segment.HasCond("IsInstalled")) return Text.Get("LineContents.not_segment");
        var mixture = Read(segment);
        if (mixture == null) return Text.Get("LineContents.unreadable");
        if (action == ReopenAction) return mixture.Closed ? null : Text.Get("LineContents.already_open");
        if (action == VentAction && !family.Gas || action == DrainAction && family.Gas) return Text.Get("LineContents.not_segment");
        var held = Held(Run(segment, family));
        if (held.Values.Sum() <= LineMixture.Tolerance) return Text.Get("LineContents.run_empty");
        if (action == VentAction) return null;
        return DrainCanisters.Find(actor, segment, held.Keys) == null ? Text.Get("LineContents.no_canister", DrainCanisterRules.ReachTiles) : null;
    }
    /// <summary>Runs a finished crew action and returns what to tell the crew member.</summary>
    public static string Perform(string action, CondOwner actor, CondOwner segment)
    {
        var reason = Refusal(action, actor, segment);
        if (reason != null) return reason;
        var family = FamilyOf(segment)!;
        var run = Run(segment, family);
        if (action == ReopenAction)
        {
            CloseRun(run, false);
            return Text.Get("LineContents.reopened", run.Count);
        }
        CloseRun(run, true);
        return action == VentAction ? Vent(segment, family, run) : Drain(actor, segment, family, run);
    }
    private static string Drain(CondOwner actor, CondOwner segment, LineHoldUpFamily family, IReadOnlyList<CondOwner> run)
    {
        var held = Held(run);
        var canister = DrainCanisters.Find(actor, segment, held.Keys);
        if (canister == null) return Text.Get("LineContents.no_canister", DrainCanisterRules.ReachTiles);
        string liquid = DrainCanisters.Commodity(canister) ?? held.OrderByDescending(p => p.Value).First().Key;
        var commodity = family.Of(liquid)!;
        double room = DrainCanisters.Room(canister, commodity);
        var mixtures = run.Select(Read).ToArray();
        var readable = mixtures.Where(m => m != null).Cast<LineMixture>().ToArray();
        var owners = run.Where((_, i) => mixtures[i] != null).ToArray();
        var taken = LinePlanner.Drain(readable, liquid, room);
        double total = taken.Sum();
        if (total <= LineMixture.Tolerance) return Text.Get("LineContents.run_empty");
        for (int i = 0; i < owners.Length; i++) if (taken[i] > 0) Write(owners[i], readable[i]);
        DrainCanisters.Fill(canister, liquid, total);
        double left = Held(run).Values.Sum();
        FrameworkLifecycle.Log(Text.Get("LineContents.drained_log", segment.strID, total, liquid, canister.strID, left));
        return Text.Get("LineContents.drained", total, liquid, left);
    }
    private static string Vent(CondOwner segment, LineHoldUpFamily family, IReadOnlyList<CondOwner> run)
    {
        double room = 0, overboard = 0;
        foreach (var co in run)
        {
            var m = Read(co);
            if (m == null || m.Empty) continue;
            var released = Release(co, family, m, all: true);
            room += released.Room; overboard += released.Overboard;
            Write(co, m);
        }
        FrameworkLifecycle.Log(Text.Get("LineContents.vented_log", segment.strID, room, overboard));
        return Text.Get("LineContents.vented", room * 1000, overboard * 1000);
    }
    /// <summary>Releases a segment's contents from <paramref name="m"/>: every gas (<paramref name="all"/>) or only a
    /// liquid's mist, each native species into the room at the segment, the rest overboard (gas) or kept (liquid).
    /// Returns the kilograms that reached the room and those that left the ship.</summary>
    private static (double Room, double Overboard) Release(CondOwner co, LineHoldUpFamily family, LineMixture m, bool all)
    {
        double room = 0, overboard = 0;
        var air = RoomHeat.Read(co);
        foreach (string name in m.Kilograms.Keys.ToArray())
        {
            var c = family.Of(name);
            double held = m.Of(name);
            if (c == null || held <= 0) continue;
            if (c.Gas)
            {
                if (!all) continue;
                m.Take(name, held);
                if (c.RoomSpecies != null && air != null) room += RoomGas.Emit(air, c.RoomSpecies, held); else overboard += held;
            }
            else if (c.MistSpecies != null)
            {
                double mist = m.Take(name, c.MistKg(held));
                if (mist > 0 && air != null) room += RoomGas.Emit(air, c.MistSpecies, mist); else overboard += mist;
            }
        }
        return (room, overboard);
    }

    // Damage, destruction and removal -------------------------------------------------------------------------------------------
    internal static void AfterModeSwitch(CondOwner coNew, LineMixture? before)
    {
        var family = FamilyOf(coNew);
        if (family == null || before == null) return;
        if (!coNew.HasCond("IsInstalled"))
        {
            // Removal is refused while a segment holds anything, so a loose segment is empty: it keeps no record and a
            // later installation starts open.
            if (before.Empty) { Forget(coNew); return; }
            SetMass(coNew, before);
            return;
        }
        if (coNew.HasCond("IsDamaged") && !before.Empty)
        {
            var released = Release(coNew, family, before, all: true);
            Write(coNew, before);
            if (released.Room + released.Overboard > 1e-12)
                Notify(coNew, "LineContents.damaged_notice", released.Room * 1000, released.Overboard * 1000);
            return;
        }
        SetMass(coNew, before);
    }
    internal static void BeforeDestroy(CondOwner co)
    {
        var family = FamilyOf(co);
        var m = family == null ? null : Read(co);
        if (family == null || m == null || m.Empty || co.ship == null) return;
        double total = m.TotalKg;
        var released = Release(co, family, m, all: true);
        double lost = m.TotalKg;
        FrameworkLifecycle.Log(Text.Get("LineContents.destroyed_log", co.strID, total, released.Room, released.Overboard, lost));
        if (CrewSim.system?.GetShipOwner(co.ship.strRegID) == CrewSim.coPlayer?.strID)
            Notify(co, "LineContents.destroyed_notice", total, released.Room * 1000);
    }
    private static void Notify(CondOwner co, string key, params object[] args)
    {
        if (co.ship == null || CrewSim.system?.GetShipOwner(co.ship.strRegID) != CrewSim.coPlayer?.strID) return;
        PlayerNotices.Post(co.ship, "PhobosFramework.lines", NoticeLevel.Caution, Text.Get(key, new object[] { Controls.ObjectPresentation.Name(co.strID) }.Concat(args).ToArray()));
    }
    /// <summary>Why removal work on a segment is refused: it still holds something.</summary>
    public static string? RemovalReason(string? action, CondOwner? us, CondOwner? them)
    {
        // This runs for every offer and completion in the game: the action name is tested before any definition.
        if (action == null || families.Count == 0 ||
            action.IndexOf("Dismantle", StringComparison.OrdinalIgnoreCase) < 0 && action.IndexOf("Uninstall", StringComparison.OrdinalIgnoreCase) < 0) return null;
        var segment = us != null && IsSegment(us) ? us : them != null && IsSegment(them) ? them : null;
        if (segment == null) return null;
        var m = Read(segment);
        if (m == null) return Text.Get("LineContents.unreadable");
        return m.Empty ? null : Text.Get("LineContents.drain_first");
    }

    /// <summary>The three crew actions, published once with Framework's own definitions.</summary>
    internal static void AddActions(NativeDefinitions d)
    {
        var template = NativeDefinitions.Clone(DataHandler.dictInteractions["Inventory"]);
        foreach (string action in Actions)
        {
            var work = NativeDefinitions.Clone(template);
            string key = action == DrainAction ? "drain" : action == VentAction ? "vent" : "reopen";
            work.strName = action; work.strTitle = work.strDesc = work.strTooltip = Text.Get("LineContents.action_" + key);
            work.strRaiseUI = null; work.fTargetPointRange = 2; work.strAnim = "Tablet"; work.strActionGroup = "Work";
            work.fDuration = (action == DrainAction ? DrainSeconds : ShortSeconds) / 3600d;
            d.Interactions[action] = work;
        }
    }
    public const double DrainSeconds = 120, ShortSeconds = 30;
    /// <summary>Offers the actions on a holding family's installed forms.</summary>
    public static void OfferActions(NativeDefinitions d, string prefix, bool gas)
    {
        foreach (string form in new[] { "Installed", "InstalledDmg" })
            if (d.Objects.TryGetValue(prefix + form, out var co))
                co.aInteractions = (co.aInteractions ?? Array.Empty<string>()).Concat(new[] { gas ? VentAction : DrainAction, ReopenAction }).Distinct().ToArray();
    }
}

[HarmonyPatch(typeof(CondOwner), nameof(CondOwner.ModeSwitch))]
internal static class LineContentsModeSwitch
{
    private static void Prefix(CondOwner __instance, out LineMixture? __state) =>
        __state = LineContents.Families.Count > 0 && LineContents.IsSegment(__instance) ? LineContents.Read(__instance) : null;
    private static void Postfix(CondOwner coNew, LineMixture? __state)
    {
        if (__state == null || coNew == null) return;
        try { LineContents.AfterModeSwitch(coNew, __state); } catch (Exception e) { FrameworkLifecycle.Log(e.ToString()); }
    }
}
// Native destruction proceeds; what a destroyed segment held is released and logged. The old half of a mode switch
// and load-time cleanup are not losses.
[HarmonyPatch(typeof(CondOwner), nameof(CondOwner.Destroy))]
internal static class LineContentsDestroy
{
    private static void Prefix(CondOwner __instance)
    {
        if (LineContents.Families.Count == 0 || CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading || __instance == null ||
            __instance.HasCond("IsModeSwitching", false) || !LineContents.IsSegment(__instance)) return;
        try { LineContents.BeforeDestroy(__instance); } catch (Exception e) { FrameworkLifecycle.Log(e.ToString()); }
    }
}
// Offer-time refusals: removal of a segment that holds anything, and the line actions when they cannot be done.
[HarmonyPatch(typeof(Interaction), "TriggeredInternal")]
internal static class LineContentsOffer
{
    private static void Postfix(Interaction __instance, CondOwner objUs, CondOwner objThem, ref bool __result)
    {
        if (!__result || LineContents.Families.Count == 0) return;
        string? reason = LineContents.Actions.Contains(__instance.strName) ? LineContents.Refusal(__instance.strName, objUs, objThem) :
            LineContents.RemovalReason(__instance.strName, objUs, objThem);
        if (reason != null) { __instance.AddFailReason("main", reason); __result = false; }
    }
}
[HarmonyPatch(typeof(Interaction), nameof(Interaction.ApplyEffects))]
internal static class LineContentsEffects
{
    // Removal refused at its finish too (the segment may have filled since the work was offered); the refusal closes the task.
    private static bool Prefix(Interaction __instance)
    {
        if (LineContents.Families.Count == 0) return true;
        var reason = LineContents.RemovalReason(__instance.strName, __instance.objUs, __instance.objThem);
        return reason == null || NativeEffects.Refuse(__instance, reason);
    }
    private static void Postfix(Interaction __instance, bool isCancelIa)
    {
        if (isCancelIa || __instance.bCancel || !LineContents.Actions.Contains(__instance.strName) || __instance.objUs == null || __instance.objThem == null) return;
        try
        {
            string message = LineContents.Perform(__instance.strName, __instance.objUs, __instance.objThem);
            if (__instance.objUs.HasCond("IsHuman")) __instance.objUs.LogMessage(message, "Neutral", "Game");
        }
        catch (Exception e) { FrameworkLifecycle.Log(e.ToString()); }
    }
}
