using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Registration;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

internal static class CollectorDefinitions
{
    internal static void Add(NativeDefinitions d, double workingKW)
    {
        string p = CollectorRules.Prefix;
        MachineDefinitions.AddFamily(d, p);
        d.Conditions[CollectorRules.Working] = new JsonCond { strName = CollectorRules.Working,
            strNameFriendly = "Collecting residue", strColor = "Neutral", nDisplaySelf = 2, nDisplayOther = 2 };
        var controls = NativeDefinitions.Clone(DataHandler.dictInteractions["Inventory"]);
        controls.strName = CollectorRules.Controls; controls.strTitle = "Control Panel";
        controls.strDesc = "[us] [checks] the residue collector's controls.";
        controls.strTooltip = "Choose a processor and collect its residue. Material remains aboard.";
        controls.strRaiseUI = null; controls.fTargetPointRange = 2;
        d.Interactions[controls.strName] = controls;
        // Reuse the wall-chute support contract. Neither fixture creates/removes walls.
        foreach (string state in new[] { "Installed", "Loose", "InstalledDmg", "LooseDmg" })
        {
            string id = p + state; bool installed = state.StartsWith("Installed", StringComparison.Ordinal);
            bool damaged = state.EndsWith("Dmg", StringComparison.Ordinal);
            var co = d.Objects[id]; var item = d.Items[id];
            co.strNameFriendly = co.strNameShort = "Phobos Residue Collector" + (damaged ? " (Damaged)" : "");
            co.strDesc = "2 wide x 1 deep; 20 kg. Mount OVER two intact exterior walls, dark pocket facing space. " +
                "Control Panel selects a processor connected by structural flooring. Inventory holds four 13 kg residue packets. " +
                "Requires separate conduit power. Collection keeps material aboard; no ejection. Saved pairs survive reload; resume collection manually.";
            co.strLoot = "Blank"; co.aSlotsWeHave = Array.Empty<string>();
            co.nContainerWidth = co.nContainerHeight = CollectorRules.StorageSide;
            co.inventoryWidth = CollectorRules.Width; co.inventoryHeight = CollectorRules.Depth;
            co.strContainerCT = "TIsFitContainerSolid";
            co.aInteractions = installed ? new[] { "Inventory", CollectorRules.Controls } : new[] { "Inventory" };
            co.mapPoints = new[] { "use,0,-16", "PowerA,-8,0", "PowerB,8,0" };
            Content.SetStat(co, "StatMass", CollectorRules.MachineKg);
            Content.SetStat(co, "StatBasePrice", damaged ? 100 : 400);
            item.nCols = CollectorRules.Width; item.fZScale = 0.75f;
            item.aSocketAdds = Enumerable.Repeat(installed ? "TILWallDecoAdds" : "TILItemAdds", 2).ToArray();
            item.aSocketReqs = Padded(installed ? "TILWall" : "Blank");
            item.aSocketForbids = Padded(installed ? "PhobosHullChuteForbids" : "TILItemForbids");
            Content.ApplyArtwork(co, item, p);
        }
        d.Power[p + "Power"] = new JsonPowerInfo { strName = p + "Power", strUsePowerCT = "TIsReadyUsePower",
            aInputPts = new[] { "PowerA", "PowerB" }, bAllowExtPower = true,
            strIntPowerOn = Content.Prefix + "PowerChange", strIntPowerOff = Content.Prefix + "PowerChange",
            fAmount = CollectorRules.IdleKW / 3600, strOverrideCond = CollectorRules.Working, fOverrideAmount = workingKW / 3600 };
    }
    private static string[] Padded(string interior) => Enumerable.Range(0, 12)
        .Select(i => i == 5 || i == 6 ? interior : "Blank").ToArray();
}
