using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Controls;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhobosAutoNav;

public sealed partial class AutoNavPanel
{
    private GameObject? overlay;
    private CondOwner? overlayConsole, overlayActor;
    private Ship? overlayTarget;
    private string overlayLanguage = "", overlayMode = "", groupSignature = "", fullWarning = "";
    private NavigationService.GroupSwitch? groupSwitch;
    private TMP_Text? warningText;

    private RectTransform Overlay(string mode, string title)
    {
        CloseOverlay(); overlayMode = mode;
        overlayConsole = COSelf; overlayActor = CrewSim.GetSelectedCrew(); overlayTarget = GUIOrbitDraw.CrossHairTarget?.Ship;
        overlayLanguage = Phobos.Ostranauts.Framework.Localization.Translations.Language;
        var root = Box(design, "body"); overlay = root.gameObject; root.name = "Polaris " + mode;
        root.gameObject.AddComponent<Image>().color = new Color32(19, 29, 36, 255);
        var list = PanelWidgets.Scroll(root, "Choices", out var scroll); PanelWidgets.Fill((RectTransform)scroll.transform, 8, 8, 8, 8);
        var heading = ConsoleWidgets.Heading(list, title); Style(heading, 26); heading.color = Amber;
        var close = PolarisWidgets.Button(list, Text.Get("Hub.close_details"), () => Invoke(CloseOverlay)); Style(close.GetComponentInChildren<TMP_Text>(), 24);
        return list;
    }
    private void CloseOverlay()
    {
        if (overlay != null) { overlay.SetActive(false); Destroy(overlay); }
        overlay = null; groupSwitch = null; warningText = null; overlayMode = ""; groupSignature = "";
    }
    private void ShowWarning()
    {
        var list = Overlay("warning", Text.Get("Hub.warning_details"));
        warningText = PanelWidgets.Label(list, fullWarning); Style(warningText, 24);
        warningText.textWrappingMode = TextWrappingModes.Normal;
    }
    private void ShowGroups() => ShowGroups(Plugin.Service.ReadHub(COSelf, "fire"));
    private void ShowGroups(HubSnapshot view)
    {
        var list = Overlay("groups", Text.Get("FCS.choose_group"));
        groupSignature = GroupSignature(view);
        foreach (var group in view.Groups)
        {
            int selected = group.Group;
            var button = PolarisWidgets.Button(list, Text.Get("FCS.group_entry", group.Group, group.Installed),
                () => Invoke(() => ChooseGroup(selected)));
            Style(button.GetComponentInChildren<TMP_Text>(), 24);
            PolarisWidgets.Selected(button, selected == view.WeaponGroup);
            button.interactable = selected != view.WeaponGroup && view.CanChangeGroup;
        }
        if (!view.Groups.Any(g => g.Group == view.WeaponGroup))
        {
            var label = PanelWidgets.Label(list, Text.Get("FCS.selected_empty", view.WeaponGroup)); Style(label, 24);
            label.textWrappingMode = TextWrappingModes.Normal;
        }
    }
    private void ChooseGroup(int selected)
    {
        var ticket = Plugin.Service.PrepareGroupSwitch(COSelf, selected);
        if (ticket == null) { CloseOverlay(); return; }
        if (!ticket.Held && !ticket.Lease) { Plugin.Service.ConfirmGroupSwitch(ticket); CloseOverlay(); return; }
        var list = Overlay("handoff", Text.Get("FCS.change_group")); groupSwitch = ticket;
        var text = PanelWidgets.Label(list, Text.Get("FCS.handoff", ticket.Before, ticket.After));
        Style(text, 24); text.textWrappingMode = TextWrappingModes.Normal;
        var confirm = PolarisWidgets.Button(list, Text.Get("FCS.confirm_handoff"), () => Invoke(() =>
        { if (groupSwitch != null) Plugin.Service.ConfirmGroupSwitch(groupSwitch); CloseOverlay(); }));
        Style(confirm.GetComponentInChildren<TMP_Text>(), 24);
    }
    private static string GroupSignature(HubSnapshot view) => view.WeaponGroup + ":" + view.CanChangeGroup + ":" + string.Join(",", view.Groups.Select(g => g.Group + "/" + g.Installed));
    private void UpdateOverlay(HubSnapshot view)
    {
        if (overlay == null) return;
        if (overlayConsole != COSelf || overlayActor != CrewSim.GetSelectedCrew() || overlayTarget != GUIOrbitDraw.CrossHairTarget?.Ship ||
            overlayLanguage != Phobos.Ostranauts.Framework.Localization.Translations.Language ||
            groupSwitch != null && !Plugin.Service.GroupSwitchCurrent(groupSwitch)) { CloseOverlay(); return; }
        if (overlayMode == "groups" && groupSignature != GroupSignature(view)) ShowGroups(view);
        if (warningText != null) Presentation.Text(warningText, view.Restriction);
    }
}
