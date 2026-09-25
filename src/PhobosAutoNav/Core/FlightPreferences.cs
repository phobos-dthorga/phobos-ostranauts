using System;
using System.Collections.Generic;
using System.Globalization;

namespace PhobosAutoNav.Core;

// Console defaults are distinct from captured flight intent and global config seeds.
internal readonly struct FlightPreferences
{
    internal const string StoreName = "AutoNav.Preferences";
    internal const int Schema = 1;
    internal const float MinimumCruiseMS = 10, MaximumCruiseMS = 5000, MaximumArrivalMS = 1000;
    internal readonly double CruiseMS, ArrivalMS, ArrivalKM;
    internal FlightPreferences(double cruise, double arrival, double distance)
    { CruiseMS = cruise; ArrivalMS = arrival; ArrivalKM = distance; }
    internal bool Valid => ArrivalBrake.Finite(CruiseMS) && CruiseMS >= MinimumCruiseMS && CruiseMS <= MaximumCruiseMS &&
        ArrivalBrake.Finite(ArrivalMS) && ArrivalMS >= 0 && ArrivalMS <= Math.Min(MaximumArrivalMS, CruiseMS) &&
        ApproachRules.ValidArrival(ArrivalKM);
    internal Dictionary<string, string> Encode() => new Dictionary<string, string>
    {
        ["cruiseMS"] = CruiseMS.ToString("R", CultureInfo.InvariantCulture),
        ["arrivalMS"] = ArrivalMS.ToString("R", CultureInfo.InvariantCulture),
        ["arrivalKM"] = ArrivalKM.ToString("R", CultureInfo.InvariantCulture)
    };
    internal static bool TryDecode(IReadOnlyDictionary<string, string> data, out FlightPreferences preferences)
    {
        double Read(string key) => data.TryGetValue(key, out var value) &&
            double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result) ? result : double.NaN;
        preferences = new FlightPreferences(Read("cruiseMS"), Read("arrivalMS"), Read("arrivalKM"));
        return data.Count == 3 && preferences.Valid;
    }
}

internal enum FlightSetting { Cruise, ArrivalSpeed, ArrivalDistance }
