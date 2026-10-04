using Phobos.Ostranauts.Framework.Notices;

namespace Phobos.Ostranauts.Framework.Data;

/// <summary>Tells the player, once per content load, that an add-on or one of their own data files was skipped
/// (Framework 0.90.0). A skipped file is never fatal and the shipped data stands, but a player who subscribed to an
/// add-on should not have to open the log to learn it did nothing. The reasons are in the console.</summary>
internal static class DataFileNotice
{
    private static int announced = -1;
    internal static void Poll()
    {
        int skipped = DataPacks.Problems.Count + AddOns.Refused.Count;
        if (skipped == 0 || skipped == announced) return;
        var ship = CrewSim.coPlayer?.ship;
        if (ship == null || CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading) return;
        announced = skipped;
        PlayerNotices.Post(ship, "PhobosFramework.data-files", NoticeLevel.Caution, Text.Get("AddOns.notice", skipped));
    }
}
