using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PhobosWarDeclared.Core;

/// <summary>Pending parts are laid automatically when due; held parts wait for the player's Lay held order.</summary>
public enum EntryState { Pending, Held }

/// <summary>Why a lay attempt ended as it did (mirrors Framework's placeholder results without depending on them).</summary>
public enum LayResult { Laid, UnknownPart, NoInstallJob, DoesNotFit, PlayerBusy, ShipUnavailable, Failed }

public enum LayDisposition { Laid, NotRebuildable, Held, Retry }

/// <summary>One installed part destroyed during a combat window, by the ID it had when installed.</summary>
public sealed class LedgerEntry
{
    public LedgerEntry(string id, string part, double x, double y, float rotation)
    {
        if (!Token(id) || !Token(part)) throw new ArgumentException("Part and ID must be plain identifiers.");
        if (double.IsNaN(x) || double.IsInfinity(x) || double.IsNaN(y) || double.IsInfinity(y) || float.IsNaN(rotation) || float.IsInfinity(rotation))
            throw new ArgumentException("Position and rotation must be finite.");
        Id = id; Part = part; X = x; Y = y; Rotation = rotation;
    }
    public string Id { get; }
    public string Part { get; }
    public double X { get; }
    public double Y { get; }
    public float Rotation { get; }
    public EntryState State { get; set; }
    public int Attempts { get; set; }
    /// <summary>Last reason code (a <see cref="LayResult"/> or "Schematic"), for status lines.</summary>
    public string Reason { get; set; } = "";

    internal static bool Token(string? s) => !string.IsNullOrWhiteSpace(s) && s!.Length <= 120 &&
        s.All(c => !char.IsControl(c) && !char.IsWhiteSpace(c) && c != '|' && c != '=' && c != ',');
}

/// <summary>One ship's combat window and the destroyed parts awaiting build sites, as saved on the ship.</summary>
public sealed class WarLedger
{
    private readonly List<LedgerEntry> entries = new();
    public CombatWindow Window { get; } = new();
    public IReadOnlyList<LedgerEntry> Entries => entries;
    /// <summary>Pending parts should be laid at the next opportunity (after stand-down, or during combat when allowed).</summary>
    public bool LayDue { get; set; }
    /// <summary>Parts that could not be recorded because the ledger was full; reported, never saved.</summary>
    public int Overflow { get; private set; }
    public int Count(EntryState state) => entries.Count(e => e.State == state);

    /// <summary>Records a destroyed part once per ID. Returns false for a duplicate or a full ledger.</summary>
    public bool Record(LedgerEntry entry)
    {
        if (entry == null) throw new ArgumentNullException(nameof(entry));
        if (entries.Any(e => e.Id == entry.Id)) return false;
        if (entries.Count >= WarRules.MaximumEntries) { Overflow++; return false; }
        entries.Add(entry);
        return true;
    }

    public bool Remove(LedgerEntry entry) => entries.Remove(entry);

    /// <summary>Floors first so walls and machinery have something to stand on, then everything else, oldest first.</summary>
    public IReadOnlyList<LedgerEntry> LayOrder(EntryState state, Func<LedgerEntry, int> rank) =>
        entries.Select((e, i) => (e, i)).Where(p => p.e.State == state).OrderBy(p => rank(p.e)).ThenBy(p => p.i).Select(p => p.e).ToArray();

    /// <summary>Applies one lay attempt's result to an entry. Laid and not-rebuildable parts leave the ledger.</summary>
    public LayDisposition Apply(LedgerEntry entry, LayResult result)
    {
        entry.Reason = result.ToString();
        switch (result)
        {
            case LayResult.Laid: entries.Remove(entry); return LayDisposition.Laid;
            case LayResult.UnknownPart:
            case LayResult.NoInstallJob: entries.Remove(entry); return LayDisposition.NotRebuildable;
            case LayResult.DoesNotFit: entry.State = EntryState.Held; return LayDisposition.Held;
            case LayResult.PlayerBusy:
            case LayResult.ShipUnavailable: return LayDisposition.Retry;
            default:
                entry.Attempts++;
                if (entry.Attempts < WarRules.MaximumLayAttempts) return LayDisposition.Retry;
                entry.State = EntryState.Held; return LayDisposition.Held;
        }
    }

    public IReadOnlyDictionary<string, string> Save()
    {
        var w = Window;
        var fields = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["window.open"] = Flag(w.Open), ["window.manual"] = Flag(w.Manual),
            ["window.openedAt"] = Number(w.OpenedAt), ["window.lastActivity"] = Number(w.LastActivity),
            ["window.suppressOngoing"] = Flag(w.SuppressOngoing), ["lay.due"] = Flag(LayDue),
            ["entries"] = entries.Count.ToString(CultureInfo.InvariantCulture)
        };
        if (w.StoodDownAt.HasValue) fields["window.stoodDownAt"] = Number(w.StoodDownAt.Value);
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            fields["entry." + i.ToString(CultureInfo.InvariantCulture)] = string.Join("|", e.Id, e.Part, Number(e.X), Number(e.Y),
                e.Rotation.ToString("R", CultureInfo.InvariantCulture), e.State == EntryState.Held ? "held" : "pending",
                e.Attempts.ToString(CultureInfo.InvariantCulture), string.IsNullOrEmpty(e.Reason) ? "-" : e.Reason);
        }
        return fields;
    }

    /// <summary>Reads a saved ledger; throws <see cref="FormatException"/> so callers can keep the record intact.</summary>
    public static WarLedger Load(IReadOnlyDictionary<string, string> fields)
    {
        var ledger = new WarLedger();
        bool open = ReadFlag(fields, "window.open");
        double? stood = fields.ContainsKey("window.stoodDownAt") ? ReadNumber(fields, "window.stoodDownAt") : null;
        ledger.Window.Restore(open, ReadFlag(fields, "window.manual"), ReadNumber(fields, "window.openedAt"),
            ReadNumber(fields, "window.lastActivity"), stood, ReadFlag(fields, "window.suppressOngoing"));
        ledger.LayDue = ReadFlag(fields, "lay.due");
        if (!fields.TryGetValue("entries", out var countText) || !int.TryParse(countText, NumberStyles.None, CultureInfo.InvariantCulture, out int count) ||
            count > WarRules.MaximumEntries) throw new FormatException("Saved entry count is missing or invalid.");
        for (int i = 0; i < count; i++)
        {
            if (!fields.TryGetValue("entry." + i.ToString(CultureInfo.InvariantCulture), out var text)) throw new FormatException("Saved entry " + i + " is missing.");
            var p = text.Split('|');
            if (p.Length != 8 || !double.TryParse(p[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double x) ||
                !double.TryParse(p[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double y) ||
                !float.TryParse(p[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float rotation) ||
                (p[5] != "held" && p[5] != "pending") || !int.TryParse(p[6], NumberStyles.None, CultureInfo.InvariantCulture, out int attempts))
                throw new FormatException("Saved entry " + i + " is malformed.");
            LedgerEntry entry;
            try { entry = new LedgerEntry(p[0], p[1], x, y, rotation); }
            catch (ArgumentException ex) { throw new FormatException("Saved entry " + i + ": " + ex.Message); }
            entry.State = p[5] == "held" ? EntryState.Held : EntryState.Pending;
            entry.Attempts = attempts; entry.Reason = p[7] == "-" ? "" : p[7];
            if (!ledger.Record(entry)) throw new FormatException("Saved entry " + i + " repeats a part ID.");
        }
        return ledger;
    }

    /// <summary>True when there is nothing worth saving (no window, no parts, nothing due).</summary>
    public bool Empty => !Window.Open && entries.Count == 0 && !LayDue && !Window.SuppressOngoing;

    private static string Flag(bool value) => value ? "1" : "0";
    private static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    private static bool ReadFlag(IReadOnlyDictionary<string, string> f, string key) =>
        f.TryGetValue(key, out var v) ? v == "1" || (v == "0" ? false : throw new FormatException(key + " is not 0 or 1.")) : throw new FormatException(key + " is missing.");
    private static double ReadNumber(IReadOnlyDictionary<string, string> f, string key) =>
        f.TryGetValue(key, out var v) && double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) && !double.IsNaN(d) && !double.IsInfinity(d)
            ? d : throw new FormatException(key + " is missing or not a number.");
}
