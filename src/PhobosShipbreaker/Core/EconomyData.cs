using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;

namespace PhobosShipbreaker.Core;

/// <summary>Shipbreaker's <c>economy</c> data pack (<c>framework/economy.json</c>, Shipbreaker 0.48.0): prices, work,
/// repair bills, salvage, sections, the coolant conduit, offers, regional factors, lots and world finds, read through
/// the Framework loader with player overrides. Identities and masses stay in the rules; the pack is checked against
/// them. Content.Prepare reloads it with the native lookups; offline checks read the shipped copy on first use.</summary>
public static class ShipbreakerEconomy
{
    public const string Schema = EconomySchema.Name, ModFolder = "PhobosShipbreaker", Resource = "PhobosShipbreaker.economy.json";
    public const string OwnerTag = "Shipbreaker";
    private static EconomyPack? pack;
    public static EconomyPack Pack => pack ??= Load();
    public static DataPackSource Source => new(Text.Owner, ModFolder, Schema, typeof(ShipbreakerEconomy).Assembly, Resource);

    /// <summary>The machine families in the order their economy rows are applied, with the mass their salvage must weigh.</summary>
    public static readonly IReadOnlyList<(string Prefix, double MassKg)> Machines = new[]
    {
        (FurnaceRules.Prefix, FurnaceRules.MachineKg), (FurnaceRules.Radiator, FurnaceRules.RadiatorKg), (FurnaceRules.ThermalPort, FurnaceRules.RadiatorKg),
        (IndustrialRules.Prefix, IndustrialRules.MassKg), (ProcessRules.Prefix, ProcessRules.MachineKg), (IntakeRules.Grabber, IntakeRules.GrabberKg),
        (IntakeRules.Chute, IntakeRules.ChuteKg), (ReclaimerRules.Prefix, ReclaimerRules.MachineKg), (CollectorRules.Prefix, CollectorRules.MachineKg),
        (ThawRules.Prefix, ThawRules.MachineKg), (LaserRules.Prefix, LaserRules.MachineKg), (BinRules.Prefix, 0)
    };
    /// <summary>The assembly sections, sold whole and dismantled to their own mass.</summary>
    public static readonly IReadOnlyList<(string Id, double MassKg)> Sections = new[]
    {
        (ProcessRules.AssemblySection, ProcessRules.AssemblySectionKg), (ReclaimerRules.Section, ReclaimerRules.SectionKg), (FurnaceRules.Section, FurnaceRules.SectionKg)
    };
    public static IReadOnlyList<string> EquipmentKeys => Machines.Select(m => m.Prefix).Concat(Sections.Select(s => s.Id)).ToArray();
    public static IReadOnlyList<string> SupplyKeys { get; } = new[] { FurnaceCooling.Conduit };

    public static EconomyPack Load(Func<string, double?>? materialMassOf = null, Func<string, bool>? merchantExists = null)
    {
        var context = new EconomyContext(EquipmentKeys, SupplyKeys) { MassOf = MassOf, MaterialMassOf = materialMassOf, MerchantExists = merchantExists };
        pack = DataPacks.Load<EconomyPack>(Source, p => EconomySchema.Validate(p, context));
        return pack;
    }
    /// <summary>The mass a family's salvage must add up to; the bin reads its own from the vessels pack.</summary>
    public static double? MassOf(string key)
    {
        if (key == BinRules.Prefix) return BinRules.DryKg;
        foreach (var m in Machines) if (m.Prefix == key) return m.MassKg;
        foreach (var s in Sections) if (s.Id == key) return s.MassKg;
        return null;
    }
    public static EquipmentEconomyEntry Entry(string key) => Pack.equipment.TryGetValue(key, out var e) ? e : throw new InvalidOperationException("No economy entry for " + key);
    public static double Price(string key) => Entry(key).price;
}
