using System;
using System.Globalization;
using Phobos.Ostranauts.Framework.Controls;
using PhobosShipbreaker.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using W = Phobos.Ostranauts.Framework.Controls.PanelWidgets;

namespace PhobosShipbreaker;

/// <summary>Display-only composition. Every input delegates to the same checked service.</summary>
public sealed class FurnaceInstrumentView : MonoBehaviour
{
    private string target = "";
    private GUIKnob? mode;
    private GUILedMeter? heat, temperature, cooling;
    private GUILamp? enabledLamp;
    private TMP_Text status = null!;
    private float nextRefresh;
    internal static void Build(Transform parent, CondOwner furnace, Action<string, string?> command)
    {
        var root = W.Rect(parent, "FurnaceInstruments");
        var group = root.gameObject.AddComponent<VerticalLayoutGroup>(); group.spacing = 8; group.childForceExpandHeight = false; group.childControlWidth = group.childControlHeight = true;
        var view = root.gameObject.AddComponent<FurnaceInstrumentView>(); view.target = furnace.strID;
        var headline = W.Label(root, Text.Get("Furnace.panel_heading")); NativeInstruments.UseNativeFont(headline);
        var row = W.Rect(root, "Meters"); var columns = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        columns.spacing = 24; columns.childForceExpandWidth = false; columns.childForceExpandHeight = false; columns.childControlWidth = columns.childControlHeight = true;
        view.mode = NativeInstruments.Clone<GUIKnob>(row, "pnlPower/knobBus", Plugin.Log);
        if (view.mode != null) view.mode.Callback = n => command(n == 0 ? "stop" : n == 1 ? "auto-run" : "step-run", null);
        view.enabledLamp = NativeInstruments.Clone<GUILamp>(row, "pnlInit/pnlStepBus/bmpGreen", Plugin.Log);
        var gauges = W.Rect(root, "Gauges"); var gaugeLayout = gauges.gameObject.AddComponent<HorizontalLayoutGroup>();
        gaugeLayout.spacing = 16; gaugeLayout.childForceExpandWidth = gaugeLayout.childForceExpandHeight = false;
        gaugeLayout.childControlWidth = gaugeLayout.childControlHeight = true;
        view.temperature = NativeInstruments.Clone<GUILedMeter>(gauges, "pnlCoreTemp/pnlLeds", Plugin.Log);
        view.heat = NativeInstruments.Clone<GUILedMeter>(gauges, "pnlPower/pnlLedsTotal", Plugin.Log);
        view.cooling = NativeInstruments.Clone<GUILedMeter>(gauges, "pnlCoreTemp/pnlLeds", Plugin.Log);
        W.Label(root, Text.Get("Furnace.meter_legend"));
        view.status = W.Label(root, ""); NativeInstruments.UseNativeFont(view.status);
        // Explicit buttons remain keyboard-accessible, and serve as diagnosed fallback
        // if a game update removes a native donor. No guard can hide emergency stop.
        foreach (string a in new[] { "seal", "resume", "next", "equalize", "release", "automatic", "step-mode" })
        { string action = a; W.Button(root, Text.Get("Furnace.action_" + a), () => command(action, null)); }
        var b = FurnaceService.Get(furnace).State.Batch;
        Setting("heat", b.HeatCapKW); Setting("ramp", b.RampKPerSecond); Setting("cool", b.CoolingCapKW);
        void Setting(string action, double initial)
        {
            W.Label(root, Text.Get("Furnace.setting_" + action)); string value = initial.ToString(CultureInfo.InvariantCulture);
            var entry = W.Search(root, value, text => value = text); entry.SetTextWithoutNotify(value);
            W.Button(root, Text.Get("Furnace.apply"), () => command(action, value));
        }
        view.Refresh();
    }
    private void Update() { if (Time.unscaledTime >= nextRefresh) { nextRefresh = Time.unscaledTime + .25f; Refresh(); } }
    private void Refresh()
    {
        var co = CollectorService.Resolve(target);
        if (co == null) { status.text = Text.Get("Industry.missing"); return; }
        var s = FurnaceService.Get(co); var b = s.State.Batch;
        bool valid = !s.Protected && FurnaceService.ProbeValid(co);
        if (mode != null) NativeInstruments.Refresh(mode, !b.Armed ? 0 : b.StepMode ? 2 : 1);
        if (temperature != null) NativeInstruments.Refresh(temperature, valid ? (b.TemperatureK - 273.15) / 750 : (double?)null);
        if (heat != null) NativeInstruments.Refresh(heat, valid ? s.DeliveredKW / (FurnaceRules.HeatLimitKW / FurnaceRules.Efficiency + FurnaceRules.HeatAuxKW) : (double?)null);
        var radiator = FurnaceService.CoolingEndpoint(co);
        if (cooling != null) NativeInstruments.Refresh(cooling, valid && radiator != null && !radiator.HasCond("IsDamaged") && !FurnaceService.Get(radiator).Protected ?
            (FurnaceRules.SinkCapacity * (FurnaceRules.SinkMaxK - FurnaceRules.ReferenceK) - FurnaceService.Get(radiator).SinkKJ) / (FurnaceRules.SinkCapacity * (FurnaceRules.SinkMaxK - FurnaceRules.ReferenceK)) : (double?)null);
        if (enabledLamp != null) enabledLamp.State = valid && b.Armed ? GUILamp.STATE_ON : GUILamp.STATE_OFF;
        status.text = Text.Get("Furnace.live", valid ? (b.TemperatureK - 273.15).ToString("F1", CultureInfo.CurrentCulture) : Text.Get("Furnace.unknown"),
            valid ? s.DeliveredKW.ToString("F1", CultureInfo.CurrentCulture) : Text.Get("Furnace.unknown"), Text.Get("Furnace.phase_" + b.Phase));
    }
    private void OnDestroy() { if (mode != null) mode.Callback = null; }
}
