using System;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Phobos.Ostranauts.Framework.Controls;

/// <summary>Opt-in native button artwork with live selection marks. No native controllers are cloned.</summary>
public static class PolarisWidgets
{
    private static readonly ConditionalWeakTable<Button, PolarisButton> Bindings = new();
    public static readonly Color Accent = new Color32(234, 196, 102, 255);
    public static readonly Color Surface = new Color32(43, 55, 65, 255);
    public static Button Button(Transform parent, string text, Action click, float height = 48)
    {
        var button = ConsoleWidgets.Button(parent, text, click, height);
        // The compact helper uses 16px for equipment rows. Taller Polaris command
        // faces must retain the existing 18px command text, not shrink it.
        button.GetComponentInChildren<TMP_Text>().fontSize = ConsoleWidgets.BodySize;
        Style(button); return button;
    }
    public static void Style(Button button)
    {
        if (Bindings.TryGetValue(button, out _)) return;
        var state = button.gameObject.AddComponent<PolarisButton>(); state.Bind(button); Bindings.Add(button, state);
    }
    public static void Selected(Button button, bool selected)
    {
        Style(button); Bindings.TryGetValue(button, out var state); state!.Select(selected);
    }
}

internal sealed class PolarisButton : MonoBehaviour
{
    private Image marker = null!;
    private TMP_Text? label;
    private bool selected;
    internal void Bind(Button button)
    {
        if (button.targetGraphic is Image image)
        {
            var donor = Resources.Load<GameObject>("GUIShip/GUIAirPump")?.transform.Find("pnlInside/btnDone")?.GetComponent<Button>();
            if (donor?.targetGraphic is Image source && source.sprite != null)
            { image.sprite = source.sprite; image.type = source.type; image.color = Color.white; }
            else image.color = new Color32(77, 94, 108, 255);
            var border = image.gameObject.AddComponent<Outline>();
            border.effectColor = new Color32(129, 148, 157, 255); border.effectDistance = new Vector2(1, -1);
        }
        button.transition = Selectable.Transition.ColorTint;
        var colors = button.colors;
        colors.normalColor = Color.white; colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
        colors.pressedColor = new Color(.65f, .75f, .8f); colors.selectedColor = Color.white;
        colors.disabledColor = new Color(.48f, .52f, .55f, 1); colors.fadeDuration = .06f; button.colors = colors;
        label = button.GetComponentInChildren<TMP_Text>();
        var edge = PanelWidgets.Rect(button.transform, "Selected tab");
        edge.anchorMin = new Vector2(0, 0); edge.anchorMax = new Vector2(1, 0);
        edge.offsetMin = new Vector2(5, 2); edge.offsetMax = new Vector2(-5, 6);
        marker = edge.gameObject.AddComponent<Image>(); marker.color = PolarisWidgets.Accent; marker.raycastTarget = false;
        marker.gameObject.SetActive(false);
    }
    internal void Select(bool value)
    {
        if (value == selected) return;
        selected = value; marker.gameObject.SetActive(value);
        if (label != null) { label.fontStyle = value ? FontStyles.Bold : FontStyles.Normal; label.color = value ? PolarisWidgets.Accent : PanelWidgets.Ink; }
    }
}
