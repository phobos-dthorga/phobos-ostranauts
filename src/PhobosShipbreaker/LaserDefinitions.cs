using System;
using System.Linq;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Registration;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

/// <summary>Native definitions for the Ablatine ML-2 mining laser on the shared machine family contract: a 2 x 2
/// exterior fixture against two intact hull walls, as the G4 mounts against four. It holds nothing, so the family's
/// container, feed slot and compartments are removed. Its local +Y faces space; the wall row, both power points and
/// the room its heat goes to are on the -Y side.</summary>
internal static class LaserDefinitions
{
    internal const string Art = "PhobosMiningLaser";
    internal static void Add(NativeDefinitions d)
    {
        string p = LaserRules.Prefix; int size = LaserRules.Footprint, side = size + 2;
        d.Conditions[LaserRules.Working] = new JsonCond { strName = LaserRules.Working,
            strNameFriendly = Text.Get("Laser.working_condition"), strColor = "Neutral", nDisplaySelf = 2, nDisplayOther = 2 };
        MachineDefinitions.AddFamily(d, p);
        foreach (string state in MachineFamilies.Forms)
        {
            bool installed = state.StartsWith("Installed", StringComparison.Ordinal), damaged = state.EndsWith("Dmg", StringComparison.Ordinal);
            var co = d.Objects[p + state]; var item = d.Items[p + state];
            co.strNameFriendly = co.strNameShort = Text.Get("Laser.name") + (damaged ? Text.Get("Content.damaged") : "");
            co.strDesc = Text.Get("Laser.description", LaserRules.MachineKg, LaserRules.ArcDegrees, LaserRules.RangeTiles, LaserRules.WorkingKW,
                LaserRules.HeatKW(LaserRules.WorkingKW));
            co.strLoot = "Blank";
            co.aSlotsWeHave = Array.Empty<string>();
            co.aStartingConds = co.aStartingConds.Where(x => !x.StartsWith("IsContainer=", StringComparison.Ordinal)).ToArray();
            co.aInteractions = Array.Empty<string>();
            co.mapGUIPropMaps = Array.Empty<string>();
            co.strContainerCT = null;
            co.nContainerWidth = co.nContainerHeight = 0;
            co.inventoryWidth = co.inventoryHeight = size;
            Content.SetStat(co, "StatMass", LaserRules.MachineKg);
            // Sixteen pixels to a tile from the centre. The crew point and the heat go to the tile behind the left
            // backing wall; conduit meets the head in the wall row, as it does on the G4.
            co.mapPoints = new[] { "use,-8,-40", "emit," + Pixels(LaserRules.EmitterPixelsX) + "," + Pixels(LaserRules.EmitterPixelsY), "PowerA,-8,-24", "PowerB,8,-24" };
            co.jsonPI = installed && !damaged ? p + "Power" : null;
            co.aTickers = co.jsonPI != null ? new[] { "Power" } : Array.Empty<string>();
            // One overhead master for every form and the portrait; damaged forms take the game's damage tint.
            Content.ApplyArtwork(co, item, Art, Art);
            co.strPortraitImg = item.strImg;
            item.fZScale = 0.5f; // The G4's wall-mounted layering.
            item.nCols = size;
            item.aSocketAdds = Enumerable.Repeat(installed ? "TILExtFixtureAdds" : "TILItemAdds", size * size).ToArray();
            item.aSocketReqs = MachineFamilies.Border(size, "Blank");
            item.aSocketForbids = MachineFamilies.Border(size, installed ? "TILObstruction" : "TILItemForbids");
            if (installed)
                for (int col = 1; col <= size; col++) item.aSocketReqs[(size + 1) * side + col] = "TILWall";
        }
        d.Power[p + "Power"] = new JsonPowerInfo { strName = p + "Power", strUsePowerCT = "TIsReadyUsePower",
            aInputPts = new[] { "PowerA", "PowerB" }, bAllowExtPower = true,
            strIntPowerOn = Content.Prefix + "PowerChange", strIntPowerOff = Content.Prefix + "PowerChange",
            fAmount = LaserRules.IdleKW / Units.SecondsPerHour, strOverrideCond = LaserRules.Working, fOverrideAmount = LaserRules.WorkingKW / Units.SecondsPerHour };
    }
    private static string Pixels(double value) => ((int)Math.Round(value)).ToString(System.Globalization.CultureInfo.InvariantCulture);
}
