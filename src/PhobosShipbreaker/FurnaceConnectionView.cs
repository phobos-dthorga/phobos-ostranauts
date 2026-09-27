using System;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

/// <summary>Native item materials keep visibility, lighting and damage tint. No independent world renderer or saved visual state.</summary>
internal static class FurnaceConnectionView
{
    private static bool diagnosed;
    internal static void Refresh(CondOwner co)
    {
        if (!FurnaceRules.Underside(co.strCODef)) return;
        try
        {
            string art = FurnaceRules.ThermalPort;
            bool loose = co.strCODef.EndsWith("Loose", StringComparison.Ordinal) || co.strCODef.EndsWith("LooseDmg", StringComparison.Ordinal);
            bool damaged = co.strCODef.EndsWith("Dmg", StringComparison.Ordinal);
            var socket = FurnaceService.ConnectedSocket(co);
            if (loose) art += damaged ? "LooseDamaged" : "Loose";
            else
            {
                if (socket == FurnaceCooling.Socket.Left) art += "ConnectRight";
                if (socket == FurnaceCooling.Socket.Right) art += "ConnectLeft";
                if (damaged) art += "Damaged";
            }
            string path = "phobos/shipbreaker/" + art;
            var item = co.GetComponent<Item>();
            if (item != null && item.ImgOverride != path)
                item.SetAlt(path, loose || damaged ? path + "Normal" : "phobos/shipbreaker/" + FurnaceRules.ThermalPort + "Normal", damaged ? path : path + "Damaged", item.jid.strDmgColor);
        }
        catch (Exception ex)
        {
            // A visual fallback must never pause or modify physical cooling.
            if (!diagnosed) { diagnosed = true; Plugin.Log("Furnace connection artwork fallback: " + ex.Message); }
        }
    }
}
