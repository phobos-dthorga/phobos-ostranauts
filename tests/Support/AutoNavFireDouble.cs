// Lifecycle suites double the fire boundary; its native adapter has its own integration suite.
namespace PhobosAutoNav;
internal sealed class FireControlController
{
    internal bool Permitted;
    internal int? InArcCount => null;
    internal int? AmmoCount => null;
    internal int? ReadyCount => null;
    internal double SampleEpoch => double.NaN;
    internal string Reason => "Pursuit.ceased";
    internal void Reset() => Permitted = false;
    internal void Cease() => Permitted = false;
    internal void Authorize(Ship ship, TargetRef target, int group) => Permitted = true;
    internal void Tick(Ship? ship, TargetRef? target, double dt, bool ready) { }
}
