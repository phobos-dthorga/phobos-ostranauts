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
public sealed class AutoNavPanel : NavModBase
{
    internal const string LayoutId = "PhobosNavFlightHub";
    internal const string FaceplatePath = "phobos/autonav/PhobosFlightHub.png";
    private static readonly Color Ink = new Color32(205, 216, 216, 255), Amber = new Color32(234, 196, 102, 255);
    private RectTransform placement = null!, design = null!;
    private CanvasGroup controls = null!;
    private TMP_FontAsset? font;
    private readonly Dictionary<string, TMP_Text> labels = new();
    private readonly Dictionary<string, Button> buttons = new();
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
        var rect = (RectTransform)root.transform; rect.SetParent(nav.transform, false); PanelWidgets.Fill(rect);
        var container = PanelWidgets.Rect(rect, "Container");
        container.anchorMin = new Vector2(PanelLayoutRules.DefaultLeft, PanelLayoutRules.DefaultTop - PanelLayoutRules.RowHeight);
        container.anchorMax = new Vector2(PanelLayoutRules.DefaultLeft + PanelLayoutRules.ColumnWidth, PanelLayoutRules.DefaultTop);
        container.offsetMin = container.offsetMax = Vector2.zero;
        container.gameObject.AddComponent<CanvasGroup>();
        container.gameObject.AddComponent<Image>().color = new Color(.12f, .17f, .21f);
        var background = PanelWidgets.Rect(container, "bg"); PanelWidgets.Fill(background);
        var panel = root.AddComponent<AutoNavPanel>(); panel.placement = container;
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
        foreach (string id in new[] { "title", "target", "offensive", "operation", "contact", "restriction", "coast" })
            labels[id] = Label(Box(layer, id), "", id == "title" ? 26 : 24);
        labels["title"].text = Text.Get("Hub.title"); labels["coast"].text = Text.Get("Hub.coast");
        foreach (string id in new[] { "operation", "contact", "restriction" })
            labels[id].textWrappingMode = TextWrappingModes.NoWrap;
        labels["restriction"].color = Amber;
        var tabs = Box(layer, "tabs");
        string[] names = { "navigation", "pursuit", "fire", "systems", "details" };
        for (int i = 0; i < names.Length; i++)
        {
            string name = names[i];
            AddButton(Cell(tabs, i, names.Length), "tab." + name, Text.Get("Hub." + name), () => { page = name; UpdateUI(); }, compact: true);
            pages[name] = Box(layer, "body"); pages[name].name = name;
        }
        var strip = Box(layer, "actions");
        AddButton(Cell(strip, 0, 3), "resume", Text.Get("Hub.resume"), () => Plugin.Service.ResumeSaved(COSelf));
        AddButton(Cell(strip, 1, 3), "stop", Text.Get("Hub.disengage"), () => Plugin.Service.Stop(COSelf, Text.Get("NavigationService.stopped_by_pilot_coasting")));
        AddButton(Cell(strip, 2, 3), "cease", Text.Get("Hub.cease"), () => Plugin.Service.CeaseFire());
        BuildNavigation(pages["navigation"]); BuildPursuit(pages["pursuit"]); BuildFire(pages["fire"]); BuildSystems(pages["systems"]);
        var content = PanelWidgets.Scroll(pages["details"], "Diagnostics", out detailScroll);
        PanelWidgets.Fill((RectTransform)detailScroll.transform);
        PanelWidgets.Button(content, Text.Get("Cue.watch"), () => Plugin.Service.WatchArrival(COSelf, true));
        PanelWidgets.Button(content, Text.Get("Cue.unwatch"), () => Plugin.Service.WatchArrival(COSelf, false));
        var cueVolume = PanelWidgets.Button(content, "", () => Phobos.Ostranauts.Framework.Audio.CompletionCues.CycleVolume());
        labels["cue-volume"] = cueVolume.GetComponentInChildren<TMP_Text>();
        labels["details"] = PanelWidgets.Label(content, "", flowing: false);
        Style(labels["details"], 24); labels["details"].overflowMode = TextOverflowModes.Overflow;
        var sizing = labels["details"].gameObject.AddComponent<LayoutElement>(); sizing.minHeight = HubLayout.Data["body"].h;
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
            propulsionKnob.Callback = state => Invoke(() => Plugin.Service.SetPanelTorch(COSelf, state > 0));
        }
        // Explicit text selection accompanies the native knob and remains available if the donor changes.
        AddButton(Rect(prop, 408, 0, 120, 52), "propulsion", Text.Get("Hub.change"), () => Plugin.Service.SetPanelTorch(COSelf, !Plugin.Service.ReadHub(COSelf).Navigation.TorchPreferred));
        Setting(preferences, "nav.cruise", "cruise", () => Plugin.Service.StepPanelSpeed(COSelf, false, -1), () => Plugin.Service.StepPanelSpeed(COSelf, false, 1));
        Setting(preferences, "nav.arrival", "arrival", () => Plugin.Service.StepPanelSpeed(COSelf, true, -1), () => Plugin.Service.StepPanelSpeed(COSelf, true, 1));
        Setting(preferences, "nav.separation", "separation", () => Plugin.Service.StepPanelArrival(COSelf, -1), () => Plugin.Service.StepPanelArrival(COSelf, 1));
        docking = PanelWidgets.Rect(parent, "Docking"); PanelWidgets.Fill(docking);
        foreach (string id in new[] { "clearance", "ports", "alignment", "progress" }) labels[id] = Readout(Box(docking, "dock." + id), 24, 4);
    }

    private void BuildPursuit(RectTransform parent)
    {
        var actions = Box(parent, "pursuit.actions");
        AddButton(Cell(actions, 0, 2), "rendezvous", Text.Get("Hub.rendezvous"), () => Plugin.Service.StartPursuit(COSelf, false));
        AddButton(Cell(actions, 1, 2), "follow", Text.Get("Hub.follow"), () => Plugin.Service.StartPursuit(COSelf, true));
        Setting(parent, "pursuit.cruise", "pursuitCruise", () => Plugin.Service.StepPanelSpeed(COSelf, false, -1), () => Plugin.Service.StepPanelSpeed(COSelf, false, 1));
        Setting(parent, "pursuit.separation", "pursuitSeparation", () => Plugin.Service.StepPanelArrival(COSelf, -1), () => Plugin.Service.StepPanelArrival(COSelf, 1));
        labels["pursuitHelp"] = Readout(Box(parent, "pursuit.help"), 24);
        labels["pursuitHelp"].text = Text.Get("FCS.pursuit_help");
    }
    private void BuildFire(RectTransform parent)
    {
        AddButton(Box(parent, "fire.target"), "firetarget", Text.Get("Hub.select_fire_target"), () => Plugin.Service.SelectFireTarget(COSelf));
        AddButton(Box(parent, "fire.group"), "group", "", () => Plugin.Service.StepWeapons(COSelf));
        AddButton(Box(parent, "fire.volleys"), "volleys", "", () => Plugin.Service.StepVolleys(COSelf));
        AddButton(Box(parent, "fire.native"), "native", "", () => Plugin.Service.ToggleFireOwnership(COSelf));
        AddButton(Box(parent, "fire.aim"), "aim", "", () => Plugin.Service.ToggleAutoAim(COSelf));
        AddButton(Box(parent, "fire.reference"), "reference", Text.Get("FCS.use_aim"), () => Plugin.Service.UseAimReference(COSelf));
        AddButton(Box(parent, "fire.weapon"), "weapon", "", () => Plugin.Service.BrowseWeapon(COSelf));
        labels["weaponCard"] = Readout(Box(parent, "fire.card"), 24, 2);
        labels["weaponCard"].textWrappingMode = TextWrappingModes.NoWrap;
        labels["ownership"] = Readout(Box(parent, "fire.ownership"), 24, 2);
        Label(Box(parent, "fire.engage"), Text.Get("FCS.engage"), 24);
        fireGuard = Guard(Box(parent, "fire.guard"), on => { if (on) Plugin.Service.EngageWeapons(COSelf); else Plugin.Service.CeaseFire(); });
        labels["fireReady"] = Readout(Box(parent, "fire.ready"), 24, 2);
    }

    private void BuildSystems(RectTransform parent)
    {
        labels["systems"] = Readout(Box(parent, "systems.metrics"), 24);
        foreach (string id in new[] { "flow", "cycle", "safety", "cycleEnable" })
            Label(Box(parent, "systems." + id + "Label"), Text.Get("Hub." + id), 24).alignment = TextAlignmentOptions.Center;
        foreach (string id in new[] { "flowValue", "cycleValue", "safetyValue", "enabledValue" })
        { labels[id] = Readout(Box(parent, "systems." + id), 24, 2); labels[id].alignment = TextAlignmentOptions.Center; }
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
        settings.Add(AddButton(Rect(row, 356, 0, 80, 56), id + "-", "−", minus));
        settings.Add(AddButton(Rect(row, 448, 0, 80, 56), id + "+", "+", plus));
    }

    private bool CanInteract => COSelf != null && (DraggableRef == null || !DraggableRef.enabled);
    private void Invoke(Action action) { if (!CanInteract) return; CrewSim.bJustClickedInput = true; action(); UpdateUI(); }
    protected override void Init() => UpdateUI();
    protected override void OnNavModMessage(NavModMessageType messageType, object arg)
    { base.OnNavModMessage(messageType, arg); if (messageType == NavModMessageType.EditNavStation) UpdateUI(); }
    protected override void UpdateUI()
    {
        if (!labels.ContainsKey("title")) return;
        NormalizePlacementBounds();
        var view = Plugin.Service.ReadHub(COSelf); var nav = view.Navigation;
        controls.interactable = controls.blocksRaycasts = CanInteract;
        bool isDocking = view.DockProgress != DockProgress.None;
        if (isDocking && !lastDocking) page = "navigation";
        lastDocking = isDocking;
        foreach (var entry in pages) entry.Value.gameObject.SetActive(entry.Key == page);
        foreach (string name in pages.Keys) buttons["tab." + name].targetGraphic.color = name == page ? new Color(.35f, .42f, .46f) : new Color(.24f, .28f, .31f);
        preferences.gameObject.SetActive(!isDocking); docking.gameObject.SetActive(isDocking);
        labels["title"].text = Text.Get("Hub.title");
        labels["target"].text = Text.Get("Hub.nav_target", nav.Target);
        labels["offensive"].text = view.WorkingFire ? Text.Get("Hub.offensive_target", view.OffensiveTarget) :
            Text.Get(view.WorkingPursuit ? "Hub.n2_ready" : view.WorkingNavigation ? "Hub.n1_ready" : "Hub.module_unavailable");
        labels["offensive"].color = view.FirePermitted ? Amber : Ink;
        labels["operation"].text = isDocking ? Text.Get("Hub.operation." + view.DockProgress) : view.WorkingFire && !view.Active ? view.Ownership : nav.Heading;
        labels["contact"].text = Text.Get("FCS.contacts", Text.Get("FCS.contact." + view.Contact.State), Text.Get("FCS.contact." + view.FireContact.State));
        labels["restriction"].text = view.Restriction;
        labels["metrics"].text = Text.Get("Hub.metrics", Number(view.RangeKM, "0.00"), Number(view.ClosingMS, "+0.0;-0.0;0.0"), Number(view.RelativeMS, "0.0"));
        labels["propulsion"].text = Text.Get("Hub.propulsion", Text.Get(nav.TorchPreferred ? "Hub.auto" : "Hub.rcs"));
        labels["cruise"].text = labels["pursuitCruise"].text = Text.Get("Hub.cruise_value", nav.CruiseMS);
        labels["arrival"].text = Text.Get("Hub.arrival_value", nav.ArrivalMS);
        labels["separation"].text = labels["pursuitSeparation"].text = Text.Get("Hub.separation_value", nav.ArrivalKM);
        labels["clearance"].text = view.Clearance;
        labels["ports"].text = Text.Get("Hub.ports", view.OwnPort, view.TargetPort);
        labels["alignment"].text = Text.Get("Hub.alignment", Number(view.AlignmentDegrees, "+0.00;-0.00;0.00"));
        labels["progress"].text = Text.Get("Hub.progress." + view.DockProgress);
        labels["weaponCard"].text = view.WeaponCard;
        labels["ownership"].text = Text.Get("FCS.ownership", view.Ownership, view.Remaining);
        buttons["weapon"].GetComponentInChildren<TMP_Text>().text = view.WeaponLabel;
        buttons["volleys"].GetComponentInChildren<TMP_Text>().text = Text.Get("FCS.volleys", view.Volleys);
        buttons["native"].GetComponentInChildren<TMP_Text>().text = Text.Get(view.FireHeld ? "FCS.return_native" : "FCS.take_control");
        buttons["aim"].GetComponentInChildren<TMP_Text>().text = Text.Get("FCS.auto_aim", State(view.AutoAiming));
        buttons["group"].GetComponentInChildren<TMP_Text>().text = Text.Get("Hub.group", view.WeaponGroup);
        labels["fireReady"].text = fireGuard == null ? Text.Get("Hub.guard_missing") : !view.WorkingFire ? Text.Get("FCS.module_required") : view.FireReason;
        labels["systems"].text = Text.Get("Hub.system_metrics", Number(view.RcsAuthorityMS2, "0.000"), Number(view.RcsFuelKG, "0.0"),
            Number(view.DeliveredMS2, "0.000"), Number(view.TorchHours, "0.00"), Number(view.ConnectedKWh, "0.0"),
            Number(view.CoreMK, "0.0"), State(view.NoWake));
        labels["flowValue"].text = Number(view.Flow * 100, "0") + " %";
        labels["cycleValue"].text = Number(view.Cycle * 100, "0") + " %";
        labels["safetyValue"].text = State(view.Safety); labels["enabledValue"].text = State(view.CycleEnabled);
        labels["cue-volume"].text = Phobos.Ostranauts.Framework.Audio.CompletionCues.VolumeLabel;
        labels["details"].text = view.CompletionCue + "\n\n" + nav.Details + "\n\n" + Text.Get("Hub.help") + "\n\n" + Plugin.Service.PursuitSummary(COSelf);
        buttons["resume"].interactable = nav.Resumable && view.WorkingNavigation;
        buttons["stop"].interactable = nav.CanStop || view.AutoAiming || view.FirePermitted;
        // Always accessible while following, including when the guarded switch is on another page.
        buttons["cease"].interactable = view.Active || view.FirePermitted || view.AutoAiming || view.FireHeld;
        buttons["approach"].interactable = nav.CanFly && !nav.Resumable && view.WorkingNavigation;
        buttons["dock"].interactable = nav.CanDock && view.WorkingNavigation;
        buttons["approachdock"].interactable = view.CanApproachDock;
        buttons["rendezvous"].interactable = buttons["follow"].interactable = nav.CanFly && !nav.Resumable && view.WorkingPursuit;
        buttons["firetarget"].interactable = buttons["volleys"].interactable = buttons["weapon"].interactable = buttons["reference"].interactable = view.CanSelectWeapons;
        buttons["group"].interactable = view.CanChangeGroup;
        buttons["native"].interactable = view.CanReturnFire || view.CanSelectWeapons;
        buttons["aim"].interactable = view.CanAim || view.AutoAiming;
        buttons["propulsion"].interactable = nav.CanAdjustPropulsion && view.WorkingNavigation;
        buttons["shutdown"].interactable = view.CanShutdown;
        foreach (var button in settings) button.interactable = nav.CanAdjustArrival && view.WorkingNavigation;
        if (propulsionKnob != null)
        {
            NativeInstruments.Refresh(propulsionKnob, nav.TorchPreferred ? 1 : 0);
            propulsionKnob.enabled = nav.CanAdjustPropulsion && view.WorkingNavigation;
        }
        Refresh(fireGuard, view.FirePermitted, view.CanEngage || view.FirePermitted);
        if (lastFire && !view.FirePermitted && fireGuard != null) fireGuard.Closed = true;
        lastFire = view.FirePermitted;
        Refresh(safetyGuard, view.Safety == true, view.CanManual && view.Safety.HasValue);
        Refresh(cycleGuard, view.CycleEnabled == true, view.CanManual && (view.CanBurn || view.CycleEnabled == true));
        Refresh(flowSlider, view.Flow, view.FlowLimit, view.CanManual);
        Refresh(cycleSlider, view.Cycle, view.CycleLimit, view.CanManual);
    }

    private static string Number(double? value, string format) => value.HasValue ? value.Value.ToString(format, System.Globalization.CultureInfo.CurrentCulture) : Text.Get("Hub.unavailable");
    private static string State(bool? value) => Text.Get(value.HasValue ? value.Value ? "Hub.on" : "Hub.off" : "Hub.unavailable");
    private static void Refresh(GUISafetyToggle? guard, bool value, bool enabled)
    { if (guard == null) return; NativeInstruments.Refresh(guard, value); guard.chkSwitch.interactable = enabled; }
    private static void Refresh(Slider? slider, double? value, float maximum, bool enabled)
    {
        if (slider == null) return;
        // Changing maxValue can invoke Unity's change event. Set the value silently first
        // and keep a fixed normalized range; service converts the selected fraction.
        slider.SetValueWithoutNotify(value.HasValue && maximum > 0 ? (float)value.Value / maximum : 0);
        slider.interactable = enabled && value.HasValue;
    }
    internal void NormalizePlacementBounds()
    {
        if (placement == null || !(placement.parent is RectTransform board)) return;
        if (!PanelLayoutRules.TryBounds(board.rect.width, board.rect.height, placement.anchorMin.x, placement.anchorMax.y, out var bounds)) return;
        placement.anchorMin = new Vector2(bounds.Left, bounds.Bottom); placement.anchorMax = new Vector2(bounds.Right, bounds.Top);
        placement.offsetMin = placement.offsetMax = Vector2.zero;
        if (design != null) design.localScale = new Vector3(board.rect.width * PanelLayoutRules.ColumnWidth / HubLayout.Data.width,
            board.rect.height * PanelLayoutRules.RowHeight / HubLayout.Data.height, 1);
    }
    private void OnRectTransformDimensionsChange() => NormalizePlacementBounds();
    private new void OnDestroy() { base.OnDestroy(); if (faceplateSprite != null) Destroy(faceplateSprite); }
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
        label.textWrappingMode = TextWrappingModes.Normal; label.overflowMode = TextOverflowModes.Ellipsis;
        label.alignment = TextAlignmentOptions.MidlineLeft; label.raycastTarget = false;
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
        var label = Label(face, text, 24); PanelWidgets.Fill((RectTransform)label.transform, 6, 2, 6, 2); label.alignment = TextAlignmentOptions.Center;
        button.onClick.AddListener(() => Invoke(action)); buttons[id] = button; return button;
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
