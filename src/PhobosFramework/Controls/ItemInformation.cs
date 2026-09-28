using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace Phobos.Ostranauts.Framework.Controls;

/// <summary>Opt-in, read-only item instructions. No inventory or machinery authority.</summary>
public sealed class ItemInformation : GUIData
{
    private sealed class Entry
    {
        internal Func<CondOwner, string> Read = null!;
        internal HashSet<string> Definitions = null!;
    }
    private static readonly Dictionary<string, Entry> Entries = new(StringComparer.Ordinal);
    internal static void Reset() => Entries.Clear();
    public static void Register(Registration.NativeDefinitions definitions, string action, string title,
        IEnumerable<string> items, Func<CondOwner, string> read)
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
        Entries[action] = new Entry { Read = read, Definitions = ids };
    }
    internal static bool Handle(Interaction action, bool cancelled)
    {
        if (string.IsNullOrEmpty(action.strName) || !Entries.TryGetValue(action.strName, out var entry)) return false;
        var target = action.objThem;
        if (!cancelled && action.objUs == CrewSim.GetSelectedCrew() && target != null && !target.bDestroyed &&
            entry.Definitions.Contains(target.strCODef)) Show(target, action.strTitle, entry.Read(target));
        return true;
    }
    public static bool Show(CondOwner target, string title, string message)
    {
        if (target == null || target.bDestroyed || CrewSim.goIntUIPanel == null || CrewSim.bUILock) return false;
        CrewSim.LowerUI(); if (CrewSim.goUI != null) return false;
        CrewSim.objInstance.LowerContextMenu();
        var root = PanelWidgets.Rect(CrewSim.goIntUIPanel.transform, "Phobos item information");
        PanelWidgets.Fill(root, 40, 40, 40, 40);
        root.gameObject.AddComponent<Image>().color = new Color(.075f, .095f, .115f);
        var panel = root.gameObject.AddComponent<ItemInformation>();
        CrewSim.goUI = root.gameObject;
        panel.Init(target, new Dictionary<string, string>(), "PhobosItemInformation");
        panel.strFriendlyName = title; panel.bActive = true;
        CrewSim.tplLastUI = CrewSim.tplCurrentUI;
        CrewSim.tplCurrentUI = new global::Ostranauts.Core.Models.Tuple<string, CondOwner>("PhobosItemInformation", target);
        var body = PanelWidgets.Scroll(root, "Instructions", out var scroll);
        PanelWidgets.Fill((RectTransform)scroll.transform, 20, 82, 20, 20);
        ConsoleWidgets.Heading(body, title);
        ConsoleWidgets.Label(body, target.FriendlyName + "\n\n" + message);
        var close = PanelWidgets.Button(root, ConsoleWidgets.Text("close"), () => CrewSim.LowerUI());
        var footer = (RectTransform)close.transform;
        footer.anchorMin = new Vector2(0, 0); footer.anchorMax = new Vector2(1, 0);
        footer.offsetMin = new Vector2(20, 20); footer.offsetMax = new Vector2(-20, 70);
        CanvasManager.instance.ShipGUI(); CrewSim.SetUIArrows(); return true;
    }
}

[HarmonyPatch(typeof(Interaction), nameof(Interaction.ApplyEffects))]
internal static class ItemInformationAction
{
    private static bool Prefix(Interaction __instance, bool isCancelIa) => !ItemInformation.Handle(__instance, isCancelIa);
}
