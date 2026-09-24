using System;
using System.Linq;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Registration;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

internal static class IndustrialDefinitions
{
    internal static void Add(NativeDefinitions d)
    {
        string p = IndustrialRules.Prefix;
        MachineDefinitions.AddFamily(d, p);
        foreach (var state in new[] { "Installed", "InstalledDmg", "Loose", "LooseDmg" })
        {
            bool installed = state.StartsWith("Installed"), damaged = state.EndsWith("Dmg");
            var co = d.Objects[p + state]; var item = d.Items[p + state];
            co.strNameFriendly = co.strNameShort = Text.Get("Industry.console_name") + (damaged ? Text.Get("Content.damaged") : "");
            co.strDesc = Text.Get("Industry.console_description");
            co.strLoot = "Blank"; co.aSlotsWeHave = Array.Empty<string>(); co.strContainerCT = null;
            co.nContainerWidth = co.nContainerHeight = 0;
            co.inventoryWidth = co.inventoryHeight = IndustrialRules.Footprint;
            co.aStartingConds = co.aStartingConds.Where(s => !s.StartsWith("IsContainer=")).Concat(new[] { "IsChair=1x1", "IsSignalable=1x1" }).ToArray();
            Content.SetStat(co, "StatMass", IndustrialRules.MassKg);
            co.mapPoints = new[] { "use,0,-24", "sit,0,-10", "PowerA,0,24" };
            co.aInteractions = installed ? new[] { IndustrialRules.Controls } : Array.Empty<string>();
            co.mapGUIPropMaps = Array.Empty<string>();
            item.nCols = IndustrialRules.Footprint; item.fZScale = 0.5f;
            item.aSocketAdds = Enumerable.Repeat(installed ? "TILFixtureAdds" : "TILItemAdds", 9).ToArray();
            item.aSocketReqs = Padded(installed ? "TILFloor" : "Blank");
            item.aSocketForbids = Padded(installed ? "TILObstruction" : "TILItemForbids");
            Content.ApplyArtwork(co, item, p + state, p + (installed ? "InstalledDmg" : "LooseDmg"));
        }
        d.Power[p + "Power"] = new JsonPowerInfo { strName = p + "Power", strUsePowerCT = "TIsReadyUsePower",
            aInputPts = new[] { "PowerA" }, bAllowExtPower = true, fAmount = IndustrialRules.PowerKW / Units.SecondsPerHour,
            strIntPowerOn = Content.Prefix + "PowerChange", strIntPowerOff = Content.Prefix + "PowerChange" };
        // Native chair seating/cleanup, without importing any navigation duties or flight behaviour.
        var controls = NativeDefinitions.Clone(DataHandler.dictInteractions["ACTChairSitShim"]);
        controls.strName = IndustrialRules.Controls; controls.strTitle = Text.Get("CollectorDefinitions.control_panel");
        controls.strDesc = Text.Get("Industry.use_console"); controls.strTooltip = Text.Get("Industry.console_description");
        controls.strTargetPoint = "use"; controls.fTargetPointRange = 1.5f;
        controls.CTTestThem = "TIsChairFree";
        d.Interactions[controls.strName] = controls;

        var local = NativeDefinitions.Clone(DataHandler.dictInteractions["Inventory"]);
        local.strName = IndustrialRules.LocalControls; local.strTitle = Text.Get("CollectorDefinitions.control_panel");
        local.strDesc = Text.Get("Industry.inspect_equipment"); local.strTooltip = Text.Get("Industry.inspect_equipment");
        local.strRaiseUI = null; local.fTargetPointRange = 2;
        d.Interactions[local.strName] = local;
        foreach (var co in d.Objects.Values.Where(x => IndustrialRules.Equipment(x.strName) && x.strName.Contains("Installed")))
            co.aInteractions = co.aInteractions.Where(x => x != ReclaimerRules.Controls && x != CollectorRules.Controls)
                .Concat(new[] { IndustrialRules.LocalControls }).Distinct().ToArray();
    }
    private static string[] Padded(string interior) => Enumerable.Range(0, 25)
        .Select(i => i / 5 >= 1 && i / 5 <= 3 && i % 5 >= 1 && i % 5 <= 3 ? interior : "Blank").ToArray();
}
