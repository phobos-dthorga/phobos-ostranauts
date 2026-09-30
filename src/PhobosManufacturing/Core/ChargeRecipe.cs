using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Processing;

namespace PhobosManufacturing.Core;

/// <summary>The commodities a charge machine settles through linked bulk vessels instead of its tray: water, every
/// Manufacturing gas-store commodity and every liquid-store commodity (sulfuric acid). Everything else is an item.</summary>
public static class ChargeCommodities
{
    public static bool Is(string? id) => id != null && (id == ManufacturingRules.Water || id == ManufacturingRules.CropNutrients || GasStores.FamilyOf(id) != null || LiquidStores.FamilyOf(id) != null);
}

/// <summary>One input line of a charge: an exact identity (an item, or a commodity drawn from a linked vessel), how
/// many units, and each unit's mass.</summary>
public sealed class ChargeInput
{
    public string Id { get; }
    public int Count { get; }
    public double Kg { get; }
    public ChargeInput(string id, int count, double kg)
    {
        if (string.IsNullOrWhiteSpace(id) || count < 1 || !ManufacturingRules.Finite(kg) || kg <= 0) throw new ArgumentException("Invalid charge input.");
        Id = id; Count = count; Kg = kg;
    }
}

/// <summary>One immutable charge recipe of a charge machine: its exact charge (items from the feed bin, commodities
/// drawn from linked vessels), the products it releases (items to the tray, commodities to linked vessels), the native
/// gas it breathes into the room, any working volume it circulates, the reaction heat it releases, and its powered
/// duration. Charge mass equals product mass plus off-gas mass exactly. The revision is the saved identity of a bound
/// charge, unique within its machine.</summary>
public sealed class ChargeRecipe
{
    public string Machine { get; }
    public string Id { get; }
    public int Revision { get; }
    public IReadOnlyList<ChargeInput> Inputs { get; }
    public IReadOnlyList<ProductSpec> Products { get; }
    /// <summary>Native room species and the kilograms of each released over the whole job.</summary>
    public IReadOnlyDictionary<string, double> OffGas { get; }
    /// <summary>Working volumes that must be present in linked vessels and are returned (not in the mass balance).</summary>
    public IReadOnlyDictionary<string, double> Circulates { get; }
    /// <summary>Heat the reaction releases into the room over the whole charge, kWh; negative when it absorbs heat.</summary>
    public double ReactionKWh { get; }
    public double Seconds { get; }
    /// <summary>A melt: a long interruption freezes it and the charge is lost to slag.</summary>
    public bool Melt { get; }
    public IReadOnlyList<string> Requires { get; }
    public bool NeedsSteelStock => Requires.Contains(ChargeCatalog.SteelStockRequirement);
    /// <summary>Every input kilogram, items and drawn commodities together (the mass balance's input side).</summary>
    public double ChargeKg => Inputs.Sum(i => i.Count * i.Kg);
    public double OffGasKg => OffGas.Values.Sum();
    public double EnergyKWh => Equipment.WorkingKW(Machine) * Seconds / 3600;
    /// <summary>Inputs that are items in the feed bin.</summary>
    public IReadOnlyList<ChargeInput> ItemInputs => Inputs.Where(i => !ChargeCommodities.Is(i.Id)).ToArray();
    /// <summary>Inputs drawn from linked vessels at settlement.</summary>
    public IReadOnlyList<ChargeInput> Draws => Inputs.Where(i => ChargeCommodities.Is(i.Id)).ToArray();
    /// <summary>Products that go to linked vessels at settlement (water, stored gases).</summary>
    public IReadOnlyList<ProductSpec> Deposits => Products.Where(p => ChargeCommodities.Is(p.Id)).ToArray();
    public bool NeedsVessel => Products.Any(p => p.Id == ManufacturingRules.Water);
    /// <summary>Products that go to a gas store: any Manufacturing gas commodity (ammonia). Never vented.</summary>
    public IReadOnlyList<ProductSpec> StoredGases => Products.Where(p => GasStores.FamilyOf(p.Id) != null).ToArray();
    /// <summary>Products that are items in the tray: everything that is not a commodity.</summary>
    public IEnumerable<ProductSpec> Solids(IEnumerable<ProductSpec> products) => products.Where(p => !ChargeCommodities.Is(p.Id));
    /// <summary>Item units the charge binds from the feed bin.</summary>
    public int Units => ItemInputs.Sum(i => i.Count);
    /// <summary>A V4 refinery recipe (the original signature).</summary>
    public ChargeRecipe(string id, int revision, IEnumerable<ChargeInput> inputs, IEnumerable<ProductSpec> products, IReadOnlyDictionary<string, double>? offGas, double seconds, bool melt, bool needsSteelStock)
        : this(ChargeCatalog.Refinery, id, revision, inputs, products, offGas, seconds, melt, needsSteelStock ? new[] { ChargeCatalog.SteelStockRequirement } : Array.Empty<string>(), null, 0) { }
    public ChargeRecipe(string machine, string id, int revision, IEnumerable<ChargeInput> inputs, IEnumerable<ProductSpec> products, IReadOnlyDictionary<string, double>? offGas,
        double seconds, bool melt, IEnumerable<string>? requires, IReadOnlyDictionary<string, double>? circulates, double reactionKWh)
    {
        var ins = inputs.ToArray(); var outs = products.ToArray(); var gas = new Dictionary<string, double>(offGas ?? new Dictionary<string, double>(), StringComparer.Ordinal);
        var loop = new Dictionary<string, double>(circulates ?? new Dictionary<string, double>(), StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(machine) || string.IsNullOrWhiteSpace(id) || revision < 1 || ins.Length == 0 || outs.Length == 0 || !ManufacturingRules.Finite(seconds) ||
            seconds < ProcessJob.MinSeconds || seconds > ProcessJob.MaxSeconds || !ManufacturingRules.Finite(reactionKWh) ||
            ins.Select(i => i.Id).Distinct(StringComparer.Ordinal).Count() != ins.Length || !ins.Any(i => !ChargeCommodities.Is(i.Id)) ||
            outs.Any(p => p == null || string.IsNullOrWhiteSpace(p.Id) || p.Count <= 0 || !ManufacturingRules.Finite(p.Kg) || p.Kg <= 0) ||
            outs.Select(p => p.Id).Distinct(StringComparer.Ordinal).Count() != outs.Length ||
            gas.Any(g => !NativeGasCanister.IsRoomSpecies(g.Key) || !ManufacturingRules.Finite(g.Value) || g.Value <= 0) ||
            loop.Any(c => !ChargeCommodities.Is(c.Key) || !ManufacturingRules.Finite(c.Value) || c.Value <= 0) ||
            melt && (ins.Any(i => ChargeCommodities.Is(i.Id)) || loop.Count > 0))
            throw new ArgumentException("Invalid charge recipe.");
        Machine = machine; Inputs = Array.AsReadOnly(ins); Products = Array.AsReadOnly(outs); OffGas = gas; Circulates = loop; ReactionKWh = reactionKWh;
        Seconds = seconds; Melt = melt; Requires = Array.AsReadOnly((requires ?? Array.Empty<string>()).ToArray());
        Id = id; Revision = revision;
        if (!ProcessMaterial.Balanced(ChargeKg, Products.Select(p => p.Kg * p.Count).Concat(OffGas.Values))) throw new ArgumentException("Charge recipe does not conserve mass: " + id);
    }
    /// <summary>Off-gas due at a progress fraction, given what the job has already released: never more than the
    /// recipe's share, never negative, and nothing at all for a recipe without off-gas.</summary>
    public double OffGasDueKg(double progressFraction, double emittedKg)
    {
        if (!ManufacturingRules.Finite(progressFraction) || !ManufacturingRules.Finite(emittedKg) || emittedKg < 0) throw new ArgumentException("Invalid off-gas progress.");
        double target = OffGasKg * Math.Max(0, Math.Min(1, progressFraction));
        return Math.Max(0, target - emittedKg);
    }
    /// <summary>Splits an amount of off-gas across the recipe's species in their declared proportions.</summary>
    public IEnumerable<KeyValuePair<string, double>> Split(double kg)
    {
        if (!ManufacturingRules.Finite(kg) || kg < 0) throw new ArgumentException("Invalid off-gas amount.");
        double total = OffGasKg;
        foreach (var pair in OffGas) yield return new KeyValuePair<string, double>(pair.Key, total <= 0 ? 0 : kg * pair.Value / total);
    }
    /// <summary>The reaction heat released over a slice of progress (negative when absorbed).</summary>
    public double ReactionKWhOver(double progressSeconds) => Seconds <= 0 ? 0 : ReactionKWh * Math.Max(0, progressSeconds) / Seconds;
}
