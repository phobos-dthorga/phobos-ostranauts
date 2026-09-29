using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using Ostranauts.InputControl;
using Ostranauts.Inventory;
using Phobos.Ostranauts.Framework.Controls;
using PhobosManufacturing.Core;
using C = Phobos.Ostranauts.Framework.Controls.ConsoleWidgets;
using W = Phobos.Ostranauts.Framework.Controls.PanelWidgets;

namespace PhobosManufacturing;

/// <summary>The local Control Panel over Framework's console shell: live status, the machine's actions, and its
/// connections as configuration drafts. Presentation only; every change goes through the provider.</summary>
public sealed class Panel : GUIData
{
    internal const string Key = "PhobosManufacturingPanel";
    private static readonly Provider provider = new();
    private string id = "", actorId = "", result = "", tab = "operation";
    private TMP_Text readout = null!, live = null!;
    private ConsoleShell shell = null!;
    private readonly PresentationRefresh refresh = new(.5);
    internal static bool Show(CondOwner co)
    {
        if (!Content.Machine(co) || Content.Access(co) != null || CrewSim.goIntUIPanel == null || CrewSim.bUILock ||
            CrewSim.objInstance.coConnectMode != null || GUIInventory.instance?.Selected != null ||
            CanvasManager.instance.State == CanvasManager.GUIState.SOCIAL || CanvasManager.instance.State == CanvasManager.GUIState.GAMEOVER) return false;
        CrewSim.LowerUI(); if (CrewSim.goUI != null) return false;
        CrewSim.objInstance.LowerContextMenu(); InputManager.ToggleMovementMode(forceOff: true);
        if (CrewSim.guiPDA != null) CrewSim.guiPDA.State = GUIPDA.UIState.Closed;
        var root = W.Rect(CrewSim.goIntUIPanel.transform, Key); W.Fill(root);
        CrewSim.goUI = root.gameObject; var panel = root.gameObject.AddComponent<Panel>();
        panel.id = co.strID; panel.actorId = CrewSim.GetSelectedCrew().strID;
        panel.Init(co, new Dictionary<string, string>(), Key); panel.strFriendlyName = co.strNameFriendly; panel.bActive = true;
        CrewSim.tplLastUI = CrewSim.tplCurrentUI; CrewSim.tplCurrentUI = new Ostranauts.Core.Models.Tuple<string, CondOwner>(Key, co);
        if (EventSystem.current != null) EventSystem.current.sendNavigationEvents = false;
        CanvasManager.instance.ShipGUI(); CrewSim.SetUIArrows();
        try { panel.Build(co); return true; } catch (Exception e) { Plugin.Log(e.ToString()); CrewSim.LowerUI(); return false; }
    }
    private void Build(CondOwner co)
    {
        shell = ConsoleShell.Create(transform, co.strNameFriendly, C.Green);
        shell.SelectionOrigin = co;
        shell.EmergencyStop = () => Execute(co, "pause");
        foreach (var page in new[] { "operation", "connections", "details" })
        { var name = page; C.Button(shell.Navigation, Text.Get("Panel." + name), () => shell.Navigate(() => { tab = name; Page(co); })); }
        C.Button(shell.Navigation, C.Text("close"), shell.Close);
        ObjectPresentation.Picture(shell.List, co, 120); C.Label(shell.List, ObjectPresentation.Location(co));
        live = C.Label(shell.List, "");
        Page(co);
    }
    private void Page(CondOwner co)
    {
        W.Clear(shell.Detail); W.Clear(shell.Actions); shell.Page(true); readout = C.Label(shell.Detail, "");
        if (tab == "details") { C.Label(shell.Detail, Text.Get("Panel.help")); C.Label(shell.Detail, co.strCODef + "\n" + co.strID); }
        else if (tab == "connections")
        {
            foreach (var field in provider.Fields(co))
            {
                var f = field;
                C.Button(shell.Detail, f.Label + ": " + f.Value, () => ConfigurationSheet.Choices(shell, f.Label, "", provider.ConfigurationStamp(co), f.Choices,
                    (string expected, string value, out string reason) => provider.ApplyConfiguration(co, null, expected, value, out reason)));
            }
            if (!provider.Fields(co).Any()) C.Label(shell.Detail, Text.Get("Panel.no_connections"));
        }
        else
        {
            C.Heading(shell.Detail, C.Text("actions"));
            var row = C.Row(shell.Detail);
            foreach (var action in provider.Snapshot(co).Actions) { var a = action; C.Button(row, a.Label, () => Execute(co, a.Id)); }
        }
        C.Button(shell.Actions, C.Text("stop"), () => Execute(co, "pause"));
        C.Button(shell.Actions, C.Text("details"), () => { tab = "details"; Page(co); }); C.Button(shell.Actions, C.Text("close"), shell.Close); Refresh(co);
    }
    private void Execute(CondOwner co, string action)
    {
        bool success = provider.Command(co, null, action, out result);
        result = PanelFeedback.Additional(success, result, provider.Snapshot(co).Activity.Detail);
        shell.Notice.text = result;
        Refresh(co);
    }
    private void Update()
    {
        if (!bActive) return;
        refresh.Bind(COSelf, CrewSim.GetSelectedCrew(), null, Phobos.Ostranauts.Framework.Localization.Translations.Language);
        if (!refresh.Due(Time.unscaledTime)) return;
        var co = Content.Resolve(id);
        if (co == null || CrewSim.GetSelectedCrew()?.strID != actorId || Content.Access(co) != null) { shell.ForceClose(); return; }
        Refresh(co);
    }
    private void Refresh(CondOwner co)
    {
        var snapshot = provider.Snapshot(co);
        live.text = C.Text("state_" + snapshot.Activity.State);
        readout.text = snapshot.Activity.Detail;
    }
    public override void SaveAndClose() { if (bActive) base.SaveAndClose(); }
}

[HarmonyPatch(typeof(CrewSim), nameof(CrewSim.RaiseUI))]
internal static class PanelRestorePatch
{
    private static bool Prefix(string strCOGUIKey, CondOwner coSelf)
    {
        if (strCOGUIKey != Panel.Key) return true;
        if (!Panel.Show(coSelf) && CrewSim.goUI == null) { CanvasManager.instance.CrewSimNormal(); if (EventSystem.current != null) EventSystem.current.sendNavigationEvents = true; }
        return false;
    }
}
