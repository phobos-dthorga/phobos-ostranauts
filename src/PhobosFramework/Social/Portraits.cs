using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Phobos.Ostranauts.Framework.Social;

/// <summary>Game-made faces for people who never exist in the world (Framework 0.121.0): story correspondents first,
/// any mod's off-screen people later. A face is rolled once by the game's own face roll (<c>FaceAnim2.GetRandomFace</c>)
/// and its parts are kept by the caller, because the game's roll cannot be repeated from a seed; the picture is then
/// composed from the game's own portrait parts exactly as the game composes a crew portrait (<c>FaceAnim2.GetPNG</c>:
/// the parts in its face part order, laid over black) and registered in the game's picture cache under a stable name,
/// which a goal's <c>strPortraitOverride</c> can name. Presentation only: nothing here is saved by the game.</summary>
public static class Portraits
{
    /// <summary>The game's own wrist PDA picture, the portrait one of its own plots gives a goal with no person.</summary>
    public const string PdaImage = "ItmWristPDA01";
    private static readonly Dictionary<string, Texture2D> made = new(StringComparer.Ordinal);
    private static readonly HashSet<string> failed = new(StringComparer.Ordinal);
    public static Action<string> Log { get; set; } = _ => { };

    /// <summary>A new face from the game's own roll for a look (<see cref="PortraitRules.Looks"/>), or null if the game
    /// has no face parts to give.</summary>
    public static string[]? RandomFace(string? look)
    {
        try
        {
            var (male, female) = PortraitRules.Flags(look);
            var parts = FaceAnim2.GetRandomFace(male, female);
            return PortraitRules.ValidParts(parts) ? parts : null;
        }
        catch (Exception ex) { Warn("roll", ex.Message); return null; }
    }

    /// <summary>The picture name of a face's portrait, registered with the game's picture cache, or null when a part
    /// cannot be read (said once in the log).</summary>
    public static string? Register(IReadOnlyList<string> parts)
    {
        if (!PortraitRules.ValidParts(parts)) return null;
        string name = PortraitRules.ImageName(string.Join("|", parts));
        if (failed.Contains(name)) return null;
        // The game may have dropped its cache (a new game); our own copy goes back in.
        if (made.TryGetValue(name, out var have) && have != null) { DataHandler.AddPNG(name + ".png", have); return name; }
        try
        {
            var order = DataHandler.GetLoot("TXTFacePartOrder").GetLootNames().Select(s => int.TryParse(s, out int i) ? i : -1).ToList();
            if (order.Count == 0 || order.Any(i => i < 0 || i >= parts.Count)) throw new InvalidOperationException("the game's face part order does not fit " + parts.Count + " parts");
            var layers = order.Select(i => DataHandler.LoadPNG("portraits/" + parts[i] + ".png", false)).ToList();
            if (layers.Any(l => l == null || l.name == "missing.png")) throw new InvalidOperationException("a portrait part is missing");
            int width = layers[0].width, height = layers[0].height;
            if (layers.Any(l => l.width != width || l.height != height)) throw new InvalidOperationException("portrait parts differ in size");
            var pixels = new Color[width * height];
            for (int n = 0; n < layers.Count; n++)
            {
                var layer = layers[n].GetPixels();
                for (int p = 0; p < pixels.Length; p++)
                    pixels[p] = Color.Lerp(n == 0 ? Color.black : pixels[p], layer[p], layer[p].a);
            }
            var texture = new Texture2D(width, height, TextureFormat.ARGB32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels(pixels);
            texture.Apply();
            DataHandler.AddPNG(name + ".png", texture);
            made[name] = texture;
            return name;
        }
        catch (Exception ex)
        {
            failed.Add(name);
            Warn(name, ex.Message);
            return null;
        }
    }

    private static readonly HashSet<string> warned = new(StringComparer.Ordinal);
    private static void Warn(string what, string message) { if (warned.Add(what)) Log("A game-made face could not be made (" + what + "): " + message); }
}
