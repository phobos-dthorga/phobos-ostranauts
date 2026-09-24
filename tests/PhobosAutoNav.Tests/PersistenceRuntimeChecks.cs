using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Persistence;
using PhobosAutoNav;
using PhobosAutoNav.Core;

// Executes the production persistence service with a tiny native-world double.
// This verifies orchestration, not Unity loading, real power checks or flight physics.
internal static class PersistenceRuntimeChecks
{
    internal static void Run(Action<bool, string> check)
    {
        (NavigationService Service, CondOwner Console, FlightSnapshot Snapshot) Setup()
        {
            AutoNavCore.ResetStatics(); AutoNavCore.FuelAvailable = true; TargetRef.Available = true;
            Plugin.ResumeAfterLoad.Value = true; Plugin.Enabled.Value = true;
            Plugin.MaxFlightSimHours.Value = 48;
            CrewSim.objInstance = new CrewSim { FinishedLoading = false };
            var ship = new Ship { strRegID = "ship" };
            CrewSim.coPlayer = new CondOwner { strID = "player", ship = ship };
            var co = new CondOwner { strID = "console", ship = ship };
            co.Items.Add(new CondOwner { strID = "module", Kind = NavigationService.ModuleId, ship = ship });
            ship.Items.Add(co);
            var snapshot = new FlightSnapshot { ConsoleId = co.strID, ModuleId = "module", ShipId = "ship", PlayerId = "player", TargetId = "target",
                CruiseMS = 100, ArrivalKM = 1, ElapsedSeconds = 1234, Coast = new CoastSettings(3,10,.75,2), Coasting = true, Mode = SavedFlightMode.Active };
            new ObjectStateStore(co.mapGUIPropMaps, FlightSnapshot.StoreName, co.strID, 1).TryWrite(snapshot.Encode());
            return (new NavigationService(), co, snapshot);
        }
        FlightSnapshot Read(CondOwner co)
        {
            new ObjectStateStore(co.mapGUIPropMaps, FlightSnapshot.StoreName, co.strID, 1).Read(out var data);
            FlightSnapshot.TryDecode(data, out var snapshot); return snapshot;
        }
        void Load(NavigationService service)
        { service.WorldChanging(); service.WorldLoaded(); CrewSim.objInstance.FinishedLoading = true; service.UpdatePersistence(); }

        var f = Setup();
        f.Service.WorldChanging(); f.Service.WorldLoaded(); f.Service.UpdatePersistence();
        check(!AutoNavCore.Engaged, "Early native finished event cannot resume while loading");
        CrewSim.objInstance.FinishedLoading = true; f.Service.UpdatePersistence();
        check(AutoNavCore.Engaged && AutoNavCore.ElapsedSeconds == 1234 && AutoNavCore.Coasting, "Complete load restores elapsed time and coasting latch");
        check(AutoNavCore.FuelReadOnly, "Restoration fuel check cannot advance target physics");
        var nativeSave = new JsonShipSitu();
        NavigationService.PrepareSavedPhysics(new ShipSitu(), nativeSave);
        check(nativeSave.fA == 2 && nativeSave.vAccRCS.x == 3, "Other ships retain their own saved actuator data");
        NavigationService.PrepareSavedPhysics(f.Console.ship.objSS, nativeSave);
        check(nativeSave.fA == 0 && nativeSave.vAccRCS.x == 0 && nativeSave.vAccIn.x == 0 && nativeSave.fW == 4 && nativeSave.velocity == 5,
            "Active ship save omits stale actuators while retaining motion");
        check(f.Console.ship.objSS.Acceleration == 6, "Sanitizing a saved copy never changes live acceleration");
        AutoNavCore.ElapsedSeconds += 7; f.Service.SaveProgressForTest();
        check(Read(f.Console).ElapsedSeconds == 1241, "Further progress updates the native map");
        f.Service.WorldChanging();
        check(!AutoNavCore.Engaged && Read(f.Console).Mode == SavedFlightMode.Active, "World transition clears references without cancelling saved intent");
        nativeSave = new JsonShipSitu(); NavigationService.PrepareSavedPhysics(f.Console.ship.objSS, nativeSave);
        check(nativeSave.fA == 2, "Uncontrolled ships are not sanitized after world transition");
        CrewSim.coPlayer.ship = new Ship { strRegID = "other-save" };
        f.Service.WorldLoaded(); f.Service.UpdatePersistence();
        check(!AutoNavCore.Engaged, "Unrelated world does not inherit the previous flight");

        f = Setup(); Plugin.ResumeAfterLoad.Value = false; Load(f.Service);
        check(!AutoNavCore.Engaged && Read(f.Console).Mode == SavedFlightMode.Suspended, "Manual-resume preference persists a suspended state");
        f.Service.ResumeSaved(f.Console);
        check(AutoNavCore.Engaged, "Explicit resume does not need a crosshair target");
        f.Service.Stop(f.Console, "pilot"); Load(f.Service);
        check(!AutoNavCore.Engaged && Read(f.Console).Mode == SavedFlightMode.Stopped, "Stop cannot resurrect across reload");

        f = Setup(); f.Console.Problem = "power"; Load(f.Service);
        check(!AutoNavCore.Engaged && Read(f.Console).Mode == SavedFlightMode.Suspended, "Unavailable hardware suspends once");
        f.Console.Problem = null; f.Service.UpdatePersistence();
        check(!AutoNavCore.Engaged, "Restoring power does not trigger a delayed automatic takeover");
        f.Service.ResumeSaved(f.Console); check(AutoNavCore.Engaged, "Explicit retry after repair works");

        f = Setup(); f.Console.Items[0].strID = "replacement"; Load(f.Service);
        check(!AutoNavCore.Engaged, "Replacement module cannot assume the original flight");
        f = Setup(); TargetRef.Available = false; Load(f.Service);
        check(!AutoNavCore.Engaged && Read(f.Console).TargetId == "target", "Missing target is retained, never substituted");
        f = Setup(); Plugin.MaxFlightSimHours.Value = .1; Load(f.Service);
        check(!AutoNavCore.Engaged && Read(f.Console).ElapsedSeconds == 1234, "Reload cannot bypass the cumulative timeout");
        f = Setup(); AutoNavCore.FuelAvailable = false; Load(f.Service);
        check(!AutoNavCore.Engaged && Read(f.Console).Mode == SavedFlightMode.Suspended, "Insufficient estimated fuel suspends without thrust");

        f = Setup();
        var original = f.Console.mapGUIPropMaps["PhobosState.AutoNav.Flight"];
        original["schema"] = "99"; Load(f.Service);
        check(!AutoNavCore.Engaged && ReferenceEquals(original, f.Console.mapGUIPropMaps["PhobosState.AutoNav.Flight"]), "Unknown record is retained byte-for-value");
        f = Setup(); f.Console.mapGUIPropMaps.Clear(); Load(f.Service);
        check(!AutoNavCore.Engaged && f.Console.mapGUIPropMaps.Count == 0, "Old saves remain idle without creating invented flights");
        f = Setup(); Load(f.Service); AutoNavCore.Engaged = false; AutoNavCore.LastResult = "ARRIVED"; f.Service.SaveProgressForTest(); Load(f.Service);
        check(!AutoNavCore.Engaged && Read(f.Console).Mode == SavedFlightMode.Arrived, "Arrival remains complete on reload");
        f = Setup();
        var second = new CondOwner { strID = "console-2", ship = f.Console.ship };
        f.Snapshot.ConsoleId = second.strID;
        new ObjectStateStore(second.mapGUIPropMaps, FlightSnapshot.StoreName, second.strID, 1).TryWrite(f.Snapshot.Encode());
        f.Console.ship.Items.Add(second); Load(f.Service);
        check(!AutoNavCore.Engaged && Read(f.Console).Mode == SavedFlightMode.Suspended && Read(second).Mode == SavedFlightMode.Suspended,
            "Competing saved controllers suspend all instead of arbitrarily choosing");
        f.Console.Problem = "power"; f.Service.ResumeSaved(f.Console);
        f.Service.ForgetForTest(second);
        check(Read(f.Console).Mode == SavedFlightMode.Suspended && second.mapGUIPropMaps.Count == 0,
            "Forgetting another console's state does not mutate the previous console");
        var retained = f.Console.mapGUIPropMaps["PhobosState.AutoNav.Flight"];
        CrewSim.coPlayer.ship = new Ship { strRegID = "another" };
        f.Service.ResumeSaved(f.Console);
        check(!AutoNavCore.Engaged && ReferenceEquals(retained, f.Console.mapGUIPropMaps["PhobosState.AutoNav.Flight"]),
            "Resume cannot mutate a console aboard another ship");
    }
}

// Native boundaries only. The production persistence logic is linked above.
internal sealed class CrewSim
{
    internal static CrewSim objInstance = new();
    internal static CondOwner coPlayer = new();
    internal bool FinishedLoading;
}
internal sealed class Ship
{
    internal ShipSitu objSS = new();
    internal string strRegID = "";
    internal List<CondOwner> Items = new();
    internal IEnumerable<CondOwner> GetCOs(object? filter, bool bSubObjects, bool bAllowDocked, bool bAllowLocked)
    { if (bSubObjects || bAllowDocked) throw new Exception("Discovery expanded beyond owning ship"); return Items; }
}
internal sealed class ShipSitu { internal double Acceleration = 6; }
internal sealed class JsonShipSitu
{
    internal UnityEngine.Vector2 vAccRCS = new() { x = 3 };
    internal UnityEngine.Vector2 vAccIn = new() { x = 7 };
    internal float fA = 2, fW = 4;
    internal double velocity = 5;
}
namespace UnityEngine { internal struct Vector2 { internal float x; internal static Vector2 zero => default; } }
internal sealed class CondOwner
{
    internal string strID = "", Kind = "";
    internal bool bDestroyed = false;
    internal string? Problem;
    internal Ship ship = new();
    internal List<CondOwner> Items = new();
    internal Dictionary<string, Dictionary<string, string>> mapGUIPropMaps = new();
    internal bool HasCond(string key) => key == "IsInstalled";
    internal IEnumerable<CondOwner> GetCOsSafe(bool recursive) => Items;
}
namespace BepInEx.Bootstrap { internal static class Chainloader { internal static Dictionary<string, object> PluginInfos = new(); } }
namespace HarmonyLib
{
    internal static class AccessTools
    {
        internal static Type? TypeByName(string name) => null;
        internal static System.Reflection.PropertyInfo? Property(Type type, string name) => null;
    }
}
namespace PhobosAutoNav
{
    internal sealed class Setting<T> { internal T Value; internal FakeConfig ConfigFile = new(); internal Setting(T value) { Value = value; } }
    internal sealed class FakeConfig { internal int Saves; internal void Save() { Saves++; } }
    internal static class Plugin
    {
        internal static Setting<bool> ResumeAfterLoad = new(true), Enabled = new(true), FuelCheck = new(true);
        internal static Setting<double> MaxFlightSimHours = new(48);
        internal static Setting<bool> PreferTorch = new(true);
        internal static Setting<float> DefaultArriveKM = new(1), DefaultCruiseMS = new(100), TorchMaximumG = new(1);
    }
    internal static class Text { internal static string Get(string key, params object[] values) => key; }
    internal sealed class TargetRef
    {
        internal static bool Available = true;
        internal string ShipId = "target", DisplayName = "target";
        internal static TargetRef? FromShipId(string id) => Available ? new TargetRef { ShipId = id, DisplayName = id } : null;
    }
    internal static class AutoNavCore
    {
        internal static bool Engaged, Coasting, FuelAvailable = true, FuelReadOnly;
        internal static Ship? EngagedPlayer;
        internal static string? LastResult;
        internal static double ElapsedSeconds;
        internal static CoastSettings FlightCoastSettings = new(3,10,.75,2);
        internal static bool FlightPrefersTorch;
        internal const double KM_TO_AU = 6.684587122268445E-09;
        internal static double ArriveAU = KM_TO_AU;
        internal enum Phase { Idle, Align, Accel, Cruise, Coast, Decel, Arrive }
        internal static Phase CurrentPhase;
        internal static TargetRef EngagedTarget = new();
        internal static void ResetStatics() { Engaged = false; EngagedPlayer = null; }
        internal static void EndFlight(Ship? ship, string result) { LastResult = result; Engaged = false; }
        internal static void RestoreFlight(Ship ship, TargetRef target, FlightSnapshot snapshot)
        { Engaged = true; EngagedPlayer = ship; ElapsedSeconds = snapshot.ElapsedSeconds; Coasting = snapshot.Coasting; }
        internal static bool AutoDockBusy() => false;
        internal static bool TryReadApproach(Ship ship, TargetRef target, double km, out ApproachPlan plan, out double speed)
        { speed = 10; return ApproachRules.TryPlan(80, km, 0, out plan); }
        internal static bool HasFuelForFlight(Ship ship, TargetRef target, bool readOnly) { FuelReadOnly = readOnly; return FuelAvailable; }
    }
    internal sealed partial class NavigationService
    {
        internal TorchDouble Torch { get; } = new();
        internal const string ModuleId = "PhobosNavModAutoNav";
        private CondOwner? console;
        private bool issuing;
        private string status = "";
        private readonly Action<string> log = _ => { };
        internal float Throttle => 1;
        private static bool HasId(CondOwner item, string id) => item.Kind == id;
        private static string? HardwareProblem(CondOwner? co) => co?.Problem;
        internal void Engage(CondOwner co) => throw new NotSupportedException();
        private void ResumeDocking(CondOwner co, TargetRef target, FlightSnapshot snapshot) => throw new NotSupportedException();
        internal void Disengage(string reason) { FinishSavedFlight(SavedFlightMode.Stopped); AutoNavCore.ResetStatics(); }
        internal void SaveProgressForTest() => PersistProgress();
        internal void ForgetForTest(CondOwner co) => ForgetSaved(co);
        internal void BindForTest(CondOwner co) => console = co;
        private static string ArrivalUsage() => "invalid arrival";
        // Read these production-private fields to ensure test compilation also checks their use.
        internal string Diagnostic => status + issuing;
    }
    internal sealed class TorchDouble
    {
        internal string Reason => "Torch.rcs";
        internal int Cuts;
        internal void Reset() { }
        internal void Cut() { Cuts++; }
    }
}

internal static class GUIOrbitDraw
{
    internal sealed class Contact { internal Ship? Ship; }
    internal static Contact? CrossHairTarget;
    internal static bool IsOpen() => true;
}
