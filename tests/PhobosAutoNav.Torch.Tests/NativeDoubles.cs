// Only the native boundaries are doubled. Tests execute the production guidance,
// torch adapter and policies. This does not emulate Unity or validate real fuel/heat.
#pragma warning disable CS0649 // Native fields may deliberately retain their default values.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PhobosAutoNav;
using PhobosAutoNav.Core;
using UnityEngine;

internal sealed class Ship
{
    internal ShipSitu objSS = new();
    internal CondOwner Reactor;
    internal bool bDestroyed, bCheckFusion, bFusionReactorRunning = true, Docked, IsNotAFullStation, IsUsingTorchDrive;
    internal double fShallowFusionRemain = 10000, Mass = 10000, RCSAccelMax = .5 * AutoNavCore.M_TO_AU,
        DeltaVRemainingRCS = 10000 * AutoNavCore.M_TO_AU, RcsFuel = 100, RcsTranslation, RcsRotation, LargestThrust;
    internal int PositiveThrusts, RetrogradeBurns;
    internal Ship() { Reactor = new CondOwner(this); }
    internal bool IsDocked() => Docked;
    internal void UnlockFromOrbit() { }
    internal double GetRCSRemain() => RcsFuel;
    internal float GetMaxTorchThrust(float limiter) => (float)(20 * limiter * limiter * AutoNavCore.M_TO_AU);
    internal string GetReactorGPMValue(string key) => Reactor.GetGPMInfo("Panel A", key);
    internal void SetReactorGPMValue(string key, string value) => Reactor.ApplyGPMChanges(new[] { "Panel A," + key + "," + value });
    internal void SetThrust(double force)
    {
        Plugin.Service.Torch.FilterThrust(this, ref force); // Mirrors the production Harmony prefix.
        IsUsingTorchDrive = force > 0;
        if (force > 0)
        {
            PositiveThrusts++; LargestThrust = Math.Max(LargestThrust, force);
            if (Math.Cos(objSS.fRot) < -.9) RetrogradeBurns++;
        }
        double acceleration = force / Mass * AutoNavCore.M_TO_AU;
        objSS.vAccIn = new Vector2(-Math.Sin(objSS.fRot) * acceleration, Math.Cos(objSS.fRot) * acceleration);
    }
    internal void Maneuver(float x, float y, float r, int noise, float dt)
    {
        RcsTranslation += (Math.Abs(x) + Math.Abs(y)) * RCSAccelMax / AutoNavCore.M_TO_AU * dt;
        RcsRotation += Math.Abs(r) * dt;
        objSS.vAccRCS = new Vector2((x * Math.Cos(objSS.fRot) - y * Math.Sin(objSS.fRot)) * RCSAccelMax,
            (x * Math.Sin(objSS.fRot) + y * Math.Cos(objSS.fRot)) * RCSAccelMax);
        if (Math.Abs(objSS.fW) > 1e-7 && objSS.fW * (objSS.fW + r * dt) < 0) objSS.fW = 0;
        else { objSS.fW += r * dt; objSS.fA = r; }
    }
}
internal sealed class ShipSitu
{
    internal double vPosx, vPosy, vVelX, vVelY;
    internal float fRot, fW, fA;
    internal bool bOrbitLocked, bBOLocked, bIsBO;
    internal Vector2 vAccIn, vAccEx, vAccRCS;
    internal void ResetNavData() { }
    internal void UnlockFromBO() { }
    internal void Integrate(double dt)
    {
        double ax = vAccIn.x + vAccRCS.x, ay = vAccIn.y + vAccRCS.y;
        vPosx += vVelX * dt + ax * dt * dt / 2; vPosy += vVelY * dt + ay * dt * dt / 2;
        vVelX += ax * dt; vVelY += ay * dt;
        fRot += (float)(fW * dt + fA * dt * dt / 2);
        float previous = fW; fW += fA * (float)dt;
        if (previous * fW < 0) { fW = 0; fA = 0; }
    }
}
internal sealed class CondOwner
{
    internal Ship ship;
    internal bool bDestroyed;
    internal HashSet<string> Conditions = new() { "IsInstalled", "IsReadyFusion" };
    internal double Temperature = TorchRules.NativeCoreTemperature;
    internal Dictionary<string, string> Props = new() { ["slidCycle"] = "0", ["slidFlow"] = "0.2", ["knobRatio"] = "0", ["bNWZ"] = "false" };
    internal FusionIC Fusion;
    internal bool FailControlWrite;
    internal CondOwner(Ship owner) { ship = owner; Fusion = new FusionIC(this); }
    internal bool HasCond(string key) => Conditions.Contains(key);
    internal double GetCondAmount(string key) => key == "StatICCoreTemp" ? Temperature : 8;
    internal string GetGPMInfo(string panel, string key) => Props.TryGetValue(key, out var value) ? value : "";
    internal T GetComponent<T>() where T : class => (Fusion as T)!;
    internal void ApplyGPMChanges(string[] changes)
    {
        if (FailControlWrite) throw new InvalidOperationException("Native control write failed");
        foreach (var change in changes) { var parts = change.Split(','); Props[parts[1]] = parts[2]; }
    }
}
internal sealed class FusionIC
{
    private readonly CondOwner core;
    internal bool Deliver = true;
    internal int Runs;
    internal double LastEpoch = double.NegativeInfinity;
    internal FusionIC(CondOwner core) { this.core = core; }
    internal void CatchUp()
    {
        if (StarSystem.fEpoch - LastEpoch < .27) return;
        LastEpoch = StarSystem.fEpoch; Runs++;
        if (!Deliver) { core.ship.SetThrust(0); return; }
        float cycle = float.Parse(core.Props["slidCycle"], CultureInfo.InvariantCulture);
        core.ship.SetThrust(core.ship.GetMaxTorchThrust(cycle) / AutoNavCore.M_TO_AU *
            core.ship.Mass * core.Temperature / TorchRules.NativeCoreTemperature);
    }
}
internal sealed class CrewSim
{
    internal static CrewSim? objInstance = new();
    internal static CondOwner? coPlayer;
    internal static StarSystem? system = new();
    internal bool FinishedLoading = true;
    internal static void ResetTimeScale() { }
}
internal sealed class StarSystem
{
    internal static double fEpoch;
    internal List<Ship> Stations = new();
    internal bool Restricted, FailQuery;
    internal bool IsWithinNoWakeRangeOfAnyStation(ShipSitu situ, double epoch)
    {
        if (FailQuery) throw new InvalidOperationException("Zone query unavailable");
        return Restricted;
    }
    internal List<Ship> GetStations() => Stations;
}
internal sealed class GUIOrbitDraw
{
    internal static GUIOrbitDraw? Instance;
    internal bool PlayerThrusting = false;
}
internal static class CollisionManager
{
    internal static double GetCollisionDistanceAU(ShipSitu a, ShipSitu b) => 10 * AutoNavCore.M_TO_AU;
}
internal sealed class JsonItem { internal JsonGUIPropMap[]? aGPMSettings; }
internal sealed class JsonGUIPropMap { internal string strName = ""; internal string[] dictGUIPropMap = Array.Empty<string>(); }
internal static class DataHandler
{
    internal static Dictionary<string, string> ConvertStringArrayToDict(string[] values) => values.Select(v => v.Split('=')).ToDictionary(v => v[0], v => v[1]);
    internal static string[] ConvertDictToStringArray(Dictionary<string, string> values) => values.Select(v => v.Key + "=" + v.Value).ToArray();
}
namespace UnityEngine
{
    internal struct Vector2
    {
        internal float x, y;
        internal Vector2(double x, double y) { this.x = (float)x; this.y = (float)y; }
        internal double magnitude => Math.Sqrt(x * x + y * y);
    }
}
namespace HarmonyLib { } // Upstream unused import.
namespace Ostranauts.ShipGUIs.NavStation
{
    internal static class NavModTorchDrive { internal static float GetLimiterSafetyMax(Ship ship) => .99f; }
}
namespace Ostranauts.ShipGUIs.Utilities
{
    internal static class NavData { internal static double GetFLOWforCYCLE(CondOwner core, double cycle) => cycle * .9; }
}
namespace PhobosAutoNav
{
    internal sealed class Setting<T> { internal T Value; internal Setting(T value) { Value = value; } }
    internal static class Plugin
    {
        internal static NavigationService Service = new();
        internal static Setting<bool> Enabled = new(true), PreferTorch = new(true), AbortOnManualThrust = new(true),
            UseThrusterRotation = new(true), VerboseLogging = new(false);
        internal static Setting<float> TorchMaximumG = new(1), TorchMinimumCorrectionMS = new(5), ArrivalSpeedTolerance = new(.5f),
            RotAccelMax = new(.5f), RotSpeedMax = new(.6f), MaxFlightSimHours = new(48);
        internal static void Verbose(string message) { }
    }
    internal sealed class NavigationService
    {
        internal TorchDriveController Torch = new();
        internal float Throttle => 1;
    }
    internal static class Text { internal static string Get(string key) => key; }
    internal sealed class TargetRef
    {
        internal string ShipId = "target";
        internal ShipSitu TargetSitu = new();
        internal string DisplayName = "target";
        internal static TargetRef? FromCrossHair() => null;
        internal bool Resolve(out double x, out double y, out double vx, out double vy)
        { x = TargetSitu.vPosx; y = TargetSitu.vPosy; vx = TargetSitu.vVelX; vy = TargetSitu.vVelY; return true; }
        internal bool ResolveAt(double dt, out double x, out double y, out double vx, out double vy)
        { Resolve(out x, out y, out vx, out vy); x += vx * dt; y += vy * dt; return true; }
    }
}
