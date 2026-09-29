using System;
using System.Linq;
using PhobosAutoNav;
using PhobosAutoNav.Core;
using Phobos.Ostranauts.Framework.Persistence;

int count = 0;
void Check(bool result, string message) { count++; if (!result) throw new Exception(message); }

DepartureChecks.Run(Check);

// Integrate the controller against translation and the native double rotation update.
foreach (double dt in new[] { .02, .1, .5, 1d })
foreach (double acceleration in new[] { .1, 1d, 5d })
foreach (double heading in new[] { 0d, .7, -2.8 })
{
    double x = 0, y = -2000, vx = 0, vy = 0, rot = heading, spin = .01;
    bool arrived = false;
    for (double time = 0; time < DockingRules.MaximumSeconds; time += dt)
    {
        bool valid = DockingRules.TryGuide(-x, -y, vx, vy, rot, spin, 200, acceleration, .8, dt, out var c);
        Check(valid, $"Safe intercept remains controllable dt={dt} a={acceleration} heading={heading} t={time} r={Math.Sqrt(x*x+y*y)} v=({vx},{vy}) rot={rot} spin={spin}");
        Check(Math.Abs(c.X) + Math.Abs(c.Y) + Math.Abs(c.Turn) <= .800001, "Aggregate throttle respected");
        if (c.Ready) { arrived = true; break; }
        double ax = (c.X * Math.Cos(rot) - c.Y * Math.Sin(rot)) * acceleration;
        double ay = (c.X * Math.Sin(rot) + c.Y * Math.Cos(rot)) * acceleration;
        x += vx * dt + .5 * ax * dt * dt; y += vy * dt + .5 * ay * dt * dt;
        vx += ax * dt; vy += ay * dt;
        spin += c.Turn * dt; rot += spin * dt + .5 * c.Turn * dt * dt; spin += c.Turn * dt;
        Check(Math.Sqrt(x*x + y*y) > 200, "No simulated hull penetration");
    }
    Check(arrived, "Capture range, motion and heading converge");
}
Check(!DockingRules.TryGuide(0,300,0,100,0,0,200,1,1,1,out _), "Unrecoverable high-speed intercept rejected");
Check(!DockingRules.TryGuide(0,300,0,0,0,0,200,1,1,2,out _), "Unsafe simulation step rejected");
Check(!DockingRules.TryGuide(0,300,0,0,0,0,200,1,0,1,out _), "Zero throttle rejected");
Check(!DockingRules.TryGuide(0,199,0,0,0,0,200,1,1,1,out _), "Inside-hull start rejected");
Check(!DockingRules.TryGuide(0,20000,0,0,0,0,200,1,1,1,out _), "Long-range request rejected");
Check(DockingRules.TryGuide(0,210,1,0,0,0,200,1,1,1,out var drifting) && !drifting.Ready, "Sideways motion blocks clamps");
Check(DockingRules.TryGuide(0,210,0,0,.1,0,200,1,1,1,out var misaligned) && !misaligned.Ready, "Wrong heading blocks clamps");
Check(!DockingRules.TryGuide(0,double.NaN,0,0,0,0,200,1,1,1,out _), "Nonfinite motion rejected");

(NavigationService Service, CondOwner Console, Ship Target) Setup(double metres = 1000)
{
    NativeContactReader.State = ContactState.Ready; NativeContactReader.ById.Clear(); NativeHazards.Rocks.Clear();
    AutoNavCore.ResetStatics(); AutoNavCore.Busy = false; AutoNavCore.FuelAvailable = true;
    HarmonyLib.AccessTools.MethodsAvailable = true;
    StarSystem.fEpoch = 0; CrewSim.system = new(); CrewSim.AttachCalls = 0; CrewSim.DuringAttach = null; CrewSim.Paused = false; CrewSim.objInstance.FinishedLoading = true;
    var own = new Ship { strRegID = "own" }; own.Ports.Add("own"); own.Comms.Clearance = new();
    var target = new Ship { strRegID = "target" }; target.objSS.vPosy = metres * AutoNavCore.M_TO_AU;
    target.Ports.Add("assigned"); target.Pairs.Add(("assigned", "own"));
    CrewSim.system.Ships.Add("own",own); CrewSim.system.Ships.Add("target",target);
    CrewSim.coPlayer = new() { strID = "player", ship = own };
    var co = new CondOwner { strID = "console", ship = own };
    co.Items.Add(new() { strID = "module", Kind = NavigationService.ModuleId, ship = own }); own.Items.Add(co);
    GUIOrbitDraw.CrossHairTarget = new() { Ship = target }; GUIDockSys.instance = new() { COSelf = co };
    return (new NavigationService(), co, target);
}
FlightSnapshot Read(CondOwner co)
{
    Check(new ObjectStateStore(co.mapGUIPropMaps, FlightSnapshot.StoreName, co.strID, FlightSnapshot.Schema).Read(out var fields) == SavedStateStatus.Ready, "Framework record readable");
    Check(FlightSnapshot.TryDecode(fields, out var record), "Saved docking contract decodes"); return record;
}
void Tick(NavigationService service, double dt = .1)
{ service.TickDocking(CrewSim.system, dt, false); StarSystem.fEpoch += dt; }
void Settle(NavigationService service) { for (int i = 0; i < 55; i++) Tick(service); }
var f = Setup();
Check(DockingAdapter.ReadAvailablePorts(f.Console.ship, f.Target, out var shownOwn, out var shownTarget) == null &&
    shownOwn == "own" && shownTarget == "assigned" && f.Target.FitChecks == 0, "Presentation reads clearance without native hull-grid work");
f.Target.Pairs.Clear(); f.Service.Dock(f.Console);
Check(!AutoNavCore.Engaged && f.Target.FitChecks > 0, "Dock command freshly rejects hull fit after a valid clearance display");
f = Setup(); f.Console.ship.Comms.Clearance = null; f.Service.Dock(f.Console);
Check(!AutoNavCore.Engaged && CrewSim.AttachCalls == 0, "No clearance cannot engage");
f = Setup(); f.Console.ship.Comms.Clearance!.ClearanceType = "PUSHBACK & TAXI"; f.Service.Dock(f.Console);
Check(!AutoNavCore.Engaged, "Undocking clearance cannot authorize docking");
f = Setup(); f.Target.Pairs.Clear(); f.Service.Dock(f.Console);
Check(!AutoNavCore.Engaged, "Native hull-fit rejection blocks engagement");
f = Setup(); f.Service.Dock(f.Console); Check(AutoNavCore.Engaged && Read(f.Console).OwnPort == "own", "Engages with native assigned pair");
GUIOrbitDraw.CrossHairTarget = null; Tick(f.Service); Check(AutoNavCore.Engaged, "Crosshair changes cannot retarget docking");
f.Console.ship.Comms.Clearance!.DockID = "changed"; Tick(f.Service); Check(!AutoNavCore.Engaged, "Port reassignment cancels before further thrust");
f = Setup(); f.Service.Dock(f.Console); f.Target.Ports.Clear(); Tick(f.Service); Check(!AutoNavCore.Engaged, "Occupied or removed port cancels");
f = Setup(); f.Service.Dock(f.Console); f.Service.HardwareFailure = "power"; Tick(f.Service); Check(!AutoNavCore.Engaged, "Hardware failure cancels");
f = Setup(); f.Service.Dock(f.Console); Tick(f.Service, 2); Check(AutoNavCore.Engaged && f.Console.ship.LastX == 0 && f.Console.ship.LastY == 0 && f.Console.ship.LastTurn == 0, "Fast-forward beyond docking step limit holds thrust for that step instead of cancelling");
Tick(f.Service); Check(AutoNavCore.Engaged, "Guidance continues on the next ordinary step");
f = Setup(); f.Service.Dock(f.Console); AutoNavCore.Busy = true; Tick(f.Service); Check(!AutoNavCore.Engaged, "Other automation wins");
f = Setup(); f.Service.Dock(f.Console); AutoNavCore.ElapsedSeconds = DockingRules.MaximumSeconds; Tick(f.Service); Check(!AutoNavCore.Engaged, "Cumulative timeout cancels");
f = Setup(); f.Service.Dock(f.Console); CrewSim.Paused = true; Tick(f.Service); Check(AutoNavCore.ElapsedSeconds == 0, "Paused physics does not consume timeout"); CrewSim.Paused = false;
f.Service.WorldChanging(); f.Service.WorldLoaded(); f.Service.UpdatePersistence();
Check(!AutoNavCore.Engaged && Read(f.Console).Mode == SavedFlightMode.DockingSuspended, "Load always suspends docking for explicit resume");
f.Service.ResumeSaved(f.Console); Check(AutoNavCore.Engaged, "Resume revalidates and keeps original docking intent");
f.Service.WorldChanging(); f.Service.WorldLoaded(); f.Service.UpdatePersistence(); f.Target.Ports.Clear(); f.Service.ResumeSaved(f.Console);
Check(!AutoNavCore.Engaged && Read(f.Console).TargetPort == "assigned", "Unavailable saved port stays suspended without replacement");
f = Setup(210); f.Service.Dock(f.Console); Settle(f.Service); f.Service.TickDocking(CrewSim.system,.1,true);
Check(CrewSim.AttachCalls == 1 && GUIDockSys.instance!.ClampCalls == 1 && AutoNavCore.Engaged, "The clamp is the docking console's own button, pressed once when its geometry admits");
f.Service.TickDocking(CrewSim.system,.1,false);
Check(!AutoNavCore.Engaged && Read(f.Console).Mode == SavedFlightMode.Docked, "The flight ends docked once the game's own sequence has attached");
f.Service.TickDocking(CrewSim.system,.1,true); Check(CrewSim.AttachCalls == 1 && GUIDockSys.instance!.ClampCalls == 1, "No repeated attachment");
f = Setup(210); f.Service.Dock(f.Console); Settle(f.Service); GUIDockSys.instance!.CanDockResult = false; f.Service.TickDocking(CrewSim.system,.1,true);
Check(CrewSim.AttachCalls == 0 && GUIDockSys.instance!.ClampCalls == 0 && AutoNavCore.Engaged, "Auto Nav holds until the console's own alignment check admits the clamp");
f = Setup(210); f.Service.Dock(f.Console); Settle(f.Service); GUIDockSys.instance!.ClampAttaches = false; f.Service.TickDocking(CrewSim.system,.1,true);
Check(GUIDockSys.instance!.ClampCalls == 1 && AutoNavCore.Engaged, "A started native clamp sequence is waited for");
AutoNavCore.ElapsedSeconds += 6; f.Service.TickDocking(CrewSim.system,.1,true);
Check(!AutoNavCore.Engaged && Read(f.Console).Mode == SavedFlightMode.Stopped, "A clamp sequence that never connects ends the attempt as failed, without a retry");
f = Setup(210); f.Service.Dock(f.Console); Settle(f.Service); GUIDockSys.instance = null; f.Service.TickDocking(CrewSim.system,.1,true);
Check(CrewSim.AttachCalls == 0 && AutoNavCore.Engaged, "Closed console holds instead of bypassing native UI legal check");
GUIDockSys.instance = new() { COSelf = f.Console }; f.Target.Pairs.Clear(); f.Service.TickDocking(CrewSim.system,.1,true);
Check(CrewSim.AttachCalls == 0, "Final clamp rechecks fit even between periodic checks");
f = Setup(210); Check(DockingAdapter.Clamp(f.Console, f.Target, "own", "wrong",1,.1)==DockingAdapter.ClampResult.Refused, "Attachment boundary rejects wrong pair independently");
f.Target.objSS.vVelX = AutoNavCore.M_TO_AU;
Check(DockingAdapter.Clamp(f.Console,f.Target,"own","assigned",1,.1)==DockingAdapter.ClampResult.Refused, "Attachment boundary independently rejects motion");

f = Setup(); f.Console.ship.Maneuver(.2f,.1f,0,0,.1f); f.Console.ship.DeltaVRemainingRCS = 0; f.Service.Dock(f.Console);
Check(!AutoNavCore.Engaged && Read(f.Console).Mode == SavedFlightMode.DockingSuspended, "Actual RCS budget gates docking even inside ordinary Fly arrival radius");
Check(f.Console.ship.LastX == 0 && f.Console.ship.LastY == 0, "Fuel rejection after acquiring docking intent clears residual thrust before dropping ownership");
f = Setup(); f.Service.Dock(f.Console);
var stored = Read(f.Console); var encoded = stored.Encode(); encoded.Remove("targetPort");
Check(!FlightSnapshot.TryDecode(encoded, out _), "Docking record missing a port is protected");
encoded = stored.Encode(); encoded["preferTorch"] = "1";
Check(!FlightSnapshot.TryDecode(encoded, out _), "Docking record cannot acquire torch authority");
foreach (var mode in new[] { SavedFlightMode.Docking, SavedFlightMode.DockingSuspended, SavedFlightMode.Docked })
{
    stored.Mode = mode;
    Check(FlightSnapshot.TryDecode(stored.Encode(), out var copy) && copy.TargetId == "target" && copy.OwnPort == "own" && copy.TargetPort == "assigned", "Docking mode retains original port binding");
    Check(!Enum.TryParse<LegacyMode>(stored.Encode()["mode"], out _), "Previous plugin refuses new docking lifecycle modes on downgrade");
}
f = Setup(); f.Service.Dock(f.Console); f.Service.Stop(f.Console,"pilot"); f.Service.WorldChanging(); f.Service.WorldLoaded(); f.Service.UpdatePersistence();
Check(!AutoNavCore.Engaged && Read(f.Console).Mode == SavedFlightMode.Stopped, "Stopped docking never resumes on reload");
f = Setup(); f.Service.Dock(f.Console); f.Console.ship.Towed = true; Tick(f.Service);
Check(!AutoNavCore.Engaged, "A secured towing brace cancels the maneuver");
f = Setup(); f.Target.Atmosphere = true; f.Service.Dock(f.Console); Check(!AutoNavCore.Engaged, "Atmospheric target refused");
f = Setup(); f.Target.Pairs.Insert(0,("wrong", "own")); f.Service.Dock(f.Console);
Check(AutoNavCore.Engaged && Read(f.Console).TargetPort == "assigned", "First unassigned port cannot replace Comms assignment");
f = Setup(210); f.Target.Station = true; f.Service.Dock(f.Console); Settle(f.Service); f.Service.TickDocking(CrewSim.system,.1,true);
Check(CrewSim.AttachCalls == 1 && GUIDockSys.instance!.BrokenLocks == 1, "Station docking preserves native AI target-lock cleanup");
f = Setup(210); f.Target.Station = true; HarmonyLib.AccessTools.MethodsAvailable = false; f.Service.Dock(f.Console); Settle(f.Service); f.Service.TickDocking(CrewSim.system,.1,true);
Check(CrewSim.AttachCalls == 0 && !AutoNavCore.Engaged, "Changed native station integration refuses attachment");
f = Setup(); f.Console.ship.objSS.vVelX = f.Target.objSS.vVelX = 25000 * AutoNavCore.M_TO_AU;
Check(DockingAdapter.Read(f.Console.ship,f.Target,1,.1,out var orbit) && orbit.SpeedMS == 0, "Shared orbital motion is not relative motion");
f.Service.Dock(f.Console); var moment = AutoNavCore.ElapsedSeconds;
f.Service.TickDocking(new StarSystem(),.1,false);
Check(AutoNavCore.ElapsedSeconds == moment, "Other system updates cannot steer this world");
f = Setup(210); f.Service.Dock(f.Console); f.Console.SoftwareDamaged = true; f.Service.TickDocking(CrewSim.system,.1,true);
Check(!AutoNavCore.Engaged && CrewSim.AttachCalls == 0, "Native damaged-software restriction is retained");
f = Setup(210); f.Service.Dock(f.Console);
Settle(f.Service); CrewSim.DuringAttach = () => f.Service.TickDocking(CrewSim.system,.1,true);
f.Service.TickDocking(CrewSim.system,.1,true);
Check(CrewSim.AttachCalls == 1, "Reentrant native callback cannot attach twice");

f = Setup(); NativeContactReader.State = ContactState.Weak; f.Service.Dock(f.Console);
Check(!AutoNavCore.Engaged && CrewSim.AttachCalls == 0 && f.Service.Diagnostic.Contains("Sensors.Weak"), "Docking requires a current contact before port selection");
f = Setup(); f.Service.Dock(f.Console); Tick(f.Service);
double elapsed = AutoNavCore.ElapsedSeconds;
NativeContactReader.State = ContactState.Occluded; Tick(f.Service);
var lost = Read(f.Console);
Check(!AutoNavCore.Engaged && lost.Mode == SavedFlightMode.DockingSuspended && lost.OwnPort == "own" &&
    lost.TargetPort == "assigned" && lost.ElapsedSeconds == elapsed, "Track loss preserves docking pair and budget for manual resume");
Check(f.Console.ship.LastX == 0 && f.Console.ship.LastY == 0 && f.Console.ship.LastTurn == 0, "Suspension calls the docking actuator stop boundary");
NativeContactReader.State = ContactState.Ready; Tick(f.Service);
Check(!AutoNavCore.Engaged, "Docking does not automatically restart on reacquisition");
f.Service.ResumeSaved(f.Console); Check(AutoNavCore.Engaged, "Explicit docking resume checks the recovered contact");
f = Setup(210); f.Service.Dock(f.Console); NativeContactReader.State = ContactState.Weak;
f.Service.TickDocking(CrewSim.system,.1,true);
Check(CrewSim.AttachCalls == 0 && !AutoNavCore.Engaged && Read(f.Console).Mode == SavedFlightMode.DockingSuspended,
    "Contact lost between physics and clamping prevents attachment");
f.Service.ResumeSaved(f.Console); Check(!AutoNavCore.Engaged, "Manual docking resume cannot bypass weak contact");

f=Setup(210); f.Service.Dock(f.Console); f.Service.TickDocking(CrewSim.system,.1,true);
Check(CrewSim.AttachCalls==0 && AutoNavCore.Engaged,"New docking mission holds until motion is observed");
Settle(f.Service); f.Target.objSS.vVelX += .1*AutoNavCore.M_TO_AU; StarSystem.fEpoch += .1;
f.Service.TickDocking(CrewSim.system,.1,true);
Check(CrewSim.AttachCalls==0 && AutoNavCore.Engaged,"New burn before clamp blocks capture even below native speed threshold");
f=Setup(); f.Service.Dock(f.Console); Tick(f.Service);
for (int i=0;i<100;i++) { f.Target.objSS.vVelY += .1*AutoNavCore.M_TO_AU; Tick(f.Service); }
Check(AutoNavCore.Engaged && CrewSim.AttachCalls==0 && f.Service.Diagnostic.Contains("Docking.holding"),"Unattainable terminal target stays in hold/brake instead of repeated dives");
Check(DockingRules.TryHold(new(0,250),new(0,2),new(0,.5),0,0,200,1,.5,.1,out var hold) && !hold.Ready &&
    Math.Abs(hold.X)+Math.Abs(hold.Y)+Math.Abs(hold.Turn)<=.500001,"Hold remains bounded and cannot authorize clamps");
f=Setup(210); f.Service.Dock(f.Console); Settle(f.Service); f.Target.objSS.fW=.01f;
f.Service.TickDocking(CrewSim.system,.1,true);
Check(CrewSim.AttachCalls==1 && GUIDockSys.instance!.ClampCalls==1,"A tumbling target does not block the clamp: the game's own docking has no spin rule");
// Combined approach-to-terminal mission: real service and persistence, doubled engine boundary.
foreach (string module in new[] { NavigationService.ModuleId, NavigationService.PursuitId })
{
    f = Setup(50000); f.Console.Items[0].Kind = module; Plugin.PreferTorch.Value = true;
    f.Service.ApproachDock(f.Console);
    var combined = Read(f.Console);
    Check(AutoNavCore.Engaged && combined.Mode == SavedFlightMode.ApproachDock, "Either healthy module can start a combined mission");
    Check(combined.ModuleId == "module" && combined.OwnPort == "own" && combined.TargetPort == "assigned" &&
        Math.Abs(combined.ArrivalKM - 1.3) < 1e-6 && combined.ArrivalMS == 0 && combined.PreferTorch,
        "Staging is 1 km beyond protective hull clearance, with matched-motion arrival and captured hardware/ports");
    var bad = combined.Encode(); bad.Remove("ownPort");
    Check(!FlightSnapshot.TryDecode(bad, out _), "Combined record without a bound port is rejected");
    bad = combined.Encode(); bad["arrivalMS"] = "1";
    Check(!FlightSnapshot.TryDecode(bad, out _), "Combined approach cannot save an unmatched-motion arrival");
    GUIOrbitDraw.CrossHairTarget = null;
    f.Service.WorldChanging(); f.Service.WorldLoaded(); f.Service.UpdatePersistence();
    Check(!AutoNavCore.Engaged && Read(f.Console).Mode == SavedFlightMode.ApproachDockSuspended,
        "Combined mission suspends even with ordinary auto-resume enabled");
    f.Service.ResumeSaved(f.Console);
    Check(AutoNavCore.Engaged && Read(f.Console).TargetId == "target", "Resume retains destination without a crosshair");
    f.Target.objSS.vPosy = 1300 * AutoNavCore.M_TO_AU;
    Check(f.Service.FinishApproach() && !AutoNavCore.Engaged, "Arrival queues handoff without steering inside a ship update");
    Check(Read(f.Console).Mode == SavedFlightMode.ApproachDockSuspended, "Saving between phases cannot restore thrust");
    f.Service.TickDocking(CrewSim.system, .1, true);
    Check(AutoNavCore.Engaged && Read(f.Console).Mode == SavedFlightMode.Docking && !Read(f.Console).PreferTorch &&
        Read(f.Console).CruiseMS == DockingRules.CruiseMS && CrewSim.AttachCalls == 0,
        "Post-physics handoff enters RCS-only terminal guidance without attaching");
    Check(Read(f.Console).ModuleId == "module" && Read(f.Console).OwnPort == "own", "Handoff keeps exact original module and ports");
}
Plugin.PreferTorch.Value = false;
f = Setup(50000); f.Console.ship.Comms.Clearance = null; f.Service.ApproachDock(f.Console);
Check(!AutoNavCore.Engaged && CrewSim.AttachCalls == 0, "Combined mission requires clearance before approach");
f = Setup(50000); f.Service.ApproachDock(f.Console); f.Console.ship.Comms.Clearance!.DockID = "changed"; Tick(f.Service);
Check(!AutoNavCore.Engaged && Read(f.Console).Mode == SavedFlightMode.ApproachDockSuspended && Read(f.Console).TargetPort == "assigned",
    "Revoked or reassigned clearance suspends instead of choosing another port");
f.Console.ship.Comms.Clearance.DockID = "assigned"; Tick(f.Service);
Check(!AutoNavCore.Engaged, "Restored clearance never restarts a suspended approach");
f.Service.ResumeSaved(f.Console); Check(AutoNavCore.Engaged, "Explicit Resume can revalidate restored clearance");
f.Target.Ports.Clear(); Tick(f.Service); Check(!AutoNavCore.Engaged, "Occupied port suspends during the approach phase");
f = Setup(50000); f.Service.ApproachDock(f.Console); f.Service.HardwareFailure = "power"; Tick(f.Service);
Check(!AutoNavCore.Engaged && Read(f.Console).IsCombinedApproach, "Power loss clears automation and retains combined intent");
f = Setup(50000); f.Service.ApproachDock(f.Console); NativeContactReader.State = ContactState.Weak; Tick(f.Service);
Check(!AutoNavCore.Engaged && Read(f.Console).Mode == SavedFlightMode.ApproachDockSuspended, "Contact loss suspends combined intent");
NativeContactReader.State = ContactState.Ready; Tick(f.Service); Check(!AutoNavCore.Engaged, "Reacquisition cannot authorize handoff");
f = Setup(50000); f.Service.ApproachDock(f.Console); f.Console.Items.Add(new() { strID="replacement", Kind=NavigationService.PursuitId });
f.Console.Items.RemoveAt(0); Tick(f.Service);
Check(!AutoNavCore.Engaged && Read(f.Console).ModuleId == "module", "A second working module cannot substitute for a removed bound module");
f = Setup(50000); f.Service.ApproachDock(f.Console); f.Target.objSS.vPosy=1300*AutoNavCore.M_TO_AU; f.Service.FinishApproach();
GUIDockSys.instance=null; f.Service.TickDocking(CrewSim.system,.1,true);
Check(!AutoNavCore.Engaged && Read(f.Console).IsCombinedApproach && f.Service.Diagnostic.Contains("Docking.open_console"),
    "Unavailable native attachment interface prevents handoff and exposes its requirement");
GUIDockSys.instance=new() { COSelf=f.Console }; f.Service.TickDocking(CrewSim.system,.1,true);
Check(!AutoNavCore.Engaged, "Opening native docking after a failed handoff does not silently retry");
f.Service.ResumeSaved(f.Console); Check(AutoNavCore.Engaged && Read(f.Console).IsDocking, "Explicit Resume inside staging uses terminal admission");
f=Setup(1000); f.Service.ApproachDock(f.Console);
Check(AutoNavCore.Engaged && Read(f.Console).Mode==SavedFlightMode.Docking, "Already inside staging starts checked terminal guidance");
f=Setup(1000); f.Console.ship.objSS.vVelY=100*AutoNavCore.M_TO_AU; f.Service.ApproachDock(f.Console);
Check(!AutoNavCore.Engaged && Read(f.Console).Mode==SavedFlightMode.ApproachDockSuspended, "Insufficient terminal braking authority refuses inside-staging admission");
f=Setup(50000); AutoNavCore.FuelAvailable=false; f.Service.ApproachDock(f.Console);
Check(!AutoNavCore.Engaged && Read(f.Console).IsCombinedApproach, "Failed approach fuel admission leaves a resumable mission without thrust");
f=Setup(50000); f.Service.ApproachDock(f.Console); f.Target.objSS.vPosy=1300*AutoNavCore.M_TO_AU; f.Service.FinishApproach();
f.Service.Stop(f.Console,"manual"); f.Service.TickDocking(CrewSim.system,.1,true);
Check(!AutoNavCore.Engaged && Read(f.Console).Mode==SavedFlightMode.Stopped, "Manual takeover cancels even a queued handoff");
f=Setup(50000); f.Service.ApproachDock(f.Console); f.Service.FinishApproach();
f.Service.WorldChanging(); f.Service.WorldLoaded(); f.Service.UpdatePersistence(); f.Service.TickDocking(CrewSim.system,.1,true);
Check(!AutoNavCore.Engaged && Read(f.Console).Mode==SavedFlightMode.ApproachDockSuspended, "Reload drops queued handoff permission");
f=Setup(1000); f.Service.ApproachDock(f.Console); Tick(f.Service);
for(int i=0;i<100;i++){f.Target.objSS.vVelY+=.1*AutoNavCore.M_TO_AU; Tick(f.Service);}
Check(AutoNavCore.Engaged && CrewSim.AttachCalls==0 && f.Service.Diagnostic.Contains("Docking.holding"),
    "Evasive target after handoff remains in bounded terminal hold");

// Execute the real industrial lease/controller against the same native boundary doubles.
f = Setup(210);
Check(IndustrialNavigation.Request("g4-one", f.Console, "module", "target", 0, () => null, out _), "Industrial pose request binds exact hardware");
Check(!IndustrialNavigation.Request("g4-two", f.Console, "module", "target", 0, () => null, out _), "A second G4 cannot steal propulsion");
f.Service.Dock(f.Console); Check(!AutoNavCore.Engaged, "Ordinary docking cannot compete with an industrial lease");
GUIDockSys.instance = null; GUIOrbitDraw.CrossHairTarget = null;
for (int i = 0; i < 65; i++)
{
    f.Service.TickIndustrial(.1, false); StarSystem.fEpoch += .1; f.Service.TickIndustrial(.1, true);
}
Check(IndustrialNavigation.Observe("g4-one", out bool ready, out _) && ready, "Closed panels and changed crosshair do not erase stable exact-target readiness");
Check(CrewSim.AttachCalls == 0, "Flight readiness itself does not mutate native attachments");
IndustrialNavigation.Release("g4-two");
Check(IndustrialNavigation.Observe("g4-one", out _, out _), "Foreign permission cannot release this lease");
var industrialSave = new JsonShipSitu { fA = 2 };
NavigationService.PrepareSavedPhysics(f.Console.ship.objSS, industrialSave);
Check(industrialSave.fA == 0, "Industrial actuator commands are removed only from saved physics copies");
f.Service.Stop(f.Console, "manual");
Check(!IndustrialNavigation.Observe("g4-one", out _, out _) && f.Console.ship.LastX == 0 && f.Console.ship.LastY == 0, "Navigation Stop releases industrial authority");
f = Setup();
Check(IndustrialNavigation.Request("lease", f.Console, "module", "target", 0, () => null, out _), "Fresh industrial request accepted");
NativeContactReader.State = ContactState.Unavailable; f.Service.TickIndustrial(.1, false);
NativeContactReader.State = ContactState.Ready;
Check(!IndustrialNavigation.Observe("lease", out _, out _), "Tracking recovery cannot silently resume industrial flight");
f = Setup(); IndustrialNavigation.Request("lease", f.Console, "module", "target", 0, () => null, out _);
f.Console.Items[0].strID = "replacement"; f.Service.TickIndustrial(.1, false);
Check(!IndustrialNavigation.Observe("lease", out _, out _), "Replacement N1 cannot inherit captured module identity");
f = Setup(); IndustrialNavigation.Request("lease", f.Console, "module", "target", 0, () => null, out _);
f.Service.TickIndustrial(2, false);
Check(IndustrialNavigation.Observe("lease", out _, out _) && f.Console.ship.LastX == 0 && f.Console.ship.LastY == 0 && f.Console.ship.LastTurn == 0,
    "Excessive terminal time compression holds thrust for that step and keeps the industrial permission");
f.Service.TickIndustrial(.1, false);
Check(IndustrialNavigation.Observe("lease", out _, out _), "The move continues on the next ordinary step");
f = Setup(); IndustrialNavigation.Request("lease", f.Console, "module", "target", 0, () => null, out _);
f.Service.HardwareFailure = "power"; f.Service.TickIndustrial(.1, false);
Check(!IndustrialNavigation.Observe("lease", out _, out _), "Power/fuel hardware boundary loss ends industrial permission");
f = Setup(); bool moved = false; IndustrialNavigation.Request("lease", f.Console, "module", "target", 0, () => moved ? "binding changed" : null, out _);
moved = true; f.Service.TickIndustrial(.1, false);
Check(!IndustrialNavigation.Observe("lease", out _, out _), "Consumer binding change ends the working pose");
f = Setup(); IndustrialNavigation.Request("lease", f.Console, "module", "target", 0, () => null, out _);
f.Service.WorldChanging(); f.Service.WorldLoaded(); f.Service.UpdatePersistence();
Check(!IndustrialNavigation.Observe("lease", out _, out _) && !AutoNavCore.Engaged, "Reload never reconstructs an industrial permission");
f = Setup(210); IndustrialNavigation.Request("lease", f.Console, "module", "target", 0, () => null, out _);
f.Target.objSS.fW = .02f;
for (int i = 0; i < 65; i++) { f.Service.TickIndustrial(.1, false); StarSystem.fEpoch += .1; f.Service.TickIndustrial(.1, true); }
Check(IndustrialNavigation.Observe("lease", out ready, out _) && ready, "A tumbling target still reaches ready-to-clamp: derelicts spawn tumbling and the game's docking has no spin rule");
f = Setup(); f.Console.ship.DeltaVRemainingRCS = 0;
Check(!IndustrialNavigation.Request("lease", f.Console, "module", "target", 0, () => null, out _), "Industrial fuel admission cannot be disabled by caller permission");
f = Setup(); IndustrialNavigation.Request("lease", f.Console, "module", "target", 0, () => null, out _);
var oldCarrier = f.Console.ship; var foreignCarrier = new Ship { strRegID = "foreign", LastX = .7 };
oldCarrier.LastX = .5; f.Console.ship = foreignCarrier; f.Service.TickIndustrial(.1, false);
Check(oldCarrier.LastX == 0 && foreignCarrier.LastX == .7 && !IndustrialNavigation.Observe("lease", out _, out _),
    "Moving a bound console stops only the original carrier, never the new host ship");
foreach(var mode in new[]{SavedFlightMode.Active,SavedFlightMode.Rendezvous,SavedFlightMode.Following,SavedFlightMode.Docking,SavedFlightMode.ApproachDock})
{
    f=Setup(4000);f.Console.Items.Add(new(){strID="n2",Kind=NavigationService.PursuitId,ship=f.Console.ship});
    f.Service.BeginAvoidanceFlight(f.Console,mode);
    var blocker=new Ship {strRegID="visible-blocker"};blocker.objSS.vPosy=1000*AutoNavCore.M_TO_AU;
    CrewSim.system.Ships[blocker.strRegID]=blocker;
    Check(f.Service.GuardNavigation(.5)&&f.Service.avoidanceActive&&AutoNavCore.Engaged,
        "Shared detour precedes controller for "+mode+": "+f.Service.Diagnostic);
    Check(!f.Service.ObstacleBurnAllowed(f.Console.ship,5,1),"Detours inhibit torch for "+mode);
    NativeContactReader.State=ContactState.Weak;f.Service.GuardNavigation(.5);
    Check(!AutoNavCore.Engaged&&f.Console.ship.LastX==0&&f.Console.ship.LastY==0,"Contact loss releases thrust for "+mode);
}
// Weak contacts stay hazards with their native position error; only a lost track suspends.
{
    f=Setup(4000);f.Service.BeginAvoidanceFlight(f.Console,SavedFlightMode.Active);
    var faint=new Ship {strRegID="faint"};faint.objSS.vPosy=1000*AutoNavCore.M_TO_AU;CrewSim.system.Ships[faint.strRegID]=faint;
    NativeContactReader.ById["faint"]=ContactState.Weak;
    Check(f.Service.GuardNavigation(.5)&&f.Service.avoidanceActive&&AutoNavCore.Engaged,"A weak contact on the leg is still avoided: "+f.Service.Diagnostic);
    NativeContactReader.ById["faint"]=ContactState.Ready;f.Service.GuardNavigation(.5);
    NativeContactReader.ById["faint"]=ContactState.Weak;f.Service.GuardNavigation(.5);
    Check(AutoNavCore.Engaged,"A threat that fades to a weak contact does not suspend the flight");
    NativeContactReader.ById["faint"]=ContactState.Occluded;f.Service.GuardNavigation(.5);
    Check(!AutoNavCore.Engaged&&f.Console.ship.LastX==0&&f.Console.ship.LastY==0,"Losing a tracked threat entirely suspends and clears thrust");
}
// Selective sensor engagement: weak hazards on the route ask for sensors, at a limited rate, and a
// weak target is restored before guidance would suspend. Nothing switches when nothing can help.
{
    f=Setup(4000);f.Service.BeginAvoidanceFlight(f.Console,SavedFlightMode.Active);NativeSensorControl.Reset();
    var faint=new Ship {strRegID="faint"};faint.objSS.vPosy=1000*AutoNavCore.M_TO_AU;CrewSim.system.Ships[faint.strRegID]=faint;
    NativeContactReader.ById["faint"]=ContactState.Weak;
    f.Service.GuardNavigation(.5);
    Check(NativeSensorControl.Surveys.Count==1&&NativeSensorControl.Surveys[0].SequenceEqual(new[]{"faint"})&&NativeSensorControl.Notices.Count==0&&AutoNavCore.Engaged,
        "A weak hazard on the route is surveyed; nothing is switched when no sensor can help");
    NativeSensorControl.Offer=true;f.Service.GuardNavigation(.5);
    Check(NativeSensorControl.Surveys.Count==1,"Hazard surveys are rate limited in game time");
    StarSystem.fEpoch+=NavigationService.HazardSurveySeconds;f.Service.GuardNavigation(.5);
    Check(NativeSensorControl.Surveys.Count==2&&NativeContactReader.ById["faint"]==ContactState.Ready&&NativeSensorControl.Notices.Count==1&&AutoNavCore.Engaged,
        "A later survey switches a helpful sensor on, with a warning, and the hazard becomes a firm track");
    f=Setup(4000);f.Service.BeginAvoidanceFlight(f.Console,SavedFlightMode.Active);NativeSensorControl.Reset();NativeSensorControl.Offer=true;
    NativeContactReader.ById["target"]=ContactState.Weak;
    f.Service.GuardNavigation(.5);
    Check(AutoNavCore.Engaged&&NativeContactReader.ById["target"]==ContactState.Ready&&NativeSensorControl.Notices.Count==1,
        "A weak target is restored by the shared guard before any suspension");
    NativeSensorControl.Reset();NativeContactReader.ById["target"]=ContactState.Occluded;f.Service.GuardNavigation(.5);
    Check(!AutoNavCore.Engaged&&NativeSensorControl.Surveys.Count==0,"An obscured target is not a sensor problem and still suspends");
    NativeSensorControl.Reset();
}
{
    f=Setup(4000);f.Service.BeginAvoidanceFlight(f.Console,SavedFlightMode.Active);
    var far=new Ship {strRegID="far-faint"};far.objSS.vPosy=2000*AutoNavCore.M_TO_AU;far.objSS.vPosx=150000*AutoNavCore.M_TO_AU;
    CrewSim.system.Ships[far.strRegID]=far;NativeContactReader.ById["far-faint"]=ContactState.Weak;
    f.Service.GuardNavigation(.5);
    Check(!f.Service.avoidanceActive&&AutoNavCore.Engaged,"A weak contact too distant to place is not guessed into the route");
}
// The target's docked partner and a weak contact far along the coast line do not seize a clear approach;
// the same contact firmly tracked does. Takeovers are counted for the status report.
{
    f=Setup(40000);f.Service.BeginAvoidanceFlight(f.Console,SavedFlightMode.Active);
    var tender=new Ship {strRegID="tender",Attached=true};tender.objSS.vPosy=f.Target.objSS.vPosy-250*AutoNavCore.M_TO_AU;
    f.Target.Attachments["port"]=tender;CrewSim.system.Ships[tender.strRegID]=tender;
    var neighbour=new Ship {strRegID="neighbour"};neighbour.objSS.vPosx=6000*AutoNavCore.M_TO_AU;neighbour.objSS.vPosy=20000*AutoNavCore.M_TO_AU;
    CrewSim.system.Ships[neighbour.strRegID]=neighbour;NativeContactReader.ById["neighbour"]=ContactState.Weak;
    f.Console.ship.objSS.vVelX=30*AutoNavCore.M_TO_AU;f.Console.ship.objSS.vVelY=100*AutoNavCore.M_TO_AU;
    Check(!f.Service.GuardNavigation(.5)&&!f.Service.avoidanceActive&&AutoNavCore.Engaged&&f.Service.AvoidanceSteps==0,
        "A ship docked at the target and a weak contact 20 km out leave the approach to the arrival controller: "+f.Service.Diagnostic);
    NativeContactReader.ById["neighbour"]=ContactState.Ready;
    Check(f.Service.GuardNavigation(.5)&&f.Service.avoidanceActive&&f.Service.Diagnostic.Contains("Avoidance."),
        "The same contact, firmly tracked on the coast line, still takes the controls: "+f.Service.Diagnostic);
    Check(f.Service.AvoidanceSteps==1&&f.Service.AvoidanceSummary=="Avoidance.summary","Takeovers are counted for the status report");
}
// Heavy time compression holds the step instead of ending the flight; the next ordinary step resumes.
{
    f=Setup(4000);f.Service.BeginAvoidanceFlight(f.Console,SavedFlightMode.Active);f.Console.ship.LastX=.4;
    Check(f.Service.GuardNavigation(20)&&AutoNavCore.Engaged&&f.Console.ship.LastX==0&&f.Console.ship.LastY==0&&f.Service.Diagnostic.Contains("step_hold"),
        "An oversized flight step is held with thrust cleared, not disengaged: "+f.Service.Diagnostic);
    Check(!f.Service.GuardNavigation(.5)&&AutoNavCore.Engaged,"Guidance resumes on the next ordinary step");
}
{
    f=Setup(4000);f.Service.BeginAvoidanceFlight(f.Console,SavedFlightMode.Active);
    var rock=new ShipSitu{vPosy=1000*AutoNavCore.M_TO_AU};
    NativeHazards.Rocks.Add(new SensedObject("rock",rock,new ContactReading(ContactState.Ready)));
    Check(f.Service.GuardNavigation(.5)&&f.Service.avoidanceActive&&AutoNavCore.Engaged,"An asteroid-field rock on the leg forces a detour: "+f.Service.Diagnostic);
    Check(!f.Service.ObstacleBurnAllowed(f.Console.ship,5,1),"Rock detours inhibit torch");
    NativeHazards.Rocks.Clear();NativeHazards.Rocks.Add(new SensedObject("rock",rock,new ContactReading(ContactState.Occluded)));
    f.Service.GuardNavigation(.5);
    Check(!AutoNavCore.Engaged,"Losing a tracked rock suspends guidance");
}
// Coupled pre-physics guard, terminal service, native kinematics, post-physics attachment.
foreach(double dt in new[]{.02,.1,.5,1d})
foreach(double mirror in new[]{-1d,1d})
{
    f=Setup(2000);var own=f.Console.ship;
    f.Target.objSS.vPosx=mirror*600*AutoNavCore.M_TO_AU;
    own.objSS.fRot=(float)(mirror*.7);own.objSS.vVelX=mirror*2*AutoNavCore.M_TO_AU;
    f.Service.Dock(f.Console);int avoided=0;
    for(double t=0;t<1800&&AutoNavCore.Engaged;t+=dt)
    {
        if(f.Service.GuardNavigation(dt))avoided++;
        else f.Service.TickDocking(CrewSim.system,dt,false);
        own.objSS.Integrate(dt);f.Target.objSS.Integrate(dt);StarSystem.fEpoch+=dt;
        if(!f.Service.avoidanceActive)f.Service.TickDocking(CrewSim.system,dt,true);
    }
    Check(CrewSim.AttachCalls==1,$"Coupled docking converges dt={dt} mirror={mirror} avoided={avoided} reason={f.Service.Diagnostic} x={own.objSS.vPosx/AutoNavCore.M_TO_AU} y={own.objSS.vPosy/AutoNavCore.M_TO_AU}");
}
Console.WriteLine($"{count} docking/industrial assertions passed. Numerical and native-boundary doubles; no in-game testing.");
internal enum LegacyMode { Active, Suspended, Stopped, Arrived }
