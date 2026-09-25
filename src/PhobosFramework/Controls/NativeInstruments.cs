using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Phobos.Ostranauts.Framework.Processing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Phobos.Ostranauts.Framework.Controls;

/// <summary>Isolated, audited native reactor instruments. Never instantiate its panel controller.</summary>
public static class NativeInstruments
{
    private static readonly HashSet<string> diagnosed = new(StringComparer.Ordinal);
    private const string Reactor = "GUIShip/GUIReactor";
    private const int DigitCount = 7;
    private const string AuditedAssembly = "91b50f45cacd64de39b9bcc30ec7b4542f3e3976ac3bc5589b346976a262425e";
    private static readonly Lazy<bool> compatible = new(() =>
        NativeAssemblyAudit.Matches(typeof(GUIKnob).Assembly, BepInEx.Paths.ManagedPath, AuditedAssembly));
    public static T? Clone<T>(Transform parent, string path, Action<string> diagnostic) where T : Component
    {
        GameObject? root = null;
        try
        {
            if (!compatible.Value) throw new InvalidOperationException("Game assembly changed; re-audit native widget behaviour before re-enabling donors.");
            var prefab = Resources.Load<GameObject>(Reactor);
            var donor = prefab == null ? null : prefab.transform.Find(path);
            if (donor == null) throw new InvalidOperationException("Missing native instrument: " + path);
            // Audit the resource before Instantiate, then use an inactive owned parent
            // so no Awake/OnEnable can run before its cloned state is checked.
            Audit(donor);
            root = new GameObject("PhobosNativeInstrument", typeof(RectTransform)); root.SetActive(false); root.transform.SetParent(parent, false);
            var clone = UnityEngine.Object.Instantiate(donor.gameObject, root.transform, false);
            Audit(clone.transform);
            Neutralize(clone.transform);
            var component = clone.GetComponent<T>(); if (component == null) throw new InvalidOperationException("Native component changed: " + path);
            if (component is GUIKnob knob) { if (knob.aStates?.Length != 3 || knob.aStates.Any(x => x == null)) throw new InvalidOperationException("Native knob sprites changed."); knob.Callback = null; knob.bWrap = false; }
            if (component is GUILamp lamp && (lamp.aSprites?.Length < 2 || lamp.aSprites.Any(x => x == null))) throw new InvalidOperationException("Native lamp sprites changed.");
            if (component is GUILedMeter meter)
            {
                if (meter.aOff?.Length != clone.transform.childCount || meter.aLEDs == null || meter.aOff.Length == 0 ||
                    meter.aOff.Any(i => i < 0 || i + 1 >= meter.aLEDs.Length || meter.aLEDs[i] == null || meter.aLEDs[i + 1] == null) ||
                    Enumerable.Range(0, clone.transform.childCount).Any(i => clone.transform.GetChild(i).childCount == 0 || clone.transform.GetChild(i).GetChild(0).GetComponent<Image>() == null))
                    throw new InvalidOperationException("Native LED hierarchy changed.");
            }
            if (component is GUI7Seg digits)
            {
                if (digits.aSpriteSheet?.Length != 11 || digits.aSpriteSheet.Any(s => s == null) || clone.transform.childCount != DigitCount * 3)
                    throw new InvalidOperationException("Native digit artwork changed.");
                for (int i = 0; i < DigitCount; i++)
                    if (clone.transform.Find("bmpDigit" + i)?.GetComponent<Image>() == null || clone.transform.Find("bmpDot" + i) == null)
                        throw new InvalidOperationException("Native digit hierarchy changed.");
                digits.enabled = false; // Keep artwork, bypass the native negative/locale/overflow formatter.
            }
            var rect = (RectTransform)clone.transform;
            var size = ((RectTransform)donor).rect.size;
            if (component is GUIKnob sizedKnob) size = sizedKnob.aStates[0].rect.size;
            if (component is GUILamp sizedLamp) size = sizedLamp.aSprites[0].rect.size;
            if (component is GUI7Seg sizedDigits)
            {
                // Keep native glyph proportions despite a root prefab with zero-sized anchors.
                const float height = 56;
                var cell = (RectTransform)clone.transform.Find("bmpDigit0");
                var span = cell.anchorMax - cell.anchorMin;
                var glyph = sizedDigits.aSpriteSheet[0].rect.size;
                if (span.x <= 0 || span.y <= 0 || glyph.y <= 0) throw new InvalidOperationException("Native digit geometry changed.");
                float width = ((height * span.y + cell.sizeDelta.y) * glyph.x / glyph.y - cell.sizeDelta.x) / span.x;
                size = new Vector2(width, height);
            }
            if (component is GUISafetyToggle) size = clone.GetComponent<Image>().sprite.rect.size;
            if (component is Slider) size = new Vector2(48, 180);
            if (component is GUILedMeter sizedMeter && (size.x <= 0 || size.y <= 0))
            {
                var led = sizedMeter.aLEDs[0].rect.size; var layout = sizedMeter.GetComponent<VerticalLayoutGroup>();
                size = new Vector2(led.x + (layout == null ? 0 : layout.padding.horizontal),
                    led.y * sizedMeter.aOff.Length + (layout == null ? 0 : layout.padding.vertical + layout.spacing * (sizedMeter.aOff.Length - 1)));
            }
            if (size.x <= 0 || size.y <= 0) throw new InvalidOperationException("Native instrument dimensions unavailable.");
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = Vector2.zero; rect.sizeDelta = size;
            float scale = Math.Min(1, Math.Min(180 / size.y, 360 / size.x)); clone.transform.localScale = Vector3.one * scale;
            ((RectTransform)root.transform).sizeDelta = size * scale;
            var le = root.AddComponent<LayoutElement>(); le.minWidth = le.preferredWidth = size.x * scale; le.minHeight = le.preferredHeight = size.y * scale;
            clone.SetActive(true); root.SetActive(true);
            return component;
        }
        catch (Exception ex)
        {
            if (root != null) { root.SetActive(false); UnityEngine.Object.Destroy(root); }
            if (diagnosed.Add(path)) diagnostic("Native widget fallback: " + path + ": " + ex.Message);
            return null;
        }
    }
    private static void Audit(Transform root)
    {
        foreach (var component in root.GetComponentsInChildren<Component>(true))
        {
            if (component == null) throw new InvalidOperationException("Missing native script.");
            var t = component.GetType();
            if (t != typeof(RectTransform) && t != typeof(CanvasRenderer) && t != typeof(Image) && t != typeof(GUIKnob) &&
                t != typeof(GUILedMeter) && t != typeof(GUILamp) && t != typeof(VerticalLayoutGroup) &&
                t != typeof(HorizontalLayoutGroup) && t != typeof(LayoutElement) && t != typeof(ContentSizeFitter) &&
                t != typeof(GUI7Seg) && t != typeof(GUISafetyToggle) && t != typeof(GUIToggleSwap) &&
                t != typeof(Toggle) && t != typeof(Button) && t != typeof(Slider) && t != typeof(CanvasGroup))
                throw new InvalidOperationException("Unaudited native component " + t.Name);
            // All behaviour references must remain inside the isolated subtree. Assets such
            // as sprites are intentionally shared; reactor controllers never are.
            if (component is GUISafetyToggle || component is GUIToggleSwap)
                foreach (var field in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    if (typeof(Component).IsAssignableFrom(field.FieldType))
                    {
                        var dependency = field.GetValue(component) as Component;
                        if (dependency == null || (dependency.transform != root && !dependency.transform.IsChildOf(root)))
                            throw new InvalidOperationException("External native control reference: " + field.Name);
                    }
            if (component is Selectable selectable)
            {
                Own(selectable.targetGraphic);
                if (component is Toggle toggle) Own(toggle.graphic);
                if (component is Slider slider) { Own(slider.fillRect); Own(slider.handleRect); }
            }
        }
        void Own(Component? reference)
        {
            if (reference != null && reference.transform != root && !reference.transform.IsChildOf(root))
                throw new InvalidOperationException("External native selectable reference.");
        }
    }
    private static void Neutralize(Transform root)
    {
        foreach (var selectable in root.GetComponentsInChildren<Selectable>(true))
        {
            selectable.navigation = new Navigation { mode = Navigation.Mode.None };
            if (selectable is Button button) button.onClick = new Button.ButtonClickedEvent();
            if (selectable is Toggle toggle) { toggle.group = null; toggle.onValueChanged = new Toggle.ToggleEvent(); toggle.SetIsOnWithoutNotify(false); }
            if (selectable is Slider slider) slider.onValueChanged = new Slider.SliderEvent();
        }
        // Awake now adds only the audited guard and sprite-swap behaviour to fresh events.
        foreach (var guard in root.GetComponentsInChildren<GUISafetyToggle>(true)) guard.bSilent = true;
    }
    public static void Refresh(GUISafetyToggle guard, bool enabled)
    {
        guard.chkSwitch.SetIsOnWithoutNotify(enabled);
        foreach (var swap in guard.GetComponentsInChildren<GUIToggleSwap>(true)) swap.OnTargetToggleValueChanged(enabled);
    }
    public static bool Refresh(GUI7Seg display, double? value, int decimalPlaces)
    {
        bool valid = InstrumentNumber.TryFormat(value, decimalPlaces, DigitCount, out var digits, out int dot, out _);
        for (int i = 0; i < DigitCount; i++)
        {
            char digit = valid ? digits[i] : ' ';
            display.transform.Find("bmpDigit" + i).GetComponent<Image>().sprite = display.aSpriteSheet[digit == ' ' ? 10 : digit - '0'];
            display.transform.Find("bmpDot" + i).gameObject.SetActive(valid && i == dot);
        }
        return valid;
    }
    public static void Refresh(GUIKnob knob, int state)
    {
        var callback = knob.Callback;
        try { knob.Callback = null; knob.SetStateSilent(Math.Max(0, Math.Min(2, state))); }
        finally { knob.Callback = callback; }
    }
    public static void Refresh(GUILedMeter meter, double? fraction)
    {
        bool valid = fraction.HasValue && ThermalMath.Finite(fraction.Value);
        meter.SetValue(valid ? (float)Math.Max(0, Math.Min(1, fraction!.Value)) : 0f);
        meter.SetState(valid ? GUILedMeter.STATE_ON : GUILedMeter.STATE_OFF);
    }
    public static void UseNativeFont(TMP_Text label)
    {
        var donor = Resources.Load<GameObject>("GUIShip/GUIAirPump")?.transform.Find("MainPanel/lblTitle")?.GetComponent<TMP_Text>();
        if (donor?.font != null) { label.font = donor.font; label.fontSharedMaterial = donor.fontSharedMaterial; }
    }
}
