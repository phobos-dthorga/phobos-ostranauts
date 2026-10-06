using System;
using System.Collections.Generic;
using System.Linq;
using PhobosAutoNav.Core;
using Phobos.Ostranauts.Framework.Persistence;

namespace PhobosAutoNav;

internal sealed partial class NavigationService
{
    private CondOwner? combatOperator;
    private int combatGroup;
    private string? combatWeapon;

    // Presentation may suggest availability; commands always re-read authoritative state.
    private bool ExclusiveMovement
    { get { bool exclusive = industrial != null || DockingActive; ReadExclusiveMovement(ref exclusive); return exclusive; } }
    partial void ResetCombatSession() { combatOperator = null; combatWeapon = null; combatGroup = 0; }
    private bool CoordinatedFlight(CondOwner? co) => !ExclusiveMovement &&
        (!AutoNavCore.Engaged || console == co && savedFlight != null &&
         (combatActive || savedFlight.Mode == SavedFlightMode.Active || savedFlight.Mode == SavedFlightMode.Rendezvous ||
          savedFlight.Mode == SavedFlightMode.Following));

    internal void ToggleCombat(CondOwner? co)
    {
        if (combatActive) { if (co == console) Disengage(Text.Get("Combat.left")); return; }
        EnterCombat(co);
    }

    private string? CombatProblem(CondOwner? co)
    {
        if (combatActive) return Text.Get("Combat.active");
        if (!Plugin.Enabled.Value) return Text.Get("NavigationService.mod_disabled");
        if (CrewSim.objInstance?.FinishedLoading != true) return Text.Get("NavigationService.world_is_loading");
        if (!HasPursuit(co)) return Text.Get("Pursuit.module_required");
        if (FireHardwareProblem(co) is string fire) return Text.Get(fire);
        if (HardwareProblem(co) is string hardware) return hardware;
        if (OtherControllerBusy()) return Text.Get("NavigationService.disengage_other_flight_automation_first");
        if (!CoordinatedFlight(co)) return Text.Get("Combat.flight");
        if (AutoNavCore.Engaged && !FlightBindingValid()) return Text.Get("Persistence.binding_changed");
        if (!IsLocalConsole(co) || CrewSim.GetSelectedCrew()?.ship != co!.ship) return Text.Get("Combat.pilot");
        if (fireConsole != co || Fire.OtherOwner(co!.strID)) return Text.Get("FCS.busy");
        if (!CanReplaceFlight(co)) return Text.Get("Persistence.invalid_state");
        return null;
    }
    internal void EnterCombat(CondOwner? co)
    {
        try
        {
            // The one thing in the way, by name (Auto Nav 0.35.0); until then one text listed every condition.
            string? blocked = CombatProblem(co);
            if (blocked != null) { status = blocked; return; }
            if (co == null) return;
            if (!FirePreferences(co, out int group, out int volleys, out _) || !ReadPreferences(co!, out var preferences))
            { status = Text.Get("Preferences.invalid"); return; }
            var target = FireTarget;
            if (AttachedFireTarget(co, target)) { status = Text.Get("FCS.attached_target"); return; }
            if (target == null || !ReadContact(co, target).Usable || aimReference == null)
            { status = Text.Get("Combat.select"); return; }
            Fire.Observe(co.ship, target, group, 0);
            var reference = Fire.Weapons.FirstOrDefault(w => w.Id == aimReference && w.Eligible && w.Loaded && w.Heading.HasValue);
            if (reference == null || AimProblem(co) != null) { status = Text.Get("FCS.no_solution"); return; }
            var problem = AdmissionProblem(co, target, preferences.ArrivalKM, 0);
            if (problem != null) { status = problem; return; }
            var flight = CaptureFlight(co, target, preferences.CruiseMS, 0, preferences.ArrivalKM, SavedFlightMode.Following);
            flight.Coast = Plugin.ReadCoastSettings(); flight.PreferTorch = Plugin.PreferTorch.Value;
            if (!flight.Valid) { status = Text.Get("Preferences.invalid"); return; }
            if (Plugin.FuelCheck.Value && !AutoNavCore.HasFuelForFlight(co.ship, target, readOnly: true, profile: flight))
            { status = Text.Get("NavigationService.insufficient_estimated_delta_v"); return; }

            // Stage both protected envelopes without touching the running mission. Commit detached
            // maps synchronously before any controller/native callback can observe the handoff.
            var staged = new Dictionary<string, Dictionary<string, string>>(co.mapGUIPropMaps);
            FlightSnapshot? previous = null;
            if (Store(co).Read(out var fields) == SavedStateStatus.Ready)
            {
                if (!FlightSnapshot.TryDecode(fields, out previous)) { status = Text.Get("Persistence.invalid_state"); return; }
                if (previous.IsActive)
                {
                    if (AutoNavCore.Engaged) { previous.ElapsedSeconds = AutoNavCore.ElapsedSeconds; previous.Coasting = AutoNavCore.Coasting; }
                    previous.Mode = previous.SuspendedMode;
                }
                if (!new ObjectStateStore(staged, FlightSnapshot.StoreName, co.strID, FlightSnapshot.Schema).TryWrite(previous.Encode()))
                { status = Text.Get("Persistence.write_failed"); return; }
            }
            if (!new ObjectStateStore(staged, "AutoNav.FirePreferences", co.strID, 1).TryWrite(new Dictionary<string,string>
                { ["group"] = group.ToString(), ["volleys"] = volleys.ToString(), ["hold"] = bool.TrueString }))
            { status = Text.Get("Persistence.write_failed"); return; }
            foreach (var key in new[] { "PhobosState.AutoNav.Flight", "PhobosState.AutoNav.FirePreferences" })
                if (staged.TryGetValue(key, out var map)) co.mapGUIPropMaps[key] = map;

            CeaseFire(); arrivalWatch.Cancel(); issuing = true;
            // From this point all failures leave the original mission suspended, never overwritten.
            console = co; combatPrevious = previous; combatActive = true; savedFlight = flight;
            try
            {
                if (AutoNavCore.Engaged) AutoNavCore.EndFlight(AutoNavCore.EngagedPlayer, "COMBAT");
                co.ship.UnlockFromOrbit(); co.ship.objSS.ResetNavData();
                AutoNavCore.RestoreFlight(co.ship, target, flight);
                Fire.SetOwnership(co.strID, co.ship, group, true);
                fireModule = FireModule(co); firePlayer = CrewSim.coPlayer; fireShip = co.ship;
                combatOperator = CrewSim.GetSelectedCrew(); combatGroup = group; combatWeapon = aimReference;
                autoAim = true; status = Text.Get("Combat.entered");
            }
            finally { issuing = false; }
        }
        catch (Exception ex) { log(ex.ToString()); if (combatActive) Disengage(Text.Get("Combat.ended")); else status = Text.Get("Persistence.write_failed"); }
    }

    private bool CombatBindingValid() => !combatActive || console != null && AutoNavCore.Engaged &&
        AutoNavCore.EngagedPlayer == console.ship && FlightBindingValidNow() && HasPursuit(console) &&
        FireHardwareProblem(console) == null && console == fireConsole && fireShip == console.ship &&
        fireModule != null && FireModule(console) == fireModule && firePlayer == CrewSim.coPlayer &&
        combatOperator == CrewSim.GetSelectedCrew() && Fire.Owns(console.strID) &&
        !Fire.OtherOwner(console.strID) && WeaponGroup(console) == combatGroup &&
        savedFlight?.TargetId == fireTargetId && aimReference == combatWeapon &&
        !AttachedFireTarget(console, FireTarget) && ReadContact(console, FireTarget).Usable;

    internal void ValidateCombat()
    { if (!CombatBindingValid()) Disengage(Text.Get("Combat.ended")); }

    private string MovementReason() => Text.Get(avoidanceActive ? "Combat.avoiding" : DockingActive ? "Combat.docking" :
        AutoNavCore.CurrentPhase == AutoNavCore.Phase.Decel ? "Combat.braking" :
        Torch.Reason == "Torch.aligning" ? "Torch.aligning" : Torch.Reason == "Torch.burning" ? "Torch.burning" :
        AutoNavCore.CurrentPhase == AutoNavCore.Phase.Align ? "Combat.aligning" : "Combat.matching");
}
