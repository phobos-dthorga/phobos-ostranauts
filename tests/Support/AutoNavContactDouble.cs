using PhobosAutoNav.Core;

namespace PhobosAutoNav;

// Other subsystem suites control the native-contact boundary explicitly. The
// Sensors suite executes the real reader and guidance-service orchestration.
internal readonly struct SensedObject
{
    internal readonly string Id;
    internal readonly ShipSitu Situ;
    internal readonly ContactReading Reading;
    internal SensedObject(string id, ShipSitu situ, ContactReading reading) { Id = id; Situ = situ; Reading = reading; }
}
// Asteroid-field rocks are supplied explicitly; the Sensors suite runs the real scan.
internal static class NativeHazards
{
    internal static readonly System.Collections.Generic.List<SensedObject> Rocks = new();
    internal static System.Collections.Generic.IEnumerable<SensedObject> Asteroids(Ship own, NavVector startM, NavVector goalM, double reachM, string? skipId)
    { foreach (var rock in Rocks) if (rock.Id != skipId) yield return rock; }
}
internal static class NativeContactReader
{
    internal static ContactState State = ContactState.Ready;
    // Optional per-object states, e.g. a ready target with a weak neighbouring contact.
    internal static readonly System.Collections.Generic.Dictionary<string, ContactState> ById = new();
    internal static ContactReading Read(Ship? observer, string? targetId) =>
        new(observer == null || string.IsNullOrEmpty(targetId) ? ContactState.Unavailable :
            ById.TryGetValue(targetId!, out var state) ? state : State);
}
