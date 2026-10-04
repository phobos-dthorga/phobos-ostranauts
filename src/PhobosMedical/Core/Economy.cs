using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;

namespace PhobosMedical.Core;

/// <summary>Phobos Medical's economy data pack: prices, work, bills, salvage, offers, lots, regions and faction-kiosk
/// tiers, read from <c>framework/economy.json</c> through the Framework loader with player overrides.</summary>
public static class Economy
{
    public const string Schema = EconomySchema.Name, Resource = "PhobosMedical.economy.json";
    private static EconomyPack? pack;
    public static EconomyPack Pack => pack ??= Load();
    public static DataPackSource Source => new(MedicalRules.Owner, MedicalRules.ModFolder, Schema, typeof(Economy).Assembly, Resource);
    /// <summary>The equipment families, in application order, with the mass their salvage must weigh.</summary>
    public static readonly IReadOnlyList<(string Prefix, double MassKg)> Machines = new[] { (MedicalRules.BedPrefix, MedicalRules.MachineKg), (MedicalRules.MonitorPrefix, MedicalRules.MonitorKg) };
    public static IReadOnlyList<string> EquipmentKeys => Machines.Select(m => m.Prefix).ToArray();

    public static EconomyPack Load(Func<string, double?>? materialMassOf = null, Func<string, bool>? merchantExists = null)
    {
        var context = new EconomyContext(EquipmentKeys, Array.Empty<string>()) { MassOf = MassOf, MaterialMassOf = materialMassOf, MerchantExists = merchantExists };
        pack = DataPacks.Load<EconomyPack>(Source, p => EconomySchema.Validate(p, context));
        return pack;
    }
    public static double? MassOf(string prefix)
    {
        foreach (var m in Machines) if (m.Prefix == prefix) return m.MassKg;
        return null;
    }
    public static EquipmentEconomyEntry Entry(string prefix) => Pack.equipment.TryGetValue(prefix, out var e) ? e : throw new InvalidOperationException("No economy entry for " + prefix);
    public static double Price(string prefix) => Entry(prefix).price;
}
