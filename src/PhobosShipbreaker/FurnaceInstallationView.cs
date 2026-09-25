using Phobos.Ostranauts.Framework.Controls;
using PhobosShipbreaker.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using W = Phobos.Ostranauts.Framework.Controls.PanelWidgets;

namespace PhobosShipbreaker;

/// <summary>Live local-axis installation key, read-only. Rotates with the equipment's world heading.</summary>
public sealed class FurnaceInstallationView : MonoBehaviour
{
    private string target = "";
    private RectTransform assembly = null!;
    private Image left = null!, right = null!, rear = null!;
    private TMP_Text caption = null!;
    private float next;
    internal static void Build(Transform parent, CondOwner furnace)
    {
        W.Label(parent, Text.Get("Furnace.installation_heading"));
        var root = W.Rect(parent, "Installation");
        var layout = root.gameObject.AddComponent<LayoutElement>(); layout.minHeight = layout.preferredHeight = 240;
        var view = root.gameObject.AddComponent<FurnaceInstallationView>(); view.target = furnace.strID;
        view.assembly = W.Rect(root, "RotatingAssembly"); view.assembly.anchorMin = view.assembly.anchorMax = new Vector2(.5f,.5f);
        view.assembly.sizeDelta = Vector2.zero;
        view.Block("F6", 0, 0, 96, 96, new Color(.34f,.39f,.40f));
        view.left = view.Block("P", -56, 8, 16, 16, Color.grey);
        view.right = view.Block("P", 56, 8, 16, 16, Color.grey);
        view.rear = view.Block("R", 0, 96, 96, 64, Color.grey);
        view.Block("", 0, 56, 96, 6, new Color(.7f,.65f,.48f));
        view.Block("▼", 0, -60, 16, 16, new Color(.25f,.5f,.7f));
        // Centre the asymmetrical rear assembly inside a rotation-safe square.
        view.assembly.localScale = Vector3.one * .82f;
        view.caption = W.Label(parent, ""); view.Refresh();
    }
    private Image Block(string text, float x, float y, float width, float height, Color color)
    {
        var rect = W.Rect(assembly, "Mount"); rect.anchorMin = rect.anchorMax = new Vector2(.5f,.5f);
        rect.anchoredPosition = new Vector2(x,y); rect.sizeDelta = new Vector2(width,height);
        var image = rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false;
        if (text.Length != 0)
        {
            var label = W.Label(rect,text,false); W.Fill((RectTransform)label.transform);
            label.fontSize = 12; label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
        }
        return image;
    }
    private void Update() { if (Time.unscaledTime >= next) { next = Time.unscaledTime + .25f; Refresh(); } }
    private void Refresh()
    {
        var furnace = CollectorService.Resolve(target); if (furnace == null) return;
        assembly.localRotation = Quaternion.Euler(0,0,furnace.tf.eulerAngles.z);
        var endpoint = FurnaceService.CoolingEndpoint(furnace);
        var socket = endpoint == null ? FurnaceCooling.Socket.None : FurnaceService.SocketAt(furnace, endpoint);
        Color active = new(.2f,.7f,.5f), unused = new(.32f,.35f,.38f);
        left.color = socket == FurnaceCooling.Socket.Left ? active : unused;
        right.color = socket == FurnaceCooling.Socket.Right ? active : unused;
        rear.color = socket == FurnaceCooling.Socket.Rear ? active : unused;
        caption.text = Text.Get("Furnace.installation_legend", Text.Get("Furnace.socket_" + socket));
    }
}
