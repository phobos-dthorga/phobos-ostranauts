using System;
using System.Linq;
using PhobosAutoNav;
using PhobosAutoNav.Core;
using Phobos.Ostranauts.Framework.Persistence;

int checks = 0;
void Check(bool ok, string message) { checks++; if (!ok) throw new Exception(message); }
(NavigationService Service, CondOwner Console, Ship Own, Ship Target) Setup()
{
    AIShipManager.Current = null; BepInEx.Bootstrap.Chainloader.PluginInfos.Clear(); GUIOrbitDraw.Instance = new();
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
Check(!f.Service.Fire.Permitted && f.Service.Fire.Owns(f.Console.strID),"Large simulation step revokes but retains hold");
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
