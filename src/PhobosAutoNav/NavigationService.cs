using System;
using System.Globalization;
using System.Linq;
using BepInEx.Bootstrap;
using HarmonyLib;
using Ostranauts.Core;
using Ostranauts.Core.Models;
using PhobosAutoNav.Core;

namespace PhobosAutoNav;

internal sealed class NavigationService
{
    internal const string ModuleId = "PhobosNavModAutoNav";
    internal const string DamagedId = ModuleId + "Dmg";
    private readonly Action<string> log;
    private CondOwner? console;
    private bool issuing;
    private string status = "Idle; test-save use only";
    private static CondOwner? OpenConsole => GUIOrbitDraw.IsOpen() ? GUIOrbitDraw.Instance.COSelfBase() : null;
    private static bool InTestSave => ArrivalBrake.TestSaveAllowed(
        (AccessTools.Field(typeof(LoadManager), "_loadedSave")?.GetValue(null) as SaveInfo)?.SaveName);
    internal NavigationService(Action<string> log) { this.log = log; }

    internal float Throttle
    {
        get
        {
            if (console != null && console.mapGUIPropMaps.TryGetValue("Panel A", out var props)
                && props.TryGetValue("slidThrottle", out string value)
                && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float throttle)
                && ArrivalBrake.Finite(throttle)) return Math.Max(0, Math.Min(1, throttle));
            return 0; // Missing throttle state must not silently become a burn.
        }
    }

    private static bool HasId(CondOwner co, string id) => !co.bDestroyed && (co.strName == id || co.strCODef == id);
    private static bool PropOn(CondOwner co, string key) => co.mapGUIPropMaps.TryGetValue("Panel A", out var props)
        && props.TryGetValue(key, out var value) && string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    private static string? HardwareProblem(CondOwner? co)
    {
        if (co == null || co.bDestroyed || !co.HasCond("IsInstalled")) return "Installed nav console required";
        if (co.HasCond("IsOff") || !co.HasCond("IsPowered") || co.HasCond("IsDamaged")) return "Console is off, unpowered or damaged";
        if (!co.GetCOsSafe(true).Any(item => HasId(item, ModuleId) && !item.HasCond("IsDamaged"))) return "Working Phobos Auto Nav module required";
        if (co.ship == null || co.ship.bDestroyed || CrewSim.coPlayer == null || CrewSim.coPlayer.ship != co.ship) return "Player must be aboard the controlled ship";
        if (co.ship.IsDocked()) return "Undock before engagement";
        if (co.ship.bCheckPower) return "Power network is updating";
        if (co.ship.IsUsingTorchDrive || co.ship.shipStationKeepingTarget != null || (co.ship.aWPs != null && co.ship.aWPs.Count > 0)
            || PropOn(co, "chkStationKeeping") || PropOn(co, "chkHoldThrust") || PropOn(co, "chkEngage")
            || AIShipManager.GetAIShipByRegID(co.ship.strRegID) != null) return "Disengage other flight automation first";
        if (CrewSim.system == null || CrewSim.system.IsInAtmo(co.ship)) return "Free-space testing only";
        if (co.ship.RCSCount <= 0 || co.ship.GetRCSRemain() <= 0) return "Working RCS and fuel required";
        if (co.ship.objSS == null || !ArrivalBrake.Finite(co.ship.RCSAccelMax) || co.ship.RCSAccelMax <= 0) return "RCS acceleration unavailable";
        return null;
    }

    internal void Engage(CondOwner? co)
    {
        if (AutoNavCore.Engaged) { status = "Already engaged; stop before changing the flight"; return; }
        try
        {
            if (!InTestSave) { status = "Use a separate save named PhobosAutoNavTest or PhobosAutoNavTest-..."; return; }
            if (!Plugin.Enabled.Value) { status = "Mod disabled in settings"; return; }
            if (CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading) { status = "World is loading"; return; }
            // No upstream dependency. Refuse this prototype alongside the original flight plugin.
            if (Chainloader.PluginInfos.ContainsKey("com.mrkmg.ostranauts.autonavigate"))
            { status = "Disable original Auto Navigate and restart before using the standalone adaptation"; return; }
            string? problem = HardwareProblem(co);
            if (problem != null) { status = problem; return; }
            console = co;
            if (Throttle <= 0) { status = "Set the nav console throttle above zero"; return; }
            var contact = GUIOrbitDraw.CrossHairTarget;
            if (contact?.Ship == null || contact.Ship == co!.ship || contact.Ship.bDestroyed || contact.Ship.HideFromSystem || contact.Ship.IsStationHidden())
            { status = "Select another ship or station; planetary travel is outside this prototype"; return; }
            if (AutoNavCore.AutoDockBusy()) { status = "Auto Dock already controls flight"; return; }
            Type? approach = AccessTools.TypeByName("PhobosApproachAssist.Plugin");
            object? approachService = approach == null ? null : AccessTools.Property(approach, "Service")?.GetValue(null);
            if (approachService != null && (bool)(AccessTools.Property(approachService.GetType(), "Active")?.GetValue(approachService) ?? false))
            { status = "Stop Approach Assist's test pulse first"; return; }
            var target = TargetRef.FromCrossHair();
            if (target == null) { status = "Target unavailable"; return; }
            float cruise = Plugin.DefaultCruiseMS.Value, arrival = Plugin.DefaultArriveSpeedMS.Value, distance = Plugin.DefaultArriveKM.Value;
            if (!ArrivalBrake.Finite(cruise) || !ArrivalBrake.Finite(arrival) || !ArrivalBrake.Finite(distance))
            { status = "Invalid flight settings"; return; }
            AutoNavCore.CruiseAU = cruise * AutoNavCore.M_TO_AU;
            AutoNavCore.ArrSpdAU = Math.Min(arrival, cruise) * AutoNavCore.M_TO_AU;
            AutoNavCore.ArriveAU = distance * AutoNavCore.KM_TO_AU;
            if (Plugin.FuelCheck.Value && !AutoNavCore.HasFuelForFlight(co!.ship, target))
            { status = "Insufficient estimated delta-v"; return; }
            issuing = true;
            try { AutoNavCore.BeginFlight(co!.ship, target); } finally { issuing = false; }
            status = "Flight engaged";
        }
        catch (Exception ex) { log(ex.ToString()); Disengage("Engagement failed; see log"); }
        log(status);
    }

    internal void Tick(ShipSitu situ, double dt, bool ignoreAcceleration)
    {
        if (!AutoNavCore.Engaged || AutoNavCore.EngagedPlayer?.objSS != situ || ignoreAcceleration || dt == 0) return;
        try
        {
            string? problem = !Plugin.Enabled.Value ? "Mod disabled" : !InTestSave ? "Test-save gate closed" : HardwareProblem(console);
            if (problem == null && (!ArrivalBrake.Finite(dt) || dt < 0 || dt > Plugin.MaximumStepSeconds.Value)) problem = "Simulation step too large or invalid; reduce time compression";
            if (problem == null && Throttle <= 0) problem = "Throttle zero or unavailable";
            if (problem == null && AutoNavCore.AutoDockBusy()) problem = "Auto Dock took control";
            if (problem == null && (!ArrivalBrake.Finite(Plugin.ArrivalSpeedTolerance.Value) || !ArrivalBrake.Finite(Plugin.MaximumStepSeconds.Value))) problem = "Invalid safety settings";
            if (problem != null) { Disengage(problem); return; }
            issuing = true;
            try { AutoNavCore.SteerFlight(AutoNavCore.EngagedPlayer, AutoNavCore.EngagedTarget, dt); }
            finally { issuing = false; }
            status = AutoNavCore.Engaged ? AutoNavCore.PhaseName : AutoNavCore.LastResult ?? "Stopped";
        }
        catch (Exception ex) { log(ex.ToString()); Disengage("Flight error; see log"); }
    }

    internal void ExternalControl(Ship ship, float x, float y, float rotation)
    {
        // Closing the native console emits a zero command; it must not cancel off-console travel.
        if (!issuing && (x != 0 || y != 0 || rotation != 0) && AutoNavCore.Engaged && AutoNavCore.EngagedPlayer == ship)
            Disengage("External maneuver command; pilot/other controller has control");
    }

    internal void Disengage(string reason)
    {
        issuing = true;
        try { if (AutoNavCore.Engaged) AutoNavCore.EndFlight(AutoNavCore.EngagedPlayer, reason); }
        catch (Exception ex) { log("Stop failed: " + ex); }
        finally { AutoNavCore.ResetStatics(); issuing = false; console = null; status = reason; }
        log(reason);
    }

    internal string ReadPanel(CondOwner co) => status + "\n" +
        (AutoNavCore.Engaged ? "Target: " + AutoNavCore.EngagedTarget.DisplayName : HardwareProblem(co) ?? "Ready to select target");

    internal bool Command(string[] words, out string response)
    {
        try
        {
            string verb = words.Length == 1 ? "help" : words[1].ToLowerInvariant();
            if (words.Length > 2) { response = "No extra arguments accepted. Use phobosnav help."; return false; }
            switch (verb)
            {
                case "help": response = "phobosnav help | status | settings | fly | stop | spawn\nFly/spawn require PhobosAutoNavTest saves. Stop clears thrust; it does not brake. Config changes apply on the next launch; cruise/arrival settings are captured per flight."; return true;
                case "status": response = $"Phobos Auto Nav {Plugin.Version}; standalone; test save={InTestSave}; engaged={AutoNavCore.Engaged}\n{status}"; return true;
                case "settings": response = $"Cruise {Plugin.DefaultCruiseMS.Value} m/s; arrival {Plugin.DefaultArriveSpeedMS.Value} m/s at {Plugin.DefaultArriveKM.Value} km; tolerance {Plugin.ArrivalSpeedTolerance.Value} m/s; max step {Plugin.MaximumStepSeconds.Value} s.\nBepInEx/config/{Plugin.Id}.cfg"; return true;
                case "fly": Engage(OpenConsole); response = status; return AutoNavCore.Engaged;
                case "stop": Disengage("Stopped by pilot; coasting"); response = status; return true;
                case "spawn": return Spawn(out response);
                default: response = "Unknown command. Use phobosnav help."; return false;
            }
        }
        catch (Exception ex) { log(ex.ToString()); response = "Command failed; see BepInEx log."; return false; }
    }

    private bool Spawn(out string response)
    {
        var co = OpenConsole;
        if (!InTestSave || CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading)
        { response = "Load a separate PhobosAutoNavTest save first"; return false; }
        if (co == null || co.bDestroyed || !co.HasCond("IsInstalled") || co.HasCond("IsLocked") || co.ship != CrewSim.coPlayer?.ship)
        { response = "Open an installed, unlocked nav console aboard your ship"; return false; }
        if (co.GetCOsSafe(true).Any(item => HasId(item, ModuleId))) { response = "Module already present"; return false; }
        if (DataHandler.GetCOOverlay(ModuleId) == null) { response = "Native Phobos Auto Nav package is not loaded"; return false; }
        var item = DataHandler.GetCondOwner(ModuleId);
        if (item == null) { response = "Could not create module"; return false; }
        try
        {
            var remainder = co.AddCO(item, bEquip: false, bOverflow: true, bIgnoreLocks: false);
            if (remainder != null) { item.Destroy(); response = "Console has no compatible free capacity"; return false; }
        }
        catch
        {
            if (!item.bDestroyed && item.objCOParent == null && item.ship == null) item.Destroy();
            throw;
        }
        response = "Module added. Reopen the console and place it using Edit.";
        return true;
    }
}
