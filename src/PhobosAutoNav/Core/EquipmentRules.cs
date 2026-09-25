namespace PhobosAutoNav.Core;

// Content-owned balance. Shared registration/maintenance stays in Framework;
// the construction bill and its duration remain in framework/recipes.json.
internal static class EquipmentRules
{
    internal const double PursuitPrice = 5400, PursuitBrokenPrice = 1350;
    internal const double FunctionalPrice = 3600, BrokenPrice = 900;
    internal const double ModuleMassKg = 0.4, AssemblyOffcutKg = 0.6;
    internal const double RepairProgress = 900, DismantleProgress = 100;
    internal const double LegacyRepairProgress = 480;
    internal const int RepairElectronicsCount = 2;
    internal const string ElectronicsTrigger = "TIsPartsElecSmall", ElectronicsItem = "ItmPartsElecSmall01";
    internal const double FixerWornChance = 0.30, KLegBrokenChance = 0.25,
        PolarisPristineChance = 0.60, VenusRefurbishedChance = 0.20;
    internal const float SalvageChance = .03f;
    internal const double DamagedSalvageShare = 2d / 3;
    // Native leaf tables only: parent random tables choose one of these, avoiding nested bonus rolls.
    internal static readonly string[] SalvageTables = {
        "ItmNavStationModsAll", "ItmNavStationModsAllDmg", "ItmNavStationModsCombat",
        "ItmNavStationModsTorchShip", "ItmNavStationModsTorchShip2", "ItmNavStationModsAtmo",
        "ItmNavStationModsPod", "ItmNavStationModsTorchCombat", "ItmNavStationModsTorchCombat2",
        "ItmNavStationModsRandomAll", "ItmNavStationModsRandomAllDmg", "ItmNavStationModsRandomPod",
        "ItmNavStationModsRandomAtmoSub", "ItmNavStationModsRandomCombatSub", "ItmNavStationModsRandomTorchSub"
    };
}
