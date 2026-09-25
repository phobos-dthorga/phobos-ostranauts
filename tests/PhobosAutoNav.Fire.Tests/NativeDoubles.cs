using System;
using System.Collections.Generic;
#pragma warning disable CS0649
using PhobosAutoNav;
using PhobosAutoNav.Core;
using Ostranauts.Ships;
using Ostranauts.Utils.Models;
internal sealed class Ship
{
    internal string strRegID = "own";
    internal ShipSitu objSS = new();
    internal WeaponsSystem WeaponsSystem = new();
    internal Ship? shipCombatTarget;
}
internal sealed class ShipSitu
{
    internal double vPosx, vPosy, vVelX, vVelY;
    internal Point vPos => new(vPosx,vPosy);
}
internal sealed class Item { internal float fLastRotation; }
internal sealed class CondOwner
{
    internal Ship ship = null!;
    internal bool bDestroyed;
    internal string strID = "weapon";
    internal Item? Item = new();
    internal HashSet<string> Conditions = new() { "IsPowered" };
    internal Dictionary<string,double> Values = new() { ["IsShipWeaponArcAngle"] = 20, ["IsShipWeaponArcRange"] = 5000,
        ["IsShipWeaponTargetingSpeed"] = 10, ["IsShipWeaponFiringGroup"] = 0, ["IsShipWeaponLaunchSpeed"] = 100 };
    internal List<CondOwner> Ammo = new();
    internal bool HasCond(string key, bool isThreshold = true) => Conditions.Contains(key);
    internal double GetCondAmount(string key, bool isThreshold = true) => Values.TryGetValue(key,out var v) ? v : 0;
}
internal sealed class StarSystem
{
    internal Dictionary<string,Ship> Ships = new();
    internal Ship? GetShipByRegID(string id) => Ships.TryGetValue(id,out var ship) ? ship : null;
}
internal sealed class CrewSim
{
    internal static StarSystem system = new();
    internal static CrewSim objInstance = new();
    internal static CondOwner coPlayer = new();
    internal bool FinishedLoading = true;
    internal static bool Paused;
    internal static int Achievements;
    internal static void UnlockAchievement(string id) => Achievements++;
}
internal sealed class GUIOrbitDraw
{
    internal static GUIOrbitDraw Instance = new();
    internal static bool Open;
    internal object ShipPropMap = new();
    internal static bool IsOpen() => Open;
    internal CondOwner COSelfBase() => CrewSim.coPlayer;
}
internal static class MathUtils { internal static int RoundToInt(double x) => (int)Math.Round(x); }
namespace Ostranauts.Utils.Models { internal readonly record struct Point(double X,double Y); }
namespace Ostranauts.ShipGUIs.Utilities
{
    internal sealed class ShipInfo
    {
        internal static double Lock;
        internal double lockingProgress => Lock;
        internal static ShipInfo GetShipInfo(Ship own, Ship other, object map) => new();
    }
}
namespace Ostranauts.Ships
{
    internal sealed class WeaponsSystem
    {
        internal List<CondOwner> Weapons = new();
        internal double fRangeModGunner = 1;
        internal int Shots, Penalties;
        internal ShipSitu? LastTarget;
        internal bool Throw;
        internal Action? DuringShot;
        internal List<CondOwner> GetActivatedWeapons(bool refetch) => Weapons;
        internal static bool IsReloading(CondOwner weapon) => weapon.HasCond("IsReloading");
        internal static List<CondOwner> GetAmmo(CondOwner weapon) => weapon.Ammo;
        internal float GetItemsDefaultFiringAngle(float angle) => angle * (float)Math.PI / 180;
        internal static bool IsPointInView(Point own, Point facing, Point other, double range, double arc)
        {
            double x=other.X-own.X,y=other.Y-own.Y,d=Math.Sqrt(x*x+y*y);
            return d > 0 && d/AutoNavCore.M_TO_AU <= range && Math.Acos(Math.Clamp((x*facing.X+y*facing.Y)/d,-1,1))*180/Math.PI <= arc/2;
        }
        internal bool ShootAuto(List<CondOwner> weapons, ShipSitu target)
        {
            DuringShot?.Invoke(); if (Throw) throw new Exception("native failure");
            foreach (var w in weapons) { if (w.Ammo.Count==0 || IsReloading(w)) continue; w.Ammo.RemoveAt(0); w.Conditions.Add("IsReloading"); Shots++; }
            LastTarget=target; return weapons.Count > 0;
        }
        internal void ApplyFactionDamage(float penalty, Ship other) { if (penalty == -2.5f) Penalties++; }
    }
}
namespace PhobosAutoNav
{
    internal sealed class TargetRef
    {
        internal string ShipId = "target";
        internal ShipSitu? TargetSitu => CrewSim.system.GetShipByRegID(ShipId)?.objSS;
    }
    internal static class AutoNavCore
    {
        internal const double M_TO_AU = 6.684587122268445E-12;
        internal static bool Engaged, Following, FaceTarget;
        internal static double? WeaponHeading;
        internal static Ship? EngagedPlayer;
        internal static TargetRef? EngagedTarget;
    }
}
