using System;
using System.Linq;
using System.Reflection;

internal static class ObservationNativeChecks
{
    internal static void Run(Action<bool,string> check)
    {
        // Verify the actual installed API used by Harmony's field injection, not just test doubles.
        foreach (var (type, field, expected) in new[] {
            (typeof(GasPressureSense), "co", typeof(CondOwner)), (typeof(GasPressureSense), "strSignalCond", typeof(string)),
            (typeof(GasPressureSense), "strPoint", typeof(string)), (typeof(Sensor), "coUs", typeof(CondOwner)), (typeof(Sensor), "strPoint", typeof(string)) })
            check(type.GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)?.FieldType == expected, "Native observation hook field: " + type.Name + "." + field);
        check(typeof(GasPressureSense).GetMethod("Run", Type.EmptyTypes) != null && typeof(Sensor).GetMethod("Run", Type.EmptyTypes) != null, "Native sensor update hooks exist");
        foreach (string family in new[] { "O2", "N2", "CO2", "Smoke", "Contaminants", "Temp" })
        {
            var green = DataHandler.dictCOs["ItmAlarm" + family + (family == "Temp" ? "OnW" : "OnG")];
            check(green.aStartingConds.Any(c => c.StartsWith("IsAlarm" + family + "=", StringComparison.Ordinal)) &&
                green.aStartingConds.Any(c => c.StartsWith(family == "Temp" ? "IsWhite=" : "IsGreen=", StringComparison.Ordinal)), "Native output identity and clear state: " + family);
            check(green.mapPoints.Any(p => p.StartsWith("RoomA,", StringComparison.Ordinal)), "Native alarm sampling point: " + family);
            check(green.aUpdateCommands.Any(c => c.StartsWith("GasPressureSense,", StringComparison.Ordinal) || c.StartsWith("Sensor,", StringComparison.Ordinal)), "Supported native sensing component: " + family);
        }
    }
}
