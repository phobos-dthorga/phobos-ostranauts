using System.Linq;
using PhobosAutoNav.Core;
using Phobos.Ostranauts.Framework.Persistence;

namespace PhobosAutoNav;

internal sealed partial class NavigationService
{
    private static bool HasPursuit(CondOwner? co) => IsLocalConsole(co) &&
        co!.GetCOsSafe(true).Any(item => HasId(item, PursuitId) && !item.HasCond("IsDamaged"));
    private static ObjectStateStore PursuitStore(CondOwner co) => new(co.mapGUIPropMaps, "AutoNav.PursuitPreferences", co.strID, 1);
    private static int WeaponGroup(CondOwner? co)
    {
        if (!IsLocalConsole(co)) return 0;
        var state = PursuitStore(co!).Read(out var fields);
        if (state == SavedStateStatus.Missing) return 1;
        return state == SavedStateStatus.Ready && fields.TryGetValue("weaponGroup", out var value) &&
            int.TryParse(value, out int group) && group >= 1 && group <= 9 ? group : 0;
    }
    private CondOwner? fireSelectionConsole;
    partial void ResetPursuit() { fireTargetId = null; fireSelectionConsole = null; }
    private string? fireTargetId;
    internal string PursuitSummary(CondOwner? co) => Text.Get("Pursuit.summary", WeaponGroup(co),
        fireSelectionConsole != co || fireTargetId == null ? Text.Get("Instruments.no_target") : TargetRef.FromShipId(fireTargetId)?.DisplayName ?? fireTargetId,
        Text.Get(Fire.Reason), AutoNavCore.PredictionHorizon, AutoNavCore.PredictionError);
    internal void SelectFireTarget(CondOwner? co)
    {
        if (!HasPursuit(co) || AutoNavCore.Engaged && console != co) return;
        CeaseFire();
        var selected = TargetRef.FromCrossHair();
        if (selected == null || selected.ShipId == co!.ship.strRegID || !ReadContact(co, selected).Usable)
        { fireTargetId = null; status = Text.Get("Pursuit.select_fire_target"); return; }
        fireTargetId = selected.ShipId; fireSelectionConsole = co;
        status = Text.Get("Pursuit.target_selected", selected.DisplayName);
    }
    internal void StartPursuit(CondOwner? co, bool follow, float? distance = null) =>
        Engage(co, distance, follow ? SavedFlightMode.Following : SavedFlightMode.Rendezvous);
    internal void PursuitOrResume(CondOwner co)
    { if (HasResumableFlight(co)) ResumeSaved(co); else StartPursuit(co, false); }
    internal void SelectWeapons(CondOwner? co, int group)
    {
        if (!HasPursuit(co) || group < 1 || group > 9 || AutoNavCore.Engaged && console != co)
        { status = Text.Get("Pursuit.module_required"); return; }
        if (WeaponGroup(co) == 0 || !PursuitStore(co!).TryWrite(new System.Collections.Generic.Dictionary<string,string>
            { ["weaponGroup"] = group.ToString(System.Globalization.CultureInfo.InvariantCulture) }))
        { status = Text.Get("Preferences.invalid"); return; }
        CeaseFire();
        status = Text.Get("Pursuit.weapon_group", group);
    }
    internal void StepWeapons(CondOwner? co) => SelectWeapons(co, WeaponGroup(co) % 9 + 1);
    internal void EngageWeapons(CondOwner? co)
    {
        // Engagement never starts navigation or takes the crosshair as permission.
        if (!HasPursuit(co) || co != console || !AutoNavCore.Engaged || savedFlight?.IsFollowing != true ||
            !ReadContact(co, AutoNavCore.EngagedTarget).Usable)
        { status = Text.Get("Pursuit.follow_first"); return; }
        var offensive = fireSelectionConsole != co || fireTargetId == null ? null : TargetRef.FromShipId(fireTargetId);
        if (offensive == null || !ReadContact(co, offensive).Usable)
        { status = Text.Get("Pursuit.select_fire_target"); return; }
        int group = WeaponGroup(co);
        if (group == 0) { status = Text.Get("Preferences.invalid"); return; }
        Fire.Authorize(co!.ship, offensive, group);
        AutoNavCore.FaceTarget = Fire.Permitted;
        status = Text.Get(Fire.Reason);
    }
    internal void CeaseFire()
    {
        Fire.Cease(); AutoNavCore.FaceTarget = false;
        status = Text.Get("Pursuit.ceased");
    }
}
