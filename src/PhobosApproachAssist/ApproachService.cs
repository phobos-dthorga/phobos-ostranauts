using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Ostranauts.Core;
using Ostranauts.Core.Models;
using Ostranauts.Ships.Sensors;
using PhobosApproachAssist.Core;

namespace PhobosApproachAssist;

internal sealed class ApproachService
{
    internal const string ModuleId = "PhobosNavModApproachAssist";
    internal const string DamagedId = ModuleId + "Dmg";
    private const double AuInMetres = 149597870700;
    private const double MaximumRangeM = 100000;
    private const double MinimumClearanceM = 5000;
    private const double MaximumRelativeSpeed = 5;
    private static readonly FieldInfo SaveField = AccessTools.Field(typeof(LoadManager), "_loadedSave");
    private readonly Action<string> log;
    private BurnPlan? pulse;
    private CondOwner? console;
    private Ship? ship;
    private Ship? target;
    private StarSystem? system;
    private bool issuingThrust;
    private bool ownsThrust;
    private double startFuel;
    internal string Status { get; private set; } = "Prototype idle";

    internal ApproachService(Action<string> log) { this.log = log; }
    internal bool Active => pulse?.Active == true;
    private static bool InTestSave => TestSavePolicy.Allows((SaveField?.GetValue(null) as SaveInfo)?.SaveName);
    private static bool PropOn(CondOwner co, string key) => co.mapGUIPropMaps.TryGetValue("Panel A", out var props)
        && props.TryGetValue(key, out var value) && string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);

    internal void AddTestModule()
    {
        var nav = GUIOrbitDraw.Instance;
        var co = nav == null ? null : nav.COSelfBase();
        if (!InTestSave || co == null) { Status = "Open a nav console in a named test save first"; return; }
        if (co.GetCOsSafe(true).Any(IsModule)) { Status = "Module already present; use nav Edit to place it"; return; }
        if (DataHandler.GetCOOverlay(ModuleId) == null) { Status = "Native Approach Assist data package is not loaded"; return; }
        var item = DataHandler.GetCondOwner(ModuleId);
        if (item == null) { Status = "Could not create test module"; return; }
        var remainder = co.AddCO(item, false, false, false);
        if (remainder != null)
        {
            remainder.Destroy();
            Status = "Console has no room for the test module";
            return;
        }
        Status = "Test module added. Close/reopen the console, then use Edit to place it.";
        log(Status);
    }

    private static bool IsModule(CondOwner co) => co != null && !co.bDestroyed
        && (co.strName == ModuleId || co.strCODef == ModuleId || co.strName == DamagedId || co.strCODef == DamagedId);

    internal string ReadPanel(CondOwner co)
    {
        try
        {
            var contact = Active && console == co ? target : GUIOrbitDraw.CrossHairTarget?.Ship;
            string? reason = Validate(co, contact, out _);
            return Status + "\n" + (reason ?? "Firm contact; ready for a two-second pulse");
        }
        catch (Exception ex) { return "Prototype unavailable: " + ex.GetType().Name; }
    }

    internal void Engage(CondOwner co)
    {
        Disengage("Selecting test contact");
        try
        {
            var contact = GUIOrbitDraw.CrossHairTarget?.Ship;
            string? reason = Validate(co, contact, out _);
            if (reason != null) { Status = reason; return; }
            console = co; ship = co.ship; target = contact; system = CrewSim.system;
            startFuel = ship.GetRCSRemain();
            pulse = new BurnPlan();
            Status = pulse.Status;
            log("Test pulse armed; fuel=" + startFuel.ToString("F3") + " kg. It will not brake afterwards.");
        }
        catch (Exception ex) { Disengage("Engagement failed: " + ex.GetType().Name); }
    }

    private string? Validate(CondOwner? co, Ship? contact, out double acceleration)
    {
        acceleration = 0;
        if (!InTestSave) return "Test-save restriction: use PhobosApproachAssistTest";
        if (CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading) return "Game is loading";
        if (co == null || co.bDestroyed || co.ship == null || co.ship.objSS == null) return "Nav console unavailable";
        var us = co.ship;
        if (CrewSim.coPlayer == null || CrewSim.coPlayer.ship != us) return "Use the player's current ship";
        if (!co.HasCond("IsInstalled") || !co.HasCond("IsPowered") || co.HasCond("IsDamaged") || co.HasCond("IsOff"))
            return "Console must be installed, powered and undamaged";
        if (us.bCheckPower) return "Power network is updating";
        if (!co.GetCOsSafe(true).Any(c => IsModule(c) && !c.HasCond("IsDamaged")
            && c.strName != DamagedId && c.strCODef != DamagedId)) return "Install an undamaged Approach Assist module";
        if (us.bDestroyed || us.IsDocked() || us.objSS.bBOLocked || us.objSS.bIsBO) return "Undock and clear the station first";
        if (us.IsUsingTorchDrive || (us.aWPs != null && us.aWPs.Count > 0)
            || us.shipStationKeepingTarget != null || PropOn(co, "chkStationKeeping") || PropOn(co, "chkHoldThrust") || PropOn(co, "chkEngage")
            || AIShipManager.GetAIShipByRegID(us.strRegID) != null) return "Disengage other flight automation first";
        if (CrewSim.system == null || CrewSim.system.IsInAtmo(us)) return "Free-space testing only";
        if (!BurnPlan.Finite(us.objSS.fW) || Math.Abs(us.objSS.fW) > 0.001) return "Stop the ship's rotation first";
        if (contact == null || contact == us || contact.bDestroyed || contact.objSS == null || contact.HideFromSystem)
            return "Select a ship or station contact";
        if (contact.IsStationHidden()) return "Contact unavailable";
        double distance = us.GetRangeTo(contact) * AuInMetres;
        double clearance = distance - (us.objSS.GetRadiusAU() + contact.objSS.GetRadiusAU()) * AuInMetres;
        if (!BurnPlan.Finite(distance) || !BurnPlan.Finite(clearance) || clearance < MinimumClearanceM || distance > MaximumRangeM)
            return "Test range: more than 5 km hull clearance, within 100 km";
        var sensors = us.ElectronicSystems;
        if (sensors == null || !sensors.HasAnySensorOn() || us.bCheckSensors) return "Native sensors unavailable or updating";
        foreach (var body in CrewSim.system.aBOs.Values)
            if (body != null && body.nDrawFlagsBody != 1 && !body.IsAsteroidField && StarSystem.IsLOSBlockedByBO(body, us, contact.objSS))
                return "Contact blocked by a celestial body";
        var signature = new ShipSignature(contact, us);
        double signal = sensors.GetSignatureStrength(signature, distance / 1000,
            Math.Min(us.fVisibilityRangeMod, contact.fVisibilityRangeMod));
        // Conservative unskilled threshold; never mutate the game's shared operator threshold.
        if (!BurnPlan.Finite(signal) || !BurnPlan.Finite(sensors.DetectionThreshold)
            || signal < Math.Max(0.3, sensors.DetectionThreshold)) return "Firm native sensor contact required";
        if (!BurnPlan.Finite(signature.VRelMS) || signature.VRelMS > MaximumRelativeSpeed) return "Reduce relative speed below 5 m/s";
        acceleration = us.RCSAccelMax * AuInMetres;
        if (us.RCSCount <= 0 || !BurnPlan.Finite(acceleration) || acceleration <= 0) return "No usable RCS";
        double reserve = Math.Max(1, 4 * us.CalculateRCSFuelConsumption(BurnPlan.MaxDeltaV / AuInMetres));
        double fuel = us.GetRCSRemain();
        if (!BurnPlan.Finite(reserve) || !BurnPlan.Finite(fuel) || fuel < reserve) return "Insufficient fuel for test and reserve";
        return null;
    }

    internal void BeforePhysics(StarSystem currentSystem, double seconds)
    {
        if (!Active) return;
        try
        {
            if (system != currentSystem) { Disengage("World changed"); return; }
            string? reason = Validate(console, target, out double maximumAcceleration);
            if (reason != null) { Disengage(reason); return; }
            if (CrewSim.Paused) return;
            double dx = target!.objSS.vPosx - ship!.objSS.vPosx;
            double dy = target.objSS.vPosy - ship.objSS.vPosy;
            if (!ThrustDirection.TryCreate(dx, dy, ship.objSS.fRot, out var direction))
            { Disengage("Invalid target direction"); return; }
            double accel = pulse!.Step(seconds, direction.AvailableAcceleration(maximumAcceleration));
            Status = pulse.Status;
            if (accel <= 0) return;
            issuingThrust = true;
            ownsThrust = true;
            ship.UnlockFromOrbit();
            ship.Maneuver((float)(direction.X * accel / maximumAcceleration), (float)(direction.Y * accel / maximumAcceleration),
                0, 0, (float)seconds, Ship.EngineMode.RCS);
        }
        catch (Exception ex) { Disengage("Test interrupted: " + ex.GetType().Name); }
        finally { issuingThrust = false; }
    }

    internal void AfterPhysics()
    {
        ClearOwnThrust();
        if (pulse != null && !pulse.Active)
        {
            log(Status + "; commanded delta-v=" + pulse.CommandedDeltaV.ToString("F4")
                + " m/s; fuel used=" + (startFuel - (ship?.GetRCSRemain() ?? startFuel)).ToString("F4") + " kg");
            pulse = null;
        }
    }

    internal void ObserveExternalThrust(Ship sender, float x, float y, float rotation)
    {
        if (issuingThrust || sender != ship || pulse == null || (x == 0 && y == 0 && rotation == 0)) return;
        // The original Maneuver call follows immediately. It owns the replacement command.
        ownsThrust = false;
        pulse.Cancel("Manual or external thrust: control released");
        Status = pulse.Status;
        pulse = null;
        log(Status);
    }

    internal void Disengage(string reason)
    {
        ClearOwnThrust();
        if (pulse != null) { pulse.Cancel(reason); log(reason); }
        pulse = null; console = null; ship = null; target = null; system = null;
        Status = reason;
    }

    private void ClearOwnThrust()
    {
        if (!ownsThrust) return;
        ownsThrust = false;
        if (ship != null && !ship.bDestroyed && ship.objSS != null) ship.StopManeuver(ship.LoadState >= Ship.Loaded.Edit, false);
    }
}
