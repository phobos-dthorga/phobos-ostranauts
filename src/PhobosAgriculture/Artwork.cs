using System;
using System.Collections.Generic;
using PhobosAgriculture.Core;
using UnityEngine;

namespace PhobosAgriculture;

// Uses the existing native Item and material: no independent renderer, lighting,
// collider, timer or saved appearance state. Native visibility and wear still apply.
internal static class Artwork
{
    private const string Folder = "phobos/agriculture/";
    private static readonly HashSet<string> reported = new(StringComparer.Ordinal);

    internal static string Key(CondOwner co, CropState state, bool protectedState)
    {
        string basis = WorkupDefinitions.IsBench(co) ? "Workup" : IrrigationDefinitions.IsSupply(co) ? "WaterSupply" : Definitions.IsCooker(co) ? "Cooker" : "Rack";
        bool loose = co.strCODef.EndsWith("Loose", StringComparison.Ordinal) || co.strCODef.EndsWith("LooseDmg", StringComparison.Ordinal);
        bool damaged = co.strCODef.EndsWith("Dmg", StringComparison.Ordinal);
        if (loose) return basis + (damaged ? "LooseDamaged" : "Loose");
        if (basis == "Rack") return CropAppearance.RackKey(state, protectedState) + (damaged ? "Damaged" : "");
        return basis + (damaged ? "Damaged" : "");
    }

    internal static Texture2D? Texture(string key)
    {
        var texture = DataHandler.LoadPNG(Folder + key + ".png", bNorm: false);
        if (texture != null) texture.filterMode = FilterMode.Point;
        return texture;
    }

    internal static void Refresh(CondOwner co, CropState state, bool protectedState)
    {
        var item = co.Item;
        if (item == null || item.rend == null) return;
        string key = Key(co, state, protectedState), path = Folder + key;
        if (item.ImgOverride == path) return;
        try
        {
            int size = WorkupDefinitions.IsBench(co) || Definitions.IsCooker(co) || IrrigationDefinitions.IsSupply(co) ? 32 : 64;
            var color = Texture(key);
            var normal = DataHandler.LoadPNG(path + "Normal.png", bNorm: true);
            if (color == null || normal == null || color.width != size || color.height != size || normal.width != size || normal.height != size)
                throw new InvalidOperationException("Missing or incorrectly sized Agriculture appearance: " + key);
            normal.filterMode = FilterMode.Point;
            string damage = Folder + (key.EndsWith("Damaged", StringComparison.Ordinal) ? key : key + "Damaged");
            item.SetAlt(path, path + "Normal", damage, item.jid.strDmgColor);
        }
        catch (Exception error)
        {
            // Art failure must never stop biology, change stored matter or fault a machine.
            if (reported.Add(key)) Plugin.Log("Agriculture appearance unavailable: " + error.Message);
        }
    }
}
