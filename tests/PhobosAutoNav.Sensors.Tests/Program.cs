using System;
using System.Linq;
using PhobosAutoNav;
using PhobosAutoNav.Core;
using Phobos.Ostranauts.Framework.Persistence;

int checks = 0;
void Check(bool ok, string message) { checks++; if (!ok) throw new Exception(message); }
(NavigationService Service, CondOwner Console, Ship Own, Ship Target) Setup()
{
    AutoNavCore.ResetStatics(); AutoNavCore.SteeringCalls = AutoNavCore.ApproachReads = TargetRef.Resolves = 0;
    AutoNavCore.ElapsedSeconds = 0; AutoNavCore.Coasting = false;
    AutoNavCore.AdmissionSafe = true;
    CrewSim.system = new(); CrewSim.objInstance = new() { FinishedLoading = true }; CrewSim.Paused = false;
    Plugin.ResumeAfterLoad.Value = true;
    var own = new Ship { strRegID = "own", publicName = "Home" };
    var target = new Ship { strRegID = "target", publicName = "Destination" };
    CrewSim.system.Ships.Add("own", own); CrewSim.system.Ships.Add("target", target);
    CrewSim.coPlayer = new() { strID = "player", ship = own }; CrewSim.Selected = CrewSim.coPlayer;
    var co = new CondOwner { strID = "console", ship = own };
    co.mapGUIPropMaps["Panel A"] = new() { ["slidThrottle"] = "1" };
    co.Items.Add(new() { strID = "module", strCODef = NavigationService.ModuleId, ship = own }); own.Items.Add(co);
    GUIOrbitDraw.Open = true; GUIOrbitDraw.Console = co; GUIOrbitDraw.CrossHairTarget = new() { Ship = target };
    Plugin.Service = new(_ => { });
    return (Plugin.Service, co, own, target);
}
ObjectStateStore Store(CondOwner co) => new(co.mapGUIPropMaps, FlightSnapshot.StoreName, co.strID, FlightSnapshot.Schema);
FlightSnapshot Read(CondOwner co)
{
    Check(Store(co).Read(out var fields) == SavedStateStatus.Ready && FlightSnapshot.TryDecode(fields, out _), "Saved flight is readable");
    FlightSnapshot.TryDecode(fields, out var flight); return flight;
}
string Raw(CondOwner co) => string.Join("|", co.mapGUIPropMaps.OrderBy(p => p.Key).SelectMany(p =>
    p.Value.OrderBy(v => v.Key).Select(v => p.Key + ":" + v.Key + "=" + v.Value)));
void Signal(Ship ship, params double[] values) => ship.ElectronicSystems.aElectronicSystems = values.ToList();

// Exercise the actual selected-target reader through narrow native boundaries.
// The native strength formula itself is not copied or claimed to be game-tested.
var f = Setup();
Check(NativeContactReader.Read(f.Own, "target").Usable, "Native usable contact qualifies");
Signal(f.Own, .16, .16);
Check(NativeContactReader.Read(f.Own, "target").Usable, "Combined native contributions are used rather than choosing one sensor");
Signal(f.Own, .27);
Check(NativeContactReader.Read(f.Own, "target").State == ContactState.Weak, "Unskilled operator needs the native default threshold");
CrewSim.Selected!.Conditions.Add("SkillOpsSensors");
Check(NativeContactReader.Read(f.Own, "target").Usable, "A skilled operator aboard this ship receives the native threshold");
GUIOrbitDraw.Open = false;
Check(NativeContactReader.Read(f.Own, "target").Usable, "Reader works with navigation UI closed");
CrewSim.Selected = new() { ship = f.Target }; CrewSim.Selected.Conditions.Add("SkillOpsSensors");
Check(NativeContactReader.Read(f.Own, "target").State == ContactState.Weak, "Foreign operator cannot lend this ship a skill bonus");
CrewSim.Selected = null;
Check(NativeContactReader.Read(f.Own, "target").State == ContactState.Weak, "No selected operator uses the unskilled threshold");
Check(f.Own.ElectronicSystems.DetectionThreshold == .001f && f.Own.ElectronicSystems.On,
    "Reads neither reuse nor change the UI's stale threshold or emission setting");
Signal(f.Own, ContactRules.DefaultThreshold);
Check(NativeContactReader.Read(f.Own, "target").Usable, "Exact native floating-point threshold qualifies");
f.Own.RangeKM = 123; f.Own.fVisibilityRangeMod = .8f; f.Target.fVisibilityRangeMod = .4f;
NativeContactReader.Read(f.Own, "target");
Check(Math.Abs(f.Own.ElectronicSystems.LastRange - 123) < 1e-9 && f.Own.ElectronicSystems.LastVisibility == .4f,
    "Native signal receives kilometres and the weaker visibility modifier");
foreach (double value in new[] { double.NaN, double.PositiveInfinity, -1d })
{
    Signal(f.Own, value); Check(NativeContactReader.Read(f.Own, "target").State == ContactState.Fault, "Invalid signal cannot authorize flight");
}
Signal(f.Own, 1); f.Own.ElectronicSystems.Throw = true;
Check(NativeContactReader.Read(f.Own, "target").State == ContactState.Fault, "Native read exceptions become unavailable telemetry");
f.Own.ElectronicSystems.Throw = false; f.Own.RangeKM = double.NaN;
Check(NativeContactReader.Read(f.Own, "target").State == ContactState.Fault, "Invalid range cannot enter the sensor formula");
f = Setup(); f.Own.ElectronicSystems.On = false;
Check(NativeContactReader.Read(f.Own, "target").State == ContactState.NoSensors && !f.Own.ElectronicSystems.On, "Disabled sensors remain disabled");
f.Own.ElectronicSystems.On = true; Signal(f.Own);
Check(NativeContactReader.Read(f.Own, "target").State == ContactState.NoSensors, "Removed sensors cannot retain a track");
Signal(f.Own, 1); f.Own.bCheckSensors = true;
Check(NativeContactReader.Read(f.Own, "target").State == ContactState.Updating, "Pending native sensor refresh invalidates a cached track");
f.Own.bCheckSensors = false; f.Own.bCheckPower = true;
Check(NativeContactReader.Read(f.Own, "target").State == ContactState.Updating, "Pending power update cannot use cached powered sensors");
f.Own.bCheckPower = false;
CrewSim.system.aBOs["occluder"] = new() { Blocks = true };
Check(NativeContactReader.Read(f.Own, "target").State == ContactState.Occluded, "Native body obstruction rejects a strong signal");
CrewSim.system.aBOs["occluder"].nDrawFlagsBody = ContactRules.PlaceholderBodyDrawFlag;
Check(NativeContactReader.Read(f.Own, "target").Usable, "Nonphysical map marker does not occlude");
CrewSim.system.aBOs["occluder"].nDrawFlagsBody = 0; CrewSim.system.aBOs["occluder"].IsAsteroidField = true;
Check(NativeContactReader.Read(f.Own, "target").Usable, "An asteroid-field marker is not an opaque celestial body");
f.Target.Hidden = true;
Check(NativeContactReader.Read(f.Own, "target").State == ContactState.Unavailable, "Hidden station cannot become a live track from its map identity");
f.Target.Hidden = false; f.Target.HideFromSystem = true;
Check(!NativeContactReader.Read(f.Own, "target").Usable, "System-hidden target is unavailable");
f.Target.HideFromSystem = false; f.Target.bDestroyed = true;
Check(!NativeContactReader.Read(f.Own, "target").Usable, "Destroyed target is unavailable");
f.Target.bDestroyed = false;
Check(!NativeContactReader.Read(f.Own, "own").Usable && !NativeContactReader.Read(f.Own, "missing").Usable, "Self and absent targets are rejected");
CrewSim.system.Ships["own"] = new() { strRegID = "own" };
Check(!NativeContactReader.Read(f.Own, "target").Usable, "Old-world observer reference cannot authorize a new-world flight");
f = Setup(); CrewSim.objInstance.FinishedLoading = false;
Check(!NativeContactReader.Read(f.Own, "target").Usable, "Partially loaded world supplies no live observation");

// Real engagement, tick, instruments and persistence orchestration, not a copy.
f = Setup(); Signal(f.Own, .1); f.Service.Engage(f.Console);
Check(!AutoNavCore.Engaged && AutoNavCore.ApproachReads == 0 && TargetRef.Resolves == 0,
    "Weak contact refuses engagement before hidden motion or hull data is read");
Check(Store(f.Console).Read(out _) == SavedStateStatus.Missing, "Failed contact does not create a flight");
var before = Raw(f.Console); var instruments = f.Service.ReadInstruments(f.Console);
Check(instruments.Range == "Instruments.range_unknown" && instruments.RelativeSpeed == "Instruments.speed_unknown" &&
    !instruments.CanFly && !instruments.CanDock && instruments.Details.Contains("Sensors.Weak"), "Panel shows unknown readings and explains blocked tracking");
f.Service.Command(new[] { "phobosnav", "status" }, out var response);
Check(response.Contains("Sensors.Weak") && AutoNavCore.ApproachReads == 0 && Raw(f.Console) == before,
    "Console diagnostics follow the same read-only contact policy");
Signal(f.Own, 1); f.Service.Engage(f.Console);
Check(AutoNavCore.Engaged && Read(f.Console).Mode == SavedFlightMode.Active, "Usable sensing allows a persistent flight");
GUIOrbitDraw.Open = false; GUIOrbitDraw.CrossHairTarget = null;
f.Service.Tick(f.Own.objSS, 1, false);
Check(AutoNavCore.SteeringCalls == 1 && f.Own.Thrust == 1, "Closed-panel flight follows its saved target");
Signal(f.Own, .1); int approachReads = AutoNavCore.ApproachReads;
f.Service.Tick(f.Own.objSS, 1, false);
var suspended = Read(f.Console);
Check(!AutoNavCore.Engaged && f.Own.Thrust == 0 && f.Service.Torch.Releases > 0 && AutoNavCore.SteeringCalls == 1,
    "Contact loss clears owned thrust without steering or blind braking");
Check(suspended.Mode == SavedFlightMode.Suspended && suspended.TargetId == "target" && suspended.ElapsedSeconds == 1,
    "Suspension retains destination and cumulative flight budget");
Check(AutoNavCore.ApproachReads == approachReads, "Losing contact does not read precise target motion");
f.Service.ResumeSaved(f.Console);
Check(!AutoNavCore.Engaged && Read(f.Console).Mode == SavedFlightMode.Suspended, "Explicit resume still requires live contact");
Signal(f.Own, 1); f.Service.Tick(f.Own.objSS, 1, false); f.Service.UpdatePersistence();
Check(!AutoNavCore.Engaged, "Reacquisition never silently restarts a suspended flight");
f.Service.ResumeSaved(f.Console);
Check(AutoNavCore.Engaged && AutoNavCore.ElapsedSeconds == 1, "Explicit resume reuses the captured profile and elapsed budget");
f.Service.WorldChanging(); Signal(f.Own, .1); f.Service.WorldLoaded(); f.Service.UpdatePersistence();
Check(!AutoNavCore.Engaged && Read(f.Console).Mode == SavedFlightMode.Suspended, "Load revalidates sensing before the ordinary automatic-resume policy");
Signal(f.Own, 1); f.Service.WorldChanging(); f.Service.WorldLoaded(); f.Service.UpdatePersistence();
Check(!AutoNavCore.Engaged, "A later reload cannot turn contact suspension into automatic resumption");
f.Service.ResumeSaved(f.Console); f.Service.WorldChanging(); f.Service.WorldLoaded(); f.Service.UpdatePersistence();
Check(AutoNavCore.Engaged, "An active saved flight with fresh usable sensing retains ordinary load behavior");

foreach (var state in new[] { "off", "removed", "refresh", "power", "occluded", "operator", "fault" })
{
    f = Setup(); CrewSim.Selected!.Conditions.Add("SkillOpsSensors"); Signal(f.Own, .27); f.Service.Engage(f.Console);
    Check(AutoNavCore.Engaged, "Fixture begins with a valid skilled track");
    switch (state)
    {
        case "off": f.Own.ElectronicSystems.On = false; break;
        case "removed": Signal(f.Own); break;
        case "refresh": f.Own.bCheckSensors = true; break;
        case "power": f.Own.bCheckPower = true; break;
        case "occluded": CrewSim.system.aBOs["planet"] = new() { Blocks = true }; break;
        case "operator": CrewSim.Selected = null; break;
        case "fault": f.Own.ElectronicSystems.Throw = true; break;
    }
    f.Service.Tick(f.Own.objSS, 1, false);
    Check(!AutoNavCore.Engaged && Read(f.Console).Mode == SavedFlightMode.Suspended && AutoNavCore.SteeringCalls == 0,
        "Changed sensing suspends before guidance: " + state);
}
f = Setup(); f.Service.Engage(f.Console);
f.Service.Torch.ContactLoss = new(ContactState.Weak); // Asynchronous fusion-boundary observation.
f.Service.Tick(f.Own.objSS, 1, false);
Check(!AutoNavCore.Engaged && Read(f.Console).Mode == SavedFlightMode.Suspended && AutoNavCore.SteeringCalls == 0,
    "Guidance honors a fusion-boundary loss even if the next native reading has recovered");
f = Setup(); f.Service.Engage(f.Console);
f.Console.mapGUIPropMaps["PhobosState." + FlightSnapshot.StoreName]["schema"] = "999";
before = Raw(f.Console); Signal(f.Own, .1);
f.Service.Tick(f.Own.objSS, 1, false);
Check(!AutoNavCore.Engaged && Raw(f.Console) == before, "Contact-loss shutdown preserves unsupported saved state");
f = Setup(); f.Service.Engage(f.Console); f.Service.Tick(f.Own.objSS, 1, false);
f.Console.mapGUIPropMaps = null!; Signal(f.Own, .1); f.Service.Tick(f.Own.objSS, 1, false);
Check(!AutoNavCore.Engaged && f.Own.Thrust == 0, "An exceptional persistence failure cannot skip the contact-loss actuator stop");
f = Setup(); f.Service.Engage(f.Console);
var docking = Read(f.Console); docking.Mode = SavedFlightMode.DockingSuspended;
docking.OwnPort = "own-port"; docking.TargetPort = "target-port"; docking.PreferTorch = false;
docking.CruiseMS = DockingRules.CruiseMS;
Check(docking.Valid && Store(f.Console).TryWrite(docking.Encode()), "Valid docking status fixture written");
AutoNavCore.ResetStatics(); Signal(f.Own, .1);
f.Service.Command(new[] { "phobosnav", "status" }, out response);
Check(response.Contains("Sensors.Weak") && response.Contains("own-port"), "Docking diagnostics retain port binding and fresh contact state together");
f = Setup(); AutoNavCore.AdmissionSafe = false;
Check(!f.Service.ReadInstruments(f.Console).CanFly && f.Service.ReadInstruments(f.Console).CanDock,
    "Unsafe Fly admission disables Fly while retaining separate Dock preflight");
f.Service.Engage(f.Console);
Check(!AutoNavCore.Engaged && Store(f.Console).Read(out _) == SavedStateStatus.Missing,
    "Unsafe engagement never writes active intent or issues thrust");
AutoNavCore.AdmissionSafe = true; f.Service.Engage(f.Console);
AutoNavCore.AdmissionSafe = false; f.Service.Tick(f.Own.objSS, 1, false);
Check(AutoNavCore.Engaged && f.Own.Thrust == 1, "Admission is not an in-flight abort that could discard ongoing braking");
f.Service.WorldChanging(); f.Service.WorldLoaded(); f.Service.UpdatePersistence();
Check(!AutoNavCore.Engaged && Read(f.Console).Mode == SavedFlightMode.Suspended,
    "Automatic restoration suspends when current braking room is insufficient");
f.Service.ResumeSaved(f.Console);
Check(!AutoNavCore.Engaged, "Explicit Resume also rechecks braking room");
AutoNavCore.AdmissionSafe = true; f.Service.ResumeSaved(f.Console);
Check(AutoNavCore.Engaged, "Explicit Resume succeeds after restoring a safe approach");
f.Service.Stop(f.Console, "test");
Check(f.Service.Command(new[] { "phobosnav", "cruise", "200" }, out _) &&
    f.Service.Command(new[] { "phobosnav", "arrivalspeed", "0.2" }, out _) &&
    f.Service.Command(new[] { "phobosnav", "arrival", "0.5" }, out _), "F3 settings share checked service mutations");
f.Service.Engage(f.Console, .75f);
var capturedProfile = Read(f.Console);
Check(capturedProfile.CruiseMS == 200 && capturedProfile.ArrivalMS == .2 && capturedProfile.ArrivalKM == .75,
    "Flight captures console defaults with a one-flight distance override");
Check(!f.Service.Command(new[] { "phobosnav", "cruise", "300" }, out _) &&
    !f.Service.Command(new[] { "phobosnav", "defaults" }, out _), "F3 cannot replace an active profile");
f.Service.Stop(f.Console, "test");
Check(f.Service.ReadInstruments(f.Console).ArrivalKM == .5, "One-flight override does not replace console distance");
f.Console.mapGUIPropMaps["PhobosState." + FlightPreferences.StoreName]["schema"] = "999";
before = Raw(f.Console);
Check(!f.Service.Command(new[] { "phobosnav", "cruise", "200" }, out _) && Raw(f.Console) == before,
    "F3 preserves future preference records");
var flightBeforeReset = Read(f.Console).Encode();
Check(f.Service.Command(new[] { "phobosnav", "defaults" }, out _) &&
    !f.Console.mapGUIPropMaps.ContainsKey("PhobosState." + FlightPreferences.StoreName) &&
    Read(f.Console).Encode().SequenceEqual(flightBeforeReset), "Explicit preference reset leaves flight history intact");
f.Service.Engage(f.Console); Signal(f.Own, .1); f.Service.Tick(f.Own.objSS, 1, false);
Signal(f.Own, 1);
Check(!f.Service.Command(new[] { "phobosnav", "fly" }, out _) && Read(f.Console).Mode == SavedFlightMode.Suspended,
    "Direct F3 Fly cannot silently replace suspended intent; Resume or Stop is required");

f = Setup(); f.Service.StartPursuit(f.Console,true);
Check(!AutoNavCore.Engaged,"N1 alone cannot grant pursuit instrument commands");
f.Console.Items.Add(new CondOwner { strID = "pursuit-module", strName = NavigationService.PursuitId, ship = f.Own });
f.Service.StartPursuit(f.Console,true);
Check(AutoNavCore.Engaged && Read(f.Console).Mode == SavedFlightMode.Following && Read(f.Console).ArrivalMS == 0,
    "Follow captures zero arrival speed and its dedicated module");
f.Service.EngageWeapons(f.Console); Check(!f.Service.Fire.Permitted,"Follow target is not implicitly an offensive target");
f.Service.SelectFireTarget(f.Console); f.Service.EngageWeapons(f.Console); Check(f.Service.Fire.Permitted,"Separate target selection and Engage grant fire authority");
f.Service.CeaseFire(); Check(AutoNavCore.Engaged && AutoNavCore.Following && !f.Service.Fire.Permitted,"Cease Fire preserves Follow");
f.Service.EngageWeapons(f.Console); f.Service.WorldChanging(); f.Service.WorldLoaded(); f.Service.UpdatePersistence();
Check(!AutoNavCore.Engaged && !f.Service.Fire.Permitted && Read(f.Console).Mode == SavedFlightMode.FollowingSuspended,
    "Follow always suspends on load, even with ordinary auto-resume enabled");
f.Service.ResumeSaved(f.Console); Check(AutoNavCore.Engaged && Read(f.Console).Mode == SavedFlightMode.Following && !f.Service.Fire.Permitted,
    "Resume restores Follow without restoring automatic-fire authority");
Signal(f.Own,.1);f.Service.Tick(f.Own.objSS,1,false);
Check(!AutoNavCore.Engaged && Read(f.Console).Mode==SavedFlightMode.FollowingSuspended,"Follow contact loss preserves mission mode");
Console.WriteLine($"{checks} sensor-boundary, guidance, display and persistence assertions passed. No in-game tests performed.");
