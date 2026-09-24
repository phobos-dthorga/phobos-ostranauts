using System;
using System.IO;
using System.Linq;
using Phobos.Ostranauts.Framework.Registration;
using PhobosShipbreaker;
using PhobosShipbreaker.Core;

internal static class IndustrialNativeChecks
{
    internal static void Run(NativeDefinitions d, string repo, Action<bool,string> check)
    {
        foreach (string state in new[] { "Installed", "InstalledDmg", "Loose", "LooseDmg" })
        {
            var co = d.Objects[IndustrialRules.Prefix + state]; var item = d.Items[co.strItemDef];
            check(item.nCols == 3 && item.aSocketAdds.Length == 9 && co.inventoryWidth == 3 && co.inventoryHeight == 3, "Console retains intended 3 x 3 footprint: " + state);
            check(co.nContainerWidth == 0 && co.aSlotsWeHave.Length == 0 && co.strLoot == "Blank", "Console inherits no machinery cargo/feed: " + state);
            check(co.aStartingConds.Any(s => s.StartsWith("IsChair=")) && !co.aStartingConds.Any(s => s.StartsWith("IsNavStation=")), "Console supports native seating without flight duties");
            foreach (string image in new[] { item.strImg, item.strImgNorm, co.strPortraitImg })
                check(File.Exists(Path.Combine(repo, "mods/PhobosShipbreaker/images", image + ".png")), "Console art shipped: " + image);
            if (state.StartsWith("Installed"))
                check(item.aSocketReqs.Count(s => s == "TILFloor") == 9 && co.aInteractions.Contains(IndustrialRules.Controls), "Installed console needs nine floor tiles and its own controls");
            else check(!co.aInteractions.Contains(IndustrialRules.Controls), "Packed console cannot operate");
        }
        var sit = d.Interactions[IndustrialRules.Controls];
        check(sit.strTeleport == "sit" && sit.strAnim == "Sitting" && sit.aInverse.SequenceEqual(DataHandler.dictInteractions["ACTChairSitShim"].aInverse), "Seating retains native inverse cleanup");
        check(sit.CTTestThem == "TIsChairFree" && sit.strRaiseUI == null, "Console uses native chair reservation without a missing Resources prefab");
        check(d.Interactions[IndustrialRules.LocalControls].strRaiseUI == null, "Local panel does not accidentally open Inventory");
        foreach (var co in d.Objects.Values.Where(c => IndustrialRules.Equipment(c.strName) && c.strName.Contains("Installed")))
            check(co.aInteractions.Count(id => id == IndustrialRules.LocalControls) == 1, "Exactly one local Control Panel: " + co.strName);
        check(Math.Abs(d.Power[IndustrialRules.Prefix + "Power"].fAmount * 3600 - 0.08) < 1e-10, "Console native hourly power units reflect its named 80 W demand");
    }
}
