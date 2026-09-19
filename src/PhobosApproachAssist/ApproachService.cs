using System;
using System.Linq;
using System.Reflection;
using System.Text;
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
    private static CondOwner? OpenConsole => GUIOrbitDraw.IsOpen() ? GUIOrbitDraw.Instance.COSelfBase() : null;
    private static bool PropOn(CondOwner co, string key) => co.mapGUIPropMaps.TryGetValue("Panel A", out var props)
        && props.TryGetValue(key, out var value) && string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);

    internal bool RunDebugCommand(DebugAction action, out string response)
    {
        bool success = true;
        try
        {
            switch (action)
            {
                case DebugAction.Help: response = DebugCommands.Help; break;
                case DebugAction.Status: response = ReadDiagnostics(); break;
                case DebugAction.Spawn:
                case DebugAction.SpawnDamaged:
                    success = AddTestModule(action == DebugAction.SpawnDamaged);
                    response = Status;
                    break;
                case DebugAction.Pulse:
                    if (Active) { success = false; response = "A pulse is already active; use phobosapproach stop first"; break; }
                    var co = OpenConsole;
                    if (co == null) { success = false; response = "Open the ship's nav console first (not PDA navigation)"; break; }
                    Engage(co);
                    success = Active;
                    response = Status;
                    break;
                case DebugAction.Stop:
                    Disengage("Approach Assist disengaged by debug command; no braking applied");
                    response = Status;
                    break;
                default:
                    success = false;
                    response = "Unknown command or extra arguments. Use phobosapproach help";
                    break;
            }
        }
        catch (Exception ex)
        {
            success = false;
            response = "Approach Assist command failed: " + ex.GetType().Name + ". See the BepInEx log.";
            log(ex.ToString());
        }
        log(response);
        return success;
    }

    private string ReadDiagnostics()
    {
        var report = new StringBuilder("Phobos Approach Assist " + Plugin.Version);
        report.Append("\nTest-save gate: ").Append(InTestSave ? "allowed" : "blocked (use PhobosApproachAssistTest)");
        report.Append("\nWorld: ").Append(CrewSim.objInstance != null && CrewSim.objInstance.FinishedLoading ? "loaded" : "not ready");
        report.Append("\nNative definitions: normal=").Append(DataHandler.GetCOOverlay(ModuleId) != null)
            .Append(", damaged=").Append(DataHandler.GetCOOverlay(DamagedId) != null);
        report.Append("\nController: ").Append(Active ? "armed" : "idle").Append("; ").Append(Status);
        if (pulse != null) report.Append("\nPulse: ").Append(pulse.Elapsed.ToString("F3")).Append(" / 2 s; commanded delta-v ")
            .Append(pulse.CommandedDeltaV.ToString("F4")).Append(" / 0.1 m/s");
        var co = OpenConsole;
        if (co == null) return report.Append("\nOpen the ship's nav console for hardware and contact checks (not PDA navigation).").ToString();
        report.Append("\nConsole: installed=").Append(co.HasCond("IsInstalled")).Append(", powered=").Append(co.HasCond("IsPowered"))
            .Append(", off=").Append(co.HasCond("IsOff")).Append(", damaged=").Append(co.HasCond("IsDamaged"));
        var modules = co.GetCOsSafe(true).Where(IsModule).ToArray();
        report.Append("\nModules: normal=").Append(modules.Count(c => HasModuleId(c, ModuleId)))
            .Append(", damaged=").Append(modules.Count(c => HasModuleId(c, DamagedId)));
        if (co.ship != null && CrewSim.coPlayer != null && co.ship == CrewSim.coPlayer.ship)
            report.Append("\nShip: RCS units=").Append(co.ship.RCSCount).Append(", fuel=").Append(co.ship.GetRCSRemain().ToString("F3"))
                .Append(" kg; any sensor on=").Append(co.ship.ElectronicSystems?.HasAnySensorOn() == true);
        var contact = Active && console == co ? target : GUIOrbitDraw.CrossHairTarget?.Ship;
        report.Append("\nPulse check: ").Append(Validate(co, contact, out _) ?? "ready; pulse has no automatic braking");
        return report.ToString();
    }

    internal bool AddTestModule(bool damaged = false)
    {
        var co = OpenConsole;
        if (!InTestSave) { Status = "Test-save restriction: use PhobosApproachAssistTest"; return false; }
        if (CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading) { Status = "Game is loading"; return false; }
        if (co == null || co.bDestroyed || co.ship == null || CrewSim.coPlayer == null || co.ship != CrewSim.coPlayer.ship
            || !co.HasCond("IsInstalled"))
        { Status = "Open an installed nav console on the player's current ship (not PDA navigation)"; return false; }
        if (co.HasCond("IsLocked")) { Status = "Unlock the nav console before adding a test module"; return false; }
        string moduleId = damaged ? DamagedId : ModuleId;
        if (co.GetCOsSafe(true).Any(c => HasModuleId(c, moduleId)))
        { Status = "That module variant is already present; use nav Edit to place it"; return false; }
        if (DataHandler.GetCOOverlay(moduleId) == null) { Status = "Native Approach Assist data package is not loaded"; return false; }
        CondOwner? item = null;
        try
        {
            item = DataHandler.GetCondOwner(moduleId);
            if (item == null) { Status = "Could not create test module"; return false; }
            // Native bOverflow must be true to continue past stacking into the container.
            // It still respects capacity and locks, and does not drop anything onto the floor.
            var remainder = co.AddCO(item, bEquip: false, bOverflow: true, bIgnoreLocks: false);
            if (remainder != null)
            {
                item.Destroy();
                Status = "Console cannot accept the test module (full, locked or incompatible)";
                return false;
            }
        }
        catch (Exception ex)
        {
            if (item != null && !item.bDestroyed && item.objCOParent == null && item.ship == null) item.Destroy();
            Status = "Module creation failed: " + ex.GetType().Name + ". Check the console and BepInEx log before retrying.";
            log(ex.ToString());
            return false;
        }
        Status = (damaged ? "Damaged test module" : "Test module") + " added. Close/reopen the console, then use Edit to place it.";
        log(Status);
        return true;
    }

    private static bool HasModuleId(CondOwner co, string id) => co != null && !co.bDestroyed && (co.strName == id || co.strCODef == id);
    private static bool IsModule(CondOwner co) => HasModuleId(co, ModuleId) || HasModuleId(co, DamagedId);

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
