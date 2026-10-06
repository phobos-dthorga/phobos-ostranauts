using Phobos.Ostranauts.Framework.Flight;

namespace PhobosAutoNav;

/// <summary>Auto Nav's torch slider limit and full acceleration, from Framework's shared rating (Framework 0.123.0),
/// which fair gig deadlines use too. One seam, so the torch controller's offline tests stand in for it.</summary>
internal static class TorchCycle
{
    internal static float Limit(Ship ship, bool safetyOn) => TorchPerformance.CycleLimit(ship, safetyOn);
    internal static double Acceleration(Ship ship, bool safetyOn) => TorchPerformance.Acceleration(ship, safetyOn);
}
