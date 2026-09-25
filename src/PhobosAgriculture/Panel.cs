using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Ostranauts.InputControl;
using Ostranauts.Inventory;
using W = Phobos.Ostranauts.Framework.Controls.PanelWidgets;

namespace PhobosAgriculture;

// Native panel lifecycle and shared widgets. Presentation delegates all mutations.
public sealed class Panel : GUIData
{
    internal const string Key = "PhobosAgriculturePanel";
    private string id = "", actorId = "", result = "";
    private TMP_Text readout = null!;
    private RawImage portrait = null!;
    private string portraitKey = "";
    private float nextRefresh;
    internal static bool Show(CondOwner co)
    {
        if (!Definitions.Machine(co) || Service.Access(co) != null || CrewSim.goIntUIPanel == null || CrewSim.bUILock ||
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
        var plate = W.Rect(transform, "Agriculture"); W.Fill(plate, 30, 30, 30, 30);
        plate.gameObject.AddComponent<Image>().color = new Color(.12f, .15f, .18f);
        var content = W.Scroll(plate, "Controls", out var scroll); W.Fill((RectTransform)scroll.transform, 24, 24, 24, 24);
        W.Label(content, co.strNameFriendly);
        var imageRect = W.Rect(content, "Crop portrait"); imageRect.gameObject.AddComponent<LayoutElement>().minHeight = 96;
        portrait = imageRect.gameObject.AddComponent<RawImage>(); portrait.raycastTarget = false;
        var aspect = imageRect.gameObject.AddComponent<AspectRatioFitter>(); aspect.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
        readout = W.Label(content, "");
        foreach (string action in Service.Actions(co)) AddButton(content, co, action);
        if (!Definitions.IsCooker(co))
        {
            foreach (string action in IrrigationDefinitions.IsSupply(co) ? new[] { "load-water", "load-irrigation", "load-nutrients", "drain" } : Definitions.Work) AddButton(content, co, action);
            W.Label(content, Text.Get("water_pair_help"));
            foreach (var candidate in Service.WaterCandidates(co))
            {
                string peerId = candidate.strID;
                W.Button(content, Text.Get("water_pair", candidate.strNameFriendly, peerId), () => { Service.Command(co, null, "link-water:" + peerId, out result); Refresh(co); });
            }
        }
        W.Label(content, Text.Get("panel_help")); W.Button(content, Text.Get("close"), () => CrewSim.LowerUI()); Refresh(co);
    }
    private void AddButton(Transform parent, CondOwner co, string action) => W.Button(parent, Text.Get(action), () => { Service.Command(co, null, action, out result); Refresh(co); });
    private void Update()
    {
        if (!bActive || Time.unscaledTime < nextRefresh) return; nextRefresh = Time.unscaledTime + .5f;
        var co = Service.Resolve(id);
        if (co == null || CrewSim.GetSelectedCrew()?.strID != actorId || Service.Access(co) != null) { CrewSim.LowerUI(); return; }
        Refresh(co);
    }
    private void Refresh(CondOwner co)
    {
        readout.text = Service.Describe(co) + "\n" + result;
        var session = Service.Get(co);
        string key = Artwork.Key(co, session.State, session.Protected);
        if (key == portraitKey) return; portraitKey = key; portrait.texture = Artwork.Texture(key);
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
