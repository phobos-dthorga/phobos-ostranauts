namespace PhobosAutoNav;

internal sealed partial class NavigationService
{
    internal bool WatchingForTest => arrivalWatch.Armed;
    internal bool CueCompletedForTest => arrivalWatch.Completed;
    internal void FinishForCueTest(string result)
    {
        AutoNavCore.EndFlight(AutoNavCore.EngagedPlayer, result);
        PersistProgress();
    }
}
