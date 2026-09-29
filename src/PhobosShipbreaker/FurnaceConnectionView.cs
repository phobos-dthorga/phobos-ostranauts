using System;
using System.Collections.Generic;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

/// <summary>Native item materials keep visibility, lighting and damage tint. No independent world renderer or saved visual state.</summary>
internal static class FurnaceConnectionView
{
    private static bool diagnosed;
    private sealed class Artwork { internal string Path = "", Normal = "", Damaged = ""; }
    // Twelve possible appearances; their paths are built once, not on every quarter-second pass per port.
    private static readonly Dictionary<(FurnaceCooling.Socket Socket, bool Loose, bool Damaged), Artwork> artwork = new();
    private static Artwork ArtworkFor((FurnaceCooling.Socket Socket, bool Loose, bool Damaged) key)
    {
        if (artwork.TryGetValue(key, out var found)) return found;
        string art = FurnaceRules.ThermalPort;
        if (key.Loose) art += key.Damaged ? "LooseDamaged" : "Loose";
        else
        {
            if (key.Socket == FurnaceCooling.Socket.Left) art += "ConnectRight";
            if (key.Socket == FurnaceCooling.Socket.Right) art += "ConnectLeft";
            if (key.Damaged) art += "Damaged";
        }
        string path = "phobos/shipbreaker/" + art;
        found = new Artwork { Path = path, Normal = key.Loose || key.Damaged ? path + "Normal" : "phobos/shipbreaker/" + FurnaceRules.ThermalPort + "Normal", Damaged = key.Damaged ? path : path + "Damaged" };
        artwork[key] = found;
        return found;
    }
    internal static void Refresh(CondOwner co)
    {
        if (!FurnaceRules.Underside(co.strCODef)) return;
        try
        {
            bool loose = co.strCODef.EndsWith("Loose", StringComparison.Ordinal) || co.strCODef.EndsWith("LooseDmg", StringComparison.Ordinal);
            bool damaged = co.strCODef.EndsWith("Dmg", StringComparison.Ordinal);
            var art = ArtworkFor((FurnaceService.ConnectedSocket(co), loose, damaged));
            var item = co.GetComponent<Item>();
            if (item != null && item.ImgOverride != art.Path) item.SetAlt(art.Path, art.Normal, art.Damaged, item.jid.strDmgColor);
        }
        catch (Exception ex)
        {
            // A visual fallback must never pause or modify physical cooling.
            if (!diagnosed) { diagnosed = true; Plugin.Log("Furnace connection artwork fallback: " + ex.Message); }
        }
    }
}
