using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Registration;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

internal static class IntakeDefinitions
{
    internal static void Add(NativeDefinitions d)
    {
        d.Conditions[IntakeRules.Working] = new JsonCond { strName = IntakeRules.Working,
            strNameFriendly = "Intake moving", strColor = "Neutral", nDisplaySelf = 2, nDisplayOther = 2 };
        d.Loot["PhobosHullChuteForbids"] = new Loot { strName = "PhobosHullChuteForbids", strType = "condition",
            aCOs = new[] { "IsFixture=1.0x1", "IsFixtureExt=1.0x1", "IsWallDeco=1.0x1" }, aLoots = Array.Empty<string>() };
        foreach (string prefix in new[] { IntakeRules.Chute, IntakeRules.Grabber })
        {
            bool grabber = prefix == IntakeRules.Grabber;
            int depth = grabber ? IntakeRules.GrabberDepth : IntakeRules.ChuteDepth;
            MachineDefinitions.AddFamily(d, prefix);
            foreach (string state in new[] { "Installed", "Loose", "InstalledDmg", "LooseDmg" })
            {
                string id = prefix + state;
                bool installed = state.StartsWith("Installed", StringComparison.Ordinal), damaged = state.EndsWith("Dmg", StringComparison.Ordinal);
                var co = d.Objects[id]; var item = d.Items[id];
                co.strNameFriendly = co.strNameShort = (grabber ? "Phobos Exterior Panel Grabber" : "Phobos Sealed Hull Chute") + (damaged ? " (Damaged)" : "");
                co.strDesc = grabber ? "4 wide x 3 deep; 80 kg. Arms face space, rear against a 4 x 1 wall chute. Inventory is the loading point for detached ordinary 24 kg walls. Small solids may be stored but are not processed. Start from the processor controls. No cutting of installed hull yet."
                    : "4 wide x 1 deep; 40 kg. Install OVER four intact wall tiles, between the exterior grabber and interior processor. Keep those walls: this sealed transfer connection does not replace the pressure hull. No inventory or door to operate.";
                co.strLoot = "Blank";
                co.aSlotsWeHave = Array.Empty<string>();
                co.aStartingConds = co.aStartingConds.Where(x => grabber || !x.StartsWith("IsContainer=", StringComparison.Ordinal)).ToArray();
                co.aInteractions = grabber ? new[] { "Inventory" } : Array.Empty<string>();
                co.mapGUIPropMaps = grabber ? new[] { "GUIInv", "Inventory" } : Array.Empty<string>();
                co.strContainerCT = grabber ? "TIsFitContainerSolidCumbersome" : null;
                co.nContainerWidth = co.nContainerHeight = grabber ? 4 : 0;
                co.inventoryWidth = 4; co.inventoryHeight = depth;
                Content.SetStat(co, "StatMass", grabber ? IntakeRules.GrabberKg : IntakeRules.ChuteKg);
                Content.SetStat(co, "StatBasePrice", (grabber ? 800 : 400) / (damaged ? 4 : 1));
                co.mapPoints = new[] { "use,0,0", "PowerA,-24,-32", "PowerB,24,-32" };
                co.jsonPI = grabber && installed && !damaged ? prefix + "Power" : null;
                co.aTickers = co.jsonPI != null ? new[] { "Power" } : Array.Empty<string>();
                // Approved static assemblies are shared across loose/installed states.
                // Native damage tint and explicit damaged name distinguish repair state.
                Content.ApplyArtwork(co, item, prefix);
                item.fZScale = grabber ? 0.5f : 0.75f; // Native wall-mounted pump layering precedent.
                item.nCols = 4;
                item.aSocketAdds = Enumerable.Repeat(installed ? (grabber ? "TILExtFixtureAdds" : "TILWallDecoAdds") : "TILItemAdds", 4 * depth).ToArray();
                item.aSocketReqs = Padded(depth, installed && !grabber ? "TILWall" : "Blank");
                item.aSocketForbids = Padded(depth, installed ? (grabber ? "TILObstruction" : "PhobosHullChuteForbids") : "TILItemForbids");
                if (installed && grabber)
                    for (int col = 1; col <= 4; col++) item.aSocketReqs[(depth + 1) * 6 + col] = "TILWall";
            }
            if (grabber)
            {
                d.Power[prefix + "Power"] = new JsonPowerInfo { strName = prefix + "Power", strUsePowerCT = "TIsReadyUsePower",
                    aInputPts = new[] { "PowerA", "PowerB" }, bAllowExtPower = true,
                    strIntPowerOn = Content.Prefix + "PowerChange", strIntPowerOff = Content.Prefix + "PowerChange",
                    fAmount = IntakeRules.IdleKW / 3600, strOverrideCond = IntakeRules.Working, fOverrideAmount = IntakeRules.WorkingKW / 3600 };
            }
        }
    }

    private static string[] Padded(int depth, string interior) => Enumerable.Range(0, (depth + 2) * 6)
        .Select(i => i % 6 > 0 && i % 6 < 5 && i / 6 > 0 && i / 6 <= depth ? interior : "Blank").ToArray();
}
