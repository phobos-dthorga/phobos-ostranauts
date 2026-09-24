using System.Linq;
using PhobosAutoNav.Core;
using Phobos.Ostranauts.Framework.Controls;
using Ostranauts.ShipGUIs.NavStation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NavPanelDraggable = Ostranauts.ShipGUIs.NavStation.Draggable;

namespace PhobosAutoNav;

/// <summary>Presentation only; flight/settings actions delegate to NavigationService.</summary>
public sealed class AutoNavPanel : NavModBase
{
    private const string FaceplatePath = "phobos/autonav/PhobosAutoNavInstruments.png";
    private const float ReferenceWidth = 600, ReferenceHeight = 250;
    private static readonly Color Ink = new Color32(198, 204, 204, 255);
    private static readonly Color Green = new Color32(144, 211, 169, 255);
    private static readonly Color Amber = new Color32(234, 196, 102, 255);
    private bool damaged, detailsOpen;
    private RectTransform placement = null!, design = null!, overview = null!, detailsRoot = null!;
    private CanvasGroup controls = null!;
    private TMP_Text heading = null!, target = null!, range = null!, speed = null!, notice = null!, details = null!;
    private TMP_Text arrival = null!, propulsion = null!, flyLabel = null!, detailLabel = null!;
    private Button fly = null!, stop = null!;
    private RotarySelector arrivalDial = null!, propulsionDial = null!;
    private ScrollRect detailScroll = null!;
    private Sprite? faceplateSprite;

    internal static void Ensure(GUIOrbitDraw nav)
    {
        Build(nav, NavigationService.ModuleId, false);
        Build(nav, NavigationService.DamagedId, true);
    }
    private static void Build(GUIOrbitDraw nav, string name, bool damaged)
    {
        if (nav.transform.Find(name) != null) return;
        var root = new GameObject(name, typeof(RectTransform)); root.SetActive(false);
        var rect = (RectTransform)root.transform; rect.SetParent(nav.transform, false);
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        var container = Region(rect, "Container", PanelLayoutRules.DefaultLeft,
            1 - PanelLayoutRules.DefaultTop, PanelLayoutRules.ColumnWidth, PanelLayoutRules.RowHeight);
        container.gameObject.AddComponent<CanvasGroup>();
        container.gameObject.AddComponent<Image>().color = new Color(.1f, .3f, .4f, .4f);
        var background = Region(container, "bg", 0, 0, 1, 1);
        background.gameObject.AddComponent<Image>().color = Color.clear;
        var panel = root.AddComponent<AutoNavPanel>();
        panel.damaged = damaged; panel.placement = container;
        panel.design = Region(background, "Faceplate", 0, 0, 1, 1);
        panel.design.anchorMin = panel.design.anchorMax = new Vector2(.5f, .5f);
        panel.design.sizeDelta = new Vector2(ReferenceWidth, ReferenceHeight);
        var image = panel.design.gameObject.AddComponent<Image>();
        var texture = DataHandler.LoadPNG(FaceplatePath, bNorm: false);
        if (texture != null)
        {
            panel.faceplateSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100);
            image.sprite = panel.faceplateSprite;
        }
        image.color = Color.white;
        var layer = Region(panel.design, "Controls", 0, 0, 1, 1);
        panel.controls = layer.gameObject.AddComponent<CanvasGroup>();
        var font = nav.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.font != null)?.font;
        Label(layer, Text.Get("Instruments.title"), .10f, .025f, .8f, .095f, font, 18, true);
        panel.overview = Region(layer, "Overview", 0, 0, 1, 1);
        panel.heading = Label(panel.overview, "", .049f, .174f, .507f, .11f, font, 21);
        panel.target = Label(panel.overview, "", .049f, .292f, .507f, .08f, font, 14);
        panel.range = Label(panel.overview, "", .049f, .398f, .507f, .085f, font, 16);
        panel.speed = Label(panel.overview, "", .049f, .486f, .507f, .085f, font, 16);
        panel.notice = Label(panel.overview, "", .049f, .588f, .507f, .084f, font, 12);
        panel.notice.color = Amber;
        var scrollHost = Region(layer, "Details", .048f, .174f, .514f, .50f);
        var content = PanelWidgets.Scroll(scrollHost, "FlightDetails", out panel.detailScroll);
        PanelWidgets.Fill((RectTransform)panel.detailScroll.transform);
        panel.detailsRoot = scrollHost;
        panel.details = PanelWidgets.Label(content, "", flowing: false);
        if (font != null) panel.details.font = font;
        panel.details.fontSize = 14; panel.details.color = Ink; panel.details.richText = false;

        Label(layer, Text.Get("Instruments.propulsion"), .608f, .181f, .156f, .075f, font, 12, true);
        Label(layer, Text.Get("Instruments.arrival"), .801f, .181f, .156f, .075f, font, 12, true);
        panel.propulsionDial = Dial(layer, .684f, font, direction => panel.ChangePropulsion(direction));
        panel.arrivalDial = Dial(layer, .878f, font, direction => panel.ChangeArrival(direction));
        panel.propulsion = Label(layer, "", .607f, .636f, .16f, .065f, font, 14, true);
        panel.arrival = Label(layer, "", .800f, .636f, .16f, .065f, font, 14, true);
        panel.fly = ActionButton(layer, .044f, .282f, font, () => panel.Fly(), out panel.flyLabel);
        panel.stop = ActionButton(layer, .360f, .280f, font, () => panel.Stop(), out var stopLabel);
        stopLabel.text = Text.Get("Instruments.disengage");
        ActionButton(layer, .671f, .280f, font, () => panel.ToggleDetails(), out panel.detailLabel);
        // Bind the navigation Draggable, never the same-named hauling component.
        panel.DraggableRef = container.gameObject.AddComponent<NavPanelDraggable>();
        panel.DraggableRef.enabled = false;
        panel.NormalizePlacementBounds();
    }
    private bool CanInteract => !damaged && COSelf != null && (DraggableRef == null || !DraggableRef.enabled);
    private void Fly()
    {
        if (!CanInteract) return;
        Plugin.Service.FlyOrResume(COSelf);
        if (!AutoNavCore.Engaged) { detailsOpen = true; detailScroll.verticalNormalizedPosition = 1; }
        UpdateUI();
    }
    private void Stop()
    {
        if (!CanInteract) return;
        Plugin.Service.Stop(COSelf, Text.Get("NavigationService.stopped_by_pilot_coasting")); UpdateUI();
    }
    private void ChangeArrival(int direction)
    {
        if (!CanInteract) return;
        Plugin.Service.StepPanelArrival(COSelf, direction); UpdateUI();
    }
    private void ChangePropulsion(int direction)
    {
        if (!CanInteract) return;
        Plugin.Service.SetPanelTorch(COSelf, direction > 0); UpdateUI();
    }
    private void ToggleDetails()
    {
        detailsOpen = !detailsOpen;
        if (detailsOpen) detailScroll.verticalNormalizedPosition = 1;
        UpdateUI();
    }
    protected override void Init() => UpdateUI();
    protected override void OnNavModMessage(NavModMessageType messageType, object arg)
    {
        base.OnNavModMessage(messageType, arg);
        if (messageType == NavModMessageType.EditNavStation) UpdateUI();
    }
    internal void NormalizePlacementBounds()
    {
        if (placement == null || !(placement.parent is RectTransform board)) return;
        if (!PanelLayoutRules.TryBounds(board.rect.width, board.rect.height, placement.anchorMin.x, placement.anchorMax.y, out var bounds)) return;
        placement.anchorMin = new Vector2(bounds.Left, bounds.Bottom); placement.anchorMax = new Vector2(bounds.Right, bounds.Top);
        placement.offsetMin = placement.offsetMax = Vector2.zero;
        if (design != null) design.localScale = new Vector3(board.rect.width * PanelLayoutRules.ColumnWidth / ReferenceWidth,
            board.rect.height * PanelLayoutRules.RowHeight / ReferenceHeight, 1);
    }
    private void OnRectTransformDimensionsChange() => NormalizePlacementBounds();
    private new void OnDestroy() { base.OnDestroy(); if (faceplateSprite != null) Destroy(faceplateSprite); }
    protected override void UpdateUI()
    {
        if (heading == null) return;
        NormalizePlacementBounds();
        var view = Plugin.Service.ReadInstruments(COSelf);
        bool editing = DraggableRef != null && DraggableRef.enabled;
        controls.interactable = !editing; controls.blocksRaycasts = !editing;
        overview.gameObject.SetActive(!detailsOpen); detailsRoot.gameObject.SetActive(detailsOpen);
        heading.text = damaged ? Text.Get("Instruments.damaged") : view.Heading;
        heading.color = damaged || view.Warning ? Amber : Green;
        target.text = view.Target; range.text = view.Range; speed.text = view.RelativeSpeed;
        notice.text = damaged ? Text.Get("AutoNavPanel.module_damaged_repair_required") : view.Notice;
        details.text = damaged ? Text.Get("AutoNavPanel.module_damaged_repair_required") : view.Details;
        arrival.text = Text.Get("Instruments.arrival_value", view.ArrivalKM);
        propulsion.text = Text.Get(view.TorchPreferred ? "Instruments.auto" : "Torch.rcs");
        arrivalDial.SetAngle(InstrumentRules.ArrivalAngle(view.ArrivalKM)); propulsionDial.SetAngle(view.TorchPreferred ? -55 : 55);
        arrivalDial.interactable = !damaged && view.CanAdjustArrival; propulsionDial.interactable = !damaged && view.CanAdjustPropulsion;
        arrival.color = arrivalDial.interactable ? Ink : Amber; propulsion.color = propulsionDial.interactable ? Ink : Amber;
        fly.interactable = !damaged && view.CanFly; stop.interactable = !damaged && view.CanStop;
        flyLabel.text = Text.Get(view.Resumable ? "Persistence.resume_button" : "AutoNavPanel.fly");
        detailLabel.text = Text.Get(detailsOpen ? "Instruments.overview" : "Instruments.details_button");
    }
    private static RectTransform Region(Transform parent, string name, float x, float y, float width, float height)
    {
        var rect = PanelWidgets.Rect(parent, name); rect.anchorMin = new Vector2(x, 1 - y - height); rect.anchorMax = new Vector2(x + width, 1 - y);
        rect.offsetMin = rect.offsetMax = Vector2.zero; return rect;
    }
    private static TMP_Text Label(Transform parent, string text, float x, float y, float width, float height, TMP_FontAsset? font, float size, bool centered = false)
    {
        var label = Region(parent, "Label", x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) label.font = font;
        label.text = text; label.fontSize = size; label.color = Ink;
        label.enableAutoSizing = true; label.fontSizeMin = size * .80f; label.fontSizeMax = size;
        label.overflowMode = TextOverflowModes.Ellipsis; label.textWrappingMode = TextWrappingModes.NoWrap;
        label.alignment = centered ? TextAlignmentOptions.Center : TextAlignmentOptions.MidlineLeft;
        label.richText = false; label.raycastTarget = false; return label;
    }
    private static Button ActionButton(Transform parent, float x, float width, TMP_FontAsset? font, UnityEngine.Events.UnityAction action, out TMP_Text label)
    {
        var rect = Region(parent, "Action", x, .765f, width, .132f);
        var image = rect.gameObject.AddComponent<Image>(); image.color = Color.clear;
        var button = rect.gameObject.AddComponent<Button>();
        label = Label(rect, "", .025f, .04f, .95f, .92f, font, 16, true); button.targetGraphic = label;
        var colors = button.colors; colors.disabledColor = new Color(.42f, .45f, .47f); button.colors = colors;
        button.onClick.AddListener(() => { CrewSim.bJustClickedInput = true; action(); }); return button;
    }
    private static RotarySelector Dial(Transform parent, float center, TMP_FontAsset? font, System.Action<int> change)
    {
        var rect = Region(parent, "Rotary", center - .069f, .279f, .138f, .33f);
        var hit = rect.gameObject.AddComponent<Image>(); hit.color = Color.clear;
        var control = rect.gameObject.AddComponent<RotarySelector>(); control.targetGraphic = hit; control.Changed = change;
        var rotor = Region(rect, "Rotor", 0, 0, 1, 1);
        var mark = Region(rotor, "Indicator", .479f, .1f, .042f, .29f).gameObject.AddComponent<Image>();
        mark.color = Ink; mark.raycastTarget = false; control.Pointer = rotor;
        Label(rect, "-", .02f, .78f, .2f, .17f, font, 12, true); Label(rect, "+", .78f, .78f, .2f, .17f, font, 12, true);
        return control;
    }
}
