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
            strNameFriendly = Text.Get("CollectorDefinitions.collecting_residue"), strColor = "Neutral", nDisplaySelf = 2, nDisplayOther = 2 };
        var controls = NativeDefinitions.Clone(DataHandler.dictInteractions["Inventory"]);
        controls.strName = CollectorRules.Controls; controls.strTitle = Text.Get("CollectorDefinitions.control_panel");
        controls.strDesc = Text.Get("CollectorDefinitions.us_checks_the_residue_collector_s_controls");
        controls.strTooltip = Text.Get("CollectorDefinitions.choose_a_processor_and_collect_its_residue");
        controls.strRaiseUI = null; controls.fTargetPointRange = 2;
        d.Interactions[controls.strName] = controls;
        // Block the footprint on open floors too. Existing wall mounts retain
        // their wall-decoration identity; neither mode creates/removes structure.
        d.Loot[p + "Adds"] = new Loot { strName = p + "Adds", strType = "condition",
            aCOs = new[] { "IsWallDeco=1.0x1", "IsFixture=1.0x1", "IsObstruction=1.0x1" }, aLoots = Array.Empty<string>() };
        foreach (string state in new[] { "Installed", "Loose", "InstalledDmg", "LooseDmg" })
        {
            string id = p + state; bool installed = state.StartsWith("Installed", StringComparison.Ordinal);
            bool damaged = state.EndsWith("Dmg", StringComparison.Ordinal);
            var co = d.Objects[id]; var item = d.Items[id];
            co.strNameFriendly = co.strNameShort = Text.Get("CollectorDefinitions.phobos_residue_collector", (damaged ? Text.Get("CollectorDefinitions.damaged") : ""));
            co.strDesc = Text.Get("CollectorDefinitions.wide_x_deep_kg_mount_over_two", CollectorRules.Width, CollectorRules.Depth, CollectorRules.MachineKg, CollectorRules.Capacity, CollectorRules.PayloadKg);
            co.strLoot = "Blank"; co.aSlotsWeHave = Array.Empty<string>();
            co.nContainerWidth = co.nContainerHeight = CollectorRules.StorageSide;
            co.inventoryWidth = CollectorRules.Width; co.inventoryHeight = CollectorRules.Depth;
            co.strContainerCT = "TIsFitContainerSolid";
            co.aInteractions = installed ? new[] { "Inventory", CollectorRules.Controls } : new[] { "Inventory" };
            co.mapPoints = new[] { "use,0,-16", "PowerA,-8,0", "PowerB,8,0" };
            Content.SetStat(co, "StatMass", CollectorRules.MachineKg);
            Content.SetStat(co, "StatBasePrice", damaged ? 100 : 400);
            item.nCols = CollectorRules.Width; item.fZScale = 0.75f;
            item.aSocketAdds = Enumerable.Repeat(installed ? p + "Adds" : "TILItemAdds", 2).ToArray();
            item.aSocketReqs = Padded(installed ? "TILWall" : "Blank");
            item.aSocketForbids = Padded(installed ? "PhobosHullChuteForbids" : "TILItemForbids");
            Content.ApplyArtwork(co, item, p);
        }
        d.Power[p + "Power"] = new JsonPowerInfo { strName = p + "Power", strUsePowerCT = "TIsReadyUsePower",
            aInputPts = new[] { "PowerA", "PowerB" }, bAllowExtPower = true,
            strIntPowerOn = Content.Prefix + "PowerChange", strIntPowerOff = Content.Prefix + "PowerChange",
            fAmount = CollectorRules.IdleKW / Phobos.Ostranauts.Framework.Units.SecondsPerHour, strOverrideCond = CollectorRules.Working, fOverrideAmount = workingKW / Phobos.Ostranauts.Framework.Units.SecondsPerHour };
    }
    private static string[] Padded(string interior) => Enumerable.Range(0, 12)
        .Select(i => i == 5 || i == 6 ? interior : "Blank").ToArray();
}
