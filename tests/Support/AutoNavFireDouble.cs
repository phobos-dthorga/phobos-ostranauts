// Lifecycle suites double native dispatch; actual ownership/firing has its own suite.
using System;
using System.Collections.Generic;
using System.Linq;
namespace PhobosAutoNav;
internal enum FireState { Native, Hold, Armed, Held, Fault }
internal sealed class WeaponReading
{
    internal string Id = "weapon", Name = "Test", Reason = "FCS.ready";
    internal bool? InArc = true;
    internal bool Loaded = true, Ready = true, Eligible = true, Manual = false;
    internal double? Heading = 0, ReloadSeconds = null, AimSeconds = null;
}
internal sealed class FireControlController
{
    internal bool Permitted;
    private HashSet<string> leases = new();
    internal int Remaining;
    internal FireState State;
    internal IReadOnlyList<WeaponReading> Weapons = new[] { new WeaponReading() };
    internal int? InArcCount => null;
    internal int? AmmoCount => null;
    internal int? ReadyCount => null;
    internal double SampleEpoch = double.NaN;
    internal string Reason = "FCS.hold";
    internal void Reset() { Permitted = false; leases.Clear(); }
    internal void Cease(string reason = "FCS.hold", bool fault = false) { Permitted = false; Remaining = 0; Reason = reason; State = fault ? FireState.Fault : FireState.Hold; }
    internal void Invalidate() { SampleEpoch = double.NaN; }
    internal bool Authorize(Ship ship, string target, int group, int volleys) { Permitted = true; Remaining = volleys; State = FireState.Armed; return true; }
    internal void SetOwnership(string console, Ship ship, int group, bool hold) { if (hold) leases.Add(console); else leases.Remove(console); }
    internal bool Owns(string console) => leases.Contains(console);
    internal bool OtherOwner(string console) => leases.Any(id => id != console);
    internal void Observe(Ship ship, TargetRef? target, int group, double dt) { SampleEpoch = StarSystem.fEpoch; }
    internal void Dispatch(Ship ship, TargetRef? target, bool safe) { }
}
