using TMPro;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

namespace Phobos.Ostranauts.Framework.Controls;

/// <summary>Change-only writes shared by existing panels; no gameplay or retained world snapshots.</summary>
public static class Presentation
{
    private sealed class TextBinding
    {
        internal readonly CompactText? Compact;
        internal TextBinding(TMP_Text label) => Compact = label.GetComponent<CompactText>();
    }
    private static readonly ConditionalWeakTable<TMP_Text, TextBinding> texts = new();
    public static void Text(TMP_Text label, string value)
    {
        var compact = texts.GetValue(label, key => new TextBinding(key)).Compact;
        if (compact != null) compact.SetSource(value);
        else if (label.text != value) label.text = value;
    }
    public static void Active(GameObject target, bool active) { if (target.activeSelf != active) target.SetActive(active); }
    public static void Enabled(Selectable target, bool enabled) { if (target.interactable != enabled) target.interactable = enabled; }
    public static void Color(Graphic target, Color color) { if (target.color != color) target.color = color; }
}
