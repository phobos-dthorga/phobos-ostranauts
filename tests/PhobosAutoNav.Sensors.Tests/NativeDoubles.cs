using System;
using System.Collections.Generic;
using System.Linq;
using PhobosAutoNav;
using PhobosAutoNav.Core;

// Native boundaries, not copies of the sensor formula or navigation service.
internal sealed class CrewSim
{
    internal static void ResetTimeScale() { }
    internal static CrewSim objInstance = new() { FinishedLoading = true };
    internal static CondOwner coPlayer = new();
    internal static CondOwner? Selected;
    internal static StarSystem system = new();
    internal static bool Paused;
    internal bool FinishedLoading;
    internal static CondOwner? GetSelectedCrew() => Selected;
}
internal sealed class StarSystem
{
    internal static double fEpoch;
    internal bool NoWake;
    internal bool IsWithinNoWakeRangeOfAnyStation(ShipSitu situ, double epoch) => NoWake;
    internal Dictionary<string, Ship> Ships = new();
    internal Dictionary<string, BodyOrbit> aBOs = new();
    internal Ship? GetShipByRegID(string id) => Ships.TryGetValue(id, out var ship) ? ship : null;
    internal bool IsInAtmo(Ship ship) => false;
    internal static bool IsLOSBlockedByBO(BodyOrbit body, Ship observer, ShipSitu target) => body.Blocks;
}
internal sealed class BodyOrbit { internal bool Blocks, IsAsteroidField; internal int nDrawFlagsBody; }
internal sealed class Ship
{
    internal void UnlockFromOrbit() { }
    internal CondOwner? Reactor;
    internal double fShallowFusionRemain = 3600;
    internal Dictionary<string,string> ReactorProps = new() { ["slidFlow"]="0", ["slidCycle"]="0", ["knobRatio"]="0", ["bNWZ"]="false" };
    internal int ReactorWrites;
    internal string GetReactorGPMValue(string key) => ReactorProps.TryGetValue(key,out var value) ? value : "";
    internal void SetReactorGPMValue(string key,string value) { if(AutoNavCore.Engaged) throw new Exception("Automation still owns flight during manual write"); ReactorWrites++; ReactorProps[key]=value; }
    internal bool IsMoored() => false;
    internal int Maneuvers; internal float LastRotation;
    internal bool FailManeuver;
    internal void Maneuver(float x, float y, float r, float throttle, float dt) { Maneuvers++; if (FailManeuver) throw new Exception("Native maneuver failure"); LastRotation = r; }

    internal bool IsUsingTorchDrive;
    internal void SetThrust(double value) { IsUsingTorchDrive = value > 0; aWPs.Clear(); }
    internal void ClearShipTarget() { }
    internal string strRegID = "", publicName = "";
    internal ShipSitu objSS = new();
    internal bool bDestroyed, HideFromSystem, Hidden, bCheckPower, bCheckSensors;
    internal float fVisibilityRangeMod = 1;
    internal int RCSCount = 4;
    internal double RCSAccelMax = 1;
    internal object? shipStationKeepingTarget = null;
    internal List<object> aWPs = new();
    internal List<CondOwner> Items = new();
    internal Ostranauts.Ships.Sensors.ElectronicSystems ElectronicSystems = new();
    internal double RangeKM = 80, Thrust;
    internal bool IsStationHidden() => Hidden;
    internal bool IsDocked() => false;
    internal double GetRangeTo(Ship other) => RangeKM * AutoNavCore.KM_TO_AU;
    internal double GetRCSRemain() => 100;
    internal IEnumerable<CondOwner> GetCOs(object? filter, bool bSubObjects, bool bAllowDocked, bool bAllowLocked)
    { if (bSubObjects || bAllowDocked) throw new Exception("Cross-ship discovery"); return Items; }
}
internal sealed class ShipSitu { internal double vPosx, vPosy, vVelX, vVelY; internal float fRot, fW = 0; internal UnityEngine.Vector2 vAccIn, vAccRCS = default; internal void ResetNavData() { } }
internal static class CollisionManager { internal static double GetCollisionDistanceAU(Ship own, Ship target) => 200 * AutoNavCore.M_TO_AU; }
internal sealed class PowerReading { internal double PowerConnected = 12; }
public sealed class CondOwner
{
    internal PowerReading? Pwr;
    internal int Messages, ConditionWrites;
    internal double GetCondAmount(string key) => 11;
    internal void AddCondAmount(string key,double amount) { ConditionWrites++; Conditions.Add(key); }
    internal void LogMessage(string message,string mood,string source) { Messages++; }

    internal string strID = "", strName = "", strCODef = "";
    internal bool bDestroyed;
    internal Ship ship = null!;
    internal CondOwner? objCOParent = null;
    internal List<CondOwner> Items = new();
    internal HashSet<string> Conditions = new() { "IsInstalled", "IsPowered" };
    internal Dictionary<string, Dictionary<string, string>> mapGUIPropMaps = new();
    internal bool HasCond(string name) => Conditions.Contains(name);
    internal IEnumerable<CondOwner> GetCOsSafe(bool recursive) => Items;
    internal CondOwner? AddCO(CondOwner co, bool bEquip, bool bOverflow, bool bIgnoreLocks) { Items.Add(co); return null; }
    internal void Destroy() => bDestroyed = true;
}
internal sealed class JsonShipSitu
{
    internal UnityEngine.Vector2 vAccRCS, vAccIn;
    internal float fA;
}
namespace UnityEngine { internal struct Vector2 { internal float x,y; internal double magnitude => Math.Sqrt(x*x+y*y); internal static Vector2 zero => default; } }
internal sealed class GUILamp { internal int State; }
internal sealed class NativeToggle { private bool state; internal bool isOn { get => state; set { state = value; Events++; } } internal int Events; internal void SetIsOnWithoutNotify(bool value) => state = value; }
internal sealed class GUIOrbitDraw
{
    internal GUILamp ledWLock = new();
    internal bool HoldingThrustActive => ledWLock.State == 3;
    internal NativeToggle chkStationKeeping = new();
    internal Ostranauts.ShipGUIs.NavStation.NavModCoursePlot Course = new();
    internal T[] GetComponentsInChildren<T>(bool inactive) => new[] { (T)(object)Course };
    internal static GUIOrbitDraw Instance = new();
    internal static bool Open = true;
    internal static CondOwner? Console;
    internal sealed class Contact { internal Ship? Ship; }
    internal static Contact? CrossHairTarget;
    internal static bool IsOpen() => Open;
    internal CondOwner? COSelfBase() => Console;
}
internal sealed class AIShip { internal string ActiveCommandName = ""; }
internal static class AIShipManager
{
    internal static AIShip? Current;
    internal static AIShip? GetAIShipByRegID(string id) => Current;
    internal static void UnregisterShip(Ship ship) => Current = null;
}
internal static class DataHandler
{
    internal static object? GetCOOverlay(string id) => null;
    internal static CondOwner? GetCondOwner(string id) => null;
}
namespace BepInEx.Bootstrap { internal static class Chainloader { internal static Dictionary<string, object> PluginInfos = new(); } }
namespace HarmonyLib
{
    internal static class AccessTools
    {
        internal static System.Reflection.FieldInfo? Field(Type type, string name) => type.GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        internal static Type? TypeByName(string name) => null;
        internal static System.Reflection.PropertyInfo? Property(Type type, string name) => null;
    }
}
namespace Ostranauts.Ships.Sensors
{
    internal sealed class ShipSignature
    {
        internal static int Reads;
        internal ShipSignature(Ship target, Ship observer) { Reads++; }
    }
    internal sealed class ElectronicSystems
    {
        internal List<double> aElectronicSystems = new() { 1 };
        internal float DetectionThreshold = .001f; // Must not leak another UI operator's threshold.
        internal bool On = true, Throw;
        internal int Reads;
        internal double LastRange;
        internal float LastVisibility;
        internal bool HasAnySensorOn() => On && aElectronicSystems.Count > 0;
        internal double GetSignatureStrength(ShipSignature signature, double km, float visibility)
        {
            Reads++; LastRange = km; LastVisibility = visibility;
            if (Throw) throw new InvalidOperationException("Unreadable native sensor data");
            return aElectronicSystems.Sum(); // Test inputs stand in for native contribution calculations.
        }
    }
}
namespace PhobosAutoNav
{
    internal sealed class Setting<T> { internal T Value; internal FakeConfig ConfigFile = new(); internal Setting(T value) { Value = value; } }
    internal sealed class FakeConfig { internal void Save() { } }
    internal static class Plugin
    {
        internal const string Id = "test", Version = "test";
        internal static NavigationService Service = null!;
        internal static Setting<bool> Enabled = new(true), FuelCheck = new(true), ResumeAfterLoad = new(true), PreferTorch = new(true);
        internal static Setting<float> DefaultCruiseMS = new(100), DefaultArriveSpeedMS = new(0), DefaultArriveKM = new(1),
            RotAccelMax = new(.5f), RotSpeedMax = new(.6f), MaximumStepSeconds = new(10), ArrivalSpeedTolerance = new(.5f), TorchMaximumG = new(1), TorchMinimumCorrectionMS = new(5), MaxFlightSimHours = new(48);
        internal static CoastSettings ReadCoastSettings() => new(3,10,.75,2);
    }
    internal static class EquipmentContent { internal static string Status => "ready"; }
    internal static class Text { internal static string Get(string key, params object[] values) => key + (values.Length == 0 ? "" : " " + string.Join(";",values)); }
    internal sealed class TorchDriveController
    {
        internal static bool Ready(CondOwner core) => core.HasCond("IsReadyFusion") && !core.HasCond("IsDamaged");
        internal ContactReading? ContactLoss;
        internal string Reason => "Torch.rcs";
        internal bool ControlsChanged => false;
        internal int Releases;
        internal static bool ThrustRequested(Ship ship) => ship.IsUsingTorchDrive || ship.GetReactorGPMValue("slidCycle") != "0";
        internal bool Owns(Ship ship) => false;
        internal bool ChangedByPilot(Ship ship, string key, string value) => false;
        internal void YieldToPilot() { }
        internal void Cut() { }
        internal void Reset() { ContactLoss = null; }
        internal void Release() { Releases++; ContactLoss = null; }
    }
    internal sealed class TargetRef
    {
        internal string ShipId = "", DisplayName = "";
        internal static int Resolves;
        internal static TargetRef? FromShipId(string id) => CrewSim.system.GetShipByRegID(id) is Ship ship ?
            new() { ShipId = id, DisplayName = ship.publicName } : null;
        internal static TargetRef? FromCrossHair() => GUIOrbitDraw.CrossHairTarget?.Ship is Ship ship ? FromShipId(ship.strRegID) : null;
        internal bool Resolve(out double x, out double y, out double vx, out double vy)
        { Resolves++; x = y = vx = vy = 0; return true; }
    }
    internal static class AutoNavCore
    {
        internal const double M_TO_AU = 6.684587122268445E-12, KM_TO_AU = 6.684587122268445E-09;
        internal enum Phase { Idle, Align, Accel, Cruise, Coast, Decel, Arrive }
        internal static Phase CurrentPhase = Phase.Idle;
        internal static string PhaseName => "phase";
        internal static bool Engaged, Coasting, Following, FaceTarget;
        internal static double? WeaponHeading;
        internal static bool ControlLimited => false;
        internal static double PredictionHorizon => 2;
        internal static double PredictionError => 0;
        internal static Ship? EngagedPlayer;
        internal static TargetRef? EngagedTarget;
        internal static string? LastResult;
        internal static double ElapsedSeconds, CruiseAU, ArrSpdAU, ArriveAU = KM_TO_AU;
        internal static CoastSettings FlightCoastSettings;
        internal static bool FlightPrefersTorch;
        internal static int SteeringCalls, ApproachReads;
        internal static void ResetStatics() { Engaged = false; EngagedPlayer = null; EngagedTarget = null; }
        internal static bool AdmissionSafe = true;
        internal static bool TryReadAdmission(Ship own, TargetRef target, double km, double speed, double throttle, double step, out BrakingRoom room)
        { room = new BrakingRoom(AdmissionSafe ? 1 : 10000, 100); return true; }
        internal static bool AutoDockBusy() => false;
        internal static bool TryReadApproach(Ship own, TargetRef target, double requested, out ApproachPlan plan, out double speed)
        { ApproachReads++; speed = 10; return ApproachRules.TryPlan(80, requested, 0, out plan); }
        internal static bool HasFuelForFlight(Ship own, TargetRef target, bool readOnly = false) => true;
        internal static void BeginFlight(Ship own, TargetRef target, CoastSettings coast, bool torch)
        { Engaged = true; EngagedPlayer = own; EngagedTarget = target; ElapsedSeconds = 0; FlightCoastSettings = coast; FlightPrefersTorch = torch; }
        internal static void RestoreFlight(Ship own, TargetRef target, FlightSnapshot flight)
        { BeginFlight(own,target,flight.Coast,flight.PreferTorch); ElapsedSeconds = flight.ElapsedSeconds; Coasting = flight.Coasting; Following = flight.IsFollowing; }
        internal static void EndFlight(Ship? own, string result)
        { Plugin.Service.Torch.Release(); if (own != null) own.Thrust = 0; LastResult = result; Engaged = false; }
        internal static void SteerFlight(Ship? own, TargetRef? target, double dt)
        { SteeringCalls++; own!.Thrust = 1; ElapsedSeconds += dt; }
    }
    // Docking orchestration has its own suite using the real docking service.
    internal sealed partial class NavigationService
    {
        private bool dockHolding => false;
        private bool DockingActive => false;
        internal void Dock(CondOwner? co) => throw new NotSupportedException();
        private void ResumeDocking(CondOwner co, TargetRef target, FlightSnapshot snapshot) => throw new NotSupportedException();
        private void ResumeApproachDock(CondOwner co, TargetRef target, FlightSnapshot snapshot) => throw new NotSupportedException();
        private bool QueueDockingHandoff() => combinedHandoffPending;
        internal void ApproachDock(CondOwner? co) => throw new NotSupportedException();
    }
}

namespace Ostranauts.ShipGUIs.NavStation { internal static class NavModTorchDrive { internal static float GetLimiterSafetyMax(Ship ship) => .5f; } }
namespace PhobosAutoNav {
    internal static class DockingAdapter {
        internal static bool HasFuel(Ship own, Ship target, float throttle) => true;
        internal static string? Check(Ship own, Ship? target, string ownPort, string targetPort, bool checkFit) => null;
        internal static string? SelectPorts(Ship own, Ship target, out string ownPort, out string targetPort) { ownPort="own";targetPort="assigned";return null; }
    }
}

namespace Ostranauts.ShipGUIs.NavStation { internal sealed class NavModCoursePlot { internal NativeToggle chkEngage = new(); } }
