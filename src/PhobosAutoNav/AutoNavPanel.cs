using System;
using System.Collections.Generic;
using System.Linq;
using PhobosAutoNav.Core;
using Phobos.Ostranauts.Framework.Controls;
using Ostranauts.ShipGUIs.NavStation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NavPanelDraggable = Ostranauts.ShipGUIs.NavStation.Draggable;

namespace PhobosAutoNav;

/// <summary>Presentation only. Every flight/propulsion/fire action belongs to NavigationService.</summary>
public sealed partial class AutoNavPanel : NavModBase
{
    internal const string LayoutId = "PhobosNavFlightHub";
    internal const string FaceplatePath = "phobos/autonav/PhobosFlightHub.png";
    private static readonly Color Ink = new Color32(205, 216, 216, 255), Amber = new Color32(234, 196, 102, 255);
    private const float CommandTextSize = 20, FlowButtonHeight = 40;
    private RectTransform placement = null!, design = null!;
    private CanvasGroup controls = null!;
    private CanvasGroup surface = null!;
    private CanvasGroup? rescue;
    private TMP_FontAsset? font;
    private readonly Dictionary<string, TMP_Text> labels = new();
    private readonly Dictionary<string, Button> buttons = new();
    private readonly Dictionary<string, TMP_Text> buttonLabels = new();
    private readonly PresentationRefresh refresh = new(.1);
    private readonly Dictionary<string, RectTransform> pages = new();
    private readonly List<Button> settings = new();
    private RectTransform preferences = null!, docking = null!;
    private ScrollRect detailScroll = null!;
    private GUISafetyToggle? fireGuard, safetyGuard, cycleGuard;
    private Slider? flowSlider, cycleSlider;
    private GUIKnob? propulsionKnob;
    private Sprite? faceplateSprite;
    private string page = "navigation";
    private bool lastFire, lastDocking;
    private FlightPreferences preferenceDraft;
    private bool preferencesDirty,torchDraft;
    private int departureDraft, departureExpected;
    private string preferenceExpected="";
    private ConsoleShell draftGuard=null!;
    private RectTransform draftActions=null!;
    private string captionLanguage = "";

    internal static void Ensure(GUIOrbitDraw nav)
    {
        if (!EquipmentContent.NativePackageEnabled) return;
        if (nav.transform.Find(LayoutId) != null) return;
        try { Build(nav); }
        catch (Exception ex)
        {
            // This prefix owns only our extension. Never let a failed hub stop
            // the native station opening, or leave a partial hub for LoadModules.
            var partial = nav.transform.Find(LayoutId);
            if (partial != null)
            {
                partial.gameObject.SetActive(false);
                partial.SetParent(null, false);
                Destroy(partial.gameObject);
            }
            Debug.LogError("Phobos Auto Nav hub could not be created; native Polaris loading will continue. " + ex);
        }
    }
    private static void Build(GUIOrbitDraw nav)
    {
        // Validate the embedded resource before attaching any native UI objects.
        var layout = HubLayout.Data;
        var root = new GameObject(LayoutId, typeof(RectTransform)); root.SetActive(false);
        var rect = (RectTransform)root.transform; rect.SetParent(nav.transform, false); rect.SetAsFirstSibling(); PanelWidgets.Fill(rect);
        var container = PanelWidgets.Rect(rect, "Container");
        container.anchorMin = new Vector2(PanelLayoutRules.DefaultLeft, PanelLayoutRules.DefaultTop - PanelLayoutRules.RowHeight);
        container.anchorMax = new Vector2(PanelLayoutRules.DefaultLeft + PanelLayoutRules.ColumnWidth, PanelLayoutRules.DefaultTop);
        container.offsetMin = container.offsetMax = Vector2.zero;
        container.gameObject.AddComponent<CanvasGroup>();
        container.gameObject.AddComponent<Image>().color = new Color(.12f, .17f, .21f);
        var background = PanelWidgets.Rect(container, "bg"); PanelWidgets.Fill(background);
        var panel = root.AddComponent<AutoNavPanel>(); panel.placement = container;
        panel.surface = container.GetComponent<CanvasGroup>();
        panel.rescue = nav.transform.Find("pnlInside")?.GetComponent<CanvasGroup>();
        panel.design = PanelWidgets.Rect(background, "Faceplate");
        panel.design.anchorMin = panel.design.anchorMax = panel.design.pivot = new Vector2(.5f, .5f);
        panel.design.sizeDelta = new Vector2(layout.width, layout.height);
        var image = panel.design.gameObject.AddComponent<Image>(); image.raycastTarget = false;
        var texture = DataHandler.LoadPNG(FaceplatePath, bNorm: false);
        if (texture != null)
        {
            panel.faceplateSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100);
            image.sprite = panel.faceplateSprite;
        }
        else image.color = new Color(.12f, .17f, .21f);
        var layer = PanelWidgets.Rect(panel.design, "Controls"); PanelWidgets.Fill(layer);
        panel.controls = layer.gameObject.AddComponent<CanvasGroup>();
        panel.font = nav.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.font != null)?.font;
        panel.BuildControls(layer);
        panel.DraggableRef = container.gameObject.AddComponent<NavPanelDraggable>();
        panel.DraggableRef.enabled = false;
        panel.NormalizePlacementBounds();
    }

    private void BuildControls(RectTransform layer)
    {
        // Retain the original outer case, but cover its obsolete painted dividers.
        // Live frames use the same registration as the controls and browser proof.
        var plate = Rect(layer, 24, 52, 552, 896);
        plate.gameObject.AddComponent<Image>().color = PolarisWidgets.Surface;
        foreach (var zone in HubLayout.Data.zones)
        {
            if (zone.id == "title") continue;
            var frame = Rect(layer, zone.x, zone.y, zone.w, zone.h);
            var rim = frame.gameObject.AddComponent<Image>(); rim.color = new Color32(86,100,109,255); rim.raycastTarget = false;
            var inset = PanelWidgets.Rect(frame, "Frame inset"); PanelWidgets.Fill(inset, 2, 2, 2, 2);
            var fill = inset.gameObject.AddComponent<Image>(); fill.raycastTarget = false;
            fill.color = zone.id == "header" || zone.id == "coast" ? new Color32(12,19,23,255) : PolarisWidgets.Surface;
        }
        foreach (string id in new[] { "title", "target", "offensive", "operation", "contact", "restriction", "coast" })
            labels[id] = Label(Box(layer, id), "", id == "title" ? 26 : id == "coast" ? CommandTextSize : 24);
        labels["title"].text = Text.Get("Hub.title"); labels["coast"].text = Text.Get("Hub.coast");
        foreach (string id in new[] { "operation", "contact", "restriction" })
            labels[id].textWrappingMode = TextWrappingModes.NoWrap;
        labels["restriction"].color = Amber;
        labels["restriction"].overflowMode = TextOverflowModes.Ellipsis;
        AddButton(Box(layer, "warningDetails"), "warningDetails", Text.Get("Hub.more"), ShowWarning);
        var tabs = Box(layer, "tabs");
        tabs.gameObject.AddComponent<Image>().color = PolarisWidgets.Surface;
        string[] names = { "navigation", "pursuit", "fire", "systems", "departure", "details" };
        for (int i = 0; i < names.Length; i++)
        {
            string name = names[i];
            AddButton(Rect(tabs, (i % 3) * (528f / 3), (i / 3) * 46, 168, FlowButtonHeight), "tab." + name, Text.Get("Hub." + name), () => { CloseOverlay(); page = name; }, compact: false);
            pages[name] = Box(layer, "body"); pages[name].name = name;
            pages[name].gameObject.AddComponent<RectMask2D>();
        }
        var strip = Box(layer, "actions");
        // Opaque fixed footer protects controls from scrolled native text meshes.
        strip.gameObject.AddComponent<Image>().color = PolarisWidgets.Surface;
        AddButton(Cell(strip, 0, 3), "resume", Text.Get("Hub.resume"), () => Plugin.Service.ResumeSaved(COSelf));
        AddButton(Cell(strip, 1, 3), "stop", Text.Get("Hub.disengage"), () => Plugin.Service.Stop(COSelf, Text.Get("NavigationService.stopped_by_pilot_coasting")));
        AddButton(Cell(strip, 2, 3), "cease", Text.Get("Hub.cease"), () => { CloseOverlay(); Plugin.Service.CeaseFire(); });
        draftActions=Box(layer,"coast");
        AddButton(Cell(draftActions,0,2),"apply-settings",ConsoleWidgets.Text("apply"),ApplyPreferences,true);
        AddButton(Cell(draftActions,1,2),"discard-settings",ConsoleWidgets.Text("discard"),DiscardPreferences,true);
        draftGuard=ConsoleShell.AttachDraftGuard(design,()=>preferencesDirty,ApplyPreferenceDraft,DiscardPreferences);
        draftGuard.EmergencyStop=()=>Plugin.Service.Stop(COSelf,Text.Get("NavigationService.stopped_by_pilot_coasting"));
        BuildNavigation(pages["navigation"]); BuildPursuit(pages["pursuit"]); BuildFire(pages["fire"]); BuildSystems(pages["systems"]);
        var dep = ScrollBody(pages["departure"], "Departure", out var depScroll);
        foreach (string action in new[] { "depart-mode", "depart", "depart-continue", "depart-resume", "depart-stop" })
          { string command = action; var button=PolarisWidgets.Button(dep, Text.Get("Departure." + action), () => Invoke(() => { if(command=="depart-mode"){BeginPreferenceDraft();departureDraft=(departureDraft+1)%Plugin.Service.PanelDepartureModeCount;}else if(!preferencesDirty||command=="depart-stop")Plugin.Service.DepartureAction(COSelf, command); }), FlowButtonHeight);
            buttons[command]=button;
          Style(button.GetComponentInChildren<TMP_Text>(),CommandTextSize); }
        labels["departure"] = PanelWidgets.Label(dep, "", flowing: true);
        Style(labels["departure"],CommandTextSize); labels["departure"].textWrappingMode=TextWrappingModes.Normal;
        var content = ScrollBody(pages["details"], "Diagnostics", out detailScroll);
        FlowButton(content, "watch", Text.Get("Cue.watch"), () => Plugin.Service.WatchArrival(COSelf, true));
        FlowButton(content,"crew-settings",ConsoleWidgets.Text("crew_settings"),()=>draftGuard.Navigate(()=>Phobos.Ostranauts.Framework.Crew.CrewPanel.Show(COSelf)));
        FlowButton(content, "unwatch", Text.Get("Cue.unwatch"), () => Plugin.Service.WatchArrival(COSelf, false));
        var cueVolume = FlowButton(content, "volume", "", () => Phobos.Ostranauts.Framework.Audio.CompletionCues.CycleVolume());
        labels["cue-volume"] = cueVolume.GetComponentInChildren<TMP_Text>();
        labels["details"] = PanelWidgets.Label(content, "", flowing: false);
        Style(labels["details"], CommandTextSize); labels["details"].textWrappingMode = TextWrappingModes.Normal;
        var sizing = labels["details"].gameObject.AddComponent<LayoutElement>(); sizing.minHeight = HubLayout.Data["body"].h;
    }

    private static RectTransform ScrollBody(RectTransform parent, string name, out ScrollRect scroll)
    {
        var content = PanelWidgets.Scroll(parent, name, out scroll);
        PanelWidgets.Fill((RectTransform)scroll.transform, 8, 8, 8, 8);
        scroll.verticalScrollbar.targetGraphic.color = new Color32(153,175,187,255);
        return content;
    }

    private Button FlowButton(Transform parent, string id, string text, Action action)
    {
        var button = PolarisWidgets.Button(parent, text, () => Invoke(action), FlowButtonHeight);
        var label = button.GetComponentInChildren<TMP_Text>(); Style(label, CommandTextSize);
        buttons[id] = button; buttonLabels[id] = label; return button;
    }
    private void RefreshCaptions()
    {
        string language = Phobos.Ostranauts.Framework.Localization.Translations.Language;
        if (captionLanguage == language) return;
        captionLanguage = language;
        foreach (string name in pages.Keys) ButtonCaption("tab." + name, Text.Get("Hub." + name));
        foreach (string name in new[] { "approach", "dock", "approachdock", "rendezvous", "follow", "resume", "shutdown" })
            ButtonCaption(name, Text.Get("Hub." + name));
        ButtonCaption("stop", Text.Get("Hub.disengage")); ButtonCaption("cease", Text.Get("Hub.cease"));
        ButtonCaption("warningDetails", Text.Get("Hub.more")); ButtonCaption("propulsion", Text.Get("Hub.change"));
        ButtonCaption("firetarget", Text.Get("Hub.select_fire_target")); ButtonCaption("reference", Text.Get("FCS.use_aim"));
        ButtonCaption("watch", Text.Get("Cue.watch")); ButtonCaption("unwatch", Text.Get("Cue.unwatch"));
        ButtonCaption("crew-settings", ConsoleWidgets.Text("crew_settings"));
        ButtonCaption("apply-settings", ConsoleWidgets.Text("apply")); ButtonCaption("discard-settings", ConsoleWidgets.Text("discard"));
        foreach (string name in new[] { "depart-mode", "depart", "depart-continue", "depart-resume", "depart-stop" })
            Presentation.Text(buttons[name].GetComponentInChildren<TMP_Text>(), Text.Get("Departure." + name));
        Caption("coast", Text.Get("Hub.coast")); Caption("pursuitHelp", Text.Get("FCS.pursuit_help"));
        foreach (string name in new[] { "flow", "cycle", "safety", "cycleEnable" }) Caption(name + "Label", Text.Get("Hub." + name));
        Caption("engageLabel", Text.Get("FCS.engage"));
    }
    private void BuildNavigation(RectTransform parent)
    {
        var actions = Box(parent, "nav.actions");
        AddButton(Cell(actions, 0, 3), "approach", Text.Get("Hub.approach"), () => Plugin.Service.Engage(COSelf));
        AddButton(Cell(actions, 1, 3), "dock", Text.Get("Hub.dock"), () => Plugin.Service.Dock(COSelf));
        AddButton(Cell(actions, 2, 3), "approachdock", Text.Get("Hub.approachdock"), () => Plugin.Service.ApproachDock(COSelf));
        labels["metrics"] = Readout(Box(parent, "nav.metrics"), 26);
        preferences = PanelWidgets.Rect(parent, "Preferences"); PanelWidgets.Fill(preferences);
        var prop = Box(preferences, "nav.propulsion");
        labels["propulsion"] = Readout(Rect(prop, 0, 0, 320, 52), 24);
        var knobHost = Rect(prop, 332, 0, 64, 52);
        propulsionKnob = NativeInstruments.Clone<GUIKnob>(knobHost, "pnlPower/knobBus", Debug.LogWarning);
        if (propulsionKnob != null)
        {
            FitNative(propulsionKnob, knobHost);
            propulsionKnob.Callback = state => Invoke(() => DraftTorch(state > 0));
        }
        // Explicit text selection accompanies the native knob and remains available if the donor changes.
        AddButton(Rect(prop, 408, 0, 120, 52), "propulsion", Text.Get("Hub.change"), () => DraftTorch(!(preferencesDirty?torchDraft:Plugin.Service.ReadHub(COSelf).Navigation.TorchPreferred)));
        Setting(preferences, "nav.cruise", "cruise", () => DraftSpeed(false,-1), () => DraftSpeed(false,1));
        Setting(preferences, "nav.arrival", "arrival", () => DraftSpeed(true,-1), () => DraftSpeed(true,1));
        Setting(preferences, "nav.separation", "separation", () => DraftArrival(-1), () => DraftArrival(1));
        docking = PanelWidgets.Rect(parent, "Docking"); PanelWidgets.Fill(docking);
        foreach (string id in new[] { "clearance", "ports", "alignment", "progress" }) labels[id] = Readout(Box(docking, "dock." + id), 24, 4);
    }

    private void BuildPursuit(RectTransform parent)
    {
        var actions = Box(parent, "pursuit.actions");
        AddButton(Cell(actions, 0, 2), "rendezvous", Text.Get("Hub.rendezvous"), () => Plugin.Service.StartPursuit(COSelf, false));
        AddButton(Cell(actions, 1, 2), "follow", Text.Get("Hub.follow"), () => Plugin.Service.StartPursuit(COSelf, true));
        AddButton(Box(parent, "pursuit.combat"), "combat", Text.Get("Combat.enter"), () => Plugin.Service.ToggleCombat(COSelf));
        Setting(parent, "pursuit.cruise", "pursuitCruise", () => DraftSpeed(false,-1), () => DraftSpeed(false,1));
        Setting(parent, "pursuit.separation", "pursuitSeparation", () => DraftArrival(-1), () => DraftArrival(1));
        var help = ScrollBody(Box(parent, "pursuit.help"), "Track instructions", out _);
        labels["pursuitHelp"] = PanelWidgets.Label(help, "", flowing: true);
        Style(labels["pursuitHelp"], CommandTextSize);
        labels["pursuitHelp"].textWrappingMode = TextWrappingModes.Normal;
        labels["pursuitHelp"].text = Text.Get("FCS.pursuit_help");
    }
    private void BuildFire(RectTransform parent)
    {
        AddButton(Box(parent, "fire.target"), "firetarget", Text.Get("Hub.select_fire_target"), () => Plugin.Service.SelectFireTarget(COSelf));
        AddButton(Box(parent, "fire.group"), "group", "", ShowGroups);
        var volleys = AddButton(Box(parent, "fire.volleys"), "volleys", "", () => Plugin.Service.StepVolleys(COSelf));
        SecondaryClick.Bind(volleys, () => Invoke(() => Plugin.Service.StepVolleys(COSelf, -1)));
        AddButton(Box(parent, "fire.native"), "native", "", () => Plugin.Service.ToggleFireOwnership(COSelf));
        AddButton(Box(parent, "fire.aim"), "aim", "", () => Plugin.Service.ToggleAutoAim(COSelf));
        AddButton(Box(parent, "fire.reference"), "reference", Text.Get("FCS.use_aim"), () => Plugin.Service.UseAimReference(COSelf));
        AddButton(Box(parent, "fire.weapon"), "weapon", "", () => Plugin.Service.BrowseWeapon(COSelf));
        labels["weaponCard"] = Readout(Box(parent, "fire.card"), 24, 2);
        labels["weaponCard"].textWrappingMode = TextWrappingModes.NoWrap;
        labels["ownership"] = Readout(Box(parent, "fire.ownership"), 24, 2);
        labels["engageLabel"] = Label(Box(parent, "fire.engage"), Text.Get("FCS.engage"), 24);
        fireGuard = Guard(Box(parent, "fire.guard"), on => { if (on) Plugin.Service.EngageWeapons(COSelf); else Plugin.Service.CeaseFire(); });
        labels["fireReady"] = Readout(Box(parent, "fire.ready"), 24, 2);
    }

    private void BuildSystems(RectTransform parent)
    {
        labels["systems"] = Readout(Box(parent, "systems.metrics"), 24);
        foreach (string id in new[] { "flow", "cycle", "safety", "cycleEnable" })
        { labels[id + "Label"] = Label(Box(parent, "systems." + id + "Label"), Text.Get("Hub." + id), 24); labels[id + "Label"].alignment = TextAlignmentOptions.Midline; }
        foreach (string id in new[] { "flowValue", "cycleValue", "safetyValue", "enabledValue" })
        { labels[id] = Readout(Box(parent, "systems." + id), 24, 2); labels[id].alignment = TextAlignmentOptions.Midline; }
        flowSlider = Slider(Box(parent, "systems.flow"), value => Plugin.Service.ManualPropulsionAction(COSelf, ManualPropulsion.Flow, value));
        cycleSlider = Slider(Box(parent, "systems.cycle"), value => Plugin.Service.ManualPropulsionAction(COSelf, ManualPropulsion.Cycle, value));
        safetyGuard = Guard(Box(parent, "systems.safety"), on => Plugin.Service.ManualPropulsionAction(COSelf, ManualPropulsion.Safety, on ? 1 : 0));
        cycleGuard = Guard(Box(parent, "systems.cycleEnable"), on => Plugin.Service.ManualPropulsionAction(COSelf, ManualPropulsion.CycleEnabled, on ? 1 : 0));
        AddButton(Box(parent, "systems.shutdown"), "shutdown", Text.Get("Hub.shutdown"), () => Plugin.Service.ManualPropulsionAction(COSelf, ManualPropulsion.Shutdown, 0));
    }

    private void Setting(Transform parent, string box, string id, Action minus, Action plus)
    {
        var row = Box(parent, box);
        labels[id] = Readout(Rect(row, 0, 0, 344, 56), 24, 2);
        settings.Add(AddButton(Rect(row, 356, 0, 80, 56), id + "-", "-", minus));
        settings.Add(AddButton(Rect(row, 448, 0, 80, 56), id + "+", "+", plus));
    }

    private bool RescueOpen => rescue != null && rescue.gameObject.activeInHierarchy && rescue.alpha > 0;
    private bool CanInteract => COSelf != null && !RescueOpen && (DraggableRef == null || !DraggableRef.enabled);
    private void LateUpdate()
    {
        // Native rescue is a sibling overlay. Keep module registration and saved placement intact.
        if (surface == null || controls == null) return;
        if (!CanInteract) CloseOverlay();
        surface.alpha = RescueOpen ? 0 : 1;
        surface.blocksRaycasts = !RescueOpen;
        controls.interactable = controls.blocksRaycasts = CanInteract;
    }
    // A click that arrives after the station changed underneath acts on nothing; the refresh shows the new station first.
    private void Invoke(Action action) { if (!CanInteract) return; if (FollowConsole()) { ForceRefresh(); return; } CrewSim.bJustClickedInput = true; action(); ForceRefresh(); }

    /// <summary>
    /// The game's crew switch (GUIOrbitDraw.CrewSwitch) keeps the open station's module panels and swaps the
    /// console under them, while NavModBase captures COSelf only once, in Start. Follow the console the screen
    /// shows, so the hub never reads or commands the previous station. Drafts and overlays belonged to it.
    /// </summary>
    internal bool FollowConsole()
    {
        var live = _guiOrbitDraw != null ? _guiOrbitDraw.COSelfBase() : null;
        if (live == null || live == COSelf) return false;
        COSelf = live;
        if (live.mapGUIPropMaps.TryGetValue("Panel A", out var props)) dictPropMap = props;
        CloseOverlay(); preferencesDirty = false; refresh.Invalidate();
        return true;
    }
    protected override void Init() => ForceRefresh();
    private void OnEnable() => refresh.Invalidate();
    private void ForceRefresh() { refresh.Invalidate(); UpdateUI(); }
    protected override void OnNavModMessage(NavModMessageType messageType, object arg)
    { base.OnNavModMessage(messageType, arg); if (messageType == NavModMessageType.EditNavStation) ForceRefresh(); }
    protected override void UpdateUI()
    {
        if (!labels.ContainsKey("title") || !gameObject.activeInHierarchy || !GUIOrbitDraw.IsOpen()) return;
        FollowConsole();
        refresh.Bind(COSelf, CrewSim.GetSelectedCrew(), GUIOrbitDraw.CrossHairTarget?.Ship,
            Phobos.Ostranauts.Framework.Localization.Translations.Language);
        if (!refresh.Due(Time.unscaledTime)) return;
        RefreshCaptions();
        using var measurement = Phobos.Ostranauts.Framework.Diagnostics.Performance.Measure(PerformanceMetrics.PanelRefresh);
        var view = Plugin.Service.ReadHub(COSelf, page);
        bool isDocking = view.DockProgress != DockProgress.None;
        if (isDocking && !lastDocking && page != "navigation")
        {
            CloseOverlay(); page = "navigation";
            // One immediate page transition. Routine refreshes read exactly one snapshot.
            view = Plugin.Service.ReadHub(COSelf, page);
        }
        lastDocking = isDocking;
        UpdateOverlay(view);
        var nav = view.Navigation;
        Presentation.Active(draftActions.gameObject, preferencesDirty);
        Presentation.Active(labels["coast"].gameObject, !preferencesDirty);
        if (preferencesDirty)
        { nav.CruiseMS = preferenceDraft.CruiseMS; nav.ArrivalMS = preferenceDraft.ArrivalMS; nav.ArrivalKM = preferenceDraft.ArrivalKM; nav.TorchPreferred = torchDraft; }
        controls.interactable = controls.blocksRaycasts = CanInteract;
        foreach (var entry in pages)
        {
            Presentation.Active(entry.Value.gameObject, entry.Key == page);
            PolarisWidgets.Selected(buttons["tab." + entry.Key], entry.Key == page);
        }
        Caption("title", Text.Get("Hub.title"));
        Caption("target", Text.Get("Hub.nav_target", nav.Target));
        Caption("offensive", view.WorkingFire ? Text.Get("Hub.offensive_target", view.OffensiveTarget) :
            Text.Get(view.WorkingPursuit ? "Hub.n2_ready" : view.WorkingNavigation ? "Hub.n1_ready" : "Hub.module_unavailable"));
        Presentation.Color(labels["offensive"], view.FirePermitted ? Amber : Ink);
        Caption("operation", view.Combat ? Text.Get("Combat.controller", view.Movement) : isDocking ? Text.Get("Hub.operation." + view.DockProgress) : view.WorkingFire && !view.Active && !nav.Warning && page == "fire" ? view.Ownership : nav.Heading);
        Caption("contact", Text.Get("FCS.contacts", Text.Get("FCS.contact." + view.Contact.State), Text.Get("FCS.contact." + view.FireContact.State)));
        fullWarning = view.Restriction;
        Caption("restriction", fullWarning);
        Enable("warningDetails", !string.IsNullOrWhiteSpace(fullWarning));
        Enable("resume", !preferencesDirty && nav.Resumable && view.WorkingNavigation);
        Enable("stop", nav.CanStop || view.AutoAiming || view.FirePermitted);
        Enable("cease", view.Active || view.FirePermitted || view.AutoAiming || view.FireHeld);
        // Keep this edge even while Fire is hidden, so its safety cover closes on cessation.
        if (lastFire && !view.FirePermitted && fireGuard != null) fireGuard.Closed = true;
        lastFire = view.FirePermitted;

        if (page == "navigation")
        {
            Presentation.Active(preferences.gameObject, !isDocking); Presentation.Active(docking.gameObject, isDocking);
            Caption("metrics", Text.Get("Hub.metrics", Number(view.RangeKM, "0.00"), Number(view.ClosingMS, "+0.0;-0.0;0.0"), Number(view.RelativeMS, "0.0")));
            Caption("propulsion", Text.Get("Hub.propulsion", Text.Get(nav.TorchPreferred ? "Hub.auto" : "Hub.rcs")));
            Caption("cruise", Text.Get("Hub.cruise_value", nav.CruiseMS));
            Caption("arrival", Text.Get("Hub.arrival_value", nav.ArrivalMS));
            Caption("separation", Text.Get("Hub.separation_value", nav.ArrivalKM));
            Caption("clearance", view.Clearance); Caption("ports", Text.Get("Hub.ports", view.OwnPort, view.TargetPort));
            Caption("alignment", Text.Get("Hub.alignment", Number(view.AlignmentDegrees, "+0.00;-0.00;0.00")));
            Caption("progress", Text.Get("Hub.progress." + view.DockProgress));
            Enable("approach", !preferencesDirty && nav.CanFly && !nav.Resumable && view.WorkingNavigation);
            Enable("dock", !preferencesDirty && nav.CanDock && view.WorkingNavigation);
            Enable("approachdock", !preferencesDirty && view.CanApproachDock);
            Enable("propulsion", nav.CanAdjustPropulsion && view.WorkingNavigation);
            if (propulsionKnob != null)
            {
                NativeInstruments.Refresh(propulsionKnob, nav.TorchPreferred ? 1 : 0);
                bool enabled = nav.CanAdjustPropulsion && view.WorkingNavigation;
                if (propulsionKnob.enabled != enabled) propulsionKnob.enabled = enabled;
            }
        }
        if (page == "navigation" || page == "pursuit")
            foreach (var button in settings) Presentation.Enabled(button, nav.CanAdjustArrival && view.WorkingNavigation);
        if (page == "pursuit")
        {
            ButtonCaption("combat", Text.Get(view.Combat ? "Combat.leave" : "Combat.enter"));
            Enable("combat", view.Combat || !preferencesDirty && view.CanCombat);
            Caption("pursuitHelp", (view.Active ? view.Movement + "\n" + nav.Range + "\n" + Text.Get("Combat.separation", nav.ArrivalKM,
                view.EffectiveSeparationKM ?? nav.ArrivalKM) + "\n\n" : "") + Text.Get("FCS.pursuit_help"));
            Caption("pursuitCruise", Text.Get("Hub.cruise_value", nav.CruiseMS));
            Caption("pursuitSeparation", Text.Get("Hub.separation_value", nav.ArrivalKM));
            Enable("rendezvous", !preferencesDirty && nav.CanFly && !nav.Resumable && view.WorkingPursuit);
            Enable("follow", !preferencesDirty && nav.CanFly && !nav.Resumable && view.WorkingPursuit);
        }
        if (page == "fire")
        {
            Caption("weaponCard", view.WeaponCard); Caption("ownership", Text.Get("FCS.ownership", view.Ownership, view.Remaining));
            ButtonCaption("weapon", view.WeaponLabel); ButtonCaption("volleys", Text.Get("FCS.volleys_hint", view.Volleys));
            ButtonCaption("native", Text.Get(view.FireHeld ? "FCS.return_native" : "FCS.take_control"));
            ButtonCaption("aim", Text.Get("FCS.auto_aim", State(view.AutoAiming))); ButtonCaption("group", Text.Get("Hub.group", view.WeaponGroup));
            Caption("fireReady", fireGuard == null ? Text.Get("Hub.guard_missing") : !view.WorkingFire ? Text.Get("FCS.module_required") : view.FireReason);
            Enable("firetarget", view.CanSelectWeapons); Enable("volleys", view.CanSelectWeapons); Enable("weapon", view.CanSelectWeapons); Enable("reference", view.CanSelectWeapons);
            Enable("group", view.CanChangeGroup); Enable("native", view.CanReturnFire || view.CanSelectWeapons); Enable("aim", view.CanAim || view.AutoAiming);
            Refresh(fireGuard, view.FirePermitted, view.CanEngage || view.FirePermitted);
        }
        if (page == "systems")
        {
            Caption("systems", Text.Get("Hub.system_metrics", Number(view.RcsAuthorityMS2, "0.000"), Number(view.RcsFuelKG, "0.0"),
                Number(view.DeliveredMS2, "0.000"), Number(view.TorchHours, "0.00"), Number(view.ConnectedKWh, "0.0"), Number(view.CoreMK, "0.0"), State(view.NoWake)));
            Caption("flowValue", Number(view.Flow * 100, "0") + " %"); Caption("cycleValue", Number(view.Cycle * 100, "0") + " %");
            Caption("safetyValue", State(view.Safety)); Caption("enabledValue", State(view.CycleEnabled)); Enable("shutdown", view.CanShutdown);
            Refresh(safetyGuard, view.Safety == true, view.CanManual && view.Safety.HasValue);
            Refresh(cycleGuard, view.CycleEnabled == true, view.CanManual && (view.CanBurn || view.CycleEnabled == true));
            Refresh(flowSlider, view.Flow, view.FlowLimit, view.CanManual); Refresh(cycleSlider, view.Cycle, view.CycleLimit, view.CanManual);
        }
        if (page == "departure")
        {
            Caption("departure", preferencesDirty ? Plugin.Service.PanelDepartureLabel(departureDraft) + "\n" + ConsoleWidgets.Text("apply_first") : Plugin.Service.DepartureDescription(COSelf));
            Enable("depart", !preferencesDirty); Enable("depart-continue", !preferencesDirty); Enable("depart-resume", !preferencesDirty);
        }
        if (page == "details")
        {
            Caption("cue-volume", Phobos.Ostranauts.Framework.Audio.CompletionCues.VolumeLabel);
            Caption("details", view.CompletionCue + "\n\n" + nav.Details + "\n\n" + NativeSensorSuite.Describe(COSelf?.ship, view.TargetId) +
                "\n\n" + Text.Get("Hub.help") + "\n\n" + Plugin.Service.PursuitSummary(COSelf));
        }
    }
    private void Caption(string id, string value) => Presentation.Text(labels[id], value);
    private void ButtonCaption(string id, string value) => Presentation.Text(buttonLabels[id], value);
    private void Enable(string id, bool enabled) => Presentation.Enabled(buttons[id], enabled);

    private void BeginPreferenceDraft()
    {
        if(preferencesDirty)return;
        preferenceDraft=Plugin.Service.PanelPreferences(COSelf);preferenceExpected=Plugin.Service.PanelPreferenceStamp(COSelf);
        torchDraft=Plugin.Service.ReadHub(COSelf).Navigation.TorchPreferred;departureDraft=departureExpected=Plugin.Service.PanelDepartureMode;preferencesDirty=true;
    }
    private void DraftSpeed(bool arrival,int direction)
    {
        BeginPreferenceDraft();double value=InstrumentRules.StepSpeed(arrival?preferenceDraft.ArrivalMS:preferenceDraft.CruiseMS,direction,arrival,preferenceDraft.CruiseMS);
        preferenceDraft=arrival?new FlightPreferences(preferenceDraft.CruiseMS,value,preferenceDraft.ArrivalKM):new FlightPreferences(value,Math.Min(value,preferenceDraft.ArrivalMS),preferenceDraft.ArrivalKM);
    }
    private void DraftArrival(int direction){BeginPreferenceDraft();preferenceDraft=new FlightPreferences(preferenceDraft.CruiseMS,preferenceDraft.ArrivalMS,InstrumentRules.StepArrival(preferenceDraft.ArrivalKM,direction));}
    private void DraftTorch(bool value){BeginPreferenceDraft();torchDraft=value;}
    private bool ApplyPreferenceDraft()
    {if(!preferencesDirty)return true;if(!Plugin.Service.ApplyNavigationPanel(COSelf,preferenceExpected,preferenceDraft,torchDraft,departureExpected,departureDraft)){ForceRefresh();return false;}preferencesDirty=false;ForceRefresh();return true;}
    private void ApplyPreferences()=>ApplyPreferenceDraft();
    private void DiscardPreferences(){preferencesDirty=false;ForceRefresh();}
    private static string Number(double? value, string format) => value.HasValue ? value.Value.ToString(format, System.Globalization.CultureInfo.CurrentCulture) : Text.Get("Hub.unavailable");
    private static string State(bool? value) => Text.Get(value.HasValue ? value.Value ? "Hub.on" : "Hub.off" : "Hub.unavailable");
    private static void Refresh(GUISafetyToggle? guard, bool value, bool enabled)
    { if (guard == null) return; NativeInstruments.Refresh(guard, value); Presentation.Enabled(guard.chkSwitch, enabled); }
    private static void Refresh(Slider? slider, double? value, float maximum, bool enabled)
    {
        if (slider == null) return;
        // Changing maxValue can invoke Unity's change event. Set the value silently first
        // and keep a fixed normalized range; service converts the selected fraction.
        float shown = value.HasValue && maximum > 0 ? (float)value.Value / maximum : 0;
        if (slider.value != shown) slider.SetValueWithoutNotify(shown);
        Presentation.Enabled(slider, enabled && value.HasValue);
    }
    internal void NormalizePlacementBounds()
    {
        if (placement == null || !(placement.parent is RectTransform board)) return;
        if (!PanelLayoutRules.TryBounds(board.rect.width, board.rect.height, placement.anchorMin.x, placement.anchorMax.y, out var bounds)) return;
        var minimum = new Vector2(bounds.Left, bounds.Bottom); var maximum = new Vector2(bounds.Right, bounds.Top);
        if (placement.anchorMin != minimum) placement.anchorMin = minimum;
        if (placement.anchorMax != maximum) placement.anchorMax = maximum;
        if (placement.offsetMin != Vector2.zero) placement.offsetMin = Vector2.zero;
        if (placement.offsetMax != Vector2.zero) placement.offsetMax = Vector2.zero;
        if (design != null)
        {
            var scale = new Vector3(board.rect.width * PanelLayoutRules.ColumnWidth / HubLayout.Data.width,
                board.rect.height * PanelLayoutRules.RowHeight / HubLayout.Data.height, 1);
            if (design.localScale != scale) design.localScale = scale;
        }
    }
    private void OnRectTransformDimensionsChange() => NormalizePlacementBounds();
    private void OnDisable() => CloseOverlay();
    private new void OnDestroy() { CloseOverlay(); base.OnDestroy(); if (faceplateSprite != null) Destroy(faceplateSprite); }
    private static RectTransform Box(Transform parent, string id)
    { var box = HubLayout.Data[id]; return Rect(parent, box.x, box.y, box.w, box.h); }
    private static RectTransform Rect(Transform parent, float x, float y, float width, float height)
    {
        var rect = PanelWidgets.Rect(parent, "Region"); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height); return rect;
    }
    private static RectTransform Cell(RectTransform parent, int i, int count)
    { float width = (parent.sizeDelta.x - (count - 1) * 8) / count; return Rect(parent, i * (width + 8), 0, width, parent.sizeDelta.y); }
    private TMP_Text Label(RectTransform parent, string text, float size)
    {
        if (parent.GetComponent<RectMask2D>() == null) parent.gameObject.AddComponent<RectMask2D>();
        var label = PanelWidgets.Label(parent, text, flowing: false); PanelWidgets.Fill((RectTransform)label.transform); Style(label, size); return label;
    }
    private TMP_Text Readout(RectTransform parent, float size, float verticalPadding = 6)
    {
        // Live display wells, independent of the raster. Their content rectangle is
        // inset from the bezel; every page uses the same measurable padding contract.
        var bezel = parent.gameObject.AddComponent<Image>(); bezel.color = new Color32(86, 100, 109, 255); bezel.raycastTarget = false;
        var glass = PanelWidgets.Rect(parent, "ReadoutGlass"); PanelWidgets.Fill(glass, 2, 2, 2, 2);
        var screen = glass.gameObject.AddComponent<Image>(); screen.color = new Color32(12, 19, 23, 255); screen.raycastTarget = false;
        var label = Label(parent, "", size);
        PanelWidgets.Fill((RectTransform)label.transform, 12, verticalPadding, 12, verticalPadding);
        return label;
    }
    private void Style(TMP_Text label, float size)
    {
        if (font != null) label.font = font;
        label.fontSize = size; label.enableAutoSizing = false; label.color = Ink; label.richText = false;
        label.alignment = TextAlignmentOptions.MidlineLeft; label.raycastTarget = false;
        PanelWidgets.FitFixedText(label);
    }
    private Button AddButton(RectTransform rect, string id, string text, Action action, bool compact = false)
    {
        var face = rect;
        var image = rect.gameObject.AddComponent<Image>();
        if (compact)
        {
            // Smaller tab faces retain the full 24 px high hit area at minimum display size.
            image.color = Color.clear;
            face = PanelWidgets.Rect(rect, "ButtonFace"); PanelWidgets.Fill(face, 2, 6, 2, 6);
            image = face.gameObject.AddComponent<Image>(); image.raycastTarget = false;
        }
        image.color = new Color(.24f, .28f, .31f);
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        // Share artwork only; no native controller, callback or unrelated panel is instantiated.
        var donor = Resources.Load<GameObject>("GUIShip/GUIAirPump")?.transform.Find("pnlInside/btnDone")?.GetComponent<Button>();
        if (donor?.targetGraphic is Image source && source.sprite != null)
        { image.sprite = source.sprite; image.type = source.type; button.transition = donor.transition; button.spriteState = donor.spriteState; button.colors = donor.colors; }
        var label = Label(face, text, CommandTextSize); PanelWidgets.Fill((RectTransform)label.transform, 6, 2, 6, 2); label.alignment = TextAlignmentOptions.Midline;
        PolarisWidgets.Style(button);
        button.onClick.AddListener(() => Invoke(action)); buttons[id] = button; buttonLabels[id] = label; return button;
    }
    private GUISafetyToggle? Guard(RectTransform host, Action<bool> action)
    {
        var guard = NativeInstruments.Clone<GUISafetyToggle>(host, "pnlPower/chkThrustSafety", Debug.LogWarning);
        if (guard == null) { Label(host, Text.Get("Hub.unavailable"), 24); return null; }
        FitNative(guard, host); guard.Closed = true; guard.bSilent = false;
        guard.chkSwitch.onValueChanged.AddListener(on => Invoke(() => action(on))); return guard;
    }
    private Slider? Slider(RectTransform host, Action<float> action)
    {
        var slider = NativeInstruments.Clone<Slider>(host, "pnlPower/SliderFlow", Debug.LogWarning);
        if (slider == null) { Label(host, Text.Get("Hub.unavailable"), 24); return null; }
        FitNative(slider, host, stretch: true); slider.minValue = 0; slider.maxValue = 1;
        slider.onValueChanged.AddListener(value => Invoke(() =>
        {
            var view = Plugin.Service.ReadHub(COSelf);
            action(value * (slider == flowSlider ? view.FlowLimit : view.CycleLimit));
        }));
        return slider;
    }
    private static void FitNative(Component component, RectTransform host, bool stretch = false)
    {
        var wrapper = (RectTransform)component.transform.parent; PanelWidgets.Fill(wrapper);
        var rect = (RectTransform)component.transform;
        if (stretch) { rect.sizeDelta = host.sizeDelta; rect.localScale = Vector3.one; }
        else rect.localScale = Vector3.one * Math.Min(host.sizeDelta.x / rect.sizeDelta.x, host.sizeDelta.y / rect.sizeDelta.y);
    }
}
