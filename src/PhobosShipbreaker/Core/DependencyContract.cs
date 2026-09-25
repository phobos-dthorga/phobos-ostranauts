using System;
using System.Collections.Generic;
using System.Linq;

namespace PhobosShipbreaker.Core;

internal static class DependencyContract
{
    internal const string MinimumFramework = "0.16.0";
    internal static readonly string[] Materials = { "ItmScrapTrash", "ItmScrapSteel", "ItmScrapAluminum",
        "ItmScrapCarbonFiber", "ItmPartsMechSmall01", "ItmPartsElecSmall01" };
    internal static readonly (string Table, string[] Names)[] Required = {
        ("objects", Materials.Concat(new[] { "ItmTable01", "ItmTable02", ProcessRules.Wall }).ToArray()),
        ("items", new[] { "Blank" }),
        ("conditions", new[] { "PhobosShipbreakerContent", "PhobosShipbreakerIsSection", "IsInstalled", "IsDamaged",
            "IsContainer", "IsSystem", "IsSolid", "IsMechanical", "IsCategoryIndustrialProducts", "IsCumbersome", "IsAluminum", "IsLocked", "IsFloor", "IsFloorSealed", "IsFloorGrate", "IsEVATile",
            "IsWall1x1", "IsWall", "IsWallDeco", "IsFixture", "IsFixtureExt", "IsRigid", "IsSalvageValueHigh", "StatMass", "StatBasePrice", "StatInstallProgressMax",
            "StatUninstallProgressMax", "StatRepairProgressMax", "StatDismantleProgressMax", "StatDamageMax", "IsChair", "IsSignalable", "IsPristine", "IsToolMortorq", "StatInstallRateMISC" }),
        ("triggers", new[] { "PhobosShipbreakerTSection", "TIsFitContainerSolid", "TIsFitContainerSolidCumbersome", "TIsReadyUsePower",
            "TIsScrapSteel", "TIsScrapAluminum", "TIsPartsMechSmall", "TIsPartsElecSmall", "TIsRepairableNotContained",
            "TIsToolMortorq", "TIsToolSoldering", "TIsToolWelding", "TCanBeDismantled", "TIsUndamageableNotContained", "TIsChairFree" }),
        ("interactions", new[] { "Inventory", "ACTRepairTEMP", "ACTRepairTEMPAllow",
            "ACTInstallNoSparksTEMP", "ACTInstallNoSparksTEMPAllow", "ACTUninstallNoSparksTEMP", "ACTUninstallNoSparksTEMPAllow",
            "ACTUndamageTEMP", "ACTUndamageTEMPAllow", "ACTDismantleTEMP", "ACTDismantleTEMPAllow", "ACTChairSitShim" }),
        ("loot", new[] { "ACTDefaultDestroy", "CTWorkProgressMISC", "CONDRepairProgressx5", "CONDInstallProgressx5", "CONDUninstallProgressx5",
            "CONDUndamageProgress", "CONDDismantleProgress", "ItmOKLGSupplyKioskInv", "ItmOKLGFixer", "ItmTraderSanDiegoHalvorsonInv", "ItmVORBScrapKioskInv",
            "Blank", "TILWall", "TILWallDecoAdds", "TILExtFixtureAdds", "TILFixtureAdds", "TILItemAdds", "TILItemForbids", "TILObstruction", "TILFloor" })
    };
    internal static string? FrameworkProblem(Version? loaded) => loaded == null ? Text.Get("DependencyContract.phobos_framework_plugin_is_not_loaded") :
        loaded < new Version(MinimumFramework) ? Text.Get("DependencyContract.phobos_framework_is_below_required", loaded, MinimumFramework) : null;
    internal static List<string> MissingDefinitions(Func<string, string, bool> contains) => Required
        .SelectMany(group => group.Names.Where(name => !contains(group.Table, name))
            .Select(name => Text.Get("DependencyContract.missing", group.Table, name))).ToList();
    internal static readonly string[] Recipes = { "PhobosCraft_PhobosBuildFurnaceSection", "PhobosCraft_PhobosBuildFurnace", "PhobosCraft_PhobosBuildFurnaceRadiator", "PhobosCraft_PhobosBuildFurnaceThermalPort", "PhobosCraft_PhobosFinishFurnaceHousing", "PhobosCraft_PhobosBuildShipbreakerSectionCast", "PhobosCraft_PhobosBuildReclaimerSectionCast", "PhobosCraft_PhobosBuildShipbreakerSection", "PhobosCraft_PhobosBuildShipbreaker",
        "PhobosCraft_PhobosBuildHullChute", "PhobosCraft_PhobosBuildExteriorGrabber", "PhobosCraft_PhobosBuildResidueCollector", "PhobosCraft_PhobosBuildReclaimerSection", "PhobosCraft_PhobosBuildReclaimer", "PhobosCraft_PhobosBuildIndustrialConsole" };
    internal static List<string> MissingRecipes(Func<string, bool> contains) => Recipes
        .Where(id => !contains(id)).Select(id => Text.Get("DependencyContract.missing_registered_recipe", id)).ToList();
}
