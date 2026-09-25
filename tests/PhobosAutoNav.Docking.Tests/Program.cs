using System;
using PhobosAutoNav;
using PhobosAutoNav.Core;
using Phobos.Ostranauts.Framework.Persistence;

int count = 0;
void Check(bool result, string message) { count++; if (!result) throw new Exception(message); }

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
    NativeContactReader.State = ContactState.Ready;
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
var f = Setup(); f.Console.ship.Comms.Clearance = null; f.Service.Dock(f.Console);
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
f = Setup(); f.Service.Dock(f.Console); Tick(f.Service, 2); Check(!AutoNavCore.Engaged, "Fast-forward beyond docking step limit cancels");
f = Setup(); f.Service.Dock(f.Console); AutoNavCore.Busy = true; Tick(f.Service); Check(!AutoNavCore.Engaged, "Other automation wins");
f = Setup(); f.Service.Dock(f.Console); AutoNavCore.ElapsedSeconds = DockingRules.MaximumSeconds; Tick(f.Service); Check(!AutoNavCore.Engaged, "Cumulative timeout cancels");
f = Setup(); f.Service.Dock(f.Console); CrewSim.Paused = true; Tick(f.Service); Check(AutoNavCore.ElapsedSeconds == 0, "Paused physics does not consume timeout"); CrewSim.Paused = false;
f.Service.WorldChanging(); f.Service.WorldLoaded(); f.Service.UpdatePersistence();
Check(!AutoNavCore.Engaged && Read(f.Console).Mode == SavedFlightMode.DockingSuspended, "Load always suspends docking for explicit resume");
f.Service.ResumeSaved(f.Console); Check(AutoNavCore.Engaged, "Resume revalidates and keeps original docking intent");
f.Service.WorldChanging(); f.Service.WorldLoaded(); f.Service.UpdatePersistence(); f.Target.Ports.Clear(); f.Service.ResumeSaved(f.Console);
Check(!AutoNavCore.Engaged && Read(f.Console).TargetPort == "assigned", "Unavailable saved port stays suspended without replacement");
f = Setup(210); f.Service.Dock(f.Console); Settle(f.Service); f.Service.TickDocking(CrewSim.system,.1,true);
Check(CrewSim.AttachCalls == 1 && !AutoNavCore.Engaged && Read(f.Console).Mode == SavedFlightMode.Docked, "Clamps attach once through native boundary");
Check(CrewSim.LastOwnPort == "own" && CrewSim.LastTargetPort == "assigned", "Native attachment uses captured pair");
f.Service.TickDocking(CrewSim.system,.1,true); Check(CrewSim.AttachCalls == 1, "No repeated attachment");
f = Setup(210); f.Service.Dock(f.Console); Settle(f.Service); GUIDockSys.instance = null; f.Service.TickDocking(CrewSim.system,.1,true);
Check(CrewSim.AttachCalls == 0 && AutoNavCore.Engaged, "Closed console holds instead of bypassing native UI legal check");
GUIDockSys.instance = new() { COSelf = f.Console }; f.Target.Pairs.Clear(); f.Service.TickDocking(CrewSim.system,.1,true);
Check(CrewSim.AttachCalls == 0, "Final clamp rechecks fit even between periodic checks");
f = Setup(210); Check(!DockingAdapter.Attach(f.Console, f.Target, "own", "wrong",1,.1), "Attachment boundary rejects wrong pair independently");
f.Target.objSS.vVelX = AutoNavCore.M_TO_AU;
Check(!DockingAdapter.Attach(f.Console,f.Target,"own","assigned",1,.1), "Attachment boundary independently rejects motion");

f = Setup(); f.Console.ship.DeltaVRemainingRCS = 0; f.Service.Dock(f.Console);
Check(!AutoNavCore.Engaged && Read(f.Console).Mode == SavedFlightMode.DockingSuspended, "Actual RCS budget gates docking even inside ordinary Fly arrival radius");
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
Check(CrewSim.AttachCalls==0 && AutoNavCore.Engaged,"Rotating target holds final capture despite matched translation");
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
Check(!IndustrialNavigation.Observe("lease", out _, out _), "Excessive terminal time compression suspends");
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
Check(IndustrialNavigation.Observe("lease", out ready, out _) && !ready, "Rotating targets never receive ready-to-clamp status");
f = Setup(); f.Console.ship.DeltaVRemainingRCS = 0;
Check(!IndustrialNavigation.Request("lease", f.Console, "module", "target", 0, () => null, out _), "Industrial fuel admission cannot be disabled by caller permission");
f = Setup(); IndustrialNavigation.Request("lease", f.Console, "module", "target", 0, () => null, out _);
var oldCarrier = f.Console.ship; var foreignCarrier = new Ship { strRegID = "foreign", LastX = .7 };
oldCarrier.LastX = .5; f.Console.ship = foreignCarrier; f.Service.TickIndustrial(.1, false);
Check(oldCarrier.LastX == 0 && foreignCarrier.LastX == .7 && !IndustrialNavigation.Observe("lease", out _, out _),
    "Moving a bound console stops only the original carrier, never the new host ship");
Console.WriteLine($"{count} docking/industrial assertions passed. Numerical and native-boundary doubles; no in-game testing.");
internal enum LegacyMode { Active, Suspended, Stopped, Arrived }
