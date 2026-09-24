using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Phobos.Ostranauts.Framework.Controls;

/// <summary>Small runtime UI primitives shared by equipment panels. No gameplay mutations.</summary>
public static class PanelWidgets
{
    public const float FontSize = 18, ButtonHeight = 54, Gap = 8;
    public static readonly Color Ink = new Color(.79f, .83f, .81f);
    public static readonly Color ButtonColor = new Color(.23f, .28f, .33f);
    public static RectTransform Rect(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false); return (RectTransform)go.transform;
    }
    public static void Fill(RectTransform rect, float left = 0, float bottom = 0, float right = 0, float top = 0)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom); rect.offsetMax = new Vector2(-right, -top);
    }
    public static TMP_Text Label(Transform parent, string text, bool flowing = true)
    {
        var rect = Rect(parent, "Label"); var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset; label.fontSize = FontSize; label.color = Ink;
        label.richText = false; label.textWrappingMode = TextWrappingModes.Normal; label.raycastTarget = false;
        label.text = text;
        if (flowing) { var le = rect.gameObject.AddComponent<LayoutElement>(); le.minHeight = FontSize * 1.5f; }
        return label;
    }
    public static Button Button(Transform parent, string title, Action click)
    {
        var rect = Rect(parent, "Button"); var image = rect.gameObject.AddComponent<Image>(); image.color = ButtonColor;
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var size = rect.gameObject.AddComponent<LayoutElement>(); size.minHeight = ButtonHeight;
        var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 8, 8); layout.childControlHeight = true; layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        var label = Label(rect, title); label.alignment = TextAlignmentOptions.MidlineLeft;
        button.onClick.AddListener(() => { CrewSim.bJustClickedInput = true; click(); });
        return button;
    }
    public static RectTransform Scroll(Transform parent, string name, out ScrollRect scroll)
    {
        var root = Rect(parent, name); scroll = root.gameObject.AddComponent<ScrollRect>();
        var viewport = Rect(root, "Viewport"); Fill(viewport, right: 16);
        viewport.gameObject.AddComponent<RectMask2D>();
        var hit = viewport.gameObject.AddComponent<Image>(); hit.color = new Color(0, 0, 0, .05f);
        var content = Rect(viewport, "Content");
        content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
        content.pivot = new Vector2(.5f, 1); content.sizeDelta = Vector2.zero;
        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(4, 4, 4, 8); layout.spacing = Gap;
        layout.childControlHeight = layout.childControlWidth = true; layout.childForceExpandHeight = false;
        var fitter = content.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = content; scroll.viewport = viewport; scroll.horizontal = false; scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 28;
        var rail = Rect(root, "Scrollbar"); rail.anchorMin = new Vector2(1, 0); rail.anchorMax = Vector2.one;
        rail.offsetMin = new Vector2(-12, 0); rail.offsetMax = Vector2.zero;
        rail.gameObject.AddComponent<Image>().color = new Color(.12f, .15f, .17f);
        var thumb = Rect(rail, "Handle"); Fill(thumb); var ti = thumb.gameObject.AddComponent<Image>(); ti.color = ButtonColor;
        var bar = rail.gameObject.AddComponent<Scrollbar>(); bar.handleRect = thumb; bar.targetGraphic = ti;
        bar.direction = Scrollbar.Direction.BottomToTop; scroll.verticalScrollbar = bar;
        return content;
    }
    public static TMP_InputField Search(Transform parent, string placeholder, Action<string> changed)
    {
        var rect = Rect(parent, "Search"); rect.gameObject.AddComponent<Image>().color = ButtonColor;
        rect.gameObject.AddComponent<LayoutElement>().minHeight = ButtonHeight;
        var field = rect.gameObject.AddComponent<TMP_InputField>();
        var viewport = Rect(rect, "TextArea"); Fill(viewport, 10, 4, 10, 4); viewport.gameObject.AddComponent<RectMask2D>();
        var text = Label(viewport, "", false); Fill((RectTransform)text.transform);
        var hint = Label(viewport, placeholder, false); Fill((RectTransform)hint.transform); hint.color = new Color(.55f, .61f, .62f);
        field.textViewport = viewport; field.textComponent = text; field.placeholder = hint;
        field.onValueChanged.AddListener(s => changed(s));
        field.onSelect.AddListener(_ => CrewSim.StartTyping()); field.onDeselect.AddListener(_ => CrewSim.EndTyping());
        return field;
    }
    public static void Clear(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--) { var child = parent.GetChild(i); child.gameObject.SetActive(false); UnityEngine.Object.Destroy(child.gameObject); }
    }
}
