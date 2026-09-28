namespace Phobos.Ostranauts.Framework.Notices;

/// <summary>Tells the player about something automation did aboard their ship, through native channels:
/// a line in the crew message log and, when that ship's navigation station is open, the native nav-map
/// banner with its warning tone. Content mods supply already-localized text.</summary>
public static class PlayerNotices
{
    // Long enough that repeated automation never turns the banner tone into an alarm.
    public const double BannerCooldownSeconds = 20;
    private static readonly NoticeCooldown banners = new(BannerCooldownSeconds);

    /// <summary>Returns true when the message was logged for a crew member aboard the ship.</summary>
    public static bool Post(Ship ship, string key, NoticeLevel level, string logText, string? bannerText = null)
    {
        if (ship == null || ship.bDestroyed || string.IsNullOrEmpty(logText) || CrewSim.objInstance == null) return false;
        var selected = CrewSim.GetSelectedCrew();
        var crew = selected != null && !selected.bDestroyed && selected.ship == ship ? selected :
            CrewSim.coPlayer != null && !CrewSim.coPlayer.bDestroyed && CrewSim.coPlayer.ship == ship ? CrewSim.coPlayer : null;
        crew?.LogMessage(logText, level == NoticeLevel.Caution ? "Badish" : "Neutral", "Game");
        if (bannerText != null && level == NoticeLevel.Caution && GUIOrbitDraw.IsOpen() && GUIOrbitDraw.Instance != null &&
            GUIOrbitDraw.Instance.COSelfBase()?.ship == ship && banners.Allow(key, UnityEngine.Time.unscaledTime))
            GUIOrbitDraw.Instance.CGClampWarning(bannerText, bSkipDH: true);
        return crew != null;
    }
}
