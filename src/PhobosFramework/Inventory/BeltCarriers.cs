using System;
using System.Collections.Generic;
using UnityEngine;

namespace Phobos.Ostranauts.Framework.Inventory;

/// <summary>The item you see riding a conveyor belt (Framework 0.62.0; owner request, 1 October 2026). Presentation only:
/// the real item stays in the sender until its checked transfer completes, exactly as before, and this is a small
/// copy of its own art, drawn on the belt under the pipe lanes and moved along the belt path as the transfer's clock
/// runs. One quad per active belt transfer, eased towards its place each frame; nothing is saved, clicked or counted.
/// Any rendering fault switches the display off for the session and never touches the transfer.</summary>
public static class BeltCarriers
{
    /// <summary>Between the belt's draw layer (1.03) and the first pipe lane (1.04), in the game's depth units.</summary>
    public const float LayerScale = 1.035f;
    /// <summary>The carried copy is half a tile across.</summary>
    public const float Size = 0.5f;
    public static bool Enabled { get; set; } = true;
    private sealed class Carrier { internal GameObject Object = null!; internal Material Material = null!; internal Vector3 Target; internal string ItemId = ""; }
    private static readonly Dictionary<string, Carrier> active = new(StringComparer.Ordinal);
    private static bool failed;

    /// <summary>Places the copy of <paramref name="item"/> for transfer <paramref name="key"/> at <paramref name="progress"/>
    /// (0 to 1) along the belt cells <paramref name="path"/> on <paramref name="ship"/>.</summary>
    public static void Show(string key, Ship ship, CondOwner item, IReadOnlyList<int>? path, double progress)
    {
        if (!Enabled || failed || path == null || path.Count == 0 || item?.Item?.rend?.sharedMaterial == null || ship == null) { Hide(key); return; }
        try
        {
            var points = new List<Vector3>(path.Count);
            foreach (int cell in path) { var tile = ship.GetTileByIndex(cell); if (tile != null) points.Add(tile.tf.position); }
            if (points.Count == 0) { Hide(key); return; }
            double t = Math.Max(0, Math.Min(1, double.IsNaN(progress) ? 0 : progress)) * (points.Count - 1);
            int i = Math.Min(points.Count - 1, (int)Math.Floor(t));
            var at = i + 1 < points.Count ? Vector3.Lerp(points[i], points[i + 1], (float)(t - i)) : points[i];
            at.z = -LayerScale * 4f;
            if (active.TryGetValue(key, out var shown) && (shown.Object == null || shown.ItemId != item.strID)) Hide(key);
            if (!active.TryGetValue(key, out var carrier))
            {
                // A bare quad with no collider, so it can never be clicked or block anything.
                var quad = new GameObject("PhobosBeltCarrier");
                quad.AddComponent<MeshFilter>().sharedMesh = QuadMesh();
                var material = new Material(item.Item.rend.sharedMaterial);
                quad.AddComponent<MeshRenderer>().sharedMaterial = material;
                quad.transform.localScale = new Vector3(Size, Size, 1f);
                quad.transform.position = at;
                active[key] = carrier = new Carrier { Object = quad, Material = material, ItemId = item.strID };
            }
            carrier.Target = at;
        }
        catch (Exception e) { failed = true; FrameworkLifecycle.Log(Text.Get("BeltCarriers.failed", e.Message)); ClearAll(); }
    }
    private static Mesh? quadMesh;
    private static Mesh QuadMesh()
    {
        if (quadMesh != null) return quadMesh;
        quadMesh = new Mesh { name = "PhobosBeltCarrierQuad" };
        quadMesh.vertices = new[] { new Vector3(-.5f, -.5f, 0), new Vector3(.5f, -.5f, 0), new Vector3(-.5f, .5f, 0), new Vector3(.5f, .5f, 0) };
        quadMesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
        quadMesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        quadMesh.RecalculateNormals();
        return quadMesh;
    }
    public static void Hide(string key)
    {
        if (!active.TryGetValue(key, out var carrier)) return;
        active.Remove(key);
        Destroy(carrier);
    }
    /// <summary>Eases every copy towards its place (called each frame by Framework's plugin).</summary>
    internal static void Poll(float deltaSeconds)
    {
        if (active.Count == 0 || failed) return;
        try
        {
            float blend = 1f - Mathf.Exp(-8f * Mathf.Max(0f, deltaSeconds));
            foreach (var carrier in active.Values)
                if (carrier.Object != null) carrier.Object.transform.position = Vector3.Lerp(carrier.Object.transform.position, carrier.Target, blend);
        }
        catch (Exception e) { failed = true; FrameworkLifecycle.Log(Text.Get("BeltCarriers.failed", e.Message)); ClearAll(); }
    }
    /// <summary>Removes every copy (a world load, or a fault).</summary>
    internal static void ClearAll()
    {
        foreach (var carrier in active.Values) Destroy(carrier);
        active.Clear();
    }
    private static void Destroy(Carrier carrier)
    {
        try { if (carrier.Object != null) UnityEngine.Object.Destroy(carrier.Object); if (carrier.Material != null) UnityEngine.Object.Destroy(carrier.Material); }
        catch { }
    }
}
