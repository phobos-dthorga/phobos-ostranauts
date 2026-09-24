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
            strNameFriendly = Text.Get("IntakeDefinitions.intake_moving"), strColor = "Neutral", nDisplaySelf = 2, nDisplayOther = 2 };
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
                co.strNameFriendly = co.strNameShort = (grabber ? Text.Get("IntakeDefinitions.phobos_exterior_panel_grabber") : Text.Get("IntakeDefinitions.phobos_sealed_hull_chute")) + (damaged ? Text.Get("IntakeDefinitions.damaged") : "");
                co.strDesc = grabber ? Text.Get("IntakeDefinitions.wide_x_deep_kg_arms_face_space", IntakeRules.Width, IntakeRules.GrabberDepth, IntakeRules.GrabberKg, IntakeRules.ChuteDepth, ProcessRules.InputKg)
                    : Text.Get("IntakeDefinitions.wide_x_deep_kg_install_over_four", IntakeRules.Width, IntakeRules.ChuteDepth, IntakeRules.ChuteKg);
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
                    fAmount = IntakeRules.IdleKW / Phobos.Ostranauts.Framework.Units.SecondsPerHour, strOverrideCond = IntakeRules.Working, fOverrideAmount = IntakeRules.WorkingKW / Phobos.Ostranauts.Framework.Units.SecondsPerHour };
            }
        }
    }

    private static string[] Padded(int depth, string interior) => Enumerable.Range(0, (depth + 2) * 6)
        .Select(i => i % 6 > 0 && i % 6 < 5 && i / 6 > 0 && i / 6 <= depth ? interior : "Blank").ToArray();
}
