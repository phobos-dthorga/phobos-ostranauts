using System;
using System.Globalization;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Controls;
using PhobosShipbreaker.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using C = Phobos.Ostranauts.Framework.Controls.ConsoleWidgets;
using W = Phobos.Ostranauts.Framework.Controls.PanelWidgets;

namespace PhobosShipbreaker;

/// <summary>Display-only composition. Every input delegates to the same checked service.</summary>
public sealed class FurnaceInstrumentView : MonoBehaviour
{
    private string target = "";
    private GUIKnob? mode;
    private GUILedMeter? heat, temperature, cooling;
    private GUILamp? enabledLamp;
    private GUISafetyToggle? heatEnable;
    private GUI7Seg? chargeDigits, powerDigits;
    private TMP_Text chargeCaption = null!, powerCaption = null!;
    private readonly List<Action> settingRefreshes = new();
    private TMP_Text status = null!;
    private float nextRefresh;
    private bool dirty;
    private readonly Dictionary<string,string> values=new();
    private string expected="";
    private ConsoleShell shell=null!;
    private ConsoleBinding? binding;
    private Func<bool>? oldDirty,oldApply,ownDirty;
    private Action? oldDiscard;
    private Button applyButton=null!,discardButton=null!;
    private bool detached;
    internal static void Build(Transform parent, CondOwner furnace, Action<string, string?> command, ConsoleShell shell, ConsoleBinding? binding)
    {
        var root = W.Rect(parent, "FurnaceInstruments");
        var group = root.gameObject.AddComponent<VerticalLayoutGroup>(); group.spacing = 8; group.childForceExpandHeight = false; group.childControlWidth = group.childControlHeight = true;
        var view = root.gameObject.AddComponent<FurnaceInstrumentView>(); view.target = furnace.strID;
        view.shell=shell;view.binding=binding;view.expected=FurnaceService.SettingsStamp(furnace);
        view.oldDirty=shell.Dirty;view.oldApply=shell.Apply;view.oldDiscard=shell.Discard;view.ownDirty=()=>view.dirty;
        shell.Dirty=view.ownDirty;shell.Apply=view.ApplyDraft;shell.Discard=view.DiscardDraft;
        view.applyButton=C.Button(shell.Actions,C.Text("apply"),()=>view.ApplyDraft());view.applyButton.transform.SetSiblingIndex(0);
        view.discardButton=C.Button(shell.Actions,C.Text("discard"),view.DiscardDraft);view.discardButton.transform.SetSiblingIndex(1);
        var headline = W.Label(root, Text.Get("Furnace.panel_heading")); NativeInstruments.UseNativeFont(headline);
        var row = W.Rect(root, "Meters"); var columns = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        columns.spacing = 24; columns.childForceExpandWidth = false; columns.childForceExpandHeight = false; columns.childControlWidth = columns.childControlHeight = true;
        view.mode = NativeInstruments.Clone<GUIKnob>(row, "pnlPower/knobBus", Plugin.Log);
        if (view.mode != null) view.mode.Callback = n => command(n == 0 ? "stop" : n == 1 ? "auto-run" : "step-run", null);
        view.heatEnable = NativeInstruments.Clone<GUISafetyToggle>(row, "pnlPower/chkThrustSafety", Plugin.Log);
        if (view.heatEnable != null)
        {
            view.heatEnable.bSilent = false;
            view.heatEnable.chkSwitch.onValueChanged.AddListener(on => { command(on ? "resume" : "stop", null); view.Refresh(); });
        }
        view.enabledLamp = NativeInstruments.Clone<GUILamp>(row, "pnlInit/pnlStepBus/bmpGreen", Plugin.Log);
        W.Label(root, Text.Get("Furnace.guard_legend"));
        var gauges = W.Rect(root, "Gauges"); var gaugeLayout = gauges.gameObject.AddComponent<HorizontalLayoutGroup>();
        gaugeLayout.spacing = 16; gaugeLayout.childForceExpandWidth = gaugeLayout.childForceExpandHeight = false;
        gaugeLayout.childControlWidth = gaugeLayout.childControlHeight = true;
        view.temperature = NativeInstruments.Clone<GUILedMeter>(gauges, "pnlCoreTemp/pnlLeds", Plugin.Log);
        view.heat = NativeInstruments.Clone<GUILedMeter>(gauges, "pnlPower/pnlLedsTotal", Plugin.Log);
        view.cooling = NativeInstruments.Clone<GUILedMeter>(gauges, "pnlCoreTemp/pnlLeds", Plugin.Log);
        W.Label(root, Text.Get("Furnace.meter_legend"));
        view.chargeCaption = W.Label(root, "");
        view.chargeDigits = NativeInstruments.Clone<GUI7Seg>(root, "pnlFuel/pnlHe3", Plugin.Log);
        view.powerCaption = W.Label(root, "");
        view.powerDigits = NativeInstruments.Clone<GUI7Seg>(root, "pnlFuel/pnlHe3", Plugin.Log);
        view.status = W.Label(root, ""); NativeInstruments.UseNativeFont(view.status);
        // Explicit buttons remain keyboard-accessible, and serve as diagnosed fallback
        // if a game update removes a native donor. No guard can hide emergency stop.
        var operations=new[]{"seal","resume","next","equalize","release","automatic","step-mode"};
        for(int i=0;i<operations.Length;i+=2){var actions=C.Row(root);foreach(var a in System.Linq.Enumerable.Take(System.Linq.Enumerable.Skip(operations,i),2)){string action=a;C.Button(actions,Text.Get("Furnace.action_"+a),()=>command(action,null));}}
        Setting("heat", FurnaceRules.MinPowerSettingKW, FurnaceRules.HeatLimitKW, b => b.HeatCapKW);
        Setting("ramp", FurnaceRules.MinRamp, FurnaceRules.MaxRamp, b => b.RampKPerSecond);
        Setting("cool", FurnaceRules.MinPowerSettingKW, FurnaceRules.CoolingKW, b => b.CoolingCapKW);
        void Setting(string action, double min, double max, Func<FurnaceBatch, double> read)
        {
            W.Label(root, Text.Get("Furnace.setting_" + action));
            double initial = read(FurnaceService.Get(furnace).State.Batch);
            string value = initial.ToString(CultureInfo.InvariantCulture);
            view.values[action]=value;
            var controlRow = W.Rect(root, "Setting"); var controlLayout = controlRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            controlLayout.spacing = 16; controlLayout.childControlWidth = controlLayout.childControlHeight = true; controlLayout.childForceExpandHeight = false;
            var slider = NativeInstruments.Clone<Slider>(controlRow, "pnlPower/SliderFlow", Plugin.Log);
            var entry = C.Input(controlRow, value, value, text => { value = text; view.values[action]=value; view.dirty = true; });
            entry.gameObject.GetComponent<LayoutElement>().flexibleWidth = 1;
            if (slider != null)
            {
                slider.minValue = (float)min; slider.maxValue = (float)max; slider.wholeNumbers = action != "ramp";
                slider.SetValueWithoutNotify((float)initial);
                slider.onValueChanged.AddListener(n =>
                {
                    value = Math.Round(n, action == "ramp" ? 1 : 0).ToString(CultureInfo.InvariantCulture);
                    entry.SetTextWithoutNotify(value); view.values[action]=value; view.dirty = true;
                });
            }
            view.settingRefreshes.Add(() =>
            {
                if (view.dirty || entry.isFocused) return;
                var target = CollectorService.Resolve(view.target); if (target == null) return;
                double current = read(FurnaceService.Get(target).State.Batch);
                value = current.ToString(CultureInfo.InvariantCulture); view.values[action]=value; entry.SetTextWithoutNotify(value);
                if (slider != null) slider.SetValueWithoutNotify((float)current);
            });
        }
        view.Refresh();
    }
    private void Update() { if (Time.unscaledTime >= nextRefresh) { nextRefresh = Time.unscaledTime + .25f; Refresh(); } }
    private void Refresh()
    {
        var co = CollectorService.Resolve(target);
        if (co == null) { status.text = Text.Get("Industry.missing"); return; }
        var s = FurnaceService.Get(co); var b = s.State.Batch;
        if(!dirty)expected=FurnaceService.SettingsStamp(co);
        bool valid = !s.Protected && FurnaceService.ProbeValid(co);
        if (mode != null) NativeInstruments.Refresh(mode, !b.Armed ? 0 : b.StepMode ? 2 : 1);
        if (heatEnable != null) NativeInstruments.Refresh(heatEnable, b.Armed);
        if (temperature != null) NativeInstruments.Refresh(temperature, valid ? (b.TemperatureK - 273.15) / 750 : (double?)null);
        if (heat != null) NativeInstruments.Refresh(heat, valid ? s.DeliveredKW / (FurnaceRules.HeatLimitKW / FurnaceRules.Efficiency + FurnaceRules.HeatAuxKW) : (double?)null);
        var radiator = FurnaceService.CoolingEndpoint(co);
        if (cooling != null) NativeInstruments.Refresh(cooling, valid && radiator != null && !radiator.HasCond("IsDamaged") && !FurnaceService.Get(radiator).Protected ?
            (FurnaceRules.SinkCapacity * (FurnaceRules.SinkMaxK - FurnaceRules.ReferenceK) - FurnaceService.Get(radiator).SinkKJ) / (FurnaceRules.SinkCapacity * (FurnaceRules.SinkMaxK - FurnaceRules.ReferenceK)) : (double?)null);
        if (enabledLamp != null) enabledLamp.State = valid && b.Armed ? GUILamp.STATE_ON : GUILamp.STATE_OFF;
        Measurement(chargeDigits, chargeCaption, "Furnace.charge_reading", valid ? b.TemperatureK - 273.15 : (double?)null);
        Measurement(powerDigits, powerCaption, "Furnace.power_reading", valid ? s.DeliveredKW : (double?)null);
        foreach (var refresh in settingRefreshes) refresh();
        status.text = Text.Get("Furnace.live", valid ? (b.TemperatureK - 273.15).ToString("F1", CultureInfo.CurrentCulture) : Text.Get("Furnace.unknown"),
            valid ? s.DeliveredKW.ToString("F1", CultureInfo.CurrentCulture) : Text.Get("Furnace.unknown"), Text.Get("Furnace.phase_" + b.Phase));
    }
    private static void Measurement(GUI7Seg? display, TMP_Text caption, string key, double? value)
    {
        if (value.HasValue && (double.IsNaN(value.Value) || double.IsInfinity(value.Value))) value = null;
        bool fits = display == null || NativeInstruments.Refresh(display, value, 1);
        // Full signed/localized reading remains available even if the numeric artwork overflows.
        caption.text = Text.Get(key, value.HasValue ? value.Value.ToString("F1", CultureInfo.CurrentCulture) : Text.Get("Furnace.unknown")) +
            (value.HasValue && !fits ? " — " + Text.Get("Furnace.display_overflow") : "");
    }
    private bool ApplyDraft()
    {
        if(!dirty){shell.Notice.text=C.Text("no_changes");return true;}
        var co=CollectorService.Resolve(target);if(co==null){shell.Notice.text=C.Text("unavailable");return false;}
        double Read(string key)=>double.TryParse(values[key],NumberStyles.Float,CultureInfo.InvariantCulture,out var n)?n:double.NaN;
        if(!FurnaceService.ApplySettings(binding,co,expected,Read("heat"),Read("ramp"),Read("cool"),out var reason)){shell.Notice.text=reason;return false;}
        dirty=false;expected=FurnaceService.SettingsStamp(co);shell.Notice.text=reason;Refresh();return true;
    }
    private void DiscardDraft(){dirty=false;var co=CollectorService.Resolve(target);if(co!=null)expected=FurnaceService.SettingsStamp(co);Refresh();}
    private void OnDisable()=>DetachDraft();
    private void OnDestroy()=>DetachDraft();
    private void DetachDraft()
    {
        if(detached)return;detached=true;
        if(mode!=null)mode.Callback=null;
        if(shell!=null&&shell.Dirty==ownDirty){shell.Dirty=oldDirty;shell.Apply=oldApply;shell.Discard=oldDiscard;}
        if(applyButton!=null){applyButton.gameObject.SetActive(false);Destroy(applyButton.gameObject);}if(discardButton!=null){discardButton.gameObject.SetActive(false);Destroy(discardButton.gameObject);}
    }
}
