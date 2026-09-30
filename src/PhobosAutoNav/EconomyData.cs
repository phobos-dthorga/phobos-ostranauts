using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;
using PhobosAutoNav.Core;

namespace PhobosAutoNav;

/// <summary>Auto Nav's <c>economy</c> data pack (<c>framework/economy.json</c>, Auto Nav 0.30.0): the three
/// navigation boards' prices, repair and dismantle work, repair bill, merchant offers, regional factors, the lot
/// and the rare derelict salvage. Identities, the board mass and the residue stay in code.</summary>
internal static class AutoNavEconomy
{
    internal const string Schema = EconomySchema.Name, ModFolder = "PhobosAutoNav", Resource = "PhobosAutoNav.economy.json";
    internal const string OwnerTag = "AutoNav";
    private static EconomyPack? pack;
    internal static EconomyPack Pack => pack ??= Load();
    internal static DataPackSource Source => new(Text.Owner, ModFolder, Schema, typeof(AutoNavEconomy).Assembly, Resource);
    /// <summary>The boards in model order: N1, N2, N3.</summary>
    internal static readonly IReadOnlyList<string> Boards = new[] { NavigationService.ModuleId, NavigationService.PursuitId, NavigationService.FireControlId };

    internal static EconomyPack Load(Func<string, double?>? materialMassOf = null, Func<string, bool>? merchantExists = null)
    {
        var context = new EconomyContext(Boards, Array.Empty<string>()) { MassOf = _ => EquipmentRules.ModuleMassKg, MaterialMassOf = materialMassOf, MerchantExists = merchantExists };
        pack = DataPacks.Load<EconomyPack>(Source, p =>
        {
            EconomySchema.Validate(p, context);
            if (p.worldLoot.Count != 1 || p.worldLoot[0].items != null || p.worldLoot[0].tables.Count == 0) throw new ArgumentException(Text.Get("EquipmentContent.salvage_entry"));
        });
        return pack;
    }
    internal static EquipmentEconomyEntry Entry(string id) => Pack.equipment.TryGetValue(id, out var e) ? e : throw new InvalidOperationException("No economy entry for " + id);
    internal static IReadOnlyList<EquipmentSale> Sales => Boards.Select(id => EquipmentSale.Of(id, Pack.equipment[id])).ToArray();
    /// <summary>The one salvage rule: its tables, chance and damaged share.</summary>
    internal static WorldLootEntry Salvage => Pack.worldLoot[0];
    internal static IReadOnlyList<string> SalvageTables => Salvage.tables;
    internal static float SalvageChance => (float)Salvage.chance;
    internal static double DamagedSalvageShare => Salvage.brokenShare;
    /// <summary>Electronic parts one repair consumes, from the N1's bill.</summary>
    internal static int RepairElectronicsCount => Entry(NavigationService.ModuleId).repairBill.TryGetValue(EquipmentRules.ElectronicsItem, out int n) ? n : 0;
}
