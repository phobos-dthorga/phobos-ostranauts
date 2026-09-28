using System;
using UnityEngine;

namespace Phobos.Ostranauts.Framework.Construction;

/// <summary>Two unfinished native-size images; the normal map is each path plus "Normal".</summary>
public sealed class SectionAssemblyAppearance
{
    public string Early { get; }
    public string Intermediate { get; }
    public int Size { get; }
    private readonly Texture2D?[] colours = new Texture2D?[2], normals = new Texture2D?[2];
    private readonly bool[] attempted = new bool[2];

    public SectionAssemblyAppearance(string early, string intermediate, int size)
    {
        if (string.IsNullOrWhiteSpace(early) || string.IsNullOrWhiteSpace(intermediate) || size <= 0)
            throw new ArgumentException("Construction artwork needs two paths and a positive native size.");
        Early = early;
        Intermediate = intermediate;
        Size = size;
    }

    internal bool Textures(bool intermediate, out Texture2D? colour, out Texture2D? normal)
    {
        int index = intermediate ? 1 : 0;
        if (!attempted[index])
        {
            attempted[index] = true;
            string path = intermediate ? Intermediate : Early;
            try
            {
                var c = DataHandler.LoadPNG(path + ".png", bNorm: false);
                var n = DataHandler.LoadPNG(path + "Normal.png", bNorm: true);
                if (c == null || n == null || c.width != Size || c.height != Size || n.width != Size || n.height != Size)
                    throw new InvalidOperationException("Missing or incorrectly sized construction image: " + path);
                c.filterMode = n.filterMode = FilterMode.Point;
                colours[index] = c;
                normals[index] = n;
            }
            catch (Exception error) { FrameworkLifecycle.Log("Construction appearance unavailable: " + error.Message); }
        }
        colour = colours[index];
        normal = normals[index];
        return colour != null && normal != null;
    }
}

// One component only on opted-in construction sites. Native Item remains responsible
// for geometry, rotation, visibility, overlays and its material property block.
internal sealed class SectionAssemblyView : MonoBehaviour
{
    internal const float RefreshSeconds = .1f;
    private Placeholder? marker;
    private Renderer? renderer;
    private Material? original, owned;
    private SectionAssemblyAppearance? shown;
    private bool stage, reported;
    private float lastRefresh = float.NegativeInfinity;

    internal static void Attach(Placeholder marker)
    {
        try
        {
            var view = marker.GetComponent<SectionAssemblyView>() ?? marker.gameObject.AddComponent<SectionAssemblyView>();
            view.Release();
            view.marker = marker;
            view.lastRefresh = float.NegativeInfinity;
            view.Refresh(Time.unscaledTime);
        }
        catch (Exception error) { FrameworkLifecycle.Log("Construction appearance attachment failed: " + error.Message); }
    }

    private void LateUpdate() => Refresh(Time.unscaledTime);
    private void OnEnable() => lastRefresh = float.NegativeInfinity;
    private void OnDisable() => Release();
    private void OnDestroy() => Release();

    internal void Refresh(float now)
    {
        if (now >= lastRefresh && now - lastRefresh < RefreshSeconds) return;
        lastRefresh = now;
        try
        {
            if (marker == null || !SectionAssembly.TryAppearance(marker, out var appearance, out bool intermediate))
            { Release(); return; }
            var item = marker.GetComponent<CondOwner>()?.Item;
            if (item == null || item.rend == null || item.rend.sharedMaterial == null) { Release(); return; }
            if (renderer != item.rend || owned == null || renderer.sharedMaterial != owned) Release();
            if (owned != null && shown == appearance && stage == intermediate) return;
            if (!appearance!.Textures(intermediate, out var colour, out var normal)) { Release(); return; }
            if (owned == null)
            {
                renderer = item.rend;
                original = renderer.sharedMaterial;
                if (original.mainTexture == null || original.mainTexture.width != appearance.Size || original.mainTexture.height != appearance.Size)
                    throw new InvalidOperationException("Construction image does not match native marker dimensions.");
                // Item.SetAlt uses a global image-keyed material cache. Clone the marker's
                // actual material instead, retaining shader, queue, aspect and native effects.
                owned = new Material(original);
                renderer.sharedMaterial = owned;
            }
            owned.SetTexture("_MainTex", colour);
            owned.SetTexture("_BumpMap", normal);
            owned.SetFloat("_DmgPresent", 0); // No finished-machine lid painted over unfinished internals.
            shown = appearance;
            stage = intermediate;
        }
        catch (Exception error)
        {
            Release();
            if (!reported) { reported = true; FrameworkLifecycle.Log("Construction appearance refresh failed: " + error.Message); }
        }
    }

    internal void Release()
    {
        if (owned != null)
        {
            if (renderer != null && original != null && renderer.sharedMaterial == owned) renderer.sharedMaterial = original;
            Destroy(owned);
        }
        owned = original = null;
        renderer = null;
        shown = null;
    }
}
