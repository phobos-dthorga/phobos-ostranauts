using System;
using Phobos.Ostranauts.Framework.Observations;

internal static class ObservationChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var reading = new Observation("probe", "ship", "room", "temperature", "probe", "K", 320, null, 100, ObservationValidity.Current);
        check(reading.Assess("ship", "room", 104, 5).Validity == ObservationValidity.Current, "Recent, same-scope evidence remains current");
        var old = reading.Assess("ship", "room", 106, 5);
        check(old.Validity == ObservationValidity.Stale && old.Value == 320 && old.NeedsAttention, "Stale evidence retains its last actual value and needs attention");
        check(old.Assess("ship", "room", 102, 5).Validity == ObservationValidity.Stale, "Assessment cannot promote stale evidence");
        foreach (var changed in new[] { reading.Assess("neighbour", "room", 101, 5), reading.Assess("ship", "new-room", 101, 5), reading.Assess("ship", "room", 99, 5) })
            check(changed.Validity == ObservationValidity.Unavailable && changed.Value == null && changed.ObservedAt == null, "Changed scope/clock cannot reuse an old value");
        var zero = new Observation("probe", "ship", "room", "pressure", "probe", "kPa", 0, null, 100, ObservationValidity.Current);
        check(zero.Value == 0 && zero.Validity == ObservationValidity.Current, "Measured zero is distinct from no measurement");
        var missing = zero.WithoutValue("no_power");
        check(missing.Value == null && missing.ObservedAt == null && missing.NeedsAttention, "Unavailable is not a fabricated zero");
        var clear = new Observation("alarm", "ship", "room", "O2", "native", "", null, AlarmState.Clear, 100, ObservationValidity.Current);
        check(!clear.NeedsAttention && clear.WithValidity(ObservationValidity.Faulty, "damaged").NeedsAttention, "A failed green alarm is not a current all-clear");
        foreach (double bad in new[] { double.NaN, double.PositiveInfinity })
        {
            bool rejected = false;
            try { _ = new Observation("probe", "ship", "room", "T", "probe", "K", bad, null, 100, ObservationValidity.Current); }
            catch (ArgumentException) { rejected = true; }
            check(rejected, "Invalid measurements cannot enter the public contract");
        }
        bool emptyRejected = false;
        try { _ = new Observation("probe", "ship", "", "T", "probe", "K", null, null, null, ObservationValidity.Current); }
        catch (ArgumentException) { emptyRejected = true; }
        check(emptyRejected, "Current readings require evidence and scope");
    }
}
