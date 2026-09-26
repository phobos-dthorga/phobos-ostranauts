using System;
using System.Linq;
using PhobosAutoNav.Core;
using Phobos.Ostranauts.Framework.Persistence;

namespace PhobosAutoNav;

internal sealed partial class NavigationService
{
    private static ObjectStateStore PreferenceStore(CondOwner co) =>
        new ObjectStateStore(co.mapGUIPropMaps, FlightPreferences.StoreName, co.strID, FlightPreferences.Schema);

    private static bool ReadPreferences(CondOwner co, out FlightPreferences preferences)
    {
        var state = PreferenceStore(co).Read(out var data);
        preferences = new FlightPreferences(Plugin.DefaultCruiseMS.Value,
            Math.Min(Plugin.DefaultArriveSpeedMS.Value, Plugin.DefaultCruiseMS.Value), Plugin.DefaultArriveKM.Value);
        if (state == SavedStateStatus.Missing) return preferences.Valid;
        if (state == SavedStateStatus.Ready && FlightPreferences.TryDecode(data, out var saved))
        { preferences = saved; return true; }
        return false;
    }

    private static bool SettingsHardwareReady(CondOwner? co) => IsLocalConsole(co) &&
        CrewSim.objInstance != null && CrewSim.objInstance.FinishedLoading && !co!.HasCond("IsDamaged") &&
        co.GetCOsSafe(true).Any(item => (HasId(item, ModuleId) || HasId(item, PursuitId)) && !item.HasCond("IsDamaged"));

    private bool CanChangePreferences(CondOwner? co)
    {
        if (!SettingsHardwareReady(co)) { status = Text.Get("Preferences.console_required"); return false; }
        if (AutoNavCore.Engaged || DisplaySnapshot(co) != null)
        { status = Text.Get("Preferences.captured"); return false; }
        return CanReplaceFlight(co!);
    }

    internal bool SetFlightSetting(CondOwner? co, FlightSetting setting, double value)
    {
        if (!CanChangePreferences(co)) return false;
        if (!ReadPreferences(co!, out var before)) { status = Text.Get("Preferences.invalid"); return false; }
        var after = setting switch
        {
            FlightSetting.Cruise => new FlightPreferences(value, Math.Min(value, before.ArrivalMS), before.ArrivalKM),
            FlightSetting.ArrivalSpeed => new FlightPreferences(before.CruiseMS, value, before.ArrivalKM),
            FlightSetting.ArrivalDistance => new FlightPreferences(before.CruiseMS, before.ArrivalMS, value),
            _ => default
        };
        if (!after.Valid) { status = Text.Get("Preferences.limits", FlightPreferences.MinimumCruiseMS,
            FlightPreferences.MaximumCruiseMS, Math.Min(FlightPreferences.MaximumArrivalMS, before.CruiseMS),
            ApproachRules.MinimumArrivalKM, ApproachRules.MaximumArrivalKM); return false; }
        if (!PreferenceStore(co!).TryWrite(after.Encode())) { status = Text.Get("Preferences.invalid"); return false; }
        status = Text.Get("Preferences.saved", after.CruiseMS, after.ArrivalMS, after.ArrivalKM);
        return true;
    }

    internal void StepPanelSpeed(CondOwner? co, bool arrival, int direction)
    {
        if (!CanChangePreferences(co)) return;
        if (!ReadPreferences(co!, out var preferences)) { status = Text.Get("Preferences.invalid"); return; }
        double value = InstrumentRules.StepSpeed(arrival ? preferences.ArrivalMS : preferences.CruiseMS,
            direction, arrival, preferences.CruiseMS);
        SetFlightSetting(co, arrival ? FlightSetting.ArrivalSpeed : FlightSetting.Cruise, value);
    }
    internal FlightPreferences PanelPreferences(CondOwner co)=>ReadPreferences(co,out var value)?value:default;
    internal string PanelPreferenceStamp(CondOwner co)=>string.Join("|",PanelPreferences(co).Encode().OrderBy(p=>p.Key).Select(p=>p.Key+":"+p.Value))+"|"+Plugin.PreferTorch.Value;
    internal bool ApplyPanelPreferences(CondOwner co,string expected,FlightPreferences value,bool torch)
    {
        if(!CanChangePreferences(co))return false;
        if(!value.Valid||PanelPreferenceStamp(co)!=expected||!ReadPreferences(co,out _))
        {status=Phobos.Ostranauts.Framework.Controls.ConsoleText.Get("stale");return false;}
        // One validated main-thread commit. Existing property identity and captured flights are untouched.
        if(!PreferenceStore(co).TryWrite(value.Encode())){status=Text.Get("Preferences.invalid");return false;}
        SetTorchPreference(torch);
        CrewSettingsChanged(co);
        status=Phobos.Ostranauts.Framework.Controls.ConsoleText.Get("applied");return true;
    }
    partial void CrewSettingsChanged(CondOwner co);

    private bool ResetPreferences(CondOwner? co)
    {
        if (!CanChangePreferences(co)) return false;
        PreferenceStore(co!).Clear();
        status = Text.Get("Preferences.reset");
        return true;
    }
}
