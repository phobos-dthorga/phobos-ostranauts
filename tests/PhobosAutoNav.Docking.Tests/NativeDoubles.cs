using System;
using System.Collections.Generic;
using System.Linq;
using PhobosAutoNav;
using PhobosAutoNav.Core;

internal sealed class CrewSim
{
    internal static CrewSim objInstance = new() { FinishedLoading = true };
    internal static CondOwner coPlayer = new();
    internal static StarSystem system = new();
    internal static bool Paused;
    internal bool FinishedLoading;
    internal static int AttachCalls;
    internal static Action? DuringAttach;
    internal static string? LastOwnPort, LastTargetPort;
    internal static void ResetTimeScale() { }
    internal static CondOwner GetSelectedCrew() => coPlayer;
    internal static Ship? DockShip(Ship own, string target, string portTarget, string portOwn, Clearance clearance)
    { AttachCalls++; DuringAttach?.Invoke(); LastOwnPort = portOwn; LastTargetPort = portTarget; own.Attached = true; return system.GetShipByRegID(target); }
}
internal sealed class StarSystem
{
    internal static double fEpoch;
    internal Dictionary<string, Ship> Ships = new();
    internal Ship? GetShipByRegID(string id) => Ships.TryGetValue(id, out var ship) ? ship : null;
    internal bool IsInAtmo(Ship ship) => ship.Atmosphere;
}
internal sealed class Ship
{
    internal string strRegID = "";
    internal ShipSitu objSS = new();
    internal bool bDestroyed = false, HideFromSystem = false, Attached = false, Moored = false, Towed = false, Atmosphere = false, Ground = false;
    internal bool Station = false;
    internal double RCSAccelMax = AutoNavCore.M_TO_AU;
    internal double DeltaVRemainingRCS = 100 * AutoNavCore.M_TO_AU;
    internal List<string> Ports = new();
    internal List<(string, string)> Pairs = new();
    internal int FitChecks;
    internal List<CondOwner> Items = new();
    internal Comms Comms = new();
    internal string PrimaryDockingPortID = "own";
    internal double LastX, LastY, LastTurn;
    internal bool IsDocked() => Attached;
    internal bool IsMoored() => Moored;
    internal bool TowBraceSecured(string id) => Towed;
    internal bool IsGroundStation() => Ground;
    internal bool IsStationHidden() => false;
    internal bool IsStation() => Station;
    internal bool IsSubStation() => false;
    internal bool IsDockedWith(Ship target) => Attached;
    internal List<string> GetOpenDockingPorts() => Ports;
    internal List<(string, string)> GetAvailableDockingPorts(Ship incoming, bool earlyOut)
    { if (earlyOut) throw new Exception("Must check all pairs for assigned port"); FitChecks++; return Pairs; }
    internal IEnumerable<CondOwner> GetCOs(object? filter, bool bSubObjects, bool bAllowDocked, bool bAllowLocked) => Items;
    internal void UnlockFromOrbit() { }
    internal void Maneuver(float x, float y, float turn, int noise, float dt) { LastX = x; LastY = y; LastTurn = turn; }
}
internal sealed class ShipSitu
{
    internal UnityEngine.Vector2 vAccRCS = default;
    internal double vPosx = 0, vPosy = 0, vVelX = 0, vVelY = 0;
    internal float fRot = 0, fW = 0;
    internal void ResetNavData() { }
}
internal sealed class Comms { internal Clearance? Clearance; }
internal sealed class Clearance { internal string TargetRegId = "target", DockID = "assigned", ClearanceType = "DOCK"; }
internal sealed class CondOwner
{
    internal string strID = "", Kind = "";
    internal bool bDestroyed = false;
    internal bool SoftwareDamaged = false;
    internal Ship ship = new();
    internal List<CondOwner> Items = new();
    internal Dictionary<string, Dictionary<string, string>> mapGUIPropMaps = new();
    internal bool HasCond(string name) => name == "IsInstalled" || name == "IsDamagedSoftware" && SoftwareDamaged;
    internal void ZeroCondAmount(string name) { }
    internal IEnumerable<CondOwner> GetCOsSafe(bool recursive) => Items;
}
internal sealed class JsonShipSitu
{
    internal UnityEngine.Vector2 vAccRCS, vAccIn;
    internal float fA;
}
namespace UnityEngine { internal struct Vector2 { internal double x = 0, y = 0; public Vector2() {} internal static Vector2 zero => default; } }
internal sealed class GUIDockSys
{
    internal static GUIDockSys? instance;
    internal CondOwner COSelf = new();
    internal bool bActive = true;
    internal static Signal DockEvent = new();
    internal int BrokenLocks;
    private void BreakAllLocks() { BrokenLocks++; }
    internal void CheckForCrimeIllegalSalvagingOKLG(string target, CondOwner player) { }
}
internal sealed class Signal { internal int Count; internal void Invoke(string target) { Count++; } }
internal static class CollisionManager
{
    internal static float GetCollisionDistanceAU(Ship own, Ship target) => (float)(200 * AutoNavCore.M_TO_AU);
}
internal static class GUIOrbitDraw
{
    internal sealed class Contact { internal Ship? Ship; }
    internal static Contact? CrossHairTarget;
}
namespace BepInEx.Bootstrap { internal static class Chainloader { internal static Dictionary<string, object> PluginInfos = new(); } }
namespace HarmonyLib
{
    internal static class AccessTools
    {
        internal static Type? TypeByName(string name) => null;
        internal static System.Reflection.PropertyInfo? Property(Type type, string name) => null;
        internal static bool MethodsAvailable = true;
        internal static System.Reflection.MethodInfo? Method(Type type, string name) => MethodsAvailable ?
            type.GetMethod(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) : null;
    }
}

namespace Ostranauts.Core { internal class MonoSingleton<T> where T : new() { internal static T Instance = new(); } }
namespace Ostranauts.Objectives { internal class ObjectiveTracker { internal void CheckObjective(string id) { } } }
namespace Ostranauts.ShipGUIs.MFD
{
    internal class MFDDockInfo { internal MFDDockInfo(int screen) { } }
    internal static class GUIMFDPageHost
    {
        internal const int DefaultCommsScreen = 0;
        internal static System.Action<MFDDockInfo>? OnRequestMFDChange = null;
    }
}
internal static class BeatManager { internal static void AutoSaveBeforePirateEncounter(Ship target) { } }
namespace PhobosAutoNav
{
    internal sealed class Setting<T> { internal T Value; internal Setting(T value) { Value = value; } }
    internal static class Plugin
    {
        internal static Setting<bool> Enabled = new(true), ResumeAfterLoad = new(true), FuelCheck = new(true), PreferTorch = new(false);
        internal static Setting<double> MaxFlightSimHours = new(48);
        internal static CoastSettings ReadCoastSettings() => new(3,10,.75,2);
    }
    internal static class Text { internal static string Get(string key, params object[] values) => key; }
    internal sealed class TargetRef
    {
        internal string ShipId = "target", DisplayName = "target";
        internal static TargetRef? FromShipId(string id) => CrewSim.system.GetShipByRegID(id) == null ? null : new() { ShipId = id };
    }
    internal static class AutoNavCore
    {
        internal const double M_TO_AU = 6.684587122268445E-12;
        internal static bool Engaged, Coasting = false, FuelAvailable = true, Busy;
        internal static Ship? EngagedPlayer;
        internal static double ElapsedSeconds;
        internal static CoastSettings FlightCoastSettings = new(3,10,.75,2);
        internal static bool FlightPrefersTorch = false;
        internal static string? LastResult;
        internal static void ResetStatics() { Engaged = false; EngagedPlayer = null; }
        internal static void EndFlight(Ship? own, string result) { own?.Maneuver(0,0,0,0,1); Engaged = false; LastResult = result; }
        internal static void RestoreFlight(Ship own, TargetRef target, FlightSnapshot snapshot)
        { EngagedPlayer = own; Engaged = true; ElapsedSeconds = snapshot.ElapsedSeconds; }
        internal static bool HasFuelForFlight(Ship own, TargetRef target, bool readOnly) => FuelAvailable;
        internal static void AdvanceDockingClock(double dt) => ElapsedSeconds += dt;
        internal static bool AutoDockBusy() => Busy;
        internal static bool TryReadApproach(Ship own, TargetRef target, double distance, out ApproachPlan plan, out double speed)
        { var other = CrewSim.system.GetShipByRegID(target.ShipId)!;
            double dx = other.objSS.vPosx-own.objSS.vPosx, dy = other.objSS.vPosy-own.objSS.vPosy;
            speed = 0; return ApproachRules.TryPlan(Math.Sqrt(dx*dx+dy*dy)/M_TO_AU/1000, distance, .2, out plan); }
    }
    internal sealed class TorchDouble { internal void Release() { } internal void Reset() { } }
    internal sealed partial class NavigationService
    {
        internal const string PursuitId = "PhobosNavModPursuit";
        internal FireControlController Fire = new();
        internal const string ModuleId = "PhobosNavModAutoNav";
        internal TorchDouble Torch { get; } = new();
        private readonly Action<string> log = _ => { };
        private CondOwner? console;
        private bool issuing;
        private string status = "";
        internal float Throttle { get; set; } = 1;
        internal string? HardwareFailure;
        internal string Diagnostic => status + issuing;
        private string? HardwareProblem(CondOwner? co) => HardwareFailure;
        private static string? AdmissionProblem(CondOwner co, TargetRef target, double km, double speed) => null;
        private static bool HasId(CondOwner co, string id) => co.Kind == id;
        private static bool ReadPreferences(CondOwner co, out FlightPreferences preferences) { preferences = new FlightPreferences(1500,0,1); return true; }
        internal bool FinishApproach() { AutoNavCore.EndFlight(console!.ship,"ARRIVED"); return QueueDockingHandoff(); }
        internal void Engage(CondOwner? co) => throw new NotSupportedException();
        internal void Disengage(string reason)
        { FinishSavedFlight(SavedFlightMode.Stopped); AutoNavCore.ResetStatics(); console?.ship.Maneuver(0,0,0,0,1); status = reason; }
    }
}
