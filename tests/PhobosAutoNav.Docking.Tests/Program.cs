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
    AutoNavCore.ResetStatics(); AutoNavCore.Busy = false; AutoNavCore.FuelAvailable = true;
    HarmonyLib.AccessTools.MethodsAvailable = true;
    CrewSim.system = new(); CrewSim.AttachCalls = 0; CrewSim.DuringAttach = null; CrewSim.Paused = false; CrewSim.objInstance.FinishedLoading = true;
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
void Tick(NavigationService service, double dt = .1) => service.TickDocking(CrewSim.system, dt, false);
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
f = Setup(210); f.Service.Dock(f.Console); f.Service.TickDocking(CrewSim.system,.1,true);
Check(CrewSim.AttachCalls == 1 && !AutoNavCore.Engaged && Read(f.Console).Mode == SavedFlightMode.Docked, "Clamps attach once through native boundary");
Check(CrewSim.LastOwnPort == "own" && CrewSim.LastTargetPort == "assigned", "Native attachment uses captured pair");
f.Service.TickDocking(CrewSim.system,.1,true); Check(CrewSim.AttachCalls == 1, "No repeated attachment");
f = Setup(210); f.Service.Dock(f.Console); GUIDockSys.instance = null; f.Service.TickDocking(CrewSim.system,.1,true);
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
f = Setup(210); f.Target.Station = true; f.Service.Dock(f.Console); f.Service.TickDocking(CrewSim.system,.1,true);
Check(CrewSim.AttachCalls == 1 && GUIDockSys.instance!.BrokenLocks == 1, "Station docking preserves native AI target-lock cleanup");
f = Setup(210); f.Target.Station = true; HarmonyLib.AccessTools.MethodsAvailable = false; f.Service.Dock(f.Console); f.Service.TickDocking(CrewSim.system,.1,true);
Check(CrewSim.AttachCalls == 0 && !AutoNavCore.Engaged, "Changed native station integration refuses attachment");
f = Setup(); f.Console.ship.objSS.vVelX = f.Target.objSS.vVelX = 25000 * AutoNavCore.M_TO_AU;
Check(DockingAdapter.Read(f.Console.ship,f.Target,1,.1,out var orbit) && orbit.SpeedMS == 0, "Shared orbital motion is not relative motion");
f.Service.Dock(f.Console); var moment = AutoNavCore.ElapsedSeconds;
f.Service.TickDocking(new StarSystem(),.1,false);
Check(AutoNavCore.ElapsedSeconds == moment, "Other system updates cannot steer this world");
f = Setup(210); f.Service.Dock(f.Console); f.Console.SoftwareDamaged = true; f.Service.TickDocking(CrewSim.system,.1,true);
Check(!AutoNavCore.Engaged && CrewSim.AttachCalls == 0, "Native damaged-software restriction is retained");
f = Setup(210); f.Service.Dock(f.Console);
CrewSim.DuringAttach = () => f.Service.TickDocking(CrewSim.system,.1,true);
f.Service.TickDocking(CrewSim.system,.1,true);
Check(CrewSim.AttachCalls == 1, "Reentrant native callback cannot attach twice");

Console.WriteLine($"{count} docking assertions passed. Numerical and native-boundary doubles; no in-game testing.");
internal enum LegacyMode { Active, Suspended, Stopped, Arrived }
