using System;
using System.Collections.Generic;
using PhobosAutoNav;
using PhobosAutoNav.Core;
using Ostranauts.ShipGUIs.Utilities;
int checks = 0;
void Check(bool value, string label) { checks++; if (!value) throw new Exception(label); }
(FireControlController Control, Ship Own, Ship Target, CondOwner Weapon, TargetRef Ref) Setup()
{
    var own = new Ship(); var target = new Ship { strRegID = "target" }; target.objSS.vPosy = 1000 * AutoNavCore.M_TO_AU;
    CrewSim.system = new(); CrewSim.system.Ships.Add("own", own); CrewSim.system.Ships.Add("target", target);
    CrewSim.coPlayer.ship = own; CrewSim.objInstance.FinishedLoading = true; CrewSim.Paused = false;
    GUIOrbitDraw.Open = false; ShipInfo.Lock = 0; NativeContactReader.State = ContactState.Ready;
    var weapon = new CondOwner { ship = own }; weapon.Ammo.Add(new()); own.WeaponsSystem.Weapons.Add(weapon);
    AutoNavCore.Engaged = AutoNavCore.Following = false; StarSystem.fEpoch = 0;
    var control = new FireControlController(); var reference = new TargetRef();
    control.Observe(own, reference, 1, .25);
    return (control, own, target, weapon, reference);
}
void Arm((FireControlController Control, Ship Own, Ship Target, CondOwner Weapon, TargetRef Ref) f, int count = 1)
{ f.Control.SetOwnership("console", f.Own, 1, true); f.Control.Observe(f.Own, f.Ref, 1, 0); f.Control.Authorize(f.Own, "target", 1, count); }
void Tick((FireControlController Control, Ship Own, Ship Target, CondOwner Weapon, TargetRef Ref) f, bool safe = true)
{ StarSystem.fEpoch += .25; f.Control.Observe(f.Own, f.Ref, 1, .25); f.Control.Dispatch(f.Own, f.Ref, safe); }
void Aim((FireControlController Control, Ship Own, Ship Target, CondOwner Weapon, TargetRef Ref) f, bool safe = true)
{ for (int i = 0; i < 12; i++) Tick(f, safe); }
var f = Setup(); Aim(f); Check(f.Own.WeaponsSystem.Shots == 0 && f.Control.ReadyCount == 1, "Observation is useful before Engage and cannot fire");
Check(!f.Control.Authorize(f.Own, "target", 1, 1), "Authority requires group ownership");
Arm(f); Aim(f); Check(f.Own.WeaponsSystem.Shots == 1 && f.Own.WeaponsSystem.Penalties == 1, "Independent native shot applies one consequence without Follow");
Check(!f.Control.Permitted && f.Control.State == FireState.Hold && f.Control.Remaining == 0, "Budget completion retains hold");
Check(f.Own.shipCombatTarget == null && f.Own.WeaponsSystem.LastTarget == f.Target.objSS, "Native target lease restored");
Check(!f.Control.AllowsNative(f.Own.WeaponsSystem, f.Weapon, f.Target.objSS), "Budget cannot be bypassed by native automatic fire");
Check(f.Control.AllowsNative(f.Own.WeaponsSystem, f.Weapon, new ShipSitu()), "Native micrometeoroid defence allowed");
var missile = new Ship { strRegID = "missile", Classification = Ship.TypeClassification.Projectile }; CrewSim.system.Ships.Add("missile", missile);
Check(f.Control.AllowsNative(f.Own.WeaponsSystem, f.Weapon, missile.objSS), "Native projectile defence allowed");
var otherWeapon = new CondOwner { ship = f.Own, strID = "other" }; otherWeapon.Values["IsShipWeaponFiringGroup"] = 1;
var queue = new List<CondOwner> { f.Weapon, otherWeapon }; f.Control.FilterNative(f.Own.WeaponsSystem, queue, f.Target.objSS);
Check(queue.Count == 1 && queue[0] == otherWeapon, "Only controlled group filtered from native queue");
f.Control.SetOwnership("console", f.Own, 1, false);
Check(f.Control.AllowsNative(f.Own.WeaponsSystem, f.Weapon, f.Target.objSS), "Explicit Native releases group");
foreach (string condition in new[] { "IsDamaged", "IsShipWeaponFiringModeManual", "IsPDCTargetModeMMMOnly", "IsJammedWeapon", "IsOff", "IsShipWeaponDecoyLauncher" })
{
    f = Setup(); f.Weapon.Conditions.Add(condition); Arm(f); Aim(f);
    Check(f.Own.WeaponsSystem.Shots == 0, "Respects native restriction " + condition);
    Check(!f.Control.Weapons[0].Ready, "Restriction is visible " + condition);
}
f = Setup(); f.Weapon.Conditions.Remove("IsPowered"); Arm(f); Aim(f); Check(f.Own.WeaponsSystem.Shots == 0, "Unpowered");
f = Setup(); f.Weapon.Conditions.Remove("IsShipWeaponPDC"); Arm(f); Aim(f); Check(f.Own.WeaponsSystem.Shots == 0, "Unknown weapon excluded");
f = Setup(); f.Weapon.Ammo[0].Conditions.Add("IsAmmoDecoyMissile"); Arm(f); Aim(f); Check(f.Own.WeaponsSystem.Shots == 0, "Decoy ammunition excluded");
f = Setup(); f.Weapon.Ammo.Clear(); Arm(f); Aim(f); Check(f.Own.WeaponsSystem.Shots == 0, "Empty ammo");
f = Setup(); f.Weapon.Item!.fLastRotation = 180; Arm(f); Aim(f); Check(f.Own.WeaponsSystem.Shots == 0, "Outside arc");
f = Setup(); f.Weapon.Values["IsShipWeaponArcRange"] = 0; Arm(f); Aim(f); Check(f.Control.Weapons[0].Reason == "FCS.envelope", "Missing native envelope not fabricated");
f = Setup(); Arm(f); Aim(f, false); Check(f.Own.WeaponsSystem.Shots == 0 && f.Control.State == FireState.Held, "Guidance veto holds shots");
f = Setup(); Arm(f); NativeContactReader.State = ContactState.Weak; Aim(f); Check(!f.Control.Permitted, "Contact loss revokes");
Check(f.Control.Weapons[0].InArc == null && f.Control.Weapons[0].AimSeconds == null, "Unqualified target geometry is unavailable, not a measured no");
NativeContactReader.State = ContactState.Ready; Aim(f); Check(f.Own.WeaponsSystem.Shots == 0, "Reacquisition cannot rearm");
f = Setup(); Arm(f); f.Control.Cease(); Aim(f); Check(!f.Control.Permitted && f.Control.Owns("console"), "Cease retains ownership");
f = Setup(); Arm(f); f.Control.Reset(); f.Control.SetOwnership("console", f.Own, 1, true); Aim(f); Check(f.Own.WeaponsSystem.Shots == 0, "Restored hold has no fire permission");
f = Setup(); Arm(f); f.Weapon.strID = "replacement"; Aim(f); Check(!f.Control.Permitted, "Weapon replacement revokes exact binding");
f = Setup(); Arm(f); f.Weapon.Values["IsShipWeaponFiringGroup"] = 1; Aim(f); Check(!f.Control.Permitted, "Membership change revokes");
f = Setup(); Arm(f); f.Weapon.Conditions.Add("IsShipWeaponFiringModeManual"); Tick(f);
f.Weapon.Conditions.Remove("IsShipWeaponFiringModeManual"); Aim(f); Check(!f.Control.Permitted && f.Own.WeaponsSystem.Shots == 0, "Changing a firing setting requires explicit rearm");
f = Setup(); Arm(f); var replacement = new CondOwner { ship = f.Own, strID = f.Weapon.strID }; replacement.Ammo.Add(new());
f.Own.WeaponsSystem.Weapons[0] = replacement; Aim(f); Check(!f.Control.Permitted, "Same-ID replacement does not inherit weapon permission");
f = Setup(); f.Weapon.Conditions.Remove("IsShipWeaponPDC"); f.Weapon.Conditions.Add("IsShipWeaponMissileLauncher"); f.Weapon.Ammo[0].Conditions.Add("IsAmmoMissile");
Arm(f); Aim(f); Check(f.Own.WeaponsSystem.Shots == 0, "Closed native UI cannot grant missile lock");
GUIOrbitDraw.Open = true; ShipInfo.Lock = 99; Aim(f); Check(f.Own.WeaponsSystem.Shots == 0, "Incomplete native lock");
ShipInfo.Lock = 100; Aim(f); Check(f.Own.WeaponsSystem.Shots == 1, "Native automatic eligible missile with full lock");
f = Setup(); var previous = new Ship { strRegID = "old" }; f.Own.shipCombatTarget = previous; Arm(f); f.Own.WeaponsSystem.Throw = true;
try { Aim(f); } catch { }
Check(f.Own.shipCombatTarget == previous && !f.Control.Permitted && f.Control.State == FireState.Fault, "Exception restores target and revokes authority");
Check(!f.Control.AllowsNative(f.Own.WeaponsSystem, f.Weapon, f.Target.objSS), "Fault retains offensive hold");
for (int budget = 1; budget <= 9; budget++)
{
    f = Setup(); Arm(f, budget);
    for (int tick = 0; tick < 300; tick++) { f.Weapon.Conditions.Remove("IsReloading"); if (f.Weapon.Ammo.Count == 0) f.Weapon.Ammo.Add(new()); Tick(f); }
    Check(f.Own.WeaponsSystem.Shots == budget && f.Own.WeaponsSystem.Penalties == budget, "Exact limited volley budget " + budget);
}
f = Setup(); var second = new CondOwner { ship = f.Own, strID = "second" }; second.Ammo.Add(new()); f.Own.WeaponsSystem.Weapons.Add(second); Arm(f); Aim(f);
Check(f.Own.WeaponsSystem.Shots == 2 && f.Own.WeaponsSystem.Penalties == 1, "Two ready weapons consume one volley and one consequence");
f = Setup(); second = new CondOwner { ship = f.Own, strID = "second" }; second.Ammo.Add(new()); second.Conditions.Add("IsReloading"); f.Own.WeaponsSystem.Weapons.Add(second); Arm(f); Aim(f);
second.Conditions.Remove("IsReloading"); Aim(f); Check(f.Own.WeaponsSystem.Shots == 1, "Unready weapon misses a completed volley");
Check(FireRules.TryLead(new(0, 1000), new(20, 0), 100, out var lead) && lead.X > 0, "Crossing lead");
Check(!FireRules.TryLead(new(0, 1000), new(0, 200), 100, out _), "Receding target unreachable");
Check(!FireRules.TryLead(new(double.NaN, 0), default, 100, out _), "Nonfinite solution rejected");
Check(!FireRules.TryLead(new(0, 1000), new(0, -100), 100, out _), "Native quadratic singularity is held even when the linear intercept exists");
for (double throttle = .1; throttle <= 1; throttle += .1)
    Check(Math.Abs(FireRules.Turn(2, .2, .25, throttle, .5, .6)) <= throttle * .5, "RCS rotation respects throttle");
Check(FireRules.Turn(1, 0, 2, 1, .5, .6) == 0 && FireRules.Turn(1, 0, .25, 0, .5, .6) == 0, "Invalid interval/zero authority cannot turn");
Console.WriteLine($"{checks} N3 fire-control/ownership/volley assertions passed; no in-game tests performed.");
