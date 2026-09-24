using PhobosAutoNav.Core;

namespace PhobosAutoNav;

// Other subsystem suites control the native-contact boundary explicitly. The
// Sensors suite executes the real reader and guidance-service orchestration.
internal static class NativeContactReader
{
    internal static ContactState State = ContactState.Ready;
    internal static ContactReading Read(Ship? observer, string? targetId) =>
        new(observer == null || string.IsNullOrEmpty(targetId) ? ContactState.Unavailable : State);
}
