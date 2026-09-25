using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
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
    private const string AuditedAssembly = "91b50f45cacd64de39b9bcc30ec7b4542f3e3976ac3bc5589b346976a262425e";
    private static readonly Lazy<bool> compatible = new(() =>
    {
        using var source = File.OpenRead(typeof(GUIKnob).Assembly.Location);
        using var hash = SHA256.Create();
        return BitConverter.ToString(hash.ComputeHash(source)).Replace("-", "").ToLowerInvariant() == AuditedAssembly;
    });
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
            var rect = (RectTransform)clone.transform;
            var size = ((RectTransform)donor).rect.size;
            if (component is GUIKnob sizedKnob) size = sizedKnob.aStates[0].rect.size;
            if (component is GUILamp sizedLamp) size = sizedLamp.aSprites[0].rect.size;
            if (component is GUILedMeter sizedMeter && (size.x <= 0 || size.y <= 0))
            {
                var led = sizedMeter.aLEDs[0].rect.size; var layout = sizedMeter.GetComponent<VerticalLayoutGroup>();
                size = new Vector2(led.x + (layout == null ? 0 : layout.padding.horizontal),
                    led.y * sizedMeter.aOff.Length + (layout == null ? 0 : layout.padding.vertical + layout.spacing * (sizedMeter.aOff.Length - 1)));
            }
            if (size.x <= 0 || size.y <= 0) throw new InvalidOperationException("Native instrument dimensions unavailable.");
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = Vector2.zero; rect.sizeDelta = size;
            float scale = Math.Min(1, 180 / size.y); clone.transform.localScale = Vector3.one * scale;
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
                t != typeof(HorizontalLayoutGroup) && t != typeof(LayoutElement) && t != typeof(ContentSizeFitter))
                throw new InvalidOperationException("Unaudited native component " + t.Name);
        }
        // These three native behaviours contain sprite/curve data only: no UnityEvents,
        // reactor controller, external target, Toggle group or animation bindings.
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
