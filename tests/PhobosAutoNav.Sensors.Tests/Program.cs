using System;
using System.Linq;
using PhobosAutoNav;
using PhobosAutoNav.Core;
using Phobos.Ostranauts.Framework.Persistence;

int checks = 0;
void Check(bool ok, string message) { checks++; if (!ok) throw new Exception(message); }
// Real-time cadences (record settlement, sensor housekeeping) are always due here: each read advances the clock.
double realSeconds = 0; NavigationService.RealClock = () => realSeconds += 10;
(NavigationService Service, CondOwner Console, Ship Own, Ship Target) Setup()
{
    AIShipManager.Current = null; BepInEx.Bootstrap.Chainloader.PluginInfos.Clear(); GUIOrbitDraw.Instance = new();
    AutoNavCore.ResetStatics(); AutoNavCore.SteeringCalls = AutoNavCore.ApproachReads = TargetRef.Resolves = 0;
    AutoNavCore.ElapsedSeconds = 0; AutoNavCore.Coasting = false;
    AutoNavCore.AdmissionSafe = true;
    DockingAdapter.Problem = null;
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
var recoveredDisplay = f.Service.ReadHub(f.Console, "navigation");
Check(!recoveredDisplay.Navigation.Warning && recoveredDisplay.Restriction == "Instruments.resume_hint", "Recovered sensors show current Resume guidance rather than the historical suspension reason");
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
    if (state == "refresh" || state == "power")
    {
        // A native sensor or power refresh, whoever caused it, is a short hold within the settle budget, not a loss.
        Check(AutoNavCore.Engaged && AutoNavCore.SteeringCalls == 0 && f.Service.StatusForTest == "SensorAssist.settling",
            "A native refresh holds guidance instead of suspending: " + state);
        for (int i = 0; i < NavigationService.SensorSettleChecks && AutoNavCore.Engaged; i++) f.Service.Tick(f.Own.objSS, 1, false);
        Check(!AutoNavCore.Engaged && Read(f.Console).Mode == SavedFlightMode.Suspended && AutoNavCore.SteeringCalls == 0,
            "A refresh that never completes still suspends the flight: " + state);
        continue;
    }
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
Signal(f.Own, 1); DockingAdapter.Problem = "Docking.clearance_lost";
var blockedDock = f.Service.ReadHub(f.Console, "navigation");
Check(blockedDock.Restriction == "Docking.clearance_lost" && blockedDock.Navigation.Warning,
    "Suspended docking shows the current admission blocker rather than a generic Resume hint");
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
// Auto Nav 0.35.0 (press twice to go ahead, owner rule): F3 saves a setting for the next flight while one runs,
// instead of refusing; the running flight keeps the profile it captured.
Check(f.Service.Command(new[] { "phobosnav", "cruise", "300" }, out _) && Read(f.Console).CruiseMS == 200,
    "F3 saves for the next flight and never replaces an active profile");
Check(f.Service.Command(new[] { "phobosnav", "cruise", "200" }, out _), "F3 can put the next flight's setting back while flying");
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
    "Direct F3 Fly cannot silently replace suspended intent; it offers to stop it first");
// Auto Nav 0.35.0 (press twice to go ahead): confirming stops the suspended flight exactly as Stop does.
f.Service.Command(new[] { "phobosnav", "fly", "confirm" }, out _);
Check(Read(f.Console).Mode != SavedFlightMode.Suspended, "F3 Fly with confirm stops the suspended flight, as Stop does");
f.Service.Stop(f.Console, "test");

f = Setup(); f.Service.StartPursuit(f.Console,true);
Check(!AutoNavCore.Engaged,"N1 alone cannot grant pursuit instrument commands");
f.Console.Items.Add(new CondOwner { strID = "pursuit-module", strName = NavigationService.PursuitId, ship = f.Own });
f.Service.StartPursuit(f.Console,true);
Check(AutoNavCore.Engaged && Read(f.Console).Mode == SavedFlightMode.Following && Read(f.Console).ArrivalMS == 0,
    "Follow captures zero arrival speed and its dedicated module");
f.Service.EngageWeapons(f.Console); Check(!f.Service.Fire.Permitted,"Follow target is not implicitly an offensive target");
f.Service.SelectFireTarget(f.Console); f.Service.EngageWeapons(f.Console); Check(!f.Service.Fire.Permitted,"N2 no longer grants fire control");
f.Console.Items.Add(new CondOwner { strID="fire-module", strName=NavigationService.FireControlId, ship=f.Own });
f.Service.SelectFireTarget(f.Console); f.Service.EngageWeapons(f.Console); Check(f.Service.Fire.Permitted,"Separate target selection and Engage grant fire authority");
f.Service.CeaseFire(); Check(AutoNavCore.Engaged && AutoNavCore.Following && !f.Service.Fire.Permitted,"Cease Fire preserves Follow");
f.Service.ToggleAutoAim(f.Console); f.Service.TickFire(.25,false);
Check(f.Service.ReadHub(f.Console).AutoAiming && AutoNavCore.WeaponHeading.HasValue,"N2 Follow accepts the explicit N3 attitude request");
f.Service.EngageWeapons(f.Console); f.Service.ExternalControl(f.Own,0,0,.5f);
Check(!AutoNavCore.Engaged && !f.Service.Fire.Permitted && !f.Service.ReadHub(f.Console).AutoAiming,"Pilot takeover of coordinated aiming cancels flight and fire");
f.Service.StartPursuit(f.Console,true); f.Service.SelectFireTarget(f.Console); f.Service.EngageWeapons(f.Console);
f.Service.ExternalControl(f.Own,1,0,0);
Check(!AutoNavCore.Engaged && f.Service.Fire.Permitted,"Manual flight takeover preserves independently authorized weapons when Auto Aim is off");
f.Service.StartPursuit(f.Console,true); Check(!f.Service.Fire.Permitted,"Starting another navigation operation ends the independent engagement");
f.Service.EngageWeapons(f.Console); f.Service.WorldChanging(); f.Service.WorldLoaded(); f.Service.UpdatePersistence();
Check(!AutoNavCore.Engaged && !f.Service.Fire.Permitted && Read(f.Console).Mode == SavedFlightMode.FollowingSuspended,
    "Follow always suspends on load, even with ordinary auto-resume enabled");
f.Service.ResumeSaved(f.Console); Check(AutoNavCore.Engaged && Read(f.Console).Mode == SavedFlightMode.Following && !f.Service.Fire.Permitted,
    "Resume restores Follow without restoring automatic-fire authority");
Signal(f.Own,.1);f.Service.Tick(f.Own.objSS,1,false);
Check(!AutoNavCore.Engaged && Read(f.Console).Mode==SavedFlightMode.FollowingSuspended,"Follow contact loss preserves mission mode");
// Shared hub is a read-only view over real service state, with exact hardware capability gates.
foreach (int installed in Enumerable.Range(0,8))
foreach (int damaged in Enumerable.Range(0,8))
{
    f=Setup(); f.Console.Items.Clear();
    if((installed&1)!=0) f.Console.Items.Add(new(){strID="n1",strCODef=NavigationService.ModuleId,ship=f.Own});
    if((installed&2)!=0) f.Console.Items.Add(new(){strID="n2",strCODef=NavigationService.PursuitId,ship=f.Own});
    if((installed&4)!=0) f.Console.Items.Add(new(){strID="n3",strCODef=NavigationService.FireControlId,ship=f.Own});
    foreach(var item in f.Console.Items) if(item.strID=="n3"&&(damaged&4)!=0 || item.strID=="n1"&&(damaged&1)!=0 || item.strID=="n2"&&(damaged&2)!=0) item.Conditions.Add("IsDamaged");
    bool n1=(installed&1)!=0&&(damaged&1)==0, n2=(installed&2)!=0&&(damaged&2)==0, n3=(installed&4)!=0&&(damaged&4)==0;
    var hub=f.Service.ReadHub(f.Console);
    Check(hub.WorkingNavigation==(n1||n2) && hub.WorkingPursuit==n2 && hub.WorkingFire==n3,"Healthy N1/N2 combinations provide only their own capabilities");
    f.Console.Conditions.Remove("IsPowered"); hub=f.Service.ReadHub(f.Console);
    Check(!hub.WorkingNavigation&&!hub.WorkingPursuit&&!hub.WorkingFire&&!hub.CanEngage&&!hub.CanManual&&!hub.Navigation.CanFly,
        "Console power loss disables operational capabilities with either/both modules");
}
f=Setup(); f.Target.objSS.vPosx=300*AutoNavCore.M_TO_AU; f.Target.objSS.vPosy=400*AutoNavCore.M_TO_AU;
f.Own.objSS.vVelX=6*AutoNavCore.M_TO_AU;f.Own.objSS.vVelY=8*AutoNavCore.M_TO_AU;
f.Own.objSS.fRot=0; StarSystem.fEpoch=1; f.Own.objSS.vAccIn=new(){x=0,y=(float)AutoNavCore.M_TO_AU};
f.Console.Pwr=new(); f.Own.Reactor=new(){ship=f.Own};f.Own.Reactor.Conditions.Add("IsReadyFusion");
before=Raw(f.Console);int resolves=TargetRef.Resolves;
for(int i=0;i<20;i++) f.Service.ReadHub(f.Console);
Check(Raw(f.Console)==before&&TargetRef.Resolves==resolves&&f.Own.ReactorWrites==0&&f.Own.Reactor.ConditionWrites==0&&!f.Service.Fire.Permitted,
    "Repeated hub refresh changes no preferences, physics, reactor or fire permission");
var reading=f.Service.ReadHub(f.Console);
Check(Math.Abs(reading.RangeKM!.Value-.5)<1e-9&&Math.Abs(reading.ClosingMS!.Value-10)<1e-9&&Math.Abs(reading.RelativeMS!.Value-10)<1e-9,
    "Hub reports centre range, positive closing and total relative speed separately");
f.Own.objSS.vVelX*=-1;f.Own.objSS.vVelY*=-1;Check(f.Service.ReadHub(f.Console).ClosingMS<0,"Separating motion has negative closing speed");
Check(reading.ConnectedKWh==12&&reading.TorchHours==1&&reading.RcsFuelKG==100,"Native available power/fuel readings retain units");
string savedThrottle=f.Console.mapGUIPropMaps["Panel A"]["slidThrottle"];
f.Console.mapGUIPropMaps["Panel A"]["slidThrottle"]="invalid";
Check(f.Service.ReadHub(f.Console).RcsAuthorityMS2==null,"Unreadable throttle makes authority unavailable rather than zero");
f.Console.mapGUIPropMaps["Panel A"]["slidThrottle"]="0";
Check(f.Service.ReadHub(f.Console).RcsAuthorityMS2==0,"Measured zero throttle displays zero authority");
f.Console.mapGUIPropMaps["Panel A"]["slidThrottle"]=savedThrottle;
f.Own.bCheckPower=true;reading=f.Service.ReadHub(f.Console);
Check(reading.RangeKM==null&&reading.RcsAuthorityMS2==null&&reading.ConnectedKWh==null,"Pending power marks target and system telemetry unavailable");
f.Own.bCheckPower=false;Signal(f.Own,.1);reading=f.Service.ReadHub(f.Console);
Check(reading.RangeKM==null&&reading.ClosingMS==null&&reading.AlignmentDegrees==null,"Weak contact cannot leak precise motion or alignment");
Signal(f.Own,1); f.Own.Reactor=null;reading=f.Service.ReadHub(f.Console);
Check(reading.TorchHours==null&&reading.Cycle==null&&!reading.CanBurn,"Missing reactor is unavailable, not zero fuel or authority");
f=Setup();f.Console.mapGUIPropMaps["NavModConfig"]=new(){[NavigationService.ModuleId]="0.35|0.05|0.60|0.25",["Map"]="0.25|0|0.65|0.8"};
f.Service.HubLoaded(f.Console);f.Service.HubLoaded(f.Console);
Check(CrewSim.coPlayer.Messages==1 && f.Console.mapGUIPropMaps["NavModConfig"].Count==2 &&
    f.Console.mapGUIPropMaps["NavModConfig"][NavigationService.ModuleId]=="0.35|0.05|0.60|0.25",
    "One-time placement notice never enlarges legacy anchors or changes other instruments");
f=Setup(); f.Own.Reactor=new(){ship=f.Own};f.Own.Reactor.Conditions.Add("IsReadyFusion");
f.Service.Engage(f.Console);f.Own.Thrust=1;f.Service.Fire.Permitted=true;
f.Service.ManualPropulsionAction(f.Console,ManualPropulsion.Flow,.2);
Check(!AutoNavCore.Engaged&&!f.Service.Fire.Permitted&&f.Own.Thrust==0&&Read(f.Console).Mode==SavedFlightMode.Stopped&&f.Own.ReactorProps["slidFlow"]=="0.2",
    "Manual flow releases automatic flight and fire before writing native actuators");
f=Setup();f.Own.Reactor=new(){ship=f.Own};f.Own.Reactor.Conditions.Add("IsReadyFusion");CrewSim.system.NoWake=true;
f.Service.ManualPropulsionAction(f.Console,ManualPropulsion.Cycle,.2);
Check(f.Own.ReactorWrites==0,"Native no-wake geometry blocks manual positive thrust");
CrewSim.system.NoWake=false;f.Own.ReactorProps["bNWZ"]="true";f.Service.ManualPropulsionAction(f.Console,ManualPropulsion.CycleEnabled,1);
Check(f.Own.ReactorWrites==0,"Native no-wake state blocks enabling thrust");
f.Own.ReactorProps["bNWZ"]="false";f.Own.Reactor.Conditions.Remove("IsReadyFusion");
f.Service.ManualPropulsionAction(f.Console,ManualPropulsion.Flow,.2);Check(f.Own.ReactorWrites==0,"Cold reactor cannot be started through the flight hub");
f.Own.Reactor.Conditions.Add("IsReadyFusion");f.Service.ManualPropulsionAction(f.Console,ManualPropulsion.Cycle,.8);
Check(f.Own.ReactorWrites==0,"Native safety maximum constrains manual thrust");
f.Console.mapGUIPropMaps["Panel A"]=new(){["bTorchSafety"]="invalid"};
f.Service.ManualPropulsionAction(f.Console,ManualPropulsion.Flow,.2);
Check(!f.Service.ReadHub(f.Console).CanBurn && f.Own.ReactorWrites==0,"Unknown safety state cannot authorize positive manual thrust");
f.Console.mapGUIPropMaps["Panel A"]["bTorchSafety"]="true";
f.Console.mapGUIPropMaps["Panel A"]["chkStationKeeping"]="true";
f.Service.ManualPropulsionAction(f.Console,ManualPropulsion.Flow,.2);
Check(f.Own.ReactorWrites==0,"Native station keeping prevents competing positive manual thrust");
f.Console.mapGUIPropMaps["Panel A"]["chkStationKeeping"]="false";
foreach(string key in new[]{"slidFlow","slidCycle","knobRatio"})
{
    string previous=f.Own.ReactorProps[key];f.Own.ReactorProps[key]="invalid";
    f.Service.ManualPropulsionAction(f.Console,ManualPropulsion.Flow,.2);
    Check(!f.Service.ReadHub(f.Console).CanBurn && f.Own.ReactorWrites==0,"Unknown native actuator reading inhibits positive manual thrust: "+key);
    f.Own.ReactorProps[key]=previous;
}
f.Service.ManualPropulsionAction(f.Console,ManualPropulsion.Shutdown,0);
Check(f.Own.Reactor.Conditions.Contains("IsOverrideOff"),"Shutdown delegates to the native reactor condition");

// N3 service lifecycle: native firing is separately checked by Fire.Tests.
f=Setup(); f.Console.Items.Clear();
f.Console.Items.Add(new(){strID="n3",strCODef=NavigationService.FireControlId,ship=f.Own});
f.Service.ToggleFireOwnership(f.Console);
Check(f.Service.Fire.Owns(f.Console.strID) && !f.Service.Fire.Permitted, "Take FCS control holds without a target or permission");
f.Service.SelectFireTarget(f.Console); f.Service.EngageWeapons(f.Console);
Check(f.Service.Fire.Permitted && !AutoNavCore.Engaged,"N3 alone can engage while flight is idle");
f.Service.ExternalControl(f.Own,1,0,0); Check(f.Service.Fire.Permitted,"Manual piloting preserves weapons-only authorization");
GUIOrbitDraw.Open=false; f.Service.TickFire(.25,false); f.Service.TickFire(.25,true);
Check(f.Service.Fire.Permitted,"Closing console does not revoke valid independent kinetic fire");
var original=Raw(f.Console); var maneuvers=f.Own.Maneuvers;
for(int i=0;i<10;i++) f.Service.ReadHub(f.Console);
Check(original==Raw(f.Console) && f.Own.Maneuvers==maneuvers,"N3 display cannot write preferences or thrust");
f.Service.StepWeapons(f.Console); Check(f.Service.ReadHub(f.Console).WeaponGroup==1,"Held group cannot change without Native return");
f.Service.CeaseFire(); Check(f.Service.Fire.Owns(f.Console.strID)&&!f.Service.Fire.Permitted,"Cease persists offensive ownership");
f.Service.EngageWeapons(f.Console); f.Service.TickFire(2,false);
Check(f.Service.Fire.Permitted && f.Service.Fire.Owns(f.Console.strID),"A large simulation step is skipped; fire permission and the hold are retained");
f.Service.EngageWeapons(f.Console); Signal(f.Own,.1); f.Service.TickFire(.25,false);
Check(!f.Service.Fire.Permitted,"Independent contact loss revokes fire");
Signal(f.Own,1); f.Service.TickFire(.25,false); Check(!f.Service.Fire.Permitted,"Independent reacquisition cannot rearm");
f.Service.EngageWeapons(f.Console); f.Console.Items[0]=new(){strID="replacement",strCODef=NavigationService.FireControlId,ship=f.Own}; f.Service.TickFire(.25,false);
Check(!f.Service.Fire.Permitted,"Same-model replacement cannot inherit module authority");
f.Service.EngageWeapons(f.Console);f.Console.Conditions.Remove("IsPowered");f.Service.TickFire(.25,false);
Check(!f.Service.Fire.Permitted,"Independent power loss revokes");f.Console.Conditions.Add("IsPowered");
f.Service.EngageWeapons(f.Console);f.Service.WorldChanging();f.Service.WorldLoaded();f.Console.Conditions.Add("IsLocked");f.Service.RestoreFireOwnership();
Check(f.Service.Fire.Owns(f.Console.strID)&&!f.Service.Fire.Permitted,"Reload restores hold without a shot budget");
f.Console.Conditions.Remove("IsLocked");
f.Service.ReturnFireToNative(f.Console); Check(!f.Service.Fire.Owns(f.Console.strID),"Explicit native handoff releases saved ownership");
f.Service.StepWeapons(f.Console); Check(f.Service.ReadHub(f.Console).WeaponGroup==2,"Group selection allowed after native handoff");
f.Service.SelectFireTarget(f.Console);f.Service.ToggleAutoAim(f.Console);f.Service.TickFire(.25,false);
Check(f.Service.ReadHub(f.Console).AutoAiming && f.Own.Maneuvers>maneuvers && !f.Service.Fire.Permitted,"Standalone Auto Aim does not authorize shooting");
var savedAim = new JsonShipSitu { fA=1, vAccRCS=new(){x=1}, vAccIn=new(){x=1} };
f.Console.ship = new Ship(); NavigationService.PrepareSavedPhysics(f.Own.objSS,savedAim); f.Console.ship = f.Own;
Check(savedAim.fA==0 && savedAim.vAccRCS.magnitude==0,"Standalone aiming actuator is not serialized even if the console moved before the next interval");
f.Service.EngageWeapons(f.Console); f.Service.ExternalControl(f.Own,0,0,.5f);
Check(!f.Service.ReadHub(f.Console).AutoAiming && !f.Service.Fire.Permitted && f.Own.LastRotation==0,"Pilot yaw clears aim before manual command and cancels firing");
f.Service.SelectFireTarget(f.Console);f.Service.ToggleAutoAim(f.Console);f.Service.TickFire(.25,false);f.Service.CeaseFire();
Check(!f.Service.ReadHub(f.Console).AutoAiming && f.Own.LastRotation==0,"Cease removes standalone RCS demand");
var mountA = new WeaponReading { Id="mount-a", Heading=.3 }; var mountB = new WeaponReading { Id="mount-b", Heading=-2 };
foreach (var mount in new[]{mountA,mountB})
{
    var installedMount = new CondOwner { strID=mount.Id, ship=f.Own };
    installedMount.Conditions.Add("IsShipWeapon"); installedMount.Amounts["IsShipWeaponFiringGroup"] = 1;
    f.Own.Items.Add(installedMount);
}
f.Service.Fire.Weapons = new[]{mountA,mountB}; f.Service.UseAimReference(f.Console); f.Service.ToggleAutoAim(f.Console); f.Service.TickFire(.25,false);
f.Service.Fire.Weapons = new[]{mountB,mountA}; f.Service.TickFire(.25,false);
Check(AutoNavCore.WeaponHeading==.3,"Aiming reference remains bound despite conflicting mounts and observation order");
f.Own.RCSCount=0; f.Service.TickFire(.25,false);
Check(!f.Service.ReadHub(f.Console).AutoAiming,"Lost RCS authority ends independent aiming"); f.Own.RCSCount=1;
f.Service.ToggleAutoAim(f.Console); f.Service.EngageWeapons(f.Console); f.Own.FailManeuver=true; f.Service.TickFire(.25,false);
Check(!f.Service.ReadHub(f.Console).AutoAiming && !f.Service.Fire.Permitted,"Native maneuver exception cannot retain aiming or fire permission"); f.Own.FailManeuver=false;
var secondConsole=new CondOwner { strID="other-console",ship=f.Own };secondConsole.Items.Add(new(){strID="other-n3",strCODef=NavigationService.FireControlId,ship=f.Own});f.Own.Items.Add(secondConsole);
f.Service.SelectFireTarget(secondConsole);f.Service.EngageWeapons(secondConsole);
Check(!f.Service.Fire.Permitted,"Another console cannot take a held group implicitly");
f = Setup();
Check(!f.Service.WatchArrival(f.Console, true), "Idle consoles cannot arm a future unspecified arrival");
f.Service.Engage(f.Console);
Check(f.Service.WatchArrival(f.Console, true) && f.Service.WatchingForTest, "Pilot can watch an active finite approach");
f.Service.FinishForCueTest("ARRIVED");
Check(f.Service.CueCompletedForTest && !f.Service.WatchingForTest, "Persisted arrival consumes watch with audio host absent");
f.Service.WorldChanging();
Check(!f.Service.CueCompletedForTest && !f.Service.WatchingForTest, "Reload clears arrival notification state");
f = Setup(); f.Service.Engage(f.Console); f.Service.WatchArrival(f.Console, true);
f.Service.FinishForCueTest("ABORTED");
Check(!f.Service.CueCompletedForTest && !f.Service.WatchingForTest, "Aborted approach never sounds success");
f = Setup(); f.Service.Engage(f.Console); f.Service.WatchArrival(f.Console, true);
f.Service.Stop(f.Console, "pilot stop");
Check(!f.Service.CueCompletedForTest && !f.Service.WatchingForTest, "Pilot stop cancels arrival watch");
Console.WriteLine($"{checks} sensor-boundary, guidance, display and persistence assertions passed. No in-game tests performed.");
f = Setup(); f.Target.objSS.vPosy = 1000 * AutoNavCore.M_TO_AU;
Check(IndustrialNavigation.Request("capture", f.Console, "module", "target", 0, () => null, out _), "Industrial request uses real hardware/sensor service boundary");
f.Service.ExternalControl(f.Own, 0, 0, 0);
Check(IndustrialNavigation.Observe("capture", out _, out _), "Closing console zero command retains industrial permission");
f.Service.ExternalControl(f.Own, 1, 0, 0);
Check(!IndustrialNavigation.Observe("capture", out _, out _), "Nonzero manual thrust immediately releases industrial permission");
Check(IndustrialNavigation.Request("capture", f.Console, "module", "target", 0, () => null, out _), "Explicit restart after takeover works");
f.Service.ExternalReactorControl(f.Own, "slidFlow", "1");
Check(!IndustrialNavigation.Observe("capture", out _, out _), "Manual reactor control immediately releases industrial permission");
Console.WriteLine($"{checks} checks including industrial manual-takeover integration passed.");

// Explicit local override of native flight intent must not depend on a Phobos save record.
f = Setup();
f.Console.mapGUIPropMaps["Panel A"] = new() { ["chkEngage"] = "true", ["chkHoldThrust"] = "false" };
var secondStation = new CondOwner { strID = "second", ship = f.Own };
secondStation.Conditions.Add("IsNavStation"); secondStation.mapGUIPropMaps["Panel A"] = new() { ["chkEngage"] = "true" };
f.Own.Items.Add(secondStation);
var beforeOverride = Raw(f.Console);
var blocked = f.Service.ReadInstruments(f.Console);
Check(blocked.CanStop && !blocked.CanFly && blocked.Notice == "Controls.native_switch", "Stale native engagement exposes its cause and a usable Disengage");
Check(Raw(f.Console) == beforeOverride, "Displaying a blocked control does not silently override native intent");
f.Service.Stop(f.Console, "pilot");
Check(f.Console.mapGUIPropMaps["Panel A"]["chkEngage"] == "false" && secondStation.mapGUIPropMaps["Panel A"]["chkEngage"] == "false", "Explicit stop clears both local nav station switches");
Check(f.Service.ReadInstruments(f.Console).CanFly, "Approach becomes available after explicit override and fresh checks");
Check(Store(f.Console).Read(out _) == SavedStateStatus.Missing, "Native override invents no saved Phobos flight");
f = Setup(); f.Own.Reactor = new CondOwner { ship = f.Own }; f.Own.ReactorProps["slidCycle"] = ".22";
f.Own.IsUsingTorchDrive = true; f.Own.objSS.vVelX = 42; f.Own.objSS.fW = .2f;
Check(f.Service.ReadInstruments(f.Console).CanStop && f.Service.ReadInstruments(f.Console).Notice == "Controls.torch_request", "Manual torch request is distinguished from another autopilot");
f.Service.Stop(f.Console, "pilot");
Check(f.Own.ReactorProps["slidCycle"] == "0" && !f.Own.IsUsingTorchDrive && f.Own.objSS.vVelX == 42 && f.Own.objSS.fW == .2f && !f.Own.Reactor.HasCond("IsOverrideOff"), "Disengage clears torch thrust while preserving velocity, spin and reactor operation");
foreach (string command in new[] { "FlyToAutoPilot", "HoldStationAutoPilot", "HoldThrustAutoPilot" })
{
    f = Setup(); AIShipManager.Current = new() { ActiveCommandName = command }; f.Own.shipStationKeepingTarget = new();
    Check(f.Service.ReadInstruments(f.Console).CanStop, "Known native pilot may be explicitly released");
    f.Service.Stop(f.Console, "pilot");
    Check(AIShipManager.Current == null && f.Own.shipStationKeepingTarget == null, "Native pilot and station target released");
}
f = Setup(); AIShipManager.Current = new() { ActiveCommandName = "PolicePatrol" };
Check(!f.Service.ReadInstruments(f.Console).CanStop, "Unrelated AI remains outside the native override");
f.Service.Stop(f.Console, "pilot"); Check(AIShipManager.Current != null, "Explicit stop does not unregister unrelated AI");
f = Setup(); f.Console.mapGUIPropMaps["Panel A"] = new() { ["chkEngage"] = "true" };
BepInEx.Bootstrap.Chainloader.PluginInfos["com.mrkmg.ostranauts.autonavigate"] = new();
Check(!f.Service.ReadInstruments(f.Console).CanStop, "An independent flight plugin must release itself");
f.Service.Stop(f.Console, "pilot"); Check(f.Console.mapGUIPropMaps["Panel A"]["chkEngage"] == "true", "Independent plugin conflict prevents native override");
f = Setup(); f.Console.mapGUIPropMaps["Panel A"] = new() { ["chkEngage"] = "true" }; CrewSim.coPlayer.ship = f.Target;
f.Service.Stop(f.Console, "pilot"); Check(f.Console.mapGUIPropMaps["Panel A"]["chkEngage"] == "true", "Foreign console cannot override the original ship");
Console.WriteLine($"{checks} checks including explicit native control override passed.");

f = Setup(); GUIOrbitDraw.Instance.ledWLock.State = 3;
GUIOrbitDraw.Instance.chkStationKeeping.isOn = true; GUIOrbitDraw.Instance.Course.chkEngage.isOn = true;
f.Console.mapGUIPropMaps["Panel A"] = new() { ["chkHoldThrust"] = "true" };
GUIOrbitDraw.Instance.chkStationKeeping.Events = GUIOrbitDraw.Instance.Course.chkEngage.Events = 0;
f.Service.Stop(f.Console, "pilot");
Check(!GUIOrbitDraw.Instance.HoldingThrustActive && !GUIOrbitDraw.Instance.chkStationKeeping.isOn && !GUIOrbitDraw.Instance.Course.chkEngage.isOn,
    "Explicit release clears native panel latches as well as saved switches");
Check(GUIOrbitDraw.Instance.chkStationKeeping.Events == 0 && GUIOrbitDraw.Instance.Course.chkEngage.Events == 0, "Native toggle synchronization never replays engagement callbacks");
f = Setup(); GUIOrbitDraw.Instance.ledWLock.State = 3; GUIOrbitDraw.Console = new() { ship = f.Target };
NativeFlightPanel.Release(f.Own);
Check(GUIOrbitDraw.Instance.HoldingThrustActive, "Another ship's open panel keeps its native latches");
Console.WriteLine($"{checks} checks including native panel latch release passed.");

// One fresh read per presentation; hidden pages must not touch their native data.
f = Setup(); f.Own.Reactor = new CondOwner { ship = f.Own };
int moduleReads = f.Console.ModuleReads, contactReads = f.Own.ElectronicSystems.Reads;
var navPage = f.Service.ReadHub(f.Console, "navigation");
Check(f.Console.ModuleReads - moduleReads == 1, "Header and visible page share one module discovery");
Check(f.Own.ElectronicSystems.Reads - contactReads == 1, "Header and visible page share their target contact");
Check(f.Own.ReactorReadKeys.All(key => key == "slidCycle"), "Hidden Systems reads only thrust intent needed for the shared navigation warning");
Check(navPage.Navigation.Details == "", "Hidden Details is not formatted: " + navPage.Navigation.Details);
Check(navPage.WeaponCard == "", "Hidden weapon card is not formatted");
f.Service.ReadHub(f.Console, "systems");
Check(f.Own.ReactorReadKeys.Contains("slidFlow") && f.Own.ReactorReadKeys.Contains("bNWZ"), "Selecting Systems reads current reactor data");
Check(f.Service.ReadHub(f.Console, "details").Navigation.Details != "", "Selecting Details supplies its help");
f.Console.Items[0].Conditions.Add("IsDamaged");
Check(!f.Service.ReadHub(f.Console, "navigation").WorkingNavigation, "Next snapshot discovers newly damaged hardware");
Check(navPage.WorkingNavigation, "Previously returned presentation remains a detached value");
f.Service.Engage(f.Console);
Check(!AutoNavCore.Engaged, "A formerly valid presentation never authorises flight after hardware damage");
Console.WriteLine($"{checks} checks including presentation snapshots passed.");

// Installed inventory must retain inactive weapons without granting fire permission.
f = Setup();
f.Console.Items.Add(new() { strID = "fire-module", strCODef = NavigationService.FireControlId, ship = f.Own });
CondOwner Weapon(string id, int group, string name)
{
    var w = new CondOwner { strID = id, strName = name, ship = f.Own };
    w.Conditions.Add("IsShipWeapon"); w.Amounts["IsShipWeaponFiringGroup"] = group - 1;
    f.Own.Items.Add(w); return w;
}
var coil = Weapon("coil", 1, "Burst Coil MK1");
var missile = Weapon("launcher", 2, "Artemis Launcher"); missile.Conditions.Add("IsOff"); missile.Conditions.Remove("IsPowered");
var ciws = Weapon("ciws", 3, "CIWS");
var fireView = f.Service.ReadHub(f.Console, "fire");
Check(fireView.Groups.Select(g => g.Group).SequenceEqual(new[]{1,2,3}), "All three installed groups remain visible");
Check(f.Service.ReadHub(f.Console, "navigation").Groups.Length == 0, "Hidden Fire performs no inventory read");
var ticket = f.Service.PrepareGroupSwitch(f.Console, 2)!;
Check(ticket != null && f.Service.ConfirmGroupSwitch(ticket), "Native-owned group switches without a lease");
fireView = f.Service.ReadHub(f.Console, "fire");
Check(fireView.WeaponCard.Contains("Artemis Launcher") && fireView.WeaponCard.Contains("FCS.switched_off"), "Inactive launcher is named with its actual reason");
Check(!fireView.FireHeld && !f.Service.Fire.Permitted, "Browsing never acquires hold or firing permission");
missile.Conditions.Add("IsDamaged");
Check(f.Service.ReadHub(f.Console,"fire").WeaponCard.Contains("FCS.damaged"), "Damage is shown before the off condition, rather than suggesting power alone resolves it");
missile.Conditions.Remove("IsOff"); missile.Conditions.Add("IsPowered"); missile.Conditions.Add("IsDamaged");
Check(f.Service.ReadHub(f.Console,"fire").WeaponCard.Contains("FCS.damaged"), "Damaged installed weapon remains visible");
missile.Conditions.Remove("IsDamaged");
Check(!f.Service.ReadHub(f.Console,"fire").WeaponCard.Contains("FCS.damaged"), "Power and damage recovery is reflected by the next read");
f.Service.ToggleFireOwnership(f.Console);
f.Service.SelectFireTarget(f.Console); f.Service.ToggleAutoAim(f.Console); f.Service.EngageWeapons(f.Console);
ticket = f.Service.PrepareGroupSwitch(f.Console,3)!;
Check(ticket.Held && f.Service.Fire.Owns(f.Console.strID) && f.Service.Fire.Permitted && f.Service.ReadHub(f.Console,"fire").AutoAiming, "Preparing or cancelling confirmation preserves original engagement");
Check(f.Service.ConfirmGroupSwitch(ticket), "Confirmed handoff succeeds");
Check(f.Service.ReadHub(f.Console,"fire").WeaponGroup == 3 && f.Service.Fire.Owns(f.Console.strID) && !f.Service.Fire.Permitted && !f.Service.ReadHub(f.Console,"fire").AutoAiming, "New group held with no firing permission or Auto Aim");
Check(!f.Service.ConfirmGroupSwitch(ticket), "Consumed confirmation cannot be replayed");
ticket = f.Service.PrepareGroupSwitch(f.Console,2)!;
missile.ship = f.Target;
Check(!f.Service.ConfirmGroupSwitch(ticket) && f.Service.ReadHub(f.Console,"fire").WeaponGroup == 3, "Moved destination rejected without releasing old group");
missile.ship = f.Own;
ticket = f.Service.PrepareGroupSwitch(f.Console,2)!;
missile.bDestroyed = true;
Check(!f.Service.ConfirmGroupSwitch(ticket), "Destroyed destination rejected");
missile.bDestroyed = false;
ticket = f.Service.PrepareGroupSwitch(f.Console,2)!;
f.Own.Items.Remove(missile); missile = Weapon("launcher",2,"Replacement launcher");
Check(!f.Service.ConfirmGroupSwitch(ticket), "Same-ID replacement does not inherit confirmation");
ticket = f.Service.PrepareGroupSwitch(f.Console,2)!;
CrewSim.Selected = new CondOwner { ship=f.Own };
Check(!f.Service.ConfirmGroupSwitch(ticket), "Operator change invalidates handoff");
CrewSim.Selected = CrewSim.coPlayer;
ticket = f.Service.PrepareGroupSwitch(f.Console,2)!;
f.Service.Fire.SetOwnership("other-console", f.Own, 1, true);
Check(!f.Service.ConfirmGroupSwitch(ticket), "Another console invalidates handoff");
f.Service.Fire.SetOwnership("other-console", f.Own, 1, false);
ticket = f.Service.PrepareGroupSwitch(f.Console,2)!;
var pref = f.Console.mapGUIPropMaps["PhobosState.AutoNav.FirePreferences"]; var schema = pref["schema"]; pref["schema"]="99";
Check(!f.Service.ConfirmGroupSwitch(ticket) && f.Service.Fire.Owns(f.Console.strID), "Unsupported save rejects handoff without releasing old ownership");
pref["schema"]=schema;
f.Service.StepVolleys(f.Console,-1);
Check(f.Service.ReadHub(f.Console,"fire").Volleys == 9,"Right click wraps one to nine");
f.Service.StepVolleys(f.Console);
Check(f.Service.ReadHub(f.Console,"fire").Volleys == 1,"Left click wraps nine to one");
f.Service.StepVolleys(f.Console); f.Service.StepVolleys(f.Console,-1);
Check(f.Service.ReadHub(f.Console,"fire").Volleys == 1 && !f.Service.Fire.Permitted,"Each direction steps once and never rearms");
ciws.Conditions.Remove("IsInstalled");
Check(f.Service.ReadHub(f.Console,"fire").WeaponCard.Contains("FCS.group_empty"),"Removed selected weapon explicitly reports empty group");
ciws.Conditions.Add("IsInstalled"); ciws.Amounts["IsShipWeaponFiringGroup"] = 5;
Check(f.Service.ReadHub(f.Console,"fire").Groups.Any(g=>g.Group==6),"Native reassignment reflected without retaining inventory");
ticket = f.Service.PrepareGroupSwitch(f.Console,2)!;
f.Service.WorldChanging();
Check(!f.Service.ConfirmGroupSwitch(ticket) && !f.Service.Fire.Permitted, "Reload invalidates an outstanding handoff and clears firing permission");
Check(f.Service.ReadHub(f.Console,"fire").Groups.Any(g=>g.Group==6), "Inventory is freshly discovered after world reset");
Console.WriteLine($"{checks} checks including weapon inventory and guided handoffs passed.");

void ReadyCombat(bool mission=true)
{
    f=Setup();
    f.Console.Items.Add(new(){strID="n2",strCODef=NavigationService.PursuitId,ship=f.Own});
    f.Console.Items.Add(new(){strID="n3",strCODef=NavigationService.FireControlId,ship=f.Own});
    Weapon("weapon",1,"Test");
    if(mission)f.Service.Engage(f.Console);
    f.Service.SelectFireTarget(f.Console);f.Service.TickFire(.1,false);f.Service.UseAimReference(f.Console);
}
ReadyCombat();var preCombatMission=Read(f.Console);f.Service.EnterCombat(f.Console);
Check(f.Service.CombatActive && AutoNavCore.Engaged && AutoNavCore.Following,"Combat acquires range matching");
Check(Read(f.Console).Mode==SavedFlightMode.Suspended && Read(f.Console).TargetId==preCombatMission.TargetId,"Original mission suspended before transfer");
Check(!f.Service.Fire.Permitted && f.Service.ReadHub(f.Console,"fire").AutoAiming,"Combat starts aiming without firing permission");
var suspendedCombatRecord=Raw(f.Console);f.Service.Tick(f.Own.objSS,.1,false);
Check(Raw(f.Console)==suspendedCombatRecord,"Combat progress never overwrites the suspended mission");
f.Service.EngageWeapons(f.Console);Check(f.Service.Fire.Permitted,"Combat still needs explicit Engage");
foreach(var phase in new[]{AutoNavCore.Phase.Decel,AutoNavCore.Phase.Align})
{AutoNavCore.CurrentPhase=phase;f.Service.TickFire(.1,true);Check(!f.Service.Fire.LastDispatchSafe && f.Service.Fire.Permitted,"Guidance hold inhibits dispatch without reauthorizing shots");}
AutoNavCore.CurrentPhase=AutoNavCore.Phase.Coast;f.Service.TickFire(.1,true);Check(f.Service.Fire.LastDispatchSafe,"Settled guidance admits fresh shot checks");
f.Service.CeaseFire();Check(f.Service.CombatActive&&AutoNavCore.Engaged&&!f.Service.Fire.Permitted&&!f.Service.ReadHub(f.Console,"fire").AutoAiming,"Cease Fire keeps range matching only");
f.Service.ToggleCombat(f.Console);Check(!f.Service.CombatActive&&!AutoNavCore.Engaged&&Read(f.Console).Mode==SavedFlightMode.Suspended,"Leave keeps old mission suspended");
f.Service.ResumeSaved(f.Console);Check(AutoNavCore.Engaged&&!f.Service.Fire.Permitted,"Explicit Resume restores original mission without shots");
ReadyCombat();var beforeCombat=Raw(f.Console);f.Console.Items.First(c=>c.strID=="n2").Conditions.Add("IsDamaged");f.Service.EnterCombat(f.Console);
Check(!f.Service.CombatActive&&AutoNavCore.Engaged&&Raw(f.Console)==beforeCombat,"Failed hardware admission leaves mission unchanged");
ReadyCombat();f.Console.mapGUIPropMaps["PhobosState.AutoNav.Flight"]["schema"]="999";beforeCombat=Raw(f.Console);f.Service.EnterCombat(f.Console);
Check(!f.Service.CombatActive&&AutoNavCore.Engaged&&Raw(f.Console)==beforeCombat,"Protected flight record cannot be overwritten during entry");
ReadyCombat();f.Service.ToggleFireOwnership(f.Console);f.Console.mapGUIPropMaps["PhobosState.AutoNav.FirePreferences"]["schema"]="999";beforeCombat=Raw(f.Console);f.Service.EnterCombat(f.Console);
Check(!f.Service.CombatActive&&AutoNavCore.Engaged&&Raw(f.Console)==beforeCombat,"Protected fire record prevents entry without suspending mission");
ReadyCombat();f.Service.EnterCombat(f.Console);f.Service.EngageWeapons(f.Console);f.Service.WorldChanging();f.Service.WorldLoaded();f.Service.UpdatePersistence();f.Service.RestoreFireOwnership();
Check(!f.Service.CombatActive&&!AutoNavCore.Engaged&&!f.Service.Fire.Permitted&&Read(f.Console).Mode==SavedFlightMode.Suspended,"Reload restores no combat, aiming or shots");
Check(f.Service.Fire.Owns(f.Console.strID),"Reload preserves FCS Hold");
foreach(string fault in new[]{"operator","contact","module","weapon","owner","manual","native","target"})
{
    ReadyCombat();f.Service.EnterCombat(f.Console);f.Service.EngageWeapons(f.Console);
    switch(fault)
    {
        case "operator":CrewSim.Selected=new(){ship=f.Own};break;
        case "contact":Signal(f.Own,.01);break;
        case "module":f.Console.Items.First(c=>c.strID=="n3").Conditions.Add("IsDamaged");break;
        case "weapon":f.Service.Fire.Weapons=Array.Empty<WeaponReading>();break;
        case "owner":f.Service.Fire.SetOwnership("other",f.Own,1,true);break;
        case "manual":f.Service.CeaseFire();f.Service.EngageWeapons(f.Console);f.Service.ExternalControl(f.Own,1,0,0);break;
        case "native":f.Service.ReturnFireToNative(f.Console);break;
        case "target":f.Service.SelectFireTarget(f.Console);break;
    }
    f.Service.TickFire(.1,false);
    Check(!f.Service.CombatActive&&!AutoNavCore.Engaged&&!f.Service.Fire.Permitted&&Read(f.Console).Mode==SavedFlightMode.Suspended,"Combat authority ends on "+fault);
}
ReadyCombat(false);f.Service.EnterCombat(f.Console);Check(f.Service.CombatActive,"Combat can start without a saved mission");f.Service.ToggleCombat(f.Console);
Check(!f.Console.mapGUIPropMaps.ContainsKey("PhobosState.AutoNav.Flight"),"Combat does not invent a resumable mission");
f=Setup();f.Console.Items.Add(new(){strID="n3",strCODef=NavigationService.FireControlId,ship=f.Own});f.Service.Engage(f.Console);
var hostile=new Ship{strRegID="hostile",publicName="Hostile"};CrewSim.system.Ships.Add("hostile",hostile);GUIOrbitDraw.CrossHairTarget=new(){Ship=hostile};
f.Service.SelectFireTarget(f.Console);f.Service.ToggleAutoAim(f.Console);f.Service.EngageWeapons(f.Console);f.Service.TickFire(.1,false);
Check(AutoNavCore.EngagedTarget?.ShipId=="target"&&f.Service.Fire.Permitted&&f.Service.ReadHub(f.Console,"fire").AutoAiming,"Approach and FCS retain separate targets");
ReadyCombat(); var priorTarget=AutoNavCore.EngagedTarget!.ShipId;
var combatTarget=new Ship{strRegID="combat-target",publicName="Combat target"}; CrewSim.system.Ships.Add(combatTarget.strRegID,combatTarget);
GUIOrbitDraw.CrossHairTarget=new(){Ship=combatTarget}; f.Service.SelectFireTarget(f.Console); f.Service.UseAimReference(f.Console); f.Service.EnterCombat(f.Console);
Check(f.Service.CombatActive && AutoNavCore.EngagedTarget!.ShipId==combatTarget.strRegID && Read(f.Console).TargetId==priorTarget,
    "Combat follows the explicit fire target while preserving the separate previous destination");
f.Service.EngageWeapons(f.Console); f.Own.IsUsingTorchDrive=true; f.Service.TickFire(.1,true);
Check(!f.Service.Fire.LastDispatchSafe && f.Service.Fire.Permitted,"Torch burn holds offensive dispatch without transferring permission");
f.Own.IsUsingTorchDrive=false; f.Service.avoidanceActive=true; f.Service.TickFire(.1,true);
Check(!f.Service.Fire.LastDispatchSafe && f.Service.CombatActive,"Avoidance holds offensive dispatch while retaining Combat intent");
f.Service.avoidanceActive=false; f.Service.Fire.Cease("FCS.complete"); f.Service.TickFire(.1,true);
Check(f.Service.CombatActive && AutoNavCore.Engaged && !f.Service.ReadHub(f.Console,"fire").AutoAiming,"Exhausted volley budget ends aiming but keeps range matching");
f.Service.Fire.Weapons[0].Loaded=false; f.Service.TickFire(.1,false);
Check(f.Service.CombatActive && !f.Service.Fire.Permitted,"Empty reference after completed volleys does not stop range matching");
ReadyCombat(); f.Service.EnterCombat(f.Console); f.Service.EngageWeapons(f.Console);
f.Service.SetPanelTorch(f.Console,!Plugin.PreferTorch.Value);
Check(f.Service.CombatActive && !f.Service.Fire.Permitted,"Changing Combat drive preference revokes shots");
ReadyCombat(); f.Service.EnterCombat(f.Console); f.Service.EngageWeapons(f.Console); Weapon("other-weapon",2,"Other");
ticket=f.Service.PrepareGroupSwitch(f.Console,2)!;
Check(f.Service.ConfirmGroupSwitch(ticket) && !f.Service.CombatActive && !f.Service.Fire.Permitted && !AutoNavCore.Engaged,
    "Confirmed group change ends Combat without transferring movement or firing authority");
ReadyCombat(); beforeCombat=Raw(f.Console); CrewSim.coPlayer=new(){strID="replacement-player",ship=f.Own}; CrewSim.Selected=CrewSim.coPlayer;
f.Service.EnterCombat(f.Console);
Check(!f.Service.CombatActive && Raw(f.Console)==beforeCombat,"Stale previous mission binding cannot transfer authority");
Console.WriteLine($"{checks} checks including Combat lifecycle and coordinated navigation passed.");

// Saved owner scenario: one reciprocal, secured tow; ordinary navigation remains available.
f=Setup(); f.Target.Attachments["peer-port"]=f.Own; f.Own.Attachments["own-port"]=f.Target;
Check(TowFlight.Problem(f.Own)==null,"Reciprocal secured tow is admitted");
foreach(var reason in new[]{"brace","refresh","moored","station","grounded","extra","reciprocal","torch","waypoint"})
{
    f=Setup(); f.Target.Attachments["peer-port"]=f.Own; f.Own.Attachments["own-port"]=f.Target;
    switch(reason)
    {
        case "brace":f.Own.TowSecured=false;break;
        case "refresh":f.Target.bCheckTowingBraces=true;break;
        case "moored":f.Target.TowMoored=true;break;
        case "station":f.Target.objSS.bIsBO=true;break;
        case "grounded":f.Target.objSS.bGrounded=true;break;
        case "extra":f.Target.Attachments["extra"]=new();break;
        case "reciprocal":f.Target.Attachments.Clear();break;
        case "torch":f.Target.IsUsingTorchDrive=true;break;
        case "waypoint":f.Target.aWPs.Add(new());break;
    }
    Check(TowFlight.Problem(f.Own)!=null,"Tow rejects "+reason);
}
f=Setup(); var load=new Ship{strRegID="load"}; CrewSim.system.Ships["load"]=load;
f.Own.Attachments["own-port"]=load; load.Attachments["peer-port"]=f.Own;
load.objSS.vPosx=250*AutoNavCore.M_TO_AU;
Check(Math.Abs(TowFlight.RadiusAU(f.Own)/AutoNavCore.M_TO_AU-350)<1e-8,"Envelope includes load offset and radius");
f.Console.Items.Add(new(){strID="n3",strCODef=NavigationService.FireControlId,ship=f.Own});
f.Service.TickFire(.1,false);
Check(f.Service.ReadHub(f.Console,"navigation").Navigation.CanFly,"Idle FCS fault does not disable secure towing navigation");
Check(f.Service.ReadHub(f.Console,"navigation").Restriction != "FCS.unavailable","Idle fire failure cannot replace ready navigation guidance");
f.Service.Engage(f.Console); Check(AutoNavCore.Engaged,"Production service starts an ordinary secured tow");
f.Own.TowSecured=false; f.Service.Tick(f.Own.objSS,.1,false);
Check(!AutoNavCore.Engaged && f.Own.Thrust==0,"Lost brace stops commanded towing thrust");
Check(f.Service.ReadHub(f.Console,"navigation").Restriction=="Tow.secure","FCS fault cannot hide the current navigation blocker");
Console.WriteLine($"{checks} checks including secured towing and warning priority passed.");

// FCS uses the same fresh secured-tow policy as navigation; permission remains explicit.
Ship AttachLoad()
{
    var tow = new Ship { strRegID = "tow", publicName = "Attached load" };
    CrewSim.system.Ships[tow.strRegID] = tow;
    f.Own.Attachments["own-port"] = tow; tow.Attachments["peer-port"] = f.Own;
    return tow;
}
ReadyCombat(false); var towLoad = AttachLoad();
f.Console.Items.RemoveAll(c => c.strID == "n2");
f.Service.TickFire(.1,false);
Check(f.Service.ReadHub(f.Console,"fire").CanEngage && !f.Service.Fire.Permitted,"Secured tow permits N3 FCS without N2 or automatic shots");
f.Service.ToggleAutoAim(f.Console); f.Service.Fire.Weapons[0].Heading=1;
f.Service.TickFire(.1,false);
Check(f.Service.ReadHub(f.Console,"fire").AutoAiming && f.Own.LastRotation!=0,"Standalone tow aiming uses native rotation");
f.Service.EngageWeapons(f.Console); f.Service.TickFire(.1,true);
Check(f.Service.Fire.Permitted && f.Service.Fire.LastDispatchSafe,"Explicit FCS Engage admits ordinary fresh shot checks while towing");
f.Service.CeaseFire();
Check(!f.Service.Fire.Permitted && f.Own.LastRotation==0,"Cease Fire clears native standalone rotation with a positive stop timestep");
foreach(var fault in new[]{"brace","refresh","moored","reciprocal","controls"})
{
    ReadyCombat(false); towLoad=AttachLoad(); f.Service.ToggleAutoAim(f.Console); f.Service.EngageWeapons(f.Console);
    switch(fault)
    {
        case "brace":f.Own.TowSecured=false;break;
        case "refresh":towLoad.bCheckTowingBraces=true;break;
        case "moored":towLoad.TowMoored=true;break;
        case "reciprocal":towLoad.Attachments.Clear();break;
        case "controls":towLoad.IsUsingTorchDrive=true;break;
    }
    f.Service.TickFire(.1,true);
    Check(!f.Service.Fire.Permitted && !f.Service.ReadHub(f.Console,"fire").AutoAiming,"Tow fault revokes shots and aiming before dispatch: "+fault);
    f.Own.TowSecured=true; towLoad.bCheckTowingBraces=false; towLoad.TowMoored=false;
    towLoad.Attachments["peer-port"]=f.Own; towLoad.IsUsingTorchDrive=false;
    f.Service.TickFire(.1,false);
    Check(!f.Service.Fire.Permitted && !f.Service.ReadHub(f.Console,"fire").AutoAiming,"Restored tow never rearms automatically: "+fault);
}
ReadyCombat(false); towLoad=AttachLoad(); GUIOrbitDraw.CrossHairTarget=new(){Ship=towLoad};
f.Service.SelectFireTarget(f.Console); f.Service.EngageWeapons(f.Console);
Check(!f.Service.Fire.Permitted && !f.Service.ReadHub(f.Console,"fire").CanEngage,"Attached hull cannot be selected as a fire target");
ReadyCombat(false); f.Service.EngageWeapons(f.Console);
f.Own.Attachments["own-port"]=f.Target; f.Target.Attachments["peer-port"]=f.Own;
f.Service.TickFire(.1,true);
Check(!f.Service.Fire.Permitted && f.Service.Fire.Reason=="FCS.attached_target","Target attachment after Engage revokes permission before dispatch");
ReadyCombat(); towLoad=AttachLoad(); f.Service.EnterCombat(f.Console);
Check(f.Service.CombatActive && !f.Service.Fire.Permitted,"Combat admits secured tow without granting firing permission");
f.Service.EngageWeapons(f.Console); f.Service.CeaseFire();
Check(f.Service.CombatActive && AutoNavCore.Engaged && !f.Service.ReadHub(f.Console,"fire").AutoAiming,"Tow Cease Fire keeps Combat range matching only");
f.Service.EngageWeapons(f.Console); f.Own.TowSecured=false; f.Service.TickFire(.1,true);
Check(!f.Service.CombatActive && !AutoNavCore.Engaged && !f.Service.Fire.Permitted && Read(f.Console).Mode==SavedFlightMode.Suspended,"Lost brace ends Combat and preserves suspended mission");
ReadyCombat(); towLoad=AttachLoad(); f.Service.EnterCombat(f.Console); f.Service.EngageWeapons(f.Console);
f.Service.WorldChanging(); f.Service.WorldLoaded(); f.Service.UpdatePersistence(); f.Service.RestoreFireOwnership();
Check(!f.Service.CombatActive && !f.Service.Fire.Permitted && !AutoNavCore.Engaged && f.Service.Fire.Owns(f.Console.strID),"Towing reload restores Hold without movement or shots");
Console.WriteLine($"{checks} checks including towing FCS and Combat passed.");

// Asteroid markers: native stellar-object sensing through the same reader, hazard scan and Fly.
{
    var s = Setup();
    double km = AutoNavCore.KM_TO_AU;
    var belt = new AsteroidField { strName = "belt", dXReal = 10 * km, dYReal = 0 };
    CrewSim.system.aBOs["belt"] = belt;
    SolarSystem.Asteroid Rock(string id, double x, double y)
    {
        var rock = new SolarSystem.Asteroid { strID = id };
        rock.objSS.bBOLocked = true; rock.objSS.strBOPORShip = "belt"; rock.objSS.vBOOffsetx = x * km; rock.objSS.vBOOffsety = y * km;
        belt.Asteroids.Add(rock); CrewSim.system.dictStellarObjects[id] = rock; return rock;
    }
    var near = Rock("rock-near", 2, 0); var far = Rock("rock-far", 2, 400);
    Signal(s.Own, .5);
    var rockReading = NativeContactReader.Read(s.Own, "rock-near");
    Check(rockReading.Usable && near.objSS.Refreshes == 1 && Math.Abs(near.objSS.vPosx / km - 12) < 1e-9, "Asteroid marker qualifies from its field-refreshed position");
    Check(Math.Abs(s.Own.ElectronicSystems.LastRange - 12) < 1e-6 && s.Own.ElectronicSystems.LastVisibility == s.Own.fVisibilityRangeMod,
        "Marker range and the observer's own visibility feed the native signal");
    Signal(s.Own, .27); CrewSim.Selected!.Conditions.Add("SkillOpsSensors");
    Check(NativeContactReader.Read(s.Own, "rock-near").State == ContactState.Weak, "Asteroid markers use the fixed native threshold without a skill bonus");
    Signal(s.Own, .9); belt.dXReal = 2000 * km;
    Check(NativeContactReader.Read(s.Own, "rock-near").State == ContactState.Weak, "Beyond 1,000 km an asteroid marker is only a partial contact");
    belt.dXReal = 10 * km; s.Own.ElectronicSystems.On = false;
    Check(NativeContactReader.Read(s.Own, "rock-near").State == ContactState.NoSensors, "No operating sensors: no asteroid track");
    s.Own.ElectronicSystems.On = true; CrewSim.system.aBOs["moon"] = new BodyOrbit { Blocks = true };
    Check(NativeContactReader.Read(s.Own, "rock-near").State == ContactState.Occluded, "A celestial body hides an asteroid marker");
    CrewSim.system.aBOs.Remove("moon");
    Check(NativeContactReader.Read(s.Own, "missing-rock").State == ContactState.Unavailable, "Unknown identity is unavailable");

    int stellarReads = Ostranauts.Ships.Sensors.ShipSignature.StellarReads;
    var hazards = new System.Collections.Generic.List<SensedObject>(); NativeHazards.Asteroids(s.Own, default, new NavVector(20000, 0), 1000, null, hazards);
    Check(hazards.Count == 1 && hazards[0].Id == "rock-near" && hazards[0].Reading.Usable, "A rock on the leg is returned with its own contact reading");
    Check(Ostranauts.Ships.Sensors.ShipSignature.StellarReads - stellarReads == 1, "Distant rocks are skipped before any sensor read");
    NativeHazards.Asteroids(s.Own, default, new NavVector(20000, 0), 1000, "rock-near", hazards);
    Check(hazards.Count == 0, "The flight's own asteroid target is not its own hazard");

    // 29 September 2026 pass (FF5): within one open step the same contact is read natively once; a sensor
    // switch invalidates the shared readings; outside a step every read is fresh.
    Signal(s.Own, 1); int shipReads = Ostranauts.Ships.Sensors.ShipSignature.Reads;
    NativeContactReader.Read(s.Own, "target"); NativeContactReader.Read(s.Own, "target");
    Check(Ostranauts.Ships.Sensors.ShipSignature.Reads - shipReads == 2 && !NativeContactReader.Sharing, "Outside a step every contact read is fresh");
    NativeContactReader.BeginStep(); shipReads = Ostranauts.Ships.Sensors.ShipSignature.Reads;
    var shared = NativeContactReader.Read(s.Own, "target"); NativeContactReader.Read(s.Own, "target"); NativeContactReader.Read(s.Own, "target");
    Check(shared.Usable && Ostranauts.Ships.Sensors.ShipSignature.Reads - shipReads == 1, "Within a step the same contact is read natively once");
    Signal(s.Own, .1);
    Check(NativeContactReader.Read(s.Own, "target").Usable, "The shared reading holds for the step");
    NativeContactReader.Invalidate();
    Check(!NativeContactReader.Read(s.Own, "target").Usable, "A sensor switch invalidates the shared readings");
    NativeContactReader.EndStep(); Signal(s.Own, 1);
    Check(NativeContactReader.Read(s.Own, "target").Usable && !NativeContactReader.Sharing, "After the step reads are fresh again");

    GUIOrbitDraw.CrossHairTarget = new() { stellarObj = near };
    s.Service.Engage(s.Console);
    Check(AutoNavCore.Engaged && AutoNavCore.EngagedTarget?.ShipId == "rock-near" && Read(s.Console).TargetId == "rock-near",
        "Fly accepts a sensed asteroid marker and saves its identity: " + s.Service.StatusForTest);
    var hub = s.Service.ReadHub(s.Console, "navigation");
    Check(hub.TargetId == "rock-near" && hub.RangeKM != null && !hub.CanApproachDock,
        "Hub shows live asteroid geometry without offering docking");
    s.Service.Disengage("test");
    Signal(s.Own, .1); GUIOrbitDraw.CrossHairTarget = new() { stellarObj = far };
    s.Service.Engage(s.Console);
    Check(!AutoNavCore.Engaged, "A weak asteroid track cannot start a flight");
}
Console.WriteLine($"{checks} checks including asteroid sensing passed.");

// Selective sensor engagement (owner direction, 28 September 2026): the real guidance service and
// assist against the native sensor-control boundary. Notices are recorded, not displayed.
{
    void Fresh() { NativeSensorControl.Reset(); Plugin.AutoEngageSensors.Value = SensorAutoEngage.All; StarSystem.fEpoch = 0; }
    NativeSensorControl.FakeSensor Sensor(string key) => NativeSensorControl.Sensors.First(s => s.Key == key);
    void Settle(NavigationService service, double seconds) { service.UpdatePersistence(); StarSystem.fEpoch += seconds; service.UpdatePersistence(); }

    var a = Setup(); Fresh();
    NativeSensorControl.Fit("IsSensorIR", "Infrared", .5); NativeSensorControl.Fit("IsSensorRadar", "Radar", 2, emits: true);
    Signal(a.Own, .1);
    a.Service.ReadHub(a.Console, "navigation"); a.Service.ReadInstruments(a.Console);
    Check(NativeSensorControl.Surveys == 0 && !NativeSensorControl.Sensors.Any(s => s.On), "Displays never switch sensors on");
    a.Service.Engage(a.Console);
    Check(AutoNavCore.Engaged, "A weak target is restored by switching sensors on before Fly is refused: " + a.Service.StatusForTest);
    Check(Sensor("IsSensorIR").On && !Sensor("IsSensorRadar").On, "Non-emitting infrared is enough, so radar stays off");
    var notice = NativeSensorControl.Notices.Single();
    Check(notice.Caution && notice.Log.StartsWith("SensorAssist.engaged Infrared") && notice.Log.Contains("SensorAssist.reason_target") &&
        notice.Banner == "SensorAssist.banner Infrared", "The player is warned in the crew log and on the nav map");
    Check(a.Service.ReadHub(a.Console, "navigation").Restriction.StartsWith("SensorAssist.in_use Infrared"), "The hub keeps a steady line while Auto Nav's sensors stay on");
    int steering = AutoNavCore.SteeringCalls;
    a.Service.Tick(a.Own.objSS, 1, false);
    Check(AutoNavCore.SteeringCalls == steering + 1 && NativeSensorControl.Notices.Count == 1, "The restored track guides without further switching or warnings");

    // A native refresh a frame after the switch is waited out, within a finite budget.
    a.Own.bCheckSensors = true; steering = AutoNavCore.SteeringCalls;
    a.Service.Tick(a.Own.objSS, 1, false);
    Check(AutoNavCore.Engaged && AutoNavCore.SteeringCalls == steering && a.Service.StatusForTest == "SensorAssist.settling",
        "A sensor refresh right after switching holds guidance instead of suspending");
    a.Own.bCheckSensors = false; a.Service.Tick(a.Own.objSS, 1, false);
    Check(AutoNavCore.SteeringCalls == steering + 1, "Guidance continues once the refresh completes");
    a.Own.bCheckSensors = true;
    for (int i = 0; i <= NavigationService.SensorSettleChecks && AutoNavCore.Engaged; i++) a.Service.Tick(a.Own.objSS, 1, false);
    Check(!AutoNavCore.Engaged && Read(a.Console).Mode == SavedFlightMode.Suspended, "A refresh that never completes still suspends the flight");
    a.Own.bCheckSensors = false;

    // Suspended flights keep their sensors; stopping switches off only Auto Nav's own.
    Settle(a.Service, 100);
    Check(Sensor("IsSensorIR").On, "Sensors stay on while a flight is suspended so Resume can reacquire");
    a.Service.ResumeSaved(a.Console);
    Check(AutoNavCore.Engaged && NativeSensorControl.Notices.Count == 1, "Resume reacquires with the sensors already on");
    a.Service.Disengage("stop");
    a.Service.UpdatePersistence(); StarSystem.fEpoch += NavigationService.SensorReleaseGraceSeconds / 2; a.Service.UpdatePersistence();
    Check(Sensor("IsSensorIR").On, "A short grace keeps sensors through controller handoffs");
    StarSystem.fEpoch += NavigationService.SensorReleaseGraceSeconds; a.Service.UpdatePersistence();
    var released = NativeSensorControl.Notices.Last();
    Check(!Sensor("IsSensorIR").On && !released.Caution && released.Banner == null && released.Log == "SensorAssist.released Infrared",
        "When Auto Nav finishes it switches off its own sensors and says so");
    Check(!a.Service.ReadHub(a.Console, "navigation").Restriction.Contains("SensorAssist.in_use"), "The hub line clears with the sensors");

    // Sensors the player already had on are never claimed or switched off.
    var b = Setup(); Fresh();
    var playerIr = NativeSensorControl.Fit("IsSensorIR", "Infrared", .2); playerIr.On = true;
    NativeSensorControl.Fit("IsSensorOptical", "Optical", .3);
    Signal(b.Own, .05, .2);
    b.Service.Engage(b.Console);
    Check(AutoNavCore.Engaged && Sensor("IsSensorOptical").On && Sensor("IsSensorOptical").LeaseHolder == "console" && playerIr.LeaseHolder == null,
        "Only switched-off sensors are candidates: " + b.Service.StatusForTest);
    b.Service.Disengage("stop"); Settle(b.Service, 1); Settle(b.Service, NavigationService.SensorReleaseGraceSeconds);
    Check(playerIr.On && !Sensor("IsSensorOptical").On, "The player's own sensor stays on after Auto Nav switches its own off");

    // Emitters only when passive sensors cannot help; the Passive and Off settings are honoured.
    var c = Setup(); Fresh();
    NativeSensorControl.Fit("IsSensorIR", "Infrared", .05); NativeSensorControl.Fit("IsSensorRadar", "Radar", 2, emits: true);
    Signal(c.Own, .1);
    Plugin.AutoEngageSensors.Value = SensorAutoEngage.Off; c.Service.Engage(c.Console);
    Check(!AutoNavCore.Engaged && NativeSensorControl.Surveys == 0 && c.Service.StatusForTest == "Sensors.Weak", "Off never switches sensors");
    Plugin.AutoEngageSensors.Value = SensorAutoEngage.Passive; c.Service.Engage(c.Console);
    Check(!AutoNavCore.Engaged && !NativeSensorControl.Sensors.Any(s => s.On) && NativeSensorControl.Notices.Count == 0,
        "Passive never uses radar, and switches nothing on that cannot restore the track");
    Plugin.AutoEngageSensors.Value = SensorAutoEngage.All; c.Service.Engage(c.Console);
    Check(AutoNavCore.Engaged && Sensor("IsSensorRadar").On && !Sensor("IsSensorIR").On, "Radar alone is used when passive sensors are not enough");
    var emitting = NativeSensorControl.Notices.Single();
    Check(emitting.Log.StartsWith("SensorAssist.engaged_emitting SensorAssist.emitting_name Radar") && emitting.Banner!.StartsWith("SensorAssist.banner_emitting"),
        "Emitting sensors are named in both warnings");

    // The player's own switch wins for the rest of the operation.
    var d = Setup(); Fresh(); NativeSensorControl.Fit("IsSensorIR", "Infrared", .5); Signal(d.Own, .1);
    d.Service.Engage(d.Console);
    NativeSensorControl.PlayerSwitch(d.Own, "IsSensorIR", false);
    d.Service.Tick(d.Own.objSS, 1, false);
    Check(!AutoNavCore.Engaged && !Sensor("IsSensorIR").On && Read(d.Console).Mode == SavedFlightMode.Suspended && NativeSensorControl.Notices.Count == 1,
        "A sensor the player switched off is not switched back on during that flight");
    d.Service.ResumeSaved(d.Console);
    Check(!AutoNavCore.Engaged && !Sensor("IsSensorIR").On, "Resume continues the same operation and keeps the player's choice");
    d.Service.Command(new[] { "phobosnav", "forget" }, out _);
    Settle(d.Service, 1); Settle(d.Service, NavigationService.SensorReleaseGraceSeconds);
    Check(Sensor("IsSensorIR").DeclinedBy == null && NativeSensorControl.Notices.Count == 1, "Ending the work clears the player's decline without switching anything");
    d.Service.Engage(d.Console);
    Check(AutoNavCore.Engaged && Sensor("IsSensorIR").On, "A new flight may use it again, with a new warning");

    var e = Setup(); Fresh();
    var ownIr = NativeSensorControl.Fit("IsSensorIR", "Infrared", .5); ownIr.On = true; NativeSensorControl.Fit("IsSensorOptical", "Optical", .5);
    Signal(e.Own, .5);
    e.Service.Engage(e.Console);
    NativeSensorControl.PlayerSwitch(e.Own, "IsSensorIR", false);
    e.Service.Tick(e.Own.objSS, 1, false);
    Check(AutoNavCore.Engaged && !ownIr.On && ownIr.DeclinedBy == "console" && Sensor("IsSensorOptical").On,
        "Switching off one's own sensor mid-flight is respected; Auto Nav uses another one instead");

    // Notes from an earlier session are reconciled after loading.
    var g = Setup(); Fresh(); NativeSensorControl.Fit("IsSensorIR", "Infrared", .5); Signal(g.Own, .1);
    g.Service.Engage(g.Console); g.Service.Command(new[] { "phobosnav", "forget" }, out _);
    g.Service.WorldChanging(); g.Service.WorldLoaded(); g.Service.UpdatePersistence();
    Check(Sensor("IsSensorIR").On, "Loading never switches sensors immediately");
    Settle(g.Service, 1); Settle(g.Service, NavigationService.SensorReleaseGraceSeconds);
    Check(!Sensor("IsSensorIR").On, "Sensors left on by Auto Nav in an earlier session are switched off once no work remains");
    var h = Setup(); Fresh(); NativeSensorControl.Fit("IsSensorIR", "Infrared", .5); Signal(h.Own, .1);
    h.Service.Engage(h.Console); h.Service.WorldChanging(); h.Service.WorldLoaded(); h.Service.UpdatePersistence();
    Settle(h.Service, 1); Settle(h.Service, NavigationService.SensorReleaseGraceSeconds);
    Check(AutoNavCore.Engaged && Sensor("IsSensorIR").On, "A flight resumed after loading keeps its sensors");
}
Console.WriteLine($"{checks} checks including selective sensor engagement passed.");
