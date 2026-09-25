using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Phobos.Ostranauts.Framework.Controls;
using PhobosShipbreaker.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Ostranauts.InputControl;
using Ostranauts.Inventory;
using W = Phobos.Ostranauts.Framework.Controls.PanelWidgets;

namespace PhobosShipbreaker;

/// <summary>Native GUIData lifecycle, presentation only. All mutations use IndustryService.</summary>
public sealed class IndustrialPanel : GUIData
{
    internal const string Key = "PhobosIndustrialPanel";
    private const float HeaderHeight = 86, FooterHeight = 96, Edge = 76, ListFraction = .36f, FramePixelsPerUnit = 200;
    private ConsoleBinding? binding;
    private string hostId = "", actorId = "", selected = "", query = "", tab = "overview", result = "";
    private int stateFilter;
    private bool compact, detailPage, invalid;
    private float nextRefresh;
    private string roster = "";
    private EquipmentCard[] cards = Array.Empty<EquipmentCard>();
    private readonly HashSet<string> collapsed = new HashSet<string>();
    private readonly Dictionary<string, TMP_Text> rows = new Dictionary<string, TMP_Text>();
    private RectTransform plate = null!, list = null!, details = null!;
    private ScrollRect listScroll = null!, detailScroll = null!;
    private TMP_Text header = null!, readout = null!;
    private CanvasGroup commands = null!;
    private Button pause = null!;
    private Button furnaceStop = null!;
    private TMP_InputField search = null!;
    private Sprite? frame;
    private bool Central => binding != null;

    internal static bool Open(CondOwner target)
    {
        if (target == null || target.bDestroyed || target.ship == null || (int)target.ship.LoadState < 2 || !target.HasCond("IsInstalled") ||
            CrewSim.goIntUIPanel == null || CrewSim.objInstance.coConnectMode != null || CrewSim.bUILock ||
            GUIInventory.instance?.Selected != null || CanvasManager.instance.State == CanvasManager.GUIState.SOCIAL || CanvasManager.instance.State == CanvasManager.GUIState.GAMEOVER) return false;
        ConsoleBinding? context = null;
        string? problem;
        if (IndustrialRules.Console(target.strCODef))
        { if (!ControlAuthority.TryBind(target, out context, out string error)) { Plugin.Log(error); return false; } }
        else
        {
            if (!IndustrialRules.Equipment(target.strCODef)) return false;
            problem = CollectorService.EndpointAccess(target);
            if (problem != null) { Plugin.Log(problem); return false; }
        }
        CrewSim.LowerUI();
        if (CrewSim.goUI != null) return false;
        CrewSim.objInstance.LowerContextMenu();
        InputManager.ToggleMovementMode(forceOff: true);
        if (CrewSim.guiPDA != null) CrewSim.guiPDA.State = GUIPDA.UIState.Closed;
        var root = W.Rect(CrewSim.goIntUIPanel.transform, Key); W.Fill(root);
        CrewSim.goUI = root.gameObject;
        var panel = root.gameObject.AddComponent<IndustrialPanel>();
        panel.binding = context; panel.hostId = target.ship.strRegID; panel.actorId = CrewSim.GetSelectedCrew().strID;
        panel.selected = context == null ? target.strID : ""; panel.tab = context == null ? "equipment" : "overview";
        panel.Init(target, new Dictionary<string, string>(), Key);
        panel.strFriendlyName = Text.Get("Industry.title"); panel.bActive = true;
        CrewSim.tplLastUI = CrewSim.tplCurrentUI;
        CrewSim.tplCurrentUI = new Ostranauts.Core.Models.Tuple<string, CondOwner>(Key, target);
        if (EventSystem.current != null) EventSystem.current.sendNavigationEvents = false;
        CanvasManager.instance.ShipGUI(); CrewSim.SetUIArrows();
        try { panel.Build(); panel.Refresh(); return CrewSim.goUI == root.gameObject; }
        catch (Exception ex) { Plugin.Log(Text.Get("Industry.ui_error", ex.Message)); CrewSim.LowerUI(); return false; }
    }
    private void Build()
    {
        plate = W.Rect(transform, "Faceplate"); W.Fill(plate, 22, 18, 22, 18);
        var art = plate.gameObject.AddComponent<Image>();
        var texture = DataHandler.LoadPNG("phobos/shipbreaker/PhobosIndustrialPanel.png", bNorm: false);
        if (texture != null)
        {
            frame = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), FramePixelsPerUnit, 0,
                SpriteMeshType.FullRect, new Vector4(110, 140, 110, 140));
            art.sprite = frame; art.type = Image.Type.Sliced;
        }
        else art.color = new Color(.12f, .15f, .18f);
        header = W.Label(plate, "", false); var h = (RectTransform)header.transform;
        W.Fill(h, Edge, 0, Edge, Edge); h.anchorMin = new Vector2(0, 1); h.offsetMin = new Vector2(Edge, -Edge - HeaderHeight);
        header.overflowMode = TextOverflowModes.Ellipsis;
        var footer = W.Rect(plate, "Footer"); W.Fill(footer, Edge, Edge, Edge, 0);
        footer.anchorMax = new Vector2(1, 0); footer.offsetMax = new Vector2(-Edge, Edge + FooterHeight);
        var layout = footer.gameObject.AddComponent<HorizontalLayoutGroup>(); layout.spacing = W.Gap;
        layout.childControlWidth = layout.childControlHeight = true; layout.childForceExpandWidth = true;
        W.Button(footer, Text.Get("Industry.back_list"), () => { detailPage = false; Layout(); }).gameObject.SetActive(Central);
        pause = W.Button(footer, Text.Get("Industry.pause_all"), () => { if (binding != null) { result = IndustryService.PauseAll(binding); ShowDetail(); } });
        pause.gameObject.SetActive(Central);
        furnaceStop = W.Button(footer, Text.Get("Furnace.action_stop"), () =>
        { IndustryService.Run(binding, selected, "stop", null, out result); RefreshReadout(); });
        furnaceStop.gameObject.SetActive(false);
        W.Button(footer, Text.Get("Industry.close"), () => CrewSim.LowerUI());

        list = W.Scroll(plate, "Equipment", out listScroll); details = W.Scroll(plate, "Details", out detailScroll);
        if (Central)
        {
            search = W.Search(list, Text.Get("Industry.search"), text => { query = text; RebuildRows(); });
            foreach (var key in new[] { "overview", "equipment", "routing", "observations", "attention" })
            {
                string captured = key;
                W.Button(list, Text.Get("Industry.tab_" + key), () => { tab = captured; selected = ""; detailPage = captured == "overview"; roster = ""; RebuildRows(); ShowDetail(); });
            }
            W.Button(list, Text.Get("Industry.cycle_state"), () => { stateFilter = (stateFilter + 1) % (Enum.GetValues(typeof(EquipmentState)).Length + 1); RebuildRows(); });
        }
        var rowsRoot = W.Rect(list, "Rows"); var group = rowsRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        group.spacing = W.Gap; group.childControlHeight = group.childControlWidth = true; group.childForceExpandHeight = false;
        Layout();
    }
    private Transform RowsRoot => list.GetChild(list.childCount - 1);
    private void Layout()
    {
        compact = plate.rect.width < IndustrialRules.CompactWidth;
        var left = (RectTransform)listScroll.transform; var right = (RectTransform)detailScroll.transform;
        W.Fill(left, Edge, Edge + FooterHeight + W.Gap, Edge, Edge + HeaderHeight);
        W.Fill(right, Edge, Edge + FooterHeight + W.Gap, Edge, Edge + HeaderHeight);
        if (!compact && Central)
        {
            left.anchorMax = new Vector2(ListFraction, 1); left.offsetMax = new Vector2(-W.Gap, -Edge - HeaderHeight);
            right.anchorMin = new Vector2(ListFraction, 0); right.offsetMin = new Vector2(W.Gap, Edge + FooterHeight + W.Gap);
        }
        left.gameObject.SetActive(Central && (!compact || !detailPage));
        right.gameObject.SetActive(!Central || !compact || detailPage);
    }
    private void Update()
    {
        if (!bActive || Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + (float)IndustrialRules.RefreshSeconds;
        try { Refresh(); }
        catch (Exception ex) { invalid = true; if (commands != null) commands.interactable = false; Plugin.Log(Text.Get("Industry.ui_error", ex.Message)); CrewSim.LowerUI(); }
    }
    private string? Access()
    {
        var actor = CrewSim.GetSelectedCrew(); var anchor = COSelf;
        // A moved/uninstalled console or changed operator ends this session permanently.
        if (anchor == null || anchor.bDestroyed || anchor.ship?.strRegID != hostId || !anchor.HasCond("IsInstalled") || actor?.strID != actorId || actor?.ship?.strRegID != hostId)
            invalid = true;
        if (invalid) return Text.Get("Industry.session_ended");
        return binding != null ? ControlAuthority.Check(anchor!, binding) : CollectorService.EndpointAccess(anchor!);
    }
    private void Refresh()
    {
        using var measurement = Phobos.Ostranauts.Framework.Diagnostics.Performance.Measure(PerformanceMetrics.PanelRefresh);
        var problem = Access();
        if (invalid) { CrewSim.LowerUI(); return; }
        header.text = Text.Get("Industry.header", Central ? Text.Get("Industry.title") : COSelf.strNameFriendly, hostId,
            problem ?? Text.Get("Industry.connected"));
        pause.interactable = problem == null;
        furnaceStop.gameObject.SetActive(FurnaceRules.Machine(CollectorService.Resolve(selected)?.strCODef));
        furnaceStop.interactable = problem == null;
        // Losing console/operator access must stop observation reads as well as commands.
        if (problem != null)
        {
            cards = Array.Empty<EquipmentCard>(); roster = ""; RebuildRows(); ShowDetail();
            return;
        }
        cards = Central ? IndustryService.SnapshotShip(COSelf.ship) : new[] { IndustryService.Snapshot(COSelf) };
        if (Central && IndustryObservations.TryRead(binding!, out var instruments, out _)) cards = cards.Concat(instruments).ToArray();
        // Stable rows while status text changes; rebuild only when membership changes.
        string signature = string.Join("|", cards.Select(c => c.Id + (tab == "attention" ? ":" + c.Attention : "") + (stateFilter != 0 ? ":" + c.State : "") + (query.Length != 0 ? ":" + MatchesQuery(c) : "")));
        if (signature != roster) { roster = signature; RebuildRows(); }
        foreach (var card in cards) if (rows.TryGetValue(card.Id, out var row)) row.text = RowText(card);
        if (readout == null || commands == null && cards.Any(c => c.Id == selected && !c.Instrument)) ShowDetail(); else RefreshReadout();
        if (commands != null) commands.interactable = problem == null;
        Layout();
    }
    private static string RowText(EquipmentCard c) => c.Name + "\n" + (c.Instrument ? c.InstrumentStatus : IndustryService.StateName(c.State)) + (c.Attention ? " — " + Text.Get("Industry.tab_attention") : "");
    private bool MatchesQuery(EquipmentCard c) => query.Length == 0 || c.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
        c.Id.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 || c.SearchScope.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
    private void RebuildRows()
    {
        if (list == null) return;
        float scroll = listScroll.verticalNormalizedPosition;
        W.Clear(RowsRoot); rows.Clear();
        W.Label(RowsRoot, Text.Get("Industry.filter_state", stateFilter == 0 ? Text.Get("Industry.all") : IndustryService.StateName((EquipmentState)(stateFilter - 1))));
        var visible = cards.Where(c => MatchesQuery(c) &&
            (tab == "attention" || c.Instrument == (tab == "observations")) &&
            (tab != "attention" || c.Attention) && (stateFilter == 0 || (int)c.State == stateFilter - 1));
        foreach (var group in visible.GroupBy(c => c.Group))
        {
            string key = group.Key;
            W.Button(RowsRoot, (collapsed.Contains(key) ? "+ " : "− ") + Text.Get("Industry.group_" + key) + " (" + group.Count() + ")", () => { if (!collapsed.Add(key)) collapsed.Remove(key); RebuildRows(); });
            if (collapsed.Contains(key)) continue;
            foreach (var card in group)
            {
                string id = card.Id;
                var button = W.Button(RowsRoot, RowText(card), () => { selected = id; result = ""; if (tab == "overview") tab = "equipment"; detailPage = true; ShowDetail(); });
                rows[id] = button.GetComponentInChildren<TMP_Text>();
            }
        }
        if (!visible.Any()) W.Label(RowsRoot, Text.Get(tab == "observations" ? "Observations.no_match" : "Industry.none"));
        listScroll.verticalNormalizedPosition = scroll;
    }
    private void RefreshReadout()
    {
        var problem = Access();
        if (problem != null) { readout.text = problem; return; }
        if (tab == "overview" && Central)
            readout.text = Text.Get("Industry.overview", cards.Count(c => !c.Instrument), cards.Count(c => !c.Instrument && c.State == EquipmentState.Running), cards.Count(c => !c.Instrument && c.Attention)) +
                "\n\n" + Text.Get("Observations.overview", cards.Count(c => c.Instrument), cards.Count(c => c.Instrument && c.Attention)) +
                "\n\n" + Text.Get("Industry.scope_help") + (result.Length == 0 ? "" : "\n\n" + result);
        else
        {
            var card = cards.FirstOrDefault(c => c.Id == selected);
            readout.text = card == null ? Text.Get("Industry.select") : card.Name + "\n" + card.Id + "\n\n" + card.Detail + (result.Length == 0 ? "" : "\n\n" + result);
        }
    }
    private void ShowDetail()
    {
        W.Clear(details); commands = null!;
        readout = W.Label(details, ""); RefreshReadout();
        var target = cards.Any(c => c.Id == selected && !c.Instrument) ? CollectorService.Resolve(selected) : null;
        if (target == null || tab == "overview" && Central) { Layout(); return; }
        var actions = W.Rect(details, "Commands");
        var layout = actions.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing = W.Gap;
        layout.childControlHeight = layout.childControlWidth = true; layout.childForceExpandHeight = false;
        commands = actions.gameObject.AddComponent<CanvasGroup>(); commands.interactable = Access() == null;
        var provider = EquipmentProviders.For(target.strCODef);
        if (provider != null)
            foreach (var command in provider.Snapshot(target).Actions) Add(actions, command.Id, label: command.Label);
        if (FurnaceRules.Machine(target.strCODef))
        {
            string targetId = target.strID;
            FurnaceInstrumentView.Build(actions, target, (action, value) =>
            { IndustryService.Run(binding, targetId, action, value, out result); RefreshReadout(); });
            if (!Central) { Add(actions, "feed"); Add(actions, "products"); }
            FurnaceInstallationView.Build(actions, target);
            Add(actions, "unpair", label: Text.Get("Furnace.action_unpair"));
            foreach (var peer in IndustryService.Discover(target.ship).Where(c => FurnaceRules.Cooling(c.strCODef)))
                Add(actions, "pair", peer.strID, Text.Get("Furnace.action_pair") + " " + CollectorService.Label(peer));
        }
        if (ProcessingService.IsProcessor(target.strCODef))
        {
            W.Label(actions, Text.Get("Industry.processing"));
            Add(actions, "start"); Add(actions, "pause"); Add(actions, "cancel");
            if (!Central) { Add(actions, "feed"); Add(actions, "products"); }
        }
        if (RoutingRules.IsReceiver(target.strCODef))
        {
            W.Label(actions, Text.Get("Industry.receiving")); Add(actions, "receive"); Add(actions, "pause-receive");
            if (!Central && !ProcessingService.IsProcessor(target.strCODef)) Add(actions, "inventory");
        }
        if (RoutingRules.IsReceiver(target.strCODef) || RoutingRules.IsSender(target.strCODef))
        {
            W.Button(actions, Text.Get("Industry.tab_routing"), () => ShowRouting(target));
        }
        if (IndustrialRules.Group(target.strCODef) == "chute" || IndustrialRules.Group(target.strCODef) == "grabber")
        {
            var processor = ProcessingService.PipelineFor(target);
            if (processor != null)
            {
                string? localProblem = Central ? null : ProcessingService.AccessProblem(processor);
                W.Button(actions, Text.Get("Industry.open_processor"), () => { if (Central) { selected = processor.strID; ShowDetail(); } else Open(processor); }).interactable = localProblem == null;
                if (localProblem != null) W.Label(actions, localProblem);
            }
        }
        if (Central) W.Label(details, Text.Get("Industry.local_only"));
        if (tab == "routing") ShowRouting(target); else Layout();
    }
    private void Add(Transform parent, string action, string? value = null, string? label = null)
    {
        string targetId = selected;
        W.Button(parent, label ?? Text.Get("Industry.action_" + action), () =>
        {
            if (!Central && (action == "feed" || action == "products" || action == "inventory")) CrewSim.LowerUI();
            bool success = IndustryService.Run(binding, targetId, action, value, out string message);
            result = success ? Text.Get("Industry.success", label ?? Text.Get("Industry.action_" + action)) : Text.Get("Industry.rejected", message);
            RefreshReadout();
        });
    }
    private void ShowRouting(CondOwner target)
    {
        var actions = commands.transform; W.Clear(actions);
        W.Button(actions, Text.Get("Industry.tab_equipment"), () => { tab = "equipment"; ShowDetail(); });
        if (RoutingRules.IsReceiver(target.strCODef))
        {
            W.Label(actions, CollectorService.DescribeLink(target, false) + "\n" + CollectorService.LinkIds(target, false) + "\n" + CollectorService.FilterLabel(target));
            Add(actions, "unlink-input");
            foreach (string filter in ProcessingService.IsReclaimer(target) ? new[] { "feed" } : new[] { "all", "feed", "rejects", "legacy" })
                Add(actions, "filter", filter, Text.Get("Industry.filter_" + filter));
            W.Label(actions, Text.Get("Industry.choose_input"));
            foreach (var peer in IndustryService.Discover(target.ship).Where(c => c.strID != target.strID && RoutingRules.CanConnect(c.strCODef, target.strCODef)))
                Add(actions, "link-input", peer.strID, CollectorService.Label(peer));
        }
        if (RoutingRules.IsSender(target.strCODef))
        {
            W.Label(actions, CollectorService.DescribeLink(target, true) + "\n" + CollectorService.LinkIds(target, true)); Add(actions, "unlink-output");
            W.Label(actions, Text.Get("Industry.choose_output"));
            foreach (var peer in IndustryService.Discover(target.ship).Where(c => c.strID != target.strID && RoutingRules.CanConnect(target.strCODef, c.strCODef)))
                Add(actions, "link-output", peer.strID, CollectorService.Label(peer));
        }
        detailPage = true; Layout();
    }
    public override void SaveAndClose()
    {
        if (!bActive) return; // GUIData and IGUIHarness call each other during native cleanup.
        if (search != null && search.isFocused) CrewSim.EndTyping();
        if (frame != null) { Destroy(frame); frame = null; }
        base.SaveAndClose();
    }
}

[HarmonyPatch(typeof(Interaction), nameof(Interaction.ApplyEffects))]
internal static class IndustrialControlsPatch
{
    private static void Postfix(Interaction __instance, bool isCancelIa)
    {
        if (!isCancelIa && (__instance.strName == IndustrialRules.Controls || __instance.strName == IndustrialRules.LocalControls) &&
            __instance.objUs == CrewSim.GetSelectedCrew() && __instance.objThem != null) IndustrialPanel.Open(__instance.objThem);
    }
}

// Exact-key restoration after native fast-forward panels; other panels keep the original path.
[HarmonyPatch(typeof(CrewSim), nameof(CrewSim.RaiseUI))]
internal static class IndustrialRestorePatch
{
    private static bool Prefix(string strCOGUIKey, CondOwner coSelf)
    {
        if (strCOGUIKey != IndustrialPanel.Key) return true;
        if (!IndustrialPanel.Open(coSelf) && CrewSim.goUI == null)
        {
            // A vanished/powerless console cannot strand the player in native FFWD restore state.
            CanvasManager.instance.CrewSimNormal();
            if (EventSystem.current != null) EventSystem.current.sendNavigationEvents = true;
        }
        return false;
    }
}
