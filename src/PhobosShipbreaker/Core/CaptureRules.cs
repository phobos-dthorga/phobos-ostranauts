using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Persistence;

namespace PhobosShipbreaker.Core;

// Contact is at the existing jaw face: a one-tile wall centre half a tile beyond it.
// This establishes contact only; it grants no cutting or remote transfer permission.
internal static class CaptureRules
{
    internal const double WallCentreY = IntakeRules.GrabberDepth / 2.0 + .5;
    internal const int MaximumFitAttempts = 64;
    internal static bool Cardinal(double angle) => !double.IsNaN(angle) && !double.IsInfinity(angle) &&
        Math.Abs(Math.IEEERemainder(angle, 90)) < IntakeRules.AngleToleranceDegrees;
    internal static bool InContact(double gx, double gy, double angle, double wx, double wy)
    {
        if (!Cardinal(angle)) return false;
        var local = IntakeRules.Rotate(wx - gx, wy - gy, -angle);
        return Math.Abs(local.X) <= (IntakeRules.Width - 1) / 2.0 + IntakeRules.PositionToleranceTiles &&
            Math.Abs(local.Y - WallCentreY) < IntakeRules.PositionToleranceTiles;
    }
    internal static string Label(string id, IEnumerable<string> ids)
    {
        var all = ids.Distinct(StringComparer.Ordinal).ToArray();
        int length = Math.Min(4, id.Length);
        while (length < id.Length && all.Any(other => other != id && other.StartsWith(id.Substring(0, length), StringComparison.Ordinal))) length++;
        return "G4 [" + id.Substring(0, length) + (length < id.Length ? "…" : "") + "]";
    }
}

internal enum CapturePhase { Bound, Approaching, CapturePending, Captured, Suspended, ReleasePending, Released }

internal sealed class CaptureRecord
{
    internal const string StoreName = "Shipbreaker.Capture";
    internal readonly Dictionary<string, string> Fields = new(StringComparer.Ordinal);
    internal string this[string key] { get => Fields.TryGetValue(key, out var value) ? value : ""; set => Fields[key] = value; }
    internal CapturePhase Phase { get => Enum.Parse<CapturePhase>(this["phase"]); set => this["phase"] = value.ToString(); }
    internal bool Valid => new[] { "g4", "chute", "processor", "console", "module", "ship", "owner", "target", "permission", "mount", "phase" }
        .All(key => ObjectStateStore.SafeValue(this[key])) && this["ship"] != this["target"] &&
        Enum.TryParse<CapturePhase>(this["phase"], out var phase) && Enum.IsDefined(typeof(CapturePhase), phase) && phase.ToString() == this["phase"] &&
        (phase != CapturePhase.CapturePending && phase != CapturePhase.Captured && phase != CapturePhase.ReleasePending ||
         new[] { "ownPort", "targetPort", "wall" }.All(key => ObjectStateStore.SafeValue(this[key])));
    internal static bool Read(IReadOnlyDictionary<string, string> fields, out CaptureRecord record)
    {
        record = new CaptureRecord();
        foreach (var pair in fields) record.Fields[pair.Key] = pair.Value;
        return record.Valid;
    }
}
