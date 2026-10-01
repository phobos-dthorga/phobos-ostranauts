using System;
using UnityEngine;

namespace Phobos.Ostranauts.Framework.Effects;

/// <summary>A beam drawn from an installed item to a point on the deck: one of the game's own light-sprite quads
/// (the kind its indicator lamps use), parented to the item so it turns and moves with the ship, stretched from the
/// item's emitter point to the target each frame. Presentation only: it lights nothing, blocks nothing, cannot be
/// clicked and is never saved; it goes with the item when that is removed or the world is unloaded. Any rendering
/// fault switches beams off for the session and never reaches the work that asked for one.</summary>
public sealed class WorldBeam : MonoBehaviour
{
    private const string Prefab = "prefabQuadLightSprite";
    private static bool failed;
    private Transform? quad;
    private Renderer? rend;
    private MaterialPropertyBlock? block;
    private string texture = "";
    private Vector2 emitter;
    private Color colour = Color.white;
    private float thickness = 0.25f;
    private Vector3 target;
    private bool visible;

    /// <summary>Whether a rendering fault has switched beams off for this session, and what it was.</summary>
    public static bool Failed => failed;
    public static string Fault { get; private set; } = "";

    /// <summary>The item's beam, created on first use when <paramref name="create"/> is set.</summary>
    public static WorldBeam? For(CondOwner? owner, bool create)
    {
        if (failed || owner == null || owner.bDestroyed || owner.gameObject == null) return null;
        var beam = owner.gameObject.GetComponent<WorldBeam>();
        return beam != null || !create ? beam : owner.gameObject.AddComponent<WorldBeam>();
    }

    /// <param name="texturePath">A mod image path without its extension, as item images are named.</param>
    /// <param name="emitterPixels">The beam's origin from the item centre, sixteen pixels to a tile, +Y up.</param>
    public void Configure(string texturePath, Vector2 emitterPixels, Color tint, float thicknessTiles)
    {
        if (texturePath != texture && quad != null) Clear();
        texture = texturePath ?? ""; emitter = emitterPixels; colour = tint; thickness = Mathf.Max(0.01f, thicknessTiles);
    }

    public void Aim(Vector3 worldTarget) { target = worldTarget; visible = true; }

    public void Hide()
    {
        visible = false;
        if (quad != null) quad.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (!visible || failed || texture.Length == 0) return;
        try
        {
            if (quad == null && !Build()) return;
            var scale = transform.localScale;
            var local = transform.InverseTransformPoint(target);
            if (!BeamTransform.Solve(emitter.x, emitter.y, scale.x, scale.y, local.x, local.y, thickness, out var pose))
            { quad!.gameObject.SetActive(false); return; }
            quad!.localPosition = new Vector3((float)pose.X, (float)pose.Y, quad.localPosition.z);
            quad.localRotation = Quaternion.Euler(0f, 0f, (float)pose.AngleDegrees);
            quad.localScale = new Vector3((float)pose.ScaleX, (float)pose.ScaleY, 1f);
            rend!.GetPropertyBlock(block);
            block!.SetColor("_LightColor", colour);
            rend.SetPropertyBlock(block);
            if (!quad.gameObject.activeSelf) quad.gameObject.SetActive(true);
        }
        catch (Exception e) { Fail(e); }
    }

    // The same steps the game takes for an item's light sprite: its quad prefab under the item, keeping the prefab's
    // own depth, with a material from the game's image cache. That cached material is shared and never changed here;
    // the tint goes through this renderer's property block.
    private bool Build()
    {
        var mesh = DataHandler.GetMesh(Prefab);
        if (mesh == null) throw new InvalidOperationException("The game's light sprite quad is unavailable.");
        mesh.name = "PhobosWorldBeam";
        foreach (string collider in new[] { "Collider", "BoxCollider", "MeshCollider" })
        {
            var component = mesh.GetComponent(collider);
            if (component != null) Destroy(component);
        }
        quad = mesh.transform;
        quad.SetParent(transform);
        rend = quad.GetComponent<Renderer>();
        if (rend == null) throw new InvalidOperationException("The game's light sprite quad has no renderer.");
        rend.sharedMaterial = DataHandler.GetMaterial(rend, texture);
        block = new MaterialPropertyBlock();
        return true;
    }

    private void Clear()
    {
        if (quad != null) Destroy(quad.gameObject);
        quad = null; rend = null; block = null;
    }

    private void Fail(Exception e)
    {
        failed = true; Fault = e.Message; visible = false;
        try { Clear(); } catch { }
        FrameworkLifecycle.Log("World beam display switched off for this session: " + e.Message);
    }

    private void OnDestroy() { try { Clear(); } catch { } }
}
