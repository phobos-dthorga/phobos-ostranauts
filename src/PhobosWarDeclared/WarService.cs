using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Construction;
using Phobos.Ostranauts.Framework.Notices;
using Phobos.Ostranauts.Framework.Observations;
using Phobos.Ostranauts.Framework.Persistence;
using PhobosWarDeclared.Core;
using UnityEngine;

namespace PhobosWarDeclared;

/// <summary>Battle-stations windows, the destroyed-part ledger and laying build sites, per player-owned ship.
/// Presentation (right-click actions, F3) only calls the public entry points here.</summary>
internal static class WarService
{
    internal sealed class Settings
    {
        internal bool Enabled = true;
        internal double QuietSeconds = WarRules.DefaultQuietMinutes * 60;
        internal bool LayDuringCombat;
        internal bool DamageStartsBattle = true;
    }

    /// <summary>What happened on one ship since its last after-action summary (this session only).</summary>
    private sealed class Tally
    {
        internal int Laid, Held, Ignored, NotRebuildable, Overflow;
        internal readonly Dictionary<string, int> Lost = new(StringComparer.Ordinal);
        internal bool Any => Laid + Held + Ignored + NotRebuildable + Overflow + Lost.Count > 0;
    }

    private sealed class Record
    {
        internal WarLedger Ledger = new();
        /// <summary>The saved record could not be read; it is left untouched and nothing new is saved over it.</summary>
        internal bool Protected;
        internal string ProtectedReason = "";
        /// <summary>Real time before which pending build sites that could not be laid are not tried again.</summary>
        internal double RetryAt = double.NegativeInfinity;
    }

    internal static Settings Options { get; set; } = new();
    private static readonly Dictionary<string, Record> records = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, Tally> tallies = new(StringComparer.Ordinal);
    // Footprint counts for performance captures (Framework 0.104.0).
    internal static int RecordCount => records.Count;
    internal static int TallyCount => tallies.Count;
    internal static int DamagedCount => damaged.Count;
    internal static int PartFactCount => partFacts.Count;
    /// <summary>Object IDs whose damage check queued a native switch, with the game time and whether it destroys.</summary>
    private static readonly Dictionary<string, (double At, bool Destroys)> damaged = new(StringComparer.Ordinal);
    // 29 September 2026 pass (FF7): schematic facts are built once per part until content reloads, and the poll's
    // working lists are reused rather than allocated.
    private static readonly Dictionary<string, PartFacts> partFacts = new(StringComparer.Ordinal);
    private static readonly List<Ship> playerShips = new();
    private static readonly List<string> expiredDamage = new();

    internal static void Reset() { records.Clear(); tallies.Clear(); damaged.Clear(); partFacts.Clear(); }

    // ---- Ship scope ------------------------------------------------------------------------------

    // The player's ships through Framework's shared rule (Framework 0.123.0), which is the rule this service used.
    internal static bool PlayerShip(Ship? ship) => Phobos.Ostranauts.Framework.Flight.PlayerFleet.Owns(ship);

    private static List<Ship> PlayerShips() => Phobos.Ostranauts.Framework.Flight.PlayerFleet.Owned(playerShips, includeUnloaded: false);

    private static Record For(Ship ship)
    {
        if (records.TryGetValue(ship.strRegID, out var record)) return record;
        record = new Record();
        var store = Store(ship);
        if (store != null)
        {
            var status = store.Read(out var fields);
            if (status == SavedStateStatus.Ready)
            {
                try { record.Ledger = WarLedger.Load(fields); }
                catch (FormatException ex) { record.Protected = true; record.ProtectedReason = ex.Message; }
            }
            else if (status != SavedStateStatus.Missing) { record.Protected = true; record.ProtectedReason = status.ToString(); }
            if (record.Protected) Plugin.Log(Text.Get("Log.protected", ship.strRegID, record.ProtectedReason));
        }
        records[ship.strRegID] = record;
        return record;
    }

    private static ObjectStateStore? Store(Ship ship)
    {
        var co = ship.ShipCO;
        if (co == null || co.mapGUIPropMaps == null) return null;
        return new ObjectStateStore(co.mapGUIPropMaps, WarRules.StateName, WarRules.Owner, WarRules.StateVersion);
    }

    private static void Save(Ship ship, Record record)
    {
        if (record.Protected) return;
        var store = Store(ship);
        if (store == null) return;
        if (record.Ledger.Empty) { if (store.Read(out _) == SavedStateStatus.Ready) store.Clear(); return; }
        if (!store.TryWrite(record.Ledger.Save())) Plugin.Log(Text.Get("Log.save_failed", ship.strRegID));
    }

    private static Tally TallyFor(Ship ship)
    {
        if (!tallies.TryGetValue(ship.strRegID, out var t)) tallies[ship.strRegID] = t = new Tally();
        return t;
    }

    // ---- Polling ---------------------------------------------------------------------------------

    internal static void Poll()
    {
        if (!Options.Enabled || CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading) return;
        using var measurement = Phobos.Ostranauts.Framework.Diagnostics.Performance.Measure(PerformanceMetrics.Poll);
        double now = StarSystem.fEpoch;
        if (damaged.Count > 0)
        {
            expiredDamage.Clear();
            foreach (var p in damaged) if (now - p.Value.At > WarRules.PendingDamageSeconds || p.Value.At > now) expiredDamage.Add(p.Key);
            foreach (var id in expiredDamage) damaged.Remove(id);
            expiredDamage.Clear();
        }
        double real = Phobos.Ostranauts.Framework.Cadence.RealTime;
        foreach (var ship in PlayerShips())
        {
            var record = For(ship);
            // A log that could not be read stays untouched: no battle state, capture or laying on that ship.
            if (record.Protected) continue;
            var ledger = record.Ledger;
            var facts = NativeCombat.Facts(ship);
            var change = ledger.Window.Observe(now, facts.Engaged || facts.Targeting,
                Options.DamageStartsBattle ? facts.LastDamageEpoch : null, Options.QuietSeconds);
            if (change == WindowChange.Opened) Opened(ship, record, manual: false);
            else if (change == WindowChange.Closed) Closed(ship, record, manual: false);
            // Build sites that could not be laid (an item in hand, the ship not yet editable) wait RetrySeconds
            // before the next attempt; a stand-down, a new loss or a Lay held order tries at once.
            if (ledger.LayDue && (!ledger.Window.Open || Options.LayDuringCombat) && real >= record.RetryAt) LayPending(ship, record);
            if (change != WindowChange.None) Save(ship, record);
        }
    }

    private static void Opened(Ship ship, Record record, bool manual)
    {
        tallies.Remove(ship.strRegID); record.RetryAt = double.NegativeInfinity;
        PlayerNotices.Post(ship, "war-open", NoticeLevel.Caution, Text.Get(manual ? "Notice.declared" : "Notice.detected", Name(ship)),
            Text.Get("Notice.declared_banner"));
    }

    private static void Closed(Ship ship, Record record, bool manual)
    {
        record.Ledger.LayDue = record.Ledger.Count(EntryState.Pending) > 0; record.RetryAt = double.NegativeInfinity;
        PlayerNotices.Post(ship, "war-close", NoticeLevel.Info, Text.Get(manual ? "Notice.stood_down" : "Notice.quiet", Name(ship)));
        if (!record.Ledger.LayDue) Summarize(ship, record);
    }

    // ---- Capturing losses ------------------------------------------------------------------------

    /// <summary>The game's damage check queued a switch for this object (StatDamage reached its maximum).</summary>
    internal static void DamageQueued(CondOwner co, IEnumerable<string> interactions)
    {
        if (!Options.Enabled || co == null || string.IsNullOrEmpty(co.strID)) return;
        damaged[co.strID] = (StarSystem.fEpoch, interactions.Any(n => n.IndexOf("Destroy", StringComparison.OrdinalIgnoreCase) >= 0));
    }

    /// <summary>What a part looked like before a native mode switch replaced it.</summary>
    internal sealed class Before
    {
        internal string Id = "", Definition = ""; internal Ship Ship = null!; internal Vector3 Position; internal float Rotation;
        internal bool Installed, Destroys; internal string Name = "";
    }

    internal static Before? BeforeSwitch(CondOwner old)
    {
        if (!Options.Enabled || old == null || old.bDestroyed || string.IsNullOrEmpty(old.strID) || !damaged.TryGetValue(old.strID, out var mark)) return null;
        if (CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading || !PlayerShip(old.ship)) return null;
        var record = For(old.ship);
        if (record.Protected || !record.Ledger.Window.Open) return null;
        return new Before
        {
            Id = old.strID, Definition = old.strCODef, Ship = old.ship, Position = old.tf.position,
            Rotation = old.Item != null ? old.Item.fLastRotation : old.tf.rotation.eulerAngles.z,
            Installed = old.objCOParent == null && old.HasCond("IsInstalled") && !old.HasCond("IsPlaceholder"),
            Destroys = mark.Destroys, Name = old.FriendlyName ?? old.strCODef
        };
    }

    internal static void AfterSwitch(Before? before, CondOwner coNew)
    {
        if (before == null || coNew == null) return;
        damaged.Remove(before.Id);
        if (before.Installed)
        {
            // Still standing (for example intact to damaged): the repair jobs the game already offers cover it.
            if (coNew.HasCond("IsInstalled")) return;
            RecordPart(before.Ship, before.Id, before.Definition, before.Position, before.Rotation);
        }
        else if (before.Destroys) Lost(before.Ship, before.Name);
    }

    /// <summary>Removals that bypass the mode switch (explosion clean-up, scuttling). Never blocks destruction.</summary>
    internal static void Destroying(CondOwner co)
    {
        if (!Options.Enabled || co == null || co.bDestroyed || string.IsNullOrEmpty(co.strID)) return;
        if (CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading || co.HasCond("IsModeSwitching", false)) return;
        if (co.objCOParent != null || !co.HasCond("IsInstalled") || co.HasCond("IsPlaceholder") || !PlayerShip(co.ship)) return;
        // Only parts the game was already destroying through damage; dismantling, selling or other mods' removals are not losses.
        bool wrecked = damaged.ContainsKey(co.strID) || (co.HasCond("StatDamageMax") && co.GetCondAmount("StatDamage") >= co.GetCondAmount("StatDamageMax"));
        var record = For(co.ship);
        if (!wrecked || record.Protected || !record.Ledger.Window.Open) return;
        damaged.Remove(co.strID);
        RecordPart(co.ship, co.strID, co.strCODef, co.tf.position, co.Item != null ? co.Item.fLastRotation : co.tf.rotation.eulerAngles.z);
    }

    private static void RecordPart(Ship ship, string id, string definition, Vector3 position, float rotation)
    {
        var record = For(ship);
        // A part with nothing to rebuild from keeps its intact name so the report can say it is not rebuildable.
        string part = NativePlaceholders.RebuildTarget(definition) ?? NativePlaceholders.IntactFormOf(definition);
        LedgerEntry entry;
        try { entry = new LedgerEntry(id, part, position.x, position.y, rotation); }
        catch (ArgumentException) { return; }
        if (!record.Ledger.Record(entry))
        {
            if (record.Ledger.Entries.All(e => e.Id != id)) TallyFor(ship).Overflow++;
            return;
        }
        if (Options.LayDuringCombat) { record.Ledger.LayDue = true; record.RetryAt = double.NegativeInfinity; }
        Save(ship, record);
    }

    private static void Lost(Ship ship, string name)
    {
        var lost = TallyFor(ship).Lost;
        lost[name] = lost.TryGetValue(name, out int n) ? n + 1 : 1;
    }

    // ---- Laying ----------------------------------------------------------------------------------

    /// <summary>Schematic "conditions" match both the part's own starting conditions and those it puts on its tiles.
    /// Facts depend only on the game's definitions, so each part's are built once until content reloads.</summary>
    internal static PartFacts Facts(string part)
    {
        part ??= "";
        if (partFacts.TryGetValue(part, out var known)) return known;
        var facts = new PartFacts(part,
            NativePlaceholders.StartingConditions(part).Concat(NativePlaceholders.TileConditions(part) ?? Array.Empty<string>()), NativePlaceholders.MenuFor(part),
            NativePlaceholders.Footprint(part) switch
            {
                PlaceholderFootprint.Walkable => PartFootprint.Walkable,
                PlaceholderFootprint.Blocks => PartFootprint.Blocks,
                _ => PartFootprint.Unknown
            });
        partFacts[part] = facts;
        return facts;
    }

    /// <summary>Floors before anything that stands on them, then walls, then everything else.</summary>
    private static int Rank(LedgerEntry e)
    {
        var facts = Facts(e.Part);
        if (facts.Conditions.Contains("IsFloor")) return 0;
        return facts.Footprint == PartFootprint.Blocks ? 1 : 2;
    }

    private static void LayPending(Ship ship, Record record)
    {
        using var measurement = Phobos.Ostranauts.Framework.Diagnostics.Performance.Measure(PerformanceMetrics.LayPending);
        var ledger = record.Ledger;
        var schematic = Schematics.Active;
        var tally = TallyFor(ship);
        bool changed = false, retry = false;
        foreach (var entry in ledger.LayOrder(EntryState.Pending, Rank))
        {
            var action = schematic.Evaluate(Facts(entry.Part), out _);
            if (action == SchematicAction.Ignore) { ledger.Remove(entry); tally.Ignored++; changed = true; continue; }
            if (action == SchematicAction.Hold) { entry.State = EntryState.Held; entry.Reason = "Schematic"; tally.Held++; changed = true; continue; }
            var disposition = ledger.Apply(entry, Lay(ship, entry));
            if (disposition == LayDisposition.Retry) retry = true;
            changed |= Count(tally, disposition);
        }
        record.RetryAt = retry ? Phobos.Ostranauts.Framework.Cadence.RealTime + WarRules.RetrySeconds : double.NegativeInfinity;
        if (ledger.Count(EntryState.Pending) == 0)
        {
            ledger.LayDue = false; changed = true;
            if (!ledger.Window.Open) Summarize(ship, record);
        }
        if (changed) Save(ship, record);
    }

    private static bool Count(Tally tally, LayDisposition disposition)
    {
        switch (disposition)
        {
            case LayDisposition.Laid: tally.Laid++; return true;
            case LayDisposition.NotRebuildable: tally.NotRebuildable++; return true;
            case LayDisposition.Held: tally.Held++; return true;
            default: return false;
        }
    }

    private static LayResult Lay(Ship ship, LedgerEntry entry)
    {
        var result = NativePlaceholders.TryLay(ship, entry.Part, new Vector3((float)entry.X, (float)entry.Y, 0), entry.Rotation, out var detail);
        if (result == PlaceholderLayResult.Failed) Plugin.Log(Text.Get("Log.lay_failed", entry.Part, entry.Id, detail));
        return result switch
        {
            PlaceholderLayResult.Laid => LayResult.Laid,
            PlaceholderLayResult.UnknownPart => LayResult.UnknownPart,
            PlaceholderLayResult.NoInstallJob => LayResult.NoInstallJob,
            PlaceholderLayResult.DoesNotFit => LayResult.DoesNotFit,
            PlaceholderLayResult.PlayerBusy => LayResult.PlayerBusy,
            PlaceholderLayResult.ShipUnavailable => LayResult.ShipUnavailable,
            _ => LayResult.Failed
        };
    }

    private static void Summarize(Ship ship, Record record)
    {
        if (!tallies.TryGetValue(ship.strRegID, out var t) || !t.Any) return;
        tallies.Remove(ship.strRegID);
        var lines = new List<string> { Text.Get("Report.title", Name(ship)) };
        if (t.Laid > 0) lines.Add(Text.Get("Report.laid", t.Laid));
        int held = record.Ledger.Count(EntryState.Held);
        if (held > 0) lines.Add(Text.Get("Report.held", held));
        if (t.Ignored > 0) lines.Add(Text.Get("Report.ignored", t.Ignored, Schematics.Active.Title));
        if (t.NotRebuildable > 0) lines.Add(Text.Get("Report.not_rebuildable", t.NotRebuildable));
        if (t.Overflow > 0) lines.Add(Text.Get("Report.overflow", t.Overflow));
        if (t.Lost.Count > 0) lines.Add(Text.Get("Report.lost", string.Join(", ", t.Lost.OrderByDescending(p => p.Value).ThenBy(p => p.Key)
            .Select(p => p.Value > 1 ? Text.Get("Report.lost_count", p.Value, p.Key) : p.Key))));
        PlayerNotices.Post(ship, "war-report", NoticeLevel.Info, string.Join(" ", lines));
    }

    // ---- Player orders (right-click and F3 delegate here) ----------------------------------------

    internal static bool Declare(Ship? ship, out string message)
    {
        if (!Usable(ship, out message)) return false;
        var record = For(ship!);
        if (record.Ledger.Window.Declare(StarSystem.fEpoch) == WindowChange.Opened) Opened(ship!, record, manual: true);
        Save(ship!, record);
        message = Text.Get("Order.declared", Name(ship!));
        return true;
    }

    internal static bool StandDown(Ship? ship, out string message)
    {
        if (!Usable(ship, out message)) return false;
        var record = For(ship!);
        if (record.Ledger.Window.Stand(StarSystem.fEpoch) != WindowChange.Closed) { message = Text.Get("Order.not_at_stations", Name(ship!)); return false; }
        Closed(ship!, record, manual: true);
        Save(ship!, record);
        message = Text.Get("Order.stood_down", Name(ship!));
        return true;
    }

    /// <summary>Lays every held part now, whatever the schematic says: the player has chosen to accept the ghosts.</summary>
    internal static bool LayHeld(Ship? ship, out string message)
    {
        if (!Usable(ship, out message)) return false;
        var record = For(ship!);
        var ledger = record.Ledger;
        var held = ledger.LayOrder(EntryState.Held, Rank);
        if (held.Count == 0) { message = Text.Get("Order.nothing_held"); return false; }
        int laid = 0, still = 0, gone = 0, busy = 0;
        foreach (var entry in held)
        {
            switch (ledger.Apply(entry, Lay(ship!, entry)))
            {
                case LayDisposition.Laid: laid++; break;
                case LayDisposition.NotRebuildable: gone++; break;
                case LayDisposition.Held: still++; break;
                default: busy++; entry.State = EntryState.Held; break;
            }
        }
        record.RetryAt = double.NegativeInfinity; // A player order restarts the automatic attempts at once.
        Save(ship!, record);
        message = Text.Get("Order.laid_held", laid, still + busy, gone);
        if (busy > 0) message += " " + Text.Get("Order.put_item_away");
        return laid > 0;
    }

    internal static bool CanDeclare(Ship? ship) => Options.Enabled && PlayerShip(ship) && !(For(ship!).Ledger.Window.Open && For(ship!).Ledger.Window.Manual) && !For(ship!).Protected;
    internal static bool CanStandDown(Ship? ship) => Options.Enabled && PlayerShip(ship) && For(ship!).Ledger.Window.Open && !For(ship!).Protected;
    internal static bool CanLayHeld(Ship? ship) => Options.Enabled && PlayerShip(ship) && For(ship!).Ledger.Count(EntryState.Held) > 0 && !For(ship!).Protected;

    internal static string Status(Ship? ship)
    {
        if (!Usable(ship, out var message)) return message;
        var record = For(ship!);
        var ledger = record.Ledger;
        var w = ledger.Window;
        string state = !w.Open ? Text.Get("Status.at_ease") : w.Manual ? Text.Get("Status.declared") :
            Text.Get("Status.detected", Math.Max(0, Math.Ceiling((Options.QuietSeconds - (StarSystem.fEpoch - w.LastActivity)) / 60)));
        var lines = new List<string>
        {
            Text.Get("Status.line", Name(ship!), state, Schematics.Active.Key, ledger.Count(EntryState.Pending), ledger.Count(EntryState.Held))
        };
        foreach (var e in ledger.Entries.Take(12))
            lines.Add(Text.Get("Status.entry", e.Part, e.X.ToString("0.0"), e.Y.ToString("0.0"),
                e.State == EntryState.Held ? Text.Get("Status.held") : Text.Get("Status.pending"), string.IsNullOrEmpty(e.Reason) ? "-" : e.Reason));
        if (ledger.Entries.Count > 12) lines.Add(Text.Get("Status.more", ledger.Entries.Count - 12));
        return string.Join("\n", lines);
    }

    private static bool Usable(Ship? ship, out string message)
    {
        message = "";
        if (!Options.Enabled) { message = Text.Get("Order.disabled"); return false; }
        if (!PlayerShip(ship)) { message = Text.Get("Order.not_your_ship"); return false; }
        var record = For(ship!);
        if (record.Protected) { message = Text.Get("Order.protected", record.ProtectedReason); return false; }
        return true;
    }

    private static string Name(Ship ship) => string.IsNullOrEmpty(ship.publicName) ? ship.strRegID : ship.publicName;
}
