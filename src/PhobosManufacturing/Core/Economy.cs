using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;

namespace PhobosManufacturing.Core;

/// <summary>Manufacturing's economy data pack: prices, work, bills, salvage, offers, lots and regional factors, read
/// from <c>framework/economy.json</c> through the Framework loader with player overrides. Identities and masses stay
/// in the rules; the pack is validated against them. Content.Prepare reloads it with the native lookups so a new game
/// load sees edited files; offline checks read the shipped copy on first use.</summary>
public static class Economy
{
    public const string Schema = EconomySchema.Name, ModFolder = "PhobosManufacturing", Resource = "PhobosManufacturing.economy.json";
    private static EconomyPack? pack;
    public static EconomyPack Pack => pack ??= Load();
    public static DataPackSource Source => new(ManufacturingRules.Owner, ModFolder, Schema, typeof(Economy).Assembly, Resource);

    /// <summary>The machine families, in the order their economy rows are applied, with the mass their salvage must weigh.</summary>
    public static readonly IReadOnlyList<(string Prefix, double MassKg)> Machines = new[]
    {
        (RefineryRules.Prefix, RefineryRules.MachineKg), (ProcessorRules.Prefix, ProcessorRules.MachineKg), (SabatierRules.Prefix, SabatierRules.MachineKg),
        (CrackerRules.Prefix, CrackerRules.MachineKg), (ManifoldRules.Prefix, ManifoldRules.MachineKg), (FillerRules.Prefix, FillerRules.MachineKg),
        (RegulatorRules.Prefix, RegulatorRules.MachineKg), (LeachRules.Prefix, LeachRules.MachineKg), (AcidPlantRules.Prefix, AcidPlantRules.MachineKg)
    };
    public static IReadOnlyList<string> EquipmentKeys => Machines.Select(m => m.Prefix).Concat(GasStores.Families.Select(f => f.SmallPrefix)).Concat(LiquidStores.Families.Select(f => f.SmallPrefix)).ToArray();
    public static IReadOnlyList<string> SupplyKeys { get; } = new[] { PropellantLineRules.Prefix };

    /// <summary>Reads the shipped pack and any player files, validates them against the code's families and returns
    /// the result. The native lookups (material masses, merchant tables) are supplied when the game's data is loaded;
    /// without them those checks are skipped.</summary>
    public static EconomyPack Load(Func<string, double?>? materialMassOf = null, Func<string, bool>? merchantExists = null)
    {
        var context = new EconomyContext(EquipmentKeys, SupplyKeys) { MassOf = MassOf, MaterialMassOf = materialMassOf, MerchantExists = merchantExists };
        pack = DataPacks.Load<EconomyPack>(Source, p => EconomySchema.Validate(p, context));
        return pack;
    }
    public static double? MassOf(string prefix)
    {
        foreach (var m in Machines) if (m.Prefix == prefix) return m.MassKg;
        var family = GasStores.Families.FirstOrDefault(f => f.SmallPrefix == prefix);
        if (family != null) return family.SmallDryKg;
        return LiquidStores.Families.FirstOrDefault(f => f.SmallPrefix == prefix)?.SmallDryKg;
    }
    public static EquipmentEconomyEntry Entry(string prefix) => Pack.equipment.TryGetValue(prefix, out var e) ? e : throw new InvalidOperationException("No economy entry for " + prefix);
    public static double Price(string prefix) => Entry(prefix).price;
    public static double SupplyPrice(string prefix) => Pack.supplies.TryGetValue(prefix, out var s) ? s.price : throw new InvalidOperationException("No economy entry for " + prefix);
}
