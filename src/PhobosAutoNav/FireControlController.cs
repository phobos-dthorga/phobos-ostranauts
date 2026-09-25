using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Ships;
using Ostranauts.ShipGUIs.Utilities;
using Ostranauts.Utils.Models;
using PhobosAutoNav.Core;
namespace PhobosAutoNav;

internal enum FireState { Native, Hold, Armed, Held, Fault }
internal sealed class WeaponReading
{
    internal string Id = "", Name = "", Reason = "FCS.unavailable";
    internal bool? InArc;
    internal bool Loaded, Ready, Eligible, Manual, Reloading;
    internal double? Heading, ReloadSeconds, AimSeconds;
}
// Native weapons own projectiles/costs. A lease prevents competing offensive
// automatic dispatch; neither observations nor ownership authorize shooting.
internal sealed class FireControlController
{
    private readonly Dictionary<string, (Ship Ship, int Group)> leases = new();
    private readonly Dictionary<string, double> aim = new();
    private readonly HashSet<string> captured = new();
    private readonly Dictionary<string, (CondOwner Weapon, int Mode)> settings = new();
    private Ship? owner;
    private string? targetId;
    private int group;
    private bool dispatch;
    internal bool Permitted { get; private set; }
    internal int Remaining { get; private set; }
    internal FireState State { get; private set; }
    internal string Reason { get; private set; } = "FCS.native";
    internal IReadOnlyList<WeaponReading> Weapons { get; private set; } = Array.Empty<WeaponReading>();
    internal double SampleEpoch { get; private set; } = double.NaN;
    internal int? InArcCount => double.IsNaN(SampleEpoch) ? null : Weapons.Count(w => w.InArc == true);
    internal int? AmmoCount => double.IsNaN(SampleEpoch) ? null : Weapons.Count(w => w.Loaded);
    internal int? ReadyCount => double.IsNaN(SampleEpoch) ? null : Weapons.Count(w => w.Ready);
    internal void Reset() { Cease(); leases.Clear(); owner = null; targetId = null; group = 0; State = FireState.Native; Reason = "FCS.native"; Invalidate(); }
    internal void Invalidate() { Weapons = Array.Empty<WeaponReading>(); SampleEpoch = double.NaN; aim.Clear(); }
    internal void SetOwnership(string consoleId, Ship ship, int selectedGroup, bool hold)
    {
        Cease();
        if (hold) leases[consoleId] = (ship, selectedGroup); else leases.Remove(consoleId);
        State = leases.Count > 0 ? FireState.Hold : FireState.Native;
        Reason = leases.Count > 0 ? "FCS.hold" : "FCS.native";
    }
    internal bool Owns(string consoleId) => leases.ContainsKey(consoleId);
    internal bool OtherOwner(string consoleId) => leases.Keys.Any(id => id != consoleId);
    internal void Cease(string reason = "FCS.hold", bool fault = false)
    {
        Permitted = false; Remaining = 0; captured.Clear(); settings.Clear(); aim.Clear();
        State = fault ? FireState.Fault : leases.Count > 0 ? FireState.Hold : FireState.Native;
        Reason = fault || leases.Count > 0 ? reason : "FCS.native";
    }
    internal bool Authorize(Ship ship, string destination, int selectedGroup, int volleys)
    {
        Cease();
        if (volleys < 1 || volleys > 9 || destination == ship.strRegID ||
            !leases.Values.Any(l => l.Ship == ship && l.Group == selectedGroup) || !Weapons.Any(w => w.Eligible && w.Loaded)) return false;
        owner = ship; targetId = destination; group = selectedGroup;
        foreach (var weapon in Weapons) captured.Add(weapon.Id);
        foreach (var weapon in ship.WeaponsSystem.GetActivatedWeapons(true))
            if (captured.Contains(weapon.strID)) settings[weapon.strID] = (weapon, FiringMode(weapon));
        Remaining = volleys; Permitted = true; State = FireState.Armed; Reason = "FCS.armed";
        return true;
    }
    internal bool AllowsNative(WeaponsSystem system, CondOwner weapon, ShipSitu? destination)
    {
        if (dispatch && owner?.WeaponsSystem == system && captured.Contains(weapon.strID) && destination == CrewSim.system.GetShipByRegID(targetId!)?.objSS) return true;
        bool offensive = destination == null || CrewSim.system.dictShips.Values.Any(s => s != null &&
            s.objSS == destination && s.Classification != Ship.TypeClassification.Projectile);
        if (!offensive) return true;
        int selectedGroup = MathUtils.RoundToInt(weapon.GetCondAmount("IsShipWeaponFiringGroup")) + 1;
        return !leases.Values.Any(l => l.Ship.WeaponsSystem == system && l.Group == selectedGroup);
    }
    internal void FilterNative(WeaponsSystem system, List<CondOwner> weapons, ShipSitu? destination) =>
        weapons.RemoveAll(w => w != null && !AllowsNative(system, w, destination));
    private static int FiringMode(CondOwner weapon) => (weapon.HasCond("IsShipWeaponFiringModeManual") ? 1 : 0) | (weapon.HasCond("IsPDCTargetModeMMMOnly") ? 2 : 0);
    private bool BindingChanged(IEnumerable<CondOwner> weapons)
    {
        var members = weapons.Where(w => w != null && w.ship == owner && !w.bDestroyed && w.Item != null &&
            MathUtils.RoundToInt(w.GetCondAmount("IsShipWeaponFiringGroup")) + 1 == group).ToArray();
        return !captured.SetEquals(members.Select(w => w.strID)) || members.Any(w =>
            !settings.TryGetValue(w.strID, out var saved) || saved.Weapon != w || saved.Mode != FiringMode(w));
    }
    internal void Observe(Ship ship, TargetRef? target, int selectedGroup, double dt)
    {
        if (owner != ship || targetId != target?.ShipId || group != selectedGroup)
        { if (Permitted) Cease(); aim.Clear(); owner = ship; targetId = target?.ShipId; group = selectedGroup; }
        var list = ship.WeaponsSystem.GetActivatedWeapons(true);
        if (list == null) { Invalidate(); return; }
        var observations = new List<WeaponReading>();
        foreach (var w in list.Where(w => w != null && w.ship == ship && !w.bDestroyed && w.Item != null &&
            MathUtils.RoundToInt(w.GetCondAmount("IsShipWeaponFiringGroup")) + 1 == selectedGroup).OrderBy(w => w.strID, StringComparer.Ordinal))
            observations.Add(ReadWeapon(ship, target, w, dt, true));
        Weapons = observations.ToArray(); SampleEpoch = StarSystem.fEpoch;
        foreach (string id in aim.Keys.Where(id => !observations.Any(w => w.Id == id)).ToArray()) aim.Remove(id);
        if (Permitted && BindingChanged(list)) Cease("FCS.weapons_changed", true);
    }
    private WeaponReading ReadWeapon(Ship ship, TargetRef? target, CondOwner weapon, double dt, bool advance)
    {
        var r = new WeaponReading { Id = weapon.strID, Name = weapon.strNameFriendly,
            Manual = weapon.HasCond("IsShipWeaponFiringModeManual"), Reloading = WeaponsSystem.IsReloading(weapon) };
        var ammo = WeaponsSystem.GetAmmo(weapon); r.Loaded = ammo != null && ammo.Count > 0;
        bool missile = weapon.HasCond("IsShipWeaponMissileLauncher");
        bool known = missile || weapon.HasCond("IsShipWeaponPDC") || weapon.HasCond("IsShipWeaponMassThrower");
        string? problem = !known || weapon.HasCond("IsShipWeaponDecoyLauncher") || ammo?.Any(a => a.HasCond("IsAmmoDecoyMissile")) == true ? "FCS.unsupported" :
            weapon.HasCond("IsOff") || !weapon.HasCond("IsPowered") ? "FCS.unpowered" : weapon.HasCond("IsDamaged") ? "FCS.damaged" :
            r.Manual ? "FCS.manual" : weapon.HasCond("IsPDCTargetModeMMMOnly") ? "FCS.defensive" : weapon.HasCond("IsJammedWeapon") ? "FCS.jammed" : null;
        double arc = weapon.GetCondAmount("IsShipWeaponArcAngle", false), range = weapon.GetCondAmount("IsShipWeaponArcRange", false), rate = weapon.GetCondAmount("IsShipWeaponTargetingSpeed", false);
        if (weapon.HasCond("IsShipWeaponMassThrower", false)) range *= ship.WeaponsSystem.fRangeModGunner;
        double angle = ship.WeaponsSystem.GetItemsDefaultFiringAngle(weapon.Item!.fLastRotation) + Math.PI / 2;
        bool envelope = TorchRules.Finite(arc, range, rate, angle) && arc > 0 && range > 0 && rate > 0;
        problem ??= !envelope ? "FCS.envelope" : null; r.Eligible = problem == null;
        var other = target == null ? null : CrewSim.system.GetShipByRegID(target.ShipId);
        bool contact = other?.objSS != null && NativeContactReader.Read(ship, target!.ShipId).Usable;
        if (contact && envelope)
        {
            r.InArc = WeaponsSystem.IsPointInView(ship.objSS.vPos, new Point(Math.Cos(angle), Math.Sin(angle)), other!.objSS.vPos, range, arc);
            var offset = new NavVector((other.objSS.vPosx - ship.objSS.vPosx) / AutoNavCore.M_TO_AU, (other.objSS.vPosy - ship.objSS.vPosy) / AutoNavCore.M_TO_AU);
            var velocity = new NavVector((other.objSS.vVelX - ship.objSS.vVelX) / AutoNavCore.M_TO_AU, (other.objSS.vVelY - ship.objSS.vVelY) / AutoNavCore.M_TO_AU);
            bool solution = FireRules.TryLead(offset, velocity, weapon.GetCondAmount("IsShipWeaponLaunchSpeed"), out var lead);
            if (missile) { lead = offset; solution = offset.Finite && offset.Length > 0; }
            if (solution) r.Heading = -Math.Atan2(lead.X, lead.Y) - weapon.Item.fLastRotation * Math.PI / 180;
            else problem ??= "FCS.no_solution";
        }
        problem ??= !contact ? "FCS.contact" : !r.Loaded ? "FCS.empty" : r.Reloading ? "FCS.reloading" : r.InArc != true ? "FCS.arc" : null;
        if (r.Reloading) { double remaining = weapon.GetCondAmount("IsReloading", false) - StarSystem.fEpoch; r.ReloadSeconds = ArrivalBrake.Finite(remaining) ? Math.Max(0, remaining) : (double?)null; }
        if (problem == null && missile && !(GUIOrbitDraw.IsOpen() && GUIOrbitDraw.Instance.COSelfBase()?.ship == ship &&
            ShipInfo.GetShipInfo(ship, other!, GUIOrbitDraw.Instance.ShipPropMap)?.lockingProgress >= 100)) problem = "FCS.lock";
        aim.TryGetValue(r.Id, out double before);
        if (advance) aim[r.Id] = FireRules.Aim(before, arc, rate, dt, problem == null);
        if (envelope && contact) r.AimSeconds = Math.Max(0, arc - FireRules.LockMarginDegrees - before) / rate;
        r.Ready = problem == null && FireRules.Locked(before, arc);
        r.Reason = problem ?? (r.Ready ? "FCS.ready" : "FCS.aiming"); return r;
    }
    internal void Dispatch(Ship ship, TargetRef? target, bool safe)
    {
        if (!Permitted) return;
        if (ship != owner || target == null || target.ShipId != targetId || !NativeContactReader.Read(ship, targetId).Usable)
        { Cease("FCS.contact", true); return; }
        if (!safe) { aim.Clear(); State = FireState.Held; Reason = "FCS.guidance"; return; }
        var other = CrewSim.system.GetShipByRegID(targetId!);
        if (other == null || ship.WeaponsSystem.GetActivatedWeapons(true) is not { } weapons) { Cease("FCS.unavailable", true); return; }
        if (BindingChanged(weapons)) { Cease("FCS.weapons_changed", true); return; }
        var salvo = weapons.Where(w => w != null && captured.Contains(w.strID) && w.ship == ship && !w.bDestroyed && w.Item != null &&
            MathUtils.RoundToInt(w.GetCondAmount("IsShipWeaponFiringGroup")) + 1 == group && ReadWeapon(ship, target, w, 0, false).Ready).ToList();
        State = FireState.Armed; Reason = "FCS.aiming";
        if (salvo.Count == 0) return;
        var previous = ship.shipCombatTarget;
        try
        {
            dispatch = true; ship.shipCombatTarget = other;
            if (ship.WeaponsSystem.ShootAuto(salvo, other.objSS))
            {
                Remaining--; // Commit before consequences; an uncertain shot is never retried.
                ship.WeaponsSystem.ApplyFactionDamage(-2.5f, other); CrewSim.UnlockAchievement("ACH_WEAPONS"); Reason = "FCS.firing";
            }
            foreach (var weapon in salvo) aim.Remove(weapon.strID);
            if (Remaining <= 0) Cease("FCS.complete");
        }
        catch { Cease("FCS.dispatch_fault", true); throw; }
        finally { ship.shipCombatTarget = previous; dispatch = false; }
    }
}
