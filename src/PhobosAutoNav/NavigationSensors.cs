using System;
using PhobosAutoNav.Core;

namespace PhobosAutoNav;

internal sealed partial class NavigationService
{
    private static ContactReading ReadContact(CondOwner? co, TargetRef? target) =>
        NativeContactReader.Read(co?.ship, target?.ShipId);

    private void SuspendForContact(ContactReading contact)
    {
        // Keep intent, assigned ports, elapsed budget and coast settings. No
        // blind braking/prediction, automatic reacquisition or saved telemetry.
        Fire.Cease();
        bool persisted = FinishSavedFlight(savedFlight?.SuspendedMode ?? SavedFlightMode.Suspended);
        issuing = true;
        try { if (AutoNavCore.Engaged) AutoNavCore.EndFlight(AutoNavCore.EngagedPlayer, "CONTACT LOST"); }
        catch (Exception ex) { log(ex.ToString()); }
        finally
        {
            AutoNavCore.ResetStatics(); issuing = false;
            status = persisted ? Text.Get("Sensors.suspended", Text.Get(contact.MessageKey)) : Text.Get("Persistence.write_failed");
        }
        log(status);
    }
}
