using System;
using System.Collections.Generic;
using PhobosAutoNav.Core;

namespace PhobosAutoNav;

internal sealed partial class NavigationService
{
    private static ContactReading ReadContact(CondOwner? co, TargetRef? target) =>
        NativeContactReader.Read(co?.ship, target?.ShipId);

    // Selective sensor engagement (NavigationSensorAssist.cs). Without it these hooks do nothing
    // and every read below behaves exactly as the plain native reader.
    partial void RestoreTargetSensors(CondOwner? co, string? targetId, ref ContactReading reading);
    partial void RestoreHazardSensors(CondOwner co, IReadOnlyList<string> weakIds);
    partial void SettleSensors(Ship own, bool hold, ref bool settling);
    partial void ReconcileSensors();
    partial void DiscoverSensorHolders(Ship ship);
    partial void ForgetSensorWork();
    partial void ReadSensorsInUse(CondOwner co, ref string line);
    partial void ReadSensorWork(CondOwner console, ref bool pending);

    /// <summary>Guidance and explicit starts: a weak or empty track may first be restored by
    /// switching on the fewest suitable sensors, then it is read again. Displays never call this.</summary>
    private ContactReading SenseTarget(CondOwner? co, string? targetId)
    {
        var reading = NativeContactReader.Read(co?.ship, targetId);
        if (!reading.Usable) RestoreTargetSensors(co, targetId, ref reading);
        return reading;
    }

    /// <summary>True while a native sensor refresh follows Auto Nav's own switching: guidance waits
    /// instead of suspending. A hold spends one settle check and clears Auto Nav's thrust for this
    /// step; hold: false only asks, for checks that never command thrust.</summary>
    private bool SensorsSettling(Ship? own, ContactReading reading, bool hold = true)
    {
        bool settling = false;
        if (own != null && reading.State == ContactState.Updating) SettleSensors(own, hold, ref settling);
        return settling;
    }
    internal bool SensorsSettlingForTorch(Ship own, ContactReading reading) => SensorsSettling(own, reading, hold: false);

    private void SuspendForContact(ContactReading contact)
    {
        // Keep intent, assigned ports, elapsed budget and coast settings. No
        // blind braking/prediction, automatic reacquisition or saved telemetry.
        CeaseFire();
        bool persisted = FinishSavedFlight(savedFlight?.SuspendedMode ?? SavedFlightMode.Suspended);
        issuing = true;
        try { if (AutoNavCore.Engaged) AutoNavCore.EndFlight(AutoNavCore.EngagedPlayer, "CONTACT LOST"); }
        catch (Exception ex) { log(ex.ToString()); }
        finally
        {
            AutoNavCore.ResetStatics(); DropCombat(); issuing = false;
            status = persisted ? Text.Get("Sensors.suspended", Text.Get(contact.MessageKey)) : Text.Get("Persistence.write_failed");
        }
        log(status);
    }
}
