using System.Linq;
using Ostranauts.ShipGUIs.NavStation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhobosApproachAssist;

/// <summary>Placeholder controls. Native module layout handles placement and visibility.</summary>
public sealed class ApproachPanel : NavModBase
{
    private TMP_Text status = null!;
    private bool damaged;

    internal static void Ensure(GUIOrbitDraw nav)
    {
        Build(nav, ApproachService.ModuleId, false);
        Build(nav, ApproachService.DamagedId, true);
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
        background.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.13f, 0.16f, 1);
        var font = nav.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.font != null)?.font;
        var panel = root.AddComponent<ApproachPanel>();
        panel.damaged = damaged;
        Label(container, "Approach Assist P0", new Vector2(0.04f, 0.79f), new Vector2(0.96f, 0.97f), font, 19);
        panel.status = Label(container, "Test pulse only — no automatic braking", new Vector2(0.04f, 0.33f), new Vector2(0.96f, 0.78f), font, 15);
        Button(container, "Run 2-second test", 0.04f, 0.57f, font, () => Plugin.Service.Engage(panel.COSelf));
        Button(container, "Disengage", 0.6f, 0.96f, font, () => Plugin.Service.Disengage("Disengaged by pilot"));
        container.gameObject.AddComponent<Draggable>().enabled = false;
    }

    protected override void UpdateUI()
    {
        if (status == null) return;
        status.text = damaged ? "Module damaged — repair required" :
            "Test pulse only — no automatic braking\n" + (COSelf == null ? "Waiting for console" : Plugin.Service.ReadPanel(COSelf));
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
        label.text = text; label.fontSize = size; label.color = new Color(0.75f, 0.9f, 0.9f);
        label.enableAutoSizing = true; label.fontSizeMin = 10; label.fontSizeMax = size;
        label.raycastTarget = false;
        return label;
    }

    private static void Button(Transform parent, string text, float min, float max, TMP_FontAsset? font, UnityEngine.Events.UnityAction click)
    {
        var rect = Rect(text, parent, new Vector2(min, 0.05f), new Vector2(max, 0.29f));
        var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(0.19f, 0.3f, 0.36f);
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        button.onClick.AddListener(click);
        Label(rect, text, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f), font, 15).alignment = TextAlignmentOptions.Center;
    }
}
