using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Phobos.Ostranauts.Framework.Controls;

/// <summary>One part of an information sheet (Framework 0.116.0): an optional heading, its text, and buttons.</summary>
public sealed class InformationSection
{
    public string Heading { get; }
    public string Body { get; }
    public IReadOnlyList<InformationLink> Links { get; }
    public InformationSection(string heading, string body, params InformationLink[] links)
    { Heading = heading ?? ""; Body = body ?? ""; Links = links ?? Array.Empty<InformationLink>(); }
}

/// <summary>A button on an information sheet. <see cref="Open"/> only delegates to a checked service or another panel,
/// and returns null when it did what its label says, or the reason it did not, which the sheet shows.</summary>
public sealed class InformationLink
{
    public string Label { get; }
    public Func<string?> Open { get; }
    /// <summary>The button's tint (Framework 0.118.0): attention when what it opens needs the player.</summary>
    public Tone Tone { get; }
    public InformationLink(string label, Func<string?> open, Tone tone = Tone.Neutral) { Label = label ?? ""; Open = open ?? (() => null); Tone = tone; }
}

/// <summary>Opt-in item instructions with no inventory or machinery authority: read-only text, and since Framework
/// 0.116.0 sections with buttons that hand over to other panels (the right-click Maintenance sheet opens the Crew panel
/// on a machine's order or the upkeep switches). The text is read again every second while the sheet is open.</summary>
public sealed class ItemInformation : GUIData
{
    private sealed class Entry
    {
        internal Func<CondOwner, IReadOnlyList<InformationSection>> Read = null!;
        internal HashSet<string> Definitions = null!;
    }
    private static readonly Dictionary<string, Entry> Entries = new(StringComparer.Ordinal);
    private const float RefreshSeconds = 1;
    private Func<IReadOnlyList<InformationSection>>? read;
    private RectTransform? body;
    private readonly List<TMP_Text> bodies = new();
    private TMP_Text? notice;
    private CondOwner? target;
    private string title = "";
    private float next;
    internal static void Reset() => Entries.Clear();
    public static void Register(Registration.NativeDefinitions definitions, string action, string title,
        IEnumerable<string> items, Func<CondOwner, string> read) =>
        Register(definitions, action, title, items, co => new[] { new InformationSection("", read(co)) });
    /// <summary>As the text form, with the sheet built of sections (Framework 0.116.0).</summary>
    public static void Register(Registration.NativeDefinitions definitions, string action, string title,
        IEnumerable<string> items, Func<CondOwner, IReadOnlyList<InformationSection>> sections)
    {
        var ids = new HashSet<string>(items, StringComparer.Ordinal);
        definitions.Interactions[action] = new JsonInteraction {
            strName = action, strTitle = title, strDesc = title, strTooltip = title,
            strActionGroup = "Use", strThemType = "Other", bIgnoreFeelings = true,
            bHumanOnly = true, fDuration = 0, fTargetPointRange = 2,
            aLootItms = Array.Empty<string>()
        };
        foreach (string id in ids)
            definitions.Objects[id].aInteractions = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Distinct(
                System.Linq.Enumerable.Concat(definitions.Objects[id].aInteractions ?? Array.Empty<string>(), new[] { action })));
        Entries[action] = new Entry { Read = sections, Definitions = ids };
    }
    internal static bool Handle(Interaction action, bool cancelled)
    {
        if (string.IsNullOrEmpty(action.strName) || !Entries.TryGetValue(action.strName, out var entry)) return false;
        var target = action.objThem;
        if (!cancelled && action.objUs == CrewSim.GetSelectedCrew() && target != null && !target.bDestroyed &&
            entry.Definitions.Contains(target.strCODef)) Show(target, action.strTitle, () => entry.Read(target));
        return true;
    }
    public static bool Show(CondOwner target, string title, string message) =>
        Show(target, title, () => new[] { new InformationSection("", message) });
    /// <summary>Opens the sheet; <paramref name="read"/> is asked again every second while it is open.</summary>
    public static bool Show(CondOwner target, string title, Func<IReadOnlyList<InformationSection>> read)
    {
        if (target == null || target.bDestroyed || read == null || CrewSim.goIntUIPanel == null || CrewSim.bUILock) return false;
        CrewSim.LowerUI(); if (CrewSim.goUI != null) return false;
        CrewSim.objInstance.LowerContextMenu();
        var root = PanelWidgets.Rect(CrewSim.goIntUIPanel.transform, "Phobos item information");
        PanelWidgets.Fill(root, 40, 40, 40, 40);
        root.gameObject.AddComponent<Image>().color = new Color(.075f, .095f, .115f);
        var panel = root.gameObject.AddComponent<ItemInformation>();
        CrewSim.goUI = root.gameObject;
        panel.Init(target, new Dictionary<string, string>(), "PhobosItemInformation");
        panel.strFriendlyName = title; panel.bActive = true;
        panel.target = target; panel.title = title; panel.read = read;
        CrewSim.tplLastUI = CrewSim.tplCurrentUI;
        CrewSim.tplCurrentUI = new global::Ostranauts.Core.Models.Tuple<string, CondOwner>("PhobosItemInformation", target);
        panel.body = PanelWidgets.Scroll(root, "Instructions", out var scroll);
        PanelWidgets.Fill((RectTransform)scroll.transform, 20, 82, 20, 20);
        panel.Build(read());
        var close = PanelWidgets.Button(root, ConsoleWidgets.Text("close"), () => CrewSim.LowerUI());
        var footer = (RectTransform)close.transform;
        footer.anchorMin = new Vector2(0, 0); footer.anchorMax = new Vector2(1, 0);
        footer.offsetMin = new Vector2(20, 20); footer.offsetMax = new Vector2(-20, 70);
        CanvasManager.instance.ShipGUI(); CrewSim.SetUIArrows(); return true;
    }
    private void Build(IReadOnlyList<InformationSection> sections)
    {
        if (body == null || target == null) return;
        PanelWidgets.Clear(body); bodies.Clear();
        ConsoleWidgets.Heading(body, title);
        ConsoleWidgets.Label(body, target.FriendlyName);
        foreach (var section in sections)
        {
            if (section.Heading.Length > 0) ConsoleWidgets.Heading(body, section.Heading);
            bodies.Add(ConsoleWidgets.Label(body, section.Body));
            if (section.Links.Count == 0) continue;
            var row = ConsoleWidgets.Row(body);
            foreach (var link in section.Links)
            {
                var l = link;
                var button = ConsoleWidgets.Button(row, l.Label, () => { string? why = l.Open(); if (why != null && notice != null) notice.text = why; });
                ConsoleWidgets.Accent(button, l.Tone);
            }
        }
        notice = ConsoleWidgets.Label(body, "");
    }
    private void Update()
    {
        if (!bActive || read == null || Time.unscaledTime < next) return;
        next = Time.unscaledTime + RefreshSeconds;
        if (target == null || target.bDestroyed) { CrewSim.LowerUI(); return; }
        IReadOnlyList<InformationSection> sections;
        try { sections = read(); }
        catch (Exception ex) { FrameworkLifecycle.Log("Item information could not be read again: " + ex.Message); read = null; return; }
        if (sections.Count != bodies.Count) { string kept = notice?.text ?? ""; Build(sections); if (notice != null) notice.text = kept; return; }
        for (int i = 0; i < sections.Count; i++) if (bodies[i].text != sections[i].Body) bodies[i].text = sections[i].Body;
    }
}

[HarmonyPatch(typeof(Interaction), nameof(Interaction.ApplyEffects))]
internal static class ItemInformationAction
{
    private static bool Prefix(Interaction __instance, bool isCancelIa) => !ItemInformation.Handle(__instance, isCancelIa);
}
