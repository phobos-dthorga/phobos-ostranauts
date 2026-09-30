using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Registration;

namespace PhobosAgriculture;

/// <summary>World finds from the economy data pack: seeds and produce in fridges, supplies in locked crates and loose
/// machinery in engineering salvage. Chances are per native contents-table roll, not per ship or search, scaled by
/// the Loot setting; each table keeps one optional choice so adding stock cannot flood containers.</summary>
internal static class LootContent
{
    internal const double DefaultMultiplier = 1, MaximumMultiplier = 3;
    internal const string FridgeTable = "ItmFridge01Contents", CrateTable = "ItmRandomCrateLockedContents";
    // Preserve our original fridge branch when expanding it beyond seeds.
    internal const string FridgeBranch = "PhobosAgricultureStoredSeeds", CrateBranch = "PhobosAgricultureCrateSupplies", MachineryBranch = "PhobosAgricultureMachinerySalvage";
    /// <summary>Chance per engineering roll of one loose machine, before the setting.</summary>
    internal static double EquipmentChance => AgricultureEconomy.Pack.worldLoot.First(l => l.branch == MachineryBranch).chance;

    internal static void Add(NativeDefinitions definitions, bool enabled, double multiplier)
    {
        if (double.IsNaN(multiplier) || double.IsInfinity(multiplier) || multiplier < 0 || multiplier > MaximumMultiplier)
            throw new ArgumentOutOfRangeException(nameof(multiplier));
        EconomyStock.AddWorldLoot(definitions, AgricultureEconomy.Pack, AgricultureEconomy.Sales, enabled ? multiplier : 0);
    }
}
