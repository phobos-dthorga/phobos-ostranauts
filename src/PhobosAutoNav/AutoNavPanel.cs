using System.Linq;
using Ostranauts.ShipGUIs.NavStation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhobosAutoNav;

/// <summary>Approved Phobos faceplate with live controls; native layout owns placement.</summary>
public sealed class AutoNavPanel : NavModBase
{
    private const string FaceplatePath = "phobos/autonav/PhobosAutoNavPanel.png";
    private static readonly Color LabelColor = new Color32(180, 183, 184, 255);
    private TMP_Text status = null!;
    private bool damaged;

    internal static void Ensure(GUIOrbitDraw nav)
    {
        Build(nav, NavigationService.ModuleId, false);
        Build(nav, NavigationService.DamagedId, true);
    }

    private static void Build(GUIOrbitDraw nav, string name, bool damaged)
    {
        if (nav.transform.Find(name) != null) return;
        var root = new GameObject(name, typeof(RectTransform));
        root.SetActive(false);
        var rect = (RectTransform)root.transform;
        rect.SetParent(nav.transform, false);
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var container = Rect("Container", rect, new Vector2(0.35f, 0.05f), new Vector2(0.65f, 0.3f));
        container.gameObject.AddComponent<CanvasGroup>();
        container.gameObject.AddComponent<Image>().color = new Color(0.1f, 0.3f, 0.4f, 0.4f);
        var background = Rect("bg", container, Vector2.zero, Vector2.one);
        // Draggable expects Container/bg to have an Image for its edit-mode tint.
        background.gameObject.AddComponent<Image>().color = Color.clear;
        var faceplate = Rect("Faceplate", background, Vector2.zero, Vector2.one);
        var fit = faceplate.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = 2f;
        var art = faceplate.gameObject.AddComponent<RawImage>();
        art.texture = DataHandler.LoadPNG(FaceplatePath, bNorm: false);
        art.color = Color.white;
        // Draw the supplied PNG unchanged. All text and interaction remain live UI.
        art.raycastTarget = true;
        var font = nav.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.font != null)?.font;
        var panel = root.AddComponent<AutoNavPanel>();
        panel.damaged = damaged;
        Label(faceplate, Text.Get("AutoNavPanel.phobos_auto_nav"), new Vector2(0.12f, 0.83f), new Vector2(0.88f, 0.97f), font, 18).alignment = TextAlignmentOptions.Center;
        Label(faceplate, Text.Get("AutoNavPanel.rcs_approach"), new Vector2(0.065f, 0.70f), new Vector2(0.935f, 0.77f), font, 12);
        panel.status = Label(faceplate, Text.Get("AutoNavPanel.waiting_for_console"), new Vector2(0.065f, 0.395f), new Vector2(0.935f, 0.685f), font, 16);
        Button(faceplate, Text.Get("AutoNavPanel.fly"), 0.065f, 0.515f, font, () => Plugin.Service.Engage(panel.COSelf));
        Button(faceplate, Text.Get("AutoNavPanel.disengage"), 0.555f, 0.935f, font, () => Plugin.Service.Disengage(Text.Get("AutoNavPanel.disengaged_by_pilot")));
        container.gameObject.AddComponent<Draggable>().enabled = false;
    }

    protected override void Init() => UpdateUI();

    protected override void UpdateUI()
    {
        if (status == null) return;
        status.text = damaged ? Text.Get("AutoNavPanel.module_damaged_repair_required") :
            COSelf == null ? Text.Get("AutoNavPanel.waiting_for_console") : Plugin.Service.ReadPanel(COSelf);
        foreach (var button in GetComponentsInChildren<Button>()) button.interactable = !damaged;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false); rect.anchorMin = min; rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static TMP_Text Label(Transform parent, string text, Vector2 min, Vector2 max, TMP_FontAsset? font, float size)
    {
        var label = Rect("label", parent, min, max).gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) label.font = font;
        label.text = text; label.fontSize = size; label.color = LabelColor;
        label.enableAutoSizing = true; label.fontSizeMin = 8; label.fontSizeMax = size;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.richText = false;
        label.raycastTarget = false;
        return label;
    }

    private static void Button(Transform parent, string text, float min, float max, TMP_FontAsset? font, UnityEngine.Events.UnityAction click)
    {
        var rect = Rect(text, parent, new Vector2(min, 0.115f), new Vector2(max, 0.275f));
        // Transparent hit area over the painted button; tint the label for feedback.
        var image = rect.gameObject.AddComponent<Image>(); image.color = Color.clear;
        var button = rect.gameObject.AddComponent<Button>();
        var label = Label(rect, text, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f), font, 16);
        label.alignment = TextAlignmentOptions.Center;
        button.targetGraphic = label;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 1);
        button.colors = colors;
        button.onClick.AddListener(click);
    }
}
