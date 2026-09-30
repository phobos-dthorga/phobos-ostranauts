namespace PhobosAutoNav.Core;

// Content-owned identities the economy data pack (framework/economy.json) is checked against; prices, work,
// offers and salvage chances live in that pack, and the construction bill and its duration remain in
// framework/recipes.json. Shared registration/maintenance stays in Framework.
internal static class EquipmentRules
{
    internal const double ModuleMassKg = 0.4, AssemblyOffcutKg = 0.6;
    internal const double LegacyRepairProgress = 480;
    internal const string ElectronicsTrigger = "TIsPartsElecSmall", ElectronicsItem = "ItmPartsElecSmall01";
}
