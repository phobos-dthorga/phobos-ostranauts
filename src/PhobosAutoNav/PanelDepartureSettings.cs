using PhobosAutoNav.Core;

namespace PhobosAutoNav;

internal sealed partial class NavigationService
{
    internal int PanelDepartureMode => departureMode;
    internal int PanelDepartureModeCount => DepartureModes.Length;
    internal string PanelDepartureLabel(int mode) => Text.Get("Departure.mode_" + DepartureModes[mode]);

    // Validate both groups before the existing preference commit. Changing intent never detaches a ship.
    internal bool ApplyNavigationPanel(CondOwner co, string expected, FlightPreferences preferences,
        bool torch, int expectedDeparture, int selectedDeparture)
    {
        if (departure != null || departureMode != expectedDeparture || selectedDeparture < 0 ||
            selectedDeparture >= DepartureModes.Length)
        {
            status = Phobos.Ostranauts.Framework.Controls.ConsoleText.Get("stale");
            return false;
        }
        if (!ApplyPanelPreferences(co, expected, preferences, torch)) return false;
        departureMode = selectedDeparture;
        return true;
    }
}
