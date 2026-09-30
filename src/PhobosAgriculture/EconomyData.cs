using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;

namespace PhobosAgriculture;

/// <summary>Agriculture's <c>economy</c> data pack (<c>framework/economy.json</c>, Agriculture 0.24.0): equipment
/// prices, repair and dismantle work, repair bills, salvage with a retained housing remainder, the irrigation pipe,
/// merchant offers, regional factors, lots and loot. Identities, masses and the food, seed and nutrient item prices
/// stay in code; the pack is checked against the families the code builds.</summary>
internal static class AgricultureEconomy
{
    internal const string Schema = EconomySchema.Name, ModFolder = "PhobosAgriculture", Resource = "PhobosAgriculture.economy.json";
    internal const string OwnerTag = "Agriculture";
    private static EconomyPack? pack;
    internal static EconomyPack Pack => pack ??= Load();
    internal static DataPackSource Source => new(Text.Owner, ModFolder, Schema, typeof(AgricultureEconomy).Assembly, Resource);

    /// <summary>The machine families in their economy order, with the mass their salvage may not exceed.</summary>
    internal static IReadOnlyList<(string Prefix, double MassKg)> Machines => new[]
    {
        (Definitions.Rack, Definitions.RackKg), (Definitions.Cooker, Definitions.CookerKg), (IrrigationDefinitions.Supply, IrrigationDefinitions.DryKg),
        (WorkupDefinitions.Bench, WorkupDefinitions.DryKg), (BulkDefinitions.Tank, BulkDefinitions.DryKg)
    };
    internal static IReadOnlyList<string> EquipmentKeys => Machines.Select(m => m.Prefix).ToArray();
    internal static IReadOnlyList<string> SupplyKeys { get; } = new[] { IrrigationDefinitions.Pipe };

    internal static EconomyPack Load(Func<string, double?>? materialMassOf = null, Func<string, bool>? merchantExists = null)
    {
        var context = new EconomyContext(EquipmentKeys, SupplyKeys) { MassOf = MassOf, MaterialMassOf = materialMassOf, MerchantExists = merchantExists };
        pack = DataPacks.Load<EconomyPack>(Source, p => EconomySchema.Validate(p, context));
        return pack;
    }
    internal static double? MassOf(string prefix) { foreach (var m in Machines) if (m.Prefix == prefix) return m.MassKg; return null; }
    internal static EquipmentEconomyEntry Entry(string prefix) => Pack.equipment.TryGetValue(prefix, out var e) ? e : throw new InvalidOperationException("No economy entry for " + prefix);
    internal static double Price(string prefix) => Entry(prefix).price;
    internal static SupplyEconomyEntry Supply(string prefix) => Pack.supplies.TryGetValue(prefix, out var s) ? s : throw new InvalidOperationException("No economy entry for " + prefix);
    /// <summary>Every saleable size: the five families and the R4 and R5 on the R3's entry.</summary>
    internal static IReadOnlyList<EquipmentSale> Sales => EquipmentKeys.Select(k => EquipmentSale.Of(k, Pack.equipment[k]))
        .Concat(BulkDefinitions.Sizes.Skip(1).Select(s => EquipmentSale.Size(s.Prefix, Pack.equipment[BulkDefinitions.Tank]))).ToArray();
}
