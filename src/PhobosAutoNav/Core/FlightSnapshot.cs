using System;
using System.Collections.Generic;
using System.Globalization;
using Phobos.Ostranauts.Framework.Persistence;

namespace PhobosAutoNav.Core;

internal enum SavedFlightMode { Active, Suspended, Stopped, Arrived, Docking, DockingSuspended, Docked }

// Original Phobos save contract. Units are metres/second, kilometres and seconds.
// No cached positions, thrust commands, translated strings or Unity references.
internal sealed class FlightSnapshot
{
    internal const string StoreName = "AutoNav.Flight";
    internal const int Schema = 1;
    internal string ConsoleId = "", ModuleId = "", ShipId = "", PlayerId = "", TargetId = "";
    internal double CruiseMS, ArrivalMS, ArrivalKM, ElapsedSeconds;
    internal CoastSettings Coast;
    internal bool Coasting, PreferTorch;
    internal SavedFlightMode Mode;
    internal string OwnPort = "", TargetPort = "";
    internal bool IsDocking => Mode == SavedFlightMode.Docking || Mode == SavedFlightMode.DockingSuspended || Mode == SavedFlightMode.Docked;
    internal bool IsActive => Mode == SavedFlightMode.Active || Mode == SavedFlightMode.Docking;
    internal bool IsResumable => IsActive || Mode == SavedFlightMode.Suspended || Mode == SavedFlightMode.DockingSuspended;
    internal SavedFlightMode SuspendedMode => IsDocking ? SavedFlightMode.DockingSuspended : SavedFlightMode.Suspended;

    internal bool Valid => ObjectStateStore.SafeValue(ConsoleId) && ObjectStateStore.SafeValue(ModuleId) &&
        ObjectStateStore.SafeValue(ShipId) && ObjectStateStore.SafeValue(PlayerId) && ObjectStateStore.SafeValue(TargetId) &&
        ShipId != TargetId && InRange(CruiseMS, FlightPreferences.MinimumCruiseMS, FlightPreferences.MaximumCruiseMS) && InRange(ArrivalMS, 0, Math.Min(FlightPreferences.MaximumArrivalMS, CruiseMS)) &&
        ApproachRules.ValidArrival(ArrivalKM) && InRange(ElapsedSeconds, 0, double.MaxValue) && Coast.IsValid &&
        Enum.IsDefined(typeof(SavedFlightMode), Mode) && (!IsDocking ||
            (ObjectStateStore.SafeValue(OwnPort) && ObjectStateStore.SafeValue(TargetPort) && !PreferTorch &&
             CruiseMS == DockingRules.CruiseMS && ArrivalMS == 0));

    internal bool Matches(string console, string module, string ship, string player) =>
        ConsoleId == console && ModuleId == module && ShipId == ship && PlayerId == player;

    internal Dictionary<string, string> Encode()
    {
        var fields = new Dictionary<string, string>
        {
        ["console"] = ConsoleId, ["module"] = ModuleId, ["ship"] = ShipId, ["player"] = PlayerId, ["target"] = TargetId,
        ["cruiseMS"] = Number(CruiseMS), ["arrivalMS"] = Number(ArrivalMS), ["arrivalKM"] = Number(ArrivalKM),
        ["elapsedSeconds"] = Number(ElapsedSeconds), ["coasting"] = Coasting ? "1" : "0", ["mode"] = Mode.ToString(),
        ["preferTorch"] = PreferTorch ? "1" : "0",
        ["coastMinimumMS"] = Number(Coast.MinimumToleranceMS), ["coastPercent"] = Number(Coast.SpeedTolerancePercent),
        ["coastEnter"] = Number(Coast.EnterFraction), ["burnHeadingDegrees"] = Number(Coast.BurnHeadingToleranceDegrees)
        };
        // New mode names make older plugins reject docking records instead of treating them as approach flights.
        if (IsDocking) { fields["ownPort"] = OwnPort; fields["targetPort"] = TargetPort; }
        return fields;
    }

    internal static bool TryDecode(IReadOnlyDictionary<string, string> fields, out FlightSnapshot snapshot)
    {
        string Value(string key) => fields.TryGetValue(key, out var value) ? value : "";
        double Parse(string key) => double.TryParse(Value(key), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : double.NaN;
        snapshot = new FlightSnapshot
        {
            ConsoleId = Value("console"), ModuleId = Value("module"), ShipId = Value("ship"), PlayerId = Value("player"), TargetId = Value("target"),
            CruiseMS = Parse("cruiseMS"), ArrivalMS = Parse("arrivalMS"), ArrivalKM = Parse("arrivalKM"), ElapsedSeconds = Parse("elapsedSeconds"),
            Coast = new CoastSettings(Parse("coastMinimumMS"), Parse("coastPercent"), Parse("coastEnter"), Parse("burnHeadingDegrees")),
            Coasting = Value("coasting") == "1", PreferTorch = Value("preferTorch") == "1",
            OwnPort = Value("ownPort"), TargetPort = Value("targetPort")
        };
        if (!Enum.TryParse(Value("mode"), out snapshot.Mode) || Value("mode") != snapshot.Mode.ToString() ||
            (Value("coasting") != "1" && Value("coasting") != "0") ||
            (fields.ContainsKey("preferTorch") && Value("preferTorch") != "1" && Value("preferTorch") != "0")) return false;
        return snapshot.Valid;
    }
    private static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    private static bool InRange(double value, double min, double max) => ArrivalBrake.Finite(value) && value >= min && value <= max;
}
