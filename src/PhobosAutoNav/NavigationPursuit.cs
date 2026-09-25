using System;
using System.Linq;
using System.Collections.Generic;
using PhobosAutoNav.Core;
using Phobos.Ostranauts.Framework.Persistence;
namespace PhobosAutoNav;

internal sealed partial class NavigationService
{
    private static bool HasPursuit(CondOwner? co) => IsLocalConsole(co) && co!.GetCOsSafe(true).Any(item => HasId(item, PursuitId) && !item.HasCond("IsDamaged"));
    private static CondOwner? FireModule(CondOwner? co) => IsLocalConsole(co) ? co!.GetCOsSafe(true).FirstOrDefault(i => HasId(i, FireControlId) && !i.HasCond("IsDamaged")) : null;
    private static ObjectStateStore PursuitStore(CondOwner co) => new(co.mapGUIPropMaps, "AutoNav.PursuitPreferences", co.strID, 1);
    private static ObjectStateStore FireStore(CondOwner co) => new(co.mapGUIPropMaps, "AutoNav.FirePreferences", co.strID, 1);
    private CondOwner? fireConsole, fireModule, firePlayer;
    private Ship? fireShip;
    private string? fireTargetId, aimReference, viewedWeapon;
    private bool autoAim, standaloneAim, fireRestorePending = true;
    internal bool StandaloneAimFor(ShipSitu situ) => standaloneAim && fireShip?.objSS == situ;
    private static bool FirePreferences(CondOwner? co, out int group, out int volleys, out bool held, bool restoring = false)
    {
        group = volleys = 1; held = false;
        if (restoring ? co == null || co.ship != CrewSim.coPlayer?.ship : !IsLocalConsole(co)) return false;
        var state = FireStore(co!).Read(out var fields);
        if (state == SavedStateStatus.Missing)
        {
            // A legacy group preference is harmless; target/permission never migrate.
            if (PursuitStore(co!).Read(out var legacy) == SavedStateStatus.Ready && legacy.TryGetValue("weaponGroup", out var old) && int.TryParse(old, out int g) && g >= 1 && g <= 9) group = g;
            return true;
        }
        return state == SavedStateStatus.Ready && fields.TryGetValue("group", out var a) && int.TryParse(a, out group) && group >= 1 && group <= 9 &&
            fields.TryGetValue("volleys", out var b) && int.TryParse(b, out volleys) && volleys >= 1 && volleys <= 9 &&
            fields.TryGetValue("hold", out var c) && bool.TryParse(c, out held);
    }
    private static int WeaponGroup(CondOwner? co) => FirePreferences(co, out int group, out _, out _) ? group : 0;
    private static bool SaveFirePreferences(CondOwner co, int group, int volleys, bool held) => FireStore(co).TryWrite(new Dictionary<string,string>
        { ["group"] = group.ToString(), ["volleys"] = volleys.ToString(), ["hold"] = held.ToString() });
    partial void ResetPursuit() { fireConsole = fireModule = firePlayer = null; fireShip = null; fireTargetId = aimReference = viewedWeapon = null; autoAim = standaloneAim = false; fireRestorePending = true; }
    internal string PursuitSummary(CondOwner? co)
    {
        bool fresh = ArrivalBrake.Finite(Fire.SampleEpoch) && StarSystem.fEpoch >= Fire.SampleEpoch && StarSystem.fEpoch - Fire.SampleEpoch <= FireRules.MaximumStep;
        return (co == fireConsole ? Text.Get("FCS.details", FireTarget?.DisplayName ?? "—",
            DisplayWeapon?.Name ?? "—", Text.Get(fresh ? DisplayWeapon?.Reason ?? "FCS.unavailable" : "FCS.unavailable"),
            Fire.Weapons.FirstOrDefault(w => w.Id == aimReference)?.Name ?? "—") + "\n\n" : "") + Text.Get("FCS.help");
    }
    internal void StartPursuit(CondOwner? co, bool follow, float? distance = null) => Engage(co, distance, follow ? SavedFlightMode.Following : SavedFlightMode.Rendezvous);
    internal void PursuitOrResume(CondOwner co) { if (HasResumableFlight(co)) ResumeSaved(co); else StartPursuit(co, false); }
    private static string? FireHardwareProblem(CondOwner? co)
    {
        if (!IsLocalConsole(co) || FireModule(co) == null) return "FCS.module_required";
        if (co!.HasCond("IsOff") || !co.HasCond("IsPowered") || co.HasCond("IsDamaged") || co.HasCond("IsDamagedSoftware")) return "FCS.unpowered";
        if (co.ship.bDestroyed || co.ship.objSS == null || co.ship.IsDocked() || co.ship.IsMoored()) return "FCS.unavailable";
        return null;
    }
    private bool BindFire(CondOwner? co)
    {
        if (!Plugin.Enabled.Value || FireHardwareProblem(co) != null || !FirePreferences(co, out _, out _, out _) || Fire.OtherOwner(co!.strID))
        { status = Text.Get(FireHardwareProblem(co) ?? "FCS.busy"); return false; }
        if (fireConsole != co) { CeaseFire(); fireTargetId = aimReference = viewedWeapon = null; Fire.Invalidate(); }
        fireConsole = co; return true;
    }
    internal void SelectFireTarget(CondOwner? co)
    {
        if (!BindFire(co)) return;
        CeaseFire(); var selected = TargetRef.FromCrossHair();
        fireTargetId = selected != null && selected.ShipId != co!.ship.strRegID && ReadContact(co, selected).Usable ? selected.ShipId : null;
        aimReference = null; Fire.Invalidate(); status = Text.Get(fireTargetId == null ? "Pursuit.select_fire_target" : "FCS.target_selected");
    }
    internal void SelectWeapons(CondOwner? co, int group)
    {
        if (!BindFire(co) || group < 1 || group > 9 || !FirePreferences(co, out _, out int volleys, out bool held)) return;
        if (held || Fire.Owns(co!.strID)) { status = Text.Get("FCS.return_first"); return; }
        CeaseFire();
        if (!SaveFirePreferences(co!, group, volleys, false)) { status = Text.Get("Preferences.invalid"); return; }
        aimReference = viewedWeapon = null; Fire.Invalidate();
    }
    internal void StepWeapons(CondOwner? co) => SelectWeapons(co, WeaponGroup(co) % 9 + 1);
    internal void StepVolleys(CondOwner? co)
    {
        if (!BindFire(co) || !FirePreferences(co, out int group, out int volleys, out bool held)) return;
        CeaseFire(); if (!SaveFirePreferences(co!, group, volleys % 9 + 1, held)) status = Text.Get("Preferences.invalid");
    }
    private bool TakeFireControl(CondOwner? co)
    {
        if (!BindFire(co) || DockingActive || AutoNavCore.Engaged && (console != co || !AutoNavCore.Following) ||
            !FirePreferences(co, out int group, out int volleys, out _) || !ReadContact(co, FireTarget).Usable)
        { status = Text.Get("FCS.contact"); return false; }
        if (!SaveFirePreferences(co!, group, volleys, true)) { status = Text.Get("Preferences.invalid"); return false; }
        if (!Fire.Owns(co!.strID)) Fire.SetOwnership(co.strID, co.ship, group, true);
        fireModule = FireModule(co); firePlayer = CrewSim.coPlayer; fireShip = co.ship;
        return true;
    }
    private TargetRef? FireTarget => fireTargetId == null ? null : TargetRef.FromShipId(fireTargetId);
    internal void ToggleFireOwnership(CondOwner? co)
    {
        if (!FirePreferences(co, out int group, out int volleys, out bool held)) return;
        if (held || Fire.Owns(co!.strID)) { ReturnFireToNative(co); return; }
        if (!BindFire(co)) return;
        CeaseFire();
        if (!SaveFirePreferences(co!, group, volleys, true)) { status = Text.Get("Preferences.invalid"); return; }
        Fire.SetOwnership(co!.strID, co.ship, group, true); status = Text.Get("FCS.hold");
    }
    internal void EngageWeapons(CondOwner? co)
    {
        if (!TakeFireControl(co) || FireTarget == null || !FirePreferences(co, out int group, out int volleys, out _)) return;
        Fire.Observe(co!.ship, FireTarget, group, 0);
        Fire.Authorize(co.ship, fireTargetId!, group, volleys); status = Text.Get(Fire.Reason);
    }
    internal void ReturnFireToNative(CondOwner? co)
    {
        if (!IsLocalConsole(co) || !FirePreferences(co, out int group, out int volleys, out _)) return;
        CeaseFire();
        if (!SaveFirePreferences(co!, group, volleys, false)) { status = Text.Get("Preferences.invalid"); return; }
        Fire.SetOwnership(co!.strID, co.ship, group, false); status = Text.Get("FCS.returned");
    }
    internal void BrowseWeapon(CondOwner? co)
    {
        if (!BindFire(co) || Fire.Weapons.Count == 0) return;
        int index = Fire.Weapons.ToList().FindIndex(w => w.Id == viewedWeapon);
        viewedWeapon = Fire.Weapons[(index + 1) % Fire.Weapons.Count].Id;
    }
    internal void UseAimReference(CondOwner? co)
    {
        if (!BindFire(co)) return;
        var weapon = DisplayWeapon; if (weapon == null || !weapon.Eligible) return;
        CeaseFire(); aimReference = weapon.Id;
    }
    private WeaponReading? DisplayWeapon => Fire.Weapons.FirstOrDefault(w => w.Id == viewedWeapon) ?? Fire.Weapons.FirstOrDefault();
    internal void ToggleAutoAim(CondOwner? co)
    {
        if (autoAim) { CeaseFire(); return; }
        if (!TakeFireControl(co) || AimProblem(co!) != null) { status = Text.Get("FCS.aim_unavailable"); return; }
        Fire.Observe(co!.ship, FireTarget, WeaponGroup(co), 0);
        aimReference ??= Fire.Weapons.FirstOrDefault(w => w.Eligible && w.Loaded && w.Heading.HasValue)?.Id;
        if (aimReference == null) { status = Text.Get("FCS.no_solution"); return; }
        Fire.Cease();
        autoAim = true;
    }
    private string? AimProblem(CondOwner co)
    {
        var ship = co.ship;
        if (DockingActive || ship.RCSCount <= 0 || ship.GetRCSRemain() <= 0 || ReadThrottle(co) <= 0 ||
            !ArrivalBrake.Finite(ship.RCSAccelMax) || ship.RCSAccelMax <= 0 || !ArrivalBrake.Finite(ship.objSS.fW) ||
            !ArrivalBrake.Finite(ship.objSS.fRot) || CrewSim.system.IsInAtmo(ship) ||
            AutoNavCore.Engaged && (console != co || !AutoNavCore.Following)) return "FCS.aim_unavailable";
        if (!AutoNavCore.Engaged && (TorchDriveController.ThrustRequested(ship) || ship.shipStationKeepingTarget != null ||
            ship.aWPs?.Count > 0 || PropOn(co, "chkStationKeeping") || PropOn(co, "chkHoldThrust") || PropOn(co, "chkEngage") ||
            AIShipManager.GetAIShipByRegID(ship.strRegID) != null || ship.objSS.vAccIn.magnitude > 0)) return "FCS.aim_unavailable";
        return null;
    }
    private void StopAim()
    {
        autoAim = false; AutoNavCore.FaceTarget = false; AutoNavCore.WeaponHeading = null;
        bool clear = standaloneAim; standaloneAim = false;
        if (clear && fireShip?.objSS != null)
        {
            issuing = true;
            try { fireShip.Maneuver(0, 0, 0, 0, 0); }
            catch (Exception ex) { log(ex.ToString()); }
            finally { issuing = false; }
        }
    }
    internal void CeaseFire() { Fire.Cease(); StopAim(); status = Text.Get(Fire.Reason); }
    private void FireFault(string reason) { Fire.Cease(reason, true); StopAim(); Fire.Invalidate(); status = Text.Get(reason); }
    // Restore ownership before any native offensive queue can dispatch. No module,
    // prediction, target or permission is synthesized during restoration.
    internal void RestoreFireOwnership()
    {
        if (!fireRestorePending || CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading || CrewSim.coPlayer?.ship == null) return;
        fireRestorePending = false;
        foreach (var co in CrewSim.coPlayer.ship.GetCOs(null, bSubObjects: false, bAllowDocked: false, bAllowLocked: true))
            if (FireStore(co).Read(out _) == SavedStateStatus.Ready && FirePreferences(co, out int group, out _, out bool held, restoring: true) && held)
                Fire.SetOwnership(co.strID, co.ship, group, true);
    }
    internal void TickFire(double dt, bool dispatch)
    {
        if (CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading) return;
        RestoreFireOwnership();
        if (CrewSim.Paused) return;
        try
        {
            if (!dispatch && fireConsole == null && FireModule(OpenConsole) != null) fireConsole = OpenConsole;
            if (fireConsole == null) return;
            var co = fireConsole; var ship = co.ship;
            if (!Plugin.Enabled.Value || FireHardwareProblem(co) is { } || !FirePreferences(co, out int group, out _, out _)) { FireFault("FCS.unavailable"); return; }
            if (!ArrivalBrake.Finite(dt) || dt <= 0 || dt > FireRules.MaximumStep) { FireFault("FCS.step"); return; }
            if ((Fire.Permitted || autoAim) && (ship != fireShip || fireModule == null || FireModule(co) != fireModule || CrewSim.coPlayer != firePlayer ||
                !ReadContact(co, FireTarget).Usable)) { FireFault("FCS.binding"); return; }
            if (dispatch)
            {
                bool safe = !DockingActive && (!AutoNavCore.Engaged || console == co && AutoNavCore.Following &&
                    AutoNavCore.CurrentPhase != AutoNavCore.Phase.Decel && !AutoNavCore.ControlLimited && !ship.IsUsingTorchDrive);
                Fire.Dispatch(ship, FireTarget, safe);
                if (Fire.State == FireState.Fault || !Fire.Permitted && Fire.Reason == "FCS.complete") StopAim();
                return;
            }
            Fire.Observe(ship, FireTarget, group, dt);
            if (Fire.State == FireState.Fault) { StopAim(); return; }
            if (!autoAim) return;
            if (AimProblem(co) != null) { FireFault("FCS.aim_unavailable"); return; }
            var reference = Fire.Weapons.FirstOrDefault(w => w.Id == aimReference && w.Eligible && w.Loaded);
            if (reference?.Heading == null) { FireFault("FCS.no_solution"); return; }
            AutoNavCore.FaceTarget = true; AutoNavCore.WeaponHeading = reference.Heading;
            if (!AutoNavCore.Engaged)
            {
                double error = DockingRules.Wrap(reference.Heading.Value - ship.objSS.fRot);
                float rotation = (float)FireRules.Turn(error, ship.objSS.fW, dt, ReadThrottle(co), Plugin.RotAccelMax.Value, Plugin.RotSpeedMax.Value);
                issuing = true; standaloneAim = true;
                try { ship.Maneuver(0, 0, rotation, 0, (float)dt); } finally { issuing = false; }
            }
        }
        catch (Exception ex) { FireFault("FCS.dispatch_fault"); log(ex.ToString()); }
    }
}
