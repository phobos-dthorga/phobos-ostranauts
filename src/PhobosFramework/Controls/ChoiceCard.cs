using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Phobos.Ostranauts.Framework.Controls;

/// <summary>A modal pick-one card over a panel (Framework 0.122.0): a dark shade, a message and one button per choice,
/// each tinted by its tone. Choosing closes the card first, then runs the choice, so a choice may open another card.
/// The console's unsaved-changes question and the Letters window's reply confirmation both use it; any mod's panel may.</summary>
public static class ChoiceCard
{
    public readonly struct Choice
    {
        public readonly string Label;
        public readonly Action Click;
        public readonly Tone Tone;
        public Choice(string label, Action click, Tone tone = Tone.Neutral) { Label = label; Click = click; Tone = tone; }
    }

    /// <summary>Shows the card over <paramref name="host"/> and returns its root, which the caller may destroy to
    /// close it without a choice.</summary>
    public static GameObject Show(Transform host, string message, IReadOnlyList<Choice> choices)
    {
        var shade = PanelWidgets.Rect(host, "Phobos choice"); PanelWidgets.Fill(shade);
        shade.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .9f);
        var card = PanelWidgets.Rect(shade, "Card"); card.anchorMin = new Vector2(.1f, .15f); card.anchorMax = new Vector2(.9f, .85f); card.offsetMin = card.offsetMax = Vector2.zero;
        var group = card.gameObject.AddComponent<VerticalLayoutGroup>(); group.spacing = 12; group.childControlHeight = group.childControlWidth = true; group.childForceExpandHeight = false;
        ConsoleWidgets.Label(card, message);
        var root = shade.gameObject;
        foreach (var choice in choices)
        {
            var c = choice;
            var button = ConsoleWidgets.Button(card, c.Label, () => { if (root != null) UnityEngine.Object.Destroy(root); c.Click(); });
            ConsoleWidgets.Accent(button, c.Tone);
        }
        return root;
    }
}
