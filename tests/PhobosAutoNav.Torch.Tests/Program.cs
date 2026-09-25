using System;
using System.Globalization;
using PhobosAutoNav;
using PhobosAutoNav.Core;

int checks = 0;
void Check(bool result, string label) { if (!result) throw new Exception(label); checks++; }
double Metres(double au) => au / AutoNavCore.M_TO_AU;
Ship Setup()
{
    NativeContactReader.State = ContactState.Ready;
    AutoNavCore.ResetStatics(); Plugin.Service = new NavigationService();
    CrewSim.system = new StarSystem(); CrewSim.objInstance = new CrewSim(); StarSystem.fEpoch = 100;
    Plugin.PreferTorch.Value = true; Plugin.TorchMaximumG.Value = 1;
    Plugin.UseThrusterRotation.Value = true;
    var ship = new Ship(); CrewSim.coPlayer = ship.Reactor;
    AutoNavCore.BeginFlight(ship, new TargetRef(), new CoastSettings(3, 10, .75, 2), true);
    return ship;
}

Check(TorchRules.ZoneClear(400000, 0, 0, 0, 10, 10), "A clear stationary zone permits torch");
Check(!TorchRules.ZoneClear(300000, 0, 0, 0, 0, 1), "No-wake boundary is forbidden");
Check(!TorchRules.ZoneClear(600000, 0, -120000, 0, 0, 10), "Sweep catches a crossing whose two endpoints are outside");
Check(!TorchRules.ZoneClear(-500000, 300999, 100000, 0, 0, 10), "Tangent inside margin is denied");
Check(!TorchRules.ZoneClear(400000, 0, 0, 0, 3000, 10), "Acceleration uncertainty can reach a zone");
Check(!TorchRules.ZoneClear(400000, 0, 0, 0, 0, 10, 100000), "Different native update epochs are bounded");
Check(!TorchRules.ZoneClear(double.NaN, 0, 0, 0, 0, 1), "Unknown geometry cannot grant permission");
Check(!TorchRules.ZoneClear(double.MaxValue, 0, 0, 0, 0, 1), "Overflow cannot grant permission");
Check(TorchRules.Aligned(.001, .0001, 10) && !TorchRules.Aligned(0, .01, 1) &&
    !TorchRules.Aligned(.01, .001, 10), "Heading and spin must stay acceptable throughout the burn");
foreach (double dt in new[] { .01, .25, 1, 10, 60 })
foreach (double gap in new[] { 0, 1, 100, 1000, 85000, 4000000 })
foreach (double speed in new[] { 0, 10, 100, 5000 })
{
    double limit = TorchRules.SafeSpeed(gap, speed, 0, .5, dt);
    double a = CoastRules.BrakingAcceleration(.5);
    Check(limit * limit / (2 * a) + limit * dt <= Math.Max(0, gap - speed * dt) + 1e-6,
        "Torch end-speed budget always retains RCS stopping room");
}
Check(TorchRules.BurnAcceleration(0, 1, 0, 10, 10) == .1 &&
    TorchRules.BurnAcceleration(0, -1, 0, 10, 1) == 0, "Fractional correction cannot reverse or overshoot");

var own = Setup(); var torch = Plugin.Service.Torch;
Check(torch.Available(own, true, 1, out var maximum) && maximum <= 9.81, "Running reactor offers bounded native acceleration");
Check(!torch.Available(own, false, 1, out _), "Saved RCS-only intent is respected");
own.Reactor.Props.Remove("bNWZ");
Check(!torch.Available(own, true, 1, out _), "Missing native no-wake state fails closed");
own.Reactor.Props["bNWZ"] = "false"; own.Reactor.Conditions.Remove("IsReadyFusion");
Check(!torch.Available(own, true, 1, out _), "Cold reactor is never started");
own.Reactor.Conditions.Add("IsReadyFusion"); own.Reactor.Temperature *= 1.3;
Check(!torch.Available(own, true, 1, out _), "Unsafe core temperature falls back");
own.Reactor.Temperature = TorchRules.NativeCoreTemperature;
own.Reactor.Props["slidCycle"] = ".1";
Check(TorchDriveController.ThrustRequested(own), "Pending manual torch request is visible before native thrust begins");
Check(!torch.Available(own, true, 1, out _), "Existing manual torch is not appropriated");
own.Reactor.Props["slidCycle"] = "0";
own.Reactor.Fusion.Deliver = false;
Check(!torch.Burn(own, 2, 1) && own.LargestThrust == 0, "A requested burn cannot create thrust without native delivery");
own.Reactor.Fusion.Deliver = true; StarSystem.fEpoch++;
Check(torch.Burn(own, 2, 1) && own.LargestThrust <= own.Mass * 2 + .001, "Native supplied thrust is capped to guidance demand");
Check(!torch.ChangedByPilot(own, "slidCycle", own.Reactor.Props["slidCycle"]) &&
    torch.ChangedByPilot(own, "slidCycle", "0") && !torch.ChangedByPilot(new Ship(), "slidCycle", "1"),
    "Only changed owned reactor controls cause manual takeover");
var liveCycle = own.Reactor.Props["slidCycle"];
var dto = new JsonItem { aGPMSettings = new[] { new JsonGUIPropMap { strName = "Panel A",
    dictGUIPropMap = DataHandler.ConvertDictToStringArray(own.Reactor.Props) } } };
torch.PrepareSavedControls(own.Reactor, dto);
var saved = DataHandler.ConvertStringArrayToDict(dto.aGPMSettings[0].dictGUIPropMap);
Check(saved["slidCycle"] == "0" && saved["knobRatio"] == "0" && saved["slidFlow"] == "0.2" &&
    own.Reactor.Props["slidCycle"] == liveCycle, "Save copy carries idle controls without changing live burn");
var foreign = new Ship();
double force = 1e8; torch.FilterThrust(foreign, ref force);
Check(force == 1e8, "Other ships' torch commands are untouched");
force = 1e8; torch.FilterThrust(own, ref force);
Check(force <= own.Mass * 2, "Later Fusion update cannot bypass the current force cap");
CrewSim.system!.FailQuery = true; force = 10000; torch.FilterThrust(own, ref force);
Check(force == 0, "Zone lookup failure cuts thrust without throwing through native Fusion.Update");
CrewSim.system.FailQuery = false; StarSystem.fEpoch += 10; force = 10000; torch.FilterThrust(own, ref force);
Check(force == 0, "Expired guidance cannot continue a reactor burn");
torch.Release();
Check(!own.IsUsingTorchDrive && own.Reactor.Props["slidCycle"] == "0" &&
    own.Reactor.Props["slidFlow"] == "0.2" && own.Reactor.Props["knobRatio"] == "0", "Release restores idle flow/mode without restarting thrust");

own = Setup(); torch = Plugin.Service.Torch; torch.Burn(own, 2, 1);
own.Reactor.Conditions.Add("IsOverrideOff"); own.Reactor.Props["slidFlow"] = "0";
torch.Release();
Check(own.Reactor.Props["slidFlow"] == "0" && !own.IsUsingTorchDrive, "Shutdown is not undone by relinquishing controls");
own = Setup(); torch = Plugin.Service.Torch;
var station = new Ship(); station.objSS.vPosx = 305000 * AutoNavCore.M_TO_AU;
own.objSS.vVelX = 1000 * AutoNavCore.M_TO_AU; CrewSim.system!.Stations.Add(station);
Check(!torch.Available(own, true, 10, out _), "Moving into a station's zone falls back before the crossing step");
station.IsNotAFullStation = true;
Check(torch.Available(own, true, 10, out _), "Station classification follows the native non-full-station exclusion");

(Ship Ship, double Distance) Fly(bool prefer, bool restricted, double distanceM, double dt, float throttle = 1, double heading = 0)
{
    var ship = Setup(); CrewSim.system!.Restricted = restricted;
    Plugin.Service.Throttle = throttle; ship.objSS.fRot = (float)heading;
    var target = new TargetRef(); target.TargetSitu.vPosy = distanceM * AutoNavCore.M_TO_AU;
    AutoNavCore.CruiseAU = 100 * AutoNavCore.M_TO_AU; AutoNavCore.ArrSpdAU = 0;
    AutoNavCore.ArriveAU = 1000 * AutoNavCore.M_TO_AU;
    AutoNavCore.BeginFlight(ship, target, new CoastSettings(3, 10, .75, 2), prefer);
    for (int i = 0; i < 100000 && AutoNavCore.Engaged; i++)
    {
        StarSystem.fEpoch += dt;
        AutoNavCore.SteerFlight(ship, target, dt);
        Check(Math.Abs(ship.LastX) + Math.Abs(ship.LastY) + Math.Abs(ship.LastTurn) <= throttle + 1e-7,
            "Every integrated flight command respects selected aggregate throttle");
        Check(TorchRules.Finite(ship.objSS.vAccIn.x, ship.objSS.vAccIn.y, ship.objSS.fRot, ship.objSS.fW), "Hybrid guidance remains finite");
        if (ship.IsUsingTorchDrive) Check(ship.objSS.vAccRCS.magnitude == 0 && ship.objSS.fA == 0, "Burn excludes translational RCS and turning");
        ship.objSS.Integrate(dt);
    }
    double dx = Metres(target.TargetSitu.vPosx - ship.objSS.vPosx), dy = Metres(target.TargetSitu.vPosy - ship.objSS.vPosy);
    double range = Math.Sqrt(dx * dx + dy * dy);
    Console.WriteLine($"Guidance double: torch={prefer} restricted={restricted} start={distanceM}m dt={dt}s result={AutoNavCore.LastResult} end={range:0.0}m RCS-dV={ship.RcsTranslation:0.00} torchCalls={ship.PositiveThrusts} retrograde={ship.RetrogradeBurns}");
    Check(AutoNavCore.LastResult == "ARRIVED" && range >= 900, "Hybrid flight completes without flying through the target");
    Check(!ship.IsUsingTorchDrive && ship.Reactor.Props["slidCycle"] == "0", "Arrival releases the torch");
    return (ship, range);
}
var baseline = Fly(false, false, 85000, .25);
var hybrid = Fly(true, false, 85000, .25);
Check(hybrid.Ship.PositiveThrusts > 0 && hybrid.Ship.RetrogradeBurns > 0, "Guidance actually uses forward and braking torch burns");
Check(hybrid.Ship.RcsTranslation < baseline.Ship.RcsTranslation, "Hybrid approach spends less translational RCS in the boundary model");
var close = Fly(true, false, 3000, .25);
Check(close.Ship.PositiveThrusts > 0, "Approach distance alone does not ban torch");
var prohibited = Fly(true, true, 3000, .25);
Check(prohibited.Ship.PositiveThrusts == 0 && prohibited.Ship.RcsTranslation > 0, "Restricted approach remains fully usable with RCS");
Fly(true, false, 85000, 1);
Fly(true, false, 85000, 10);
Fly(false, false, 3000, .25, .1f, Math.PI / 3);
Fly(true, false, 3000, 1, .25f, -Math.PI / 2);

own = Setup(); torch = Plugin.Service.Torch;
torch.Burn(own, 2, 1); torch.Cut();
Check(!torch.ControlsChanged && !own.IsUsingTorchDrive, "Coasting clears the burn without declaring its own writes manual");
own.Reactor.Props["slidFlow"] = ".3";
Check(torch.ControlsChanged, "Direct reactor-panel changes during coasting also yield control");
own = Setup(); torch = Plugin.Service.Torch; own.Reactor.Props["knobRatio"] = "1";
torch.Burn(own, 2, 1); own.Reactor.Props["bNWZ"] = "true"; torch.Cut();
Check(own.Reactor.Props["knobRatio"] == "0", "Cutting a burn never undoes the native no-wake mode interlock");
own = Setup(); torch = Plugin.Service.Torch; torch.Burn(own, 2, 1);
CrewSim.system!.Restricted = true; force = 10000; torch.FilterThrust(own, ref force);
Check(force == 0, "Entering a restricted zone revokes an already-issued burn");
CrewSim.system.Restricted = false; Plugin.PreferTorch.Value = false; force = 10000; torch.FilterThrust(own, ref force);
Check(force == 0, "Live torch disable revokes an existing permission");
Plugin.PreferTorch.Value = true; own.objSS.fW = .5f; force = 10000; torch.FilterThrust(own, ref force);
Check(force == 0, "Unexpected spin revokes thrust between guidance updates");
own.objSS.fW = 0; CrewSim.objInstance!.FinishedLoading = false; force = 10000; torch.FilterThrust(own, ref force);
Check(force == 0, "Loading cannot execute a retained burn");
CrewSim.objInstance.FinishedLoading = true; torch.Release();
// A detached reload begins with idle native controls and recomputes the burn.
own = Setup(); torch = Plugin.Service.Torch;
own.Reactor.Props = new System.Collections.Generic.Dictionary<string, string>(saved);
Check(!own.IsUsingTorchDrive && torch.Available(own, true, 1, out _), "Reloaded native controls are idle but eligible after validation");
own = Setup(); torch = Plugin.Service.Torch; torch.Burn(own, 2, 1);
var manualFlow = own.Reactor.Props["slidFlow"]; torch.YieldToPilot();
Check(!own.IsUsingTorchDrive && own.Reactor.Props["slidCycle"] == "0" &&
    own.Reactor.Props["knobRatio"] == "1" && own.Reactor.Props["slidFlow"] == manualFlow && !torch.Owns(own),
    "Incoming native manual cycle can take over without Auto Nav undoing its mode/flow");
own = Setup(); torch = Plugin.Service.Torch; Check(torch.Burn(own, 2, 1), "Contact fixture begins with native torch delivery");
NativeContactReader.State = ContactState.Weak; force = 10000; torch.FilterThrust(own, ref force);
Check(force == 0 && torch.Reason == "Sensors.Weak", "Native fusion update cannot deliver an earlier burn after contact loss");
NativeContactReader.State = ContactState.Ready; force = 10000; torch.FilterThrust(own, ref force);
Check(force == 0 && torch.ContactLoss?.State == ContactState.Weak, "Brief reacquisition cannot revive the old burn before suspension is recorded");
Check(!torch.Burn(own, 2, 1), "Lost contact cannot acquire a fresh torch burn");
AutoNavCore.EndFlight(own, "CONTACT LOST");
Check(!AutoNavCore.Engaged && own.objSS.vAccRCS.magnitude == 0 && !own.IsUsingTorchDrive,
    "Contact-loss stop releases native torch and RCS commands");
own = Setup(); torch = Plugin.Service.Torch; torch.Burn(own, 2, 1);
own.Maneuver(1,0,0,0,1); own.Reactor.FailControlWrite = true;
AutoNavCore.EndFlight(own, "CONTACT LOST");
Check(!AutoNavCore.Engaged && own.objSS.vAccRCS.magnitude == 0 && !own.IsUsingTorchDrive,
    "A failed reactor-control write cannot skip the independent RCS stop");
// Production guidance regression: translation, turning, coasting spin damping and torch alignment
// all pass through the same selected-throttle budget at the native boundary.
foreach (float throttle in new[] { .01f, .1f, .25f, 1f })
foreach (bool preferTorch in new[] { false, true })
{
    own = Setup(); Plugin.Service.Throttle = throttle;
    var target = new TargetRef(); target.TargetSitu.vPosx = target.TargetSitu.vPosy = 85000 * AutoNavCore.M_TO_AU;
    AutoNavCore.CruiseAU = 100 * AutoNavCore.M_TO_AU; AutoNavCore.ArrSpdAU = 0; AutoNavCore.ArriveAU = 1000 * AutoNavCore.M_TO_AU;
    AutoNavCore.BeginFlight(own, target, new CoastSettings(3,10,.75,2), preferTorch);
    AutoNavCore.SteerFlight(own, target, .25);
    Check(Math.Abs(own.LastX) + Math.Abs(own.LastY) + Math.Abs(own.LastTurn) <= throttle + 1e-7,
        "Diagonal guidance/torch alignment respects selected throttle including rotation");
    if (!preferTorch) Check(Math.Abs(own.LastTurn) <= throttle * RcsBudget.CombinedRotationShare + 1e-7,
        "Ordinary turn cannot consume the reserved braking budget");
    own.objSS.vVelX = own.objSS.vVelY = 100 / Math.Sqrt(2) * AutoNavCore.M_TO_AU;
    own.objSS.fW = .3f;
    AutoNavCore.SteerFlight(own, target, .25);
    Check(Math.Abs(own.LastX) + Math.Abs(own.LastY) + Math.Abs(own.LastTurn) <= throttle + 1e-7,
        "Coasting spin correction respects low throttle");
}
own = Setup();
var admissionTarget = new TargetRef(); admissionTarget.TargetSitu.vPosy = 2000 * AutoNavCore.M_TO_AU;
own.objSS.vVelY = 100 * AutoNavCore.M_TO_AU;
Check(AutoNavCore.TryReadAdmission(own, admissionTarget, 1, 0, 1, 10, out var room) && !room.Safe,
    "Production target adapter rejects the audited unsafe intercept despite ample fuel");
admissionTarget.TargetSitu.vPosy = 500 * AutoNavCore.M_TO_AU; own.objSS.vVelY = .1 * AutoNavCore.M_TO_AU;
Check(AutoNavCore.TryReadAdmission(own, admissionTarget, 1, 0, .1, 10, out room) && room.Safe,
    "Production target adapter permits a slow approach inside the selected arrival band");
own = Setup();
var brakingTarget = new TargetRef(); brakingTarget.TargetSitu.vPosy = 1500 * AutoNavCore.M_TO_AU;
AutoNavCore.BeginFlight(own, brakingTarget, new CoastSettings(3,10,.75,2), false);
AutoNavCore.Following = true; AutoNavCore.FaceTarget = true; AutoNavCore.WeaponHeading = Math.PI;
own.objSS.vVelY = 100 * AutoNavCore.M_TO_AU;
AutoNavCore.SteerFlight(own, brakingTarget, .25);
Check(AutoNavCore.ControlLimited || AutoNavCore.CurrentPhase == AutoNavCore.Phase.Decel, "Unsafe Follow intercept engages braking/control limit");
Check(Math.Abs(own.LastTurn) < 1e-7, "Opposite N3 weapon attitude cannot take precedence over braking/clearance heading");
Console.WriteLine($"{checks} torch policy, native-boundary and guidance assertions passed. No in-game tests performed.");
PursuitChecks.Run(Check);
