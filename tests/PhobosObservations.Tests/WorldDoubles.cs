// Narrow native-world doubles around the production adapters/services. No game simulation is claimed.
using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Controls;

public class CondOwner
{
    public string strID = "", strCODef = "", strNameFriendly = "";
    public Ship ship = null!;
    public CondOwner? objCOParent;
    public bool bDestroyed;
    public GasContainer? GasContainer;
    public readonly HashSet<string> Conditions = new();
    public readonly Dictionary<string, double> Values = new();
    public bool HasCond(string key) => Conditions.Contains(key);
    public double GetCondAmount(string key) => Values.TryGetValue(key, out double v) ? v : 0;
    public int GetPos(string point = "") => 0;
}
public sealed class GasContainer { public double StoredHeat = 500; }
public sealed class Room { public CondOwner CO = null!; }
public sealed class Ship
{
    public string strRegID = "";
    public int LoadState = 2, DiscoveryCalls;
    public Room Room = new();
    public readonly List<CondOwner> Objects = new(), Neighbours = new();
    public Room GetRoomAtWorldCoords1(int position, bool docked) => Room;
    public void GetCOsAtWorldCoords1(int position, object? filter, bool docked, bool locked, List<CondOwner> output)
    { output.Add(Room.CO); if (docked) output.AddRange(Neighbours); }
    public List<CondOwner> GetCOs(object? filter, bool sub, bool docked, bool locked)
    { DiscoveryCalls++; return Objects.Concat(docked ? Neighbours : Array.Empty<CondOwner>()).ToList(); }
}
public sealed class StarSystem
{
    public static double fEpoch = 100;
    public string Owner = "player";
    public string GetShipOwner(string id) => Owner;
}
public sealed class CrewSim
{
    public static StarSystem system = new();
    public static CondOwner coPlayer = new() { strID = "player" };
    public static CondOwner Actor = null!;
    public static CondOwner GetSelectedCrew() => Actor;
    public void LoadGame() { }
    public void NewGame() { }
}
public sealed class GasPressureSense { public void Run() { } }
public sealed class Sensor { public void Run() { } }
public static class TileUtils { public static float Distance; public static float TileRange(int a, int b) => Distance; }
namespace UnityEngine { public static class Time { public static float unscaledTime; public static int frameCount = 1; } }
namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Class)] public sealed class HarmonyPatch : Attribute
    { public HarmonyPatch() { } public HarmonyPatch(Type type, string name) { } }
}
namespace Phobos.Ostranauts.Framework { public static class Units { public const double CelsiusToKelvin = 273.15; } }
namespace PhobosShipbreaker.Core { internal static class IndustrialRules { internal const string Prefix = "Console"; internal const double AccessTiles = 2.5; } }
namespace PhobosShipbreaker
{
    internal static class Text { internal static string Get(string key, params object[] args) => key + ": " + string.Join("|", args); }
    internal sealed class EquipmentCard
    {
        internal string Id = "", Name = "", Group = "", Detail = "", InstrumentStatus = "", SearchScope = "";
        internal EquipmentState State;
        internal bool Attention, Instrument;
    }
    internal static class CollectorService
    {
        internal static readonly Dictionary<string, CondOwner> Objects = new();
        internal static CondOwner? Resolve(string id) => Objects.TryGetValue(id, out var c) ? c : null;
    }
    internal static class ProcessingService { internal static bool IsReclaimer(CondOwner c) => c.strCODef == "Reclaimer"; }
}
