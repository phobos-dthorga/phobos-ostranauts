using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Ships;
using Ostranauts.ShipGUIs.Utilities;
using Ostranauts.Utils.Models;
using PhobosAutoNav.Core;

namespace PhobosAutoNav;

// Session-only offensive authority. Never serialize this object or native aim progress.
internal sealed class FireControlController
{
    private Ship? owner;
    private TargetRef? target;
    private int group;
    private readonly Dictionary<string, double> aim = new();
    private bool dispatch;
    private ShipSitu? inhibitedTarget;
    internal bool Permitted { get; private set; }
    internal string Reason { get; private set; } = "Pursuit.ceased";
    internal void Reset() { owner = null; target = null; group = 0; inhibitedTarget = null; Reason = "Pursuit.ceased"; aim.Clear(); dispatch = false; Permitted = false; AutoNavCore.FaceTarget = false; AutoNavCore.WeaponHeading = null; }
    internal void Cease() { var held = target?.TargetSitu ?? inhibitedTarget; Reset(); inhibitedTarget = held; }
    internal void Authorize(Ship ship, TargetRef destination, int selectedGroup)
    {
        Cease();
        if (selectedGroup < 1 || selectedGroup > 9 || destination.ShipId == ship.strRegID) return;
        owner = ship; target = destination; group = selectedGroup;
        Permitted = true; Reason = "Pursuit.armed";
    }
    internal bool AllowsNative(WeaponsSystem system, ShipSitu? destination) => dispatch ||
        !AutoNavCore.Engaged || !AutoNavCore.Following || AutoNavCore.EngagedPlayer?.WeaponsSystem != system ||
        (destination != AutoNavCore.EngagedTarget?.TargetSitu && destination != target?.TargetSitu && destination != inhibitedTarget);

    internal void Tick(Ship ship, TargetRef destination, double dt, bool guidanceAllowsFire)
    {
        if (!Permitted || target == null) return;
        destination = target;
        if (ship != owner || !AutoNavCore.Engaged || !AutoNavCore.Following ||
            !NativeContactReader.Read(ship, destination.ShipId).Usable || CrewSim.system?.GetShipByRegID(destination.ShipId) == null ||
            CrewSim.coPlayer?.ship != ship || !CrewSim.objInstance.FinishedLoading)
        { Cease(); return; }
        AutoNavCore.FaceTarget = true;
        AutoNavCore.WeaponHeading = null;
        if (!guidanceAllowsFire || dt <= 0 || dt > FireRules.MaximumStep || CrewSim.Paused)
        { aim.Clear(); Reason = "Pursuit.fire_held"; return; }
        var weapons = ship.WeaponsSystem.GetActivatedWeapons(true);
        if (weapons == null) { aim.Clear(); Reason = "Pursuit.no_weapons"; return; }
        var other = CrewSim.system.GetShipByRegID(destination.ShipId);
        if (other?.objSS == null) { Cease(); return; }
        var salvo = new List<CondOwner>();
        Reason = "Pursuit.aiming";
        bool selected = false;
        var present = new HashSet<string>();
        foreach (var weapon in weapons)
        {
            if (weapon == null || weapon.ship != ship || weapon.bDestroyed || weapon.Item == null ||
                MathUtils.RoundToInt(weapon.GetCondAmount("IsShipWeaponFiringGroup")) + 1 != group) continue;
            selected = true; present.Add(weapon.strID);
            // Respect native manual-only and defensive-only settings, as well as power and damage.
            bool eligible = weapon.HasCond("IsPowered") && !weapon.HasCond("IsDamaged") &&
                !weapon.HasCond("IsShipWeaponFiringModeManual") && !weapon.HasCond("IsPDCTargetModeMMMOnly") &&
                !weapon.HasCond("IsJammedWeapon");
            bool ready = eligible && !WeaponsSystem.IsReloading(weapon);
            double arc = weapon.GetCondAmount("IsShipWeaponArcAngle", false);
            double range = weapon.GetCondAmount("IsShipWeaponArcRange", false);
            if (weapon.HasCond("IsShipWeaponMassThrower", false)) range *= ship.WeaponsSystem.fRangeModGunner;
            double angle = ship.WeaponsSystem.GetItemsDefaultFiringAngle(weapon.Item.fLastRotation) + Math.PI / 2;
            ready &= TorchRules.Finite(arc, range, angle) && arc > 0 && range > 0 &&
                WeaponsSystem.IsPointInView(ship.objSS.vPos, new Point(Math.Cos(angle), Math.Sin(angle)), other.objSS.vPos, range, arc);
            var ammo = WeaponsSystem.GetAmmo(weapon);
            ready &= ammo != null && ammo.Count > 0;
            bool missile = ammo?.Any(a => a != null && a.HasCond("IsAmmoMissile")) == true;
            // Native player missiles require the native sensor lock; never invoke the AI shortcut.
            if (missile) ready &= GUIOrbitDraw.IsOpen() && GUIOrbitDraw.Instance.COSelfBase()?.ship == ship &&
                ShipInfo.GetShipInfo(ship, other, GUIOrbitDraw.Instance.ShipPropMap).lockingProgress >= 100;
            if (AutoNavCore.WeaponHeading == null && eligible && ammo != null && ammo.Count > 0)
            {
                var r = new NavVector((other.objSS.vPosx - ship.objSS.vPosx) / AutoNavCore.M_TO_AU,
                    (other.objSS.vPosy - ship.objSS.vPosy) / AutoNavCore.M_TO_AU);
                var v = new NavVector((other.objSS.vVelX - ship.objSS.vVelX) / AutoNavCore.M_TO_AU,
                    (other.objSS.vVelY - ship.objSS.vVelY) / AutoNavCore.M_TO_AU);
                var lead = missile ? r : FireRules.Lead(r, v, weapon.GetCondAmount("IsShipWeaponLaunchSpeed"));
                AutoNavCore.WeaponHeading = -Math.Atan2(lead.X, lead.Y) - weapon.Item.fLastRotation * Math.PI / 180;
            }
            aim.TryGetValue(weapon.strID, out double before);
            double after = FireRules.Aim(before, arc, weapon.GetCondAmount("IsShipWeaponTargetingSpeed", false), dt, ready);
            aim[weapon.strID] = after;
            if (ready && FireRules.Locked(before, arc)) salvo.Add(weapon);
        }
        foreach (string id in aim.Keys.Where(id => !present.Contains(id)).ToArray()) aim.Remove(id);
        if (!selected) { aim.Clear(); Reason = "Pursuit.no_weapons"; }
        if (salvo.Count == 0) return;
        // Combat selection is leased only during native projectile creation (missile API requirement).
        // The player's persistent combat selection is never used as our engagement permission.
        var previousTarget = ship.shipCombatTarget;
        try
        {
            dispatch = true; ship.shipCombatTarget = other;
            if (ship.WeaponsSystem.ShootAuto(salvo, other.objSS))
            { ship.WeaponsSystem.ApplyFactionDamage(-2.5f, other); CrewSim.UnlockAchievement("ACH_WEAPONS"); Reason = "Pursuit.firing"; }
            foreach (var weapon in salvo) aim.Remove(weapon.strID);
        }
        finally { ship.shipCombatTarget = previousTarget; dispatch = false; }
    }
}
