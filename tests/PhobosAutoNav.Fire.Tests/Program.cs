using System;
using PhobosAutoNav;
using PhobosAutoNav.Core;
using Ostranauts.ShipGUIs.Utilities;
int checks = 0;
void Check(bool value,string label) { checks++; if (!value) throw new Exception(label); }
(FireControlController Control,Ship Own, Ship Target,CondOwner Weapon,TargetRef Ref) Setup()
{
    var own = new Ship(); var target = new Ship { strRegID="target" }; target.objSS.vPosy=1000*AutoNavCore.M_TO_AU;
    CrewSim.system = new(); CrewSim.system.Ships.Add("own",own); CrewSim.system.Ships.Add("target",target);
    CrewSim.coPlayer.ship=own; CrewSim.objInstance.FinishedLoading=true; CrewSim.Paused=false;
    GUIOrbitDraw.Open=false; ShipInfo.Lock=0; NativeContactReader.State=ContactState.Ready;
    var weapon = new CondOwner {ship=own}; weapon.Ammo.Add(new()); own.WeaponsSystem.Weapons.Add(weapon);
    var r = new TargetRef(); AutoNavCore.Engaged=true; AutoNavCore.Following=true; AutoNavCore.EngagedPlayer=own; AutoNavCore.EngagedTarget=r;
    return(new(),own,target,weapon,r);
}
void Aim((FireControlController Control,Ship Own,Ship Target,CondOwner Weapon,TargetRef Ref) f, bool safe=true, double dt=.25)
{ for (int i=0;i<12;i++) f.Control.Tick(f.Own,f.Ref,dt,safe); }
var f = Setup(); Aim(f); Check(f.Own.WeaponsSystem.Shots==0,"No shot without explicit Engage");
f.Control.Authorize(f.Own,f.Ref,1); Aim(f); Check(f.Own.WeaponsSystem.Shots==1 && f.Own.WeaponsSystem.Penalties==1,"Native shot consumes ammo and applies exactly one consequence");
Check(f.Own.shipCombatTarget==null && f.Own.WeaponsSystem.LastTarget==f.Target.objSS,"Fire target lease is restored after native lead/firing");
Check(!f.Control.AllowsNative(f.Own.WeaponsSystem,f.Target.objSS),"Native panel cannot duplicate a pursuit salvo");
Check(f.Control.AllowsNative(f.Own.WeaponsSystem,new ShipSitu()),"Unrelated defensive shots retain native control");
f.Control.Cease(); f.Weapon.Conditions.Remove("IsReloading"); f.Weapon.Ammo.Add(new()); Aim(f);
Check(f.Own.WeaponsSystem.Shots==1 && AutoNavCore.Following && !f.Control.Permitted,"Cease Fire retains Follow but revokes all service fire");
foreach (string condition in new[]{"IsDamaged","IsShipWeaponFiringModeManual","IsPDCTargetModeMMMOnly","IsJammedWeapon","IsReloading"})
{ f=Setup(); f.Weapon.Conditions.Add(condition); f.Control.Authorize(f.Own,f.Ref,1); Aim(f); Check(f.Own.WeaponsSystem.Shots==0,"Respects native restriction "+condition); }
f=Setup();f.Weapon.Conditions.Remove("IsPowered");f.Control.Authorize(f.Own,f.Ref,1);Aim(f);Check(f.Own.WeaponsSystem.Shots==0,"Unpowered weapon cannot shoot");
f=Setup();f.Weapon.Ammo.Clear();f.Control.Authorize(f.Own,f.Ref,1);Aim(f);Check(f.Own.WeaponsSystem.Shots==0,"Empty ammunition cannot shoot");
f=Setup();f.Control.Authorize(f.Own,f.Ref,2);Aim(f);Check(f.Own.WeaponsSystem.Shots==0,"Only selected group can shoot");
f=Setup();f.Weapon.Item!.fLastRotation=180;f.Control.Authorize(f.Own,f.Ref,1);Aim(f);Check(f.Own.WeaponsSystem.Shots==0,"Target outside installed weapon arc cannot shoot");
f=Setup();f.Control.Authorize(f.Own,f.Ref,1);Aim(f,false);Check(f.Own.WeaponsSystem.Shots==0,"Navigation and braking veto shots");
f=Setup();f.Control.Authorize(f.Own,f.Ref,1);Aim(f,true,10);Check(f.Own.WeaponsSystem.Shots==0,"Time acceleration cannot batch fire or accumulate unchecked aim");
f=Setup();f.Control.Authorize(f.Own,f.Ref,1);NativeContactReader.State=ContactState.Weak;Aim(f);Check(!f.Control.Permitted,"Sensor loss revokes permission");
NativeContactReader.State=ContactState.Ready;Aim(f);Check(f.Own.WeaponsSystem.Shots==0,"Reacquisition cannot silently rearm");
f=Setup();f.Control.Authorize(f.Own,f.Ref,1);f.Control.Reset();Aim(f);Check(f.Own.WeaponsSystem.Shots==0,"World reset has no live fire authority");
f=Setup();f.Control.Authorize(f.Own,f.Ref,1);AutoNavCore.Engaged=false;Aim(f);Check(!f.Control.Permitted,"Manual takeover revokes fire");
f=Setup();f.Weapon.Ammo[0].Conditions.Add("IsAmmoMissile");f.Control.Authorize(f.Own,f.Ref,1);Aim(f);Check(f.Own.WeaponsSystem.Shots==0,"Closed native sensors do not grant missile lock");
GUIOrbitDraw.Open=true;ShipInfo.Lock=99;Aim(f);Check(f.Own.WeaponsSystem.Shots==0,"Incomplete native lock does not grant missile fire");
ShipInfo.Lock=100;Aim(f);Check(f.Own.WeaponsSystem.Shots==1,"Qualified native missile lock permits native shot");
f=Setup();var old=new Ship{strRegID="old"};f.Own.shipCombatTarget=old;f.Own.WeaponsSystem.Throw=true;f.Control.Authorize(f.Own,f.Ref,1);
try { Aim(f); } catch { }
Check(f.Own.shipCombatTarget==old && !f.Control.AllowsNative(f.Own.WeaponsSystem,f.Target.objSS),"Native exception restores combat target and dispatch guard");
var lead=FireRules.Lead(new(0,1000),new(20,0),100);Check(lead.X>0 && lead.Y==1000,"Attitude leads a crossing contact");
f=Setup();StarSystem.fEpoch=42;f.Control.Authorize(f.Own,f.Ref,1);f.Control.Tick(f.Own,f.Ref,.25,true);
Check(f.Control.InArcCount==1 && f.Control.AmmoCount==1 && f.Control.ReadyCount==0 && f.Control.SampleEpoch==42,
    "Readiness reports measured arc and ammo while native aim is incomplete");
f.Control.Tick(f.Own,f.Ref,.25,false);
Check(f.Control.InArcCount==null && f.Control.AmmoCount==null && f.Control.ReadyCount==null && double.IsNaN(f.Control.SampleEpoch),
    "Guidance veto invalidates readiness rather than retaining a stale ready indication");
f.Weapon.Item!.fLastRotation=180;f.Weapon.Ammo.Clear();f.Control.Tick(f.Own,f.Ref,.25,true);
Check(f.Control.InArcCount==0 && f.Control.AmmoCount==0 && f.Control.ReadyCount==0,"Observed unavailable weapons read zero, separately from stale telemetry");
f.Control.Cease();Check(f.Control.InArcCount==null && double.IsNaN(f.Control.SampleEpoch),"Cease Fire clears readiness and observation time");
Check(FireRules.Aim(10,20,10,double.NaN,true)==0,"Nonfinite aim interval cannot acquire lock");
Console.WriteLine($"{checks} fire-control native-boundary assertions passed; no in-game tests performed.");
