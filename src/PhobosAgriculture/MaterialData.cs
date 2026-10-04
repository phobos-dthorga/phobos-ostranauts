using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;
using PhobosAgriculture.Core;

namespace PhobosAgriculture;

/// <summary>Agriculture's <c>materials</c> data pack (<c>framework/materials.json</c>, Agriculture 0.25.0): seeds,
/// nutrients, irrigation charges, produce, meals, recovery supplies and the recorded wastes, each with its mass,
/// price, stack, market category and art. Identities, translation keys and the donor each clones stay in code; masses
/// the crop, recovery and workup models are written for are bound to their constants.</summary>
internal static class AgricultureMaterials
{
    internal const string Schema = MaterialSchema.Name, ModFolder = "PhobosAgriculture", Resource = "PhobosAgriculture.materials.json";
    internal const string Stock = "stock", Food = "food", Waste = "waste";
    internal static readonly IReadOnlyList<string> Kinds = new[] { Stock, Food, Waste };
    /// <summary>Every material the code builds: the fixed supplies and wastes, and the crop items the shipped crops pack
    /// names (read from its text, so the two packs can check each other without loading in a circle).</summary>
    internal static IReadOnlyList<string> Ids => new[]
    {
        RecyclerCapture.Wet, Definitions.Nutrient,
        Definitions.Residue, Definitions.Drainage, Service.CharacterizedDrainage, Service.RecoveryReject, Service.RecoveryCartridge, Definitions.Irrigation,
        WorkupDefinitions.Residue, WorkupDefinitions.Concentrate, WorkupDefinitions.Spent, WorkupDefinitions.Makeup, WorkupDefinitions.Mixture, BulkDefinitions.Nutrients
    }.Concat(Crops.ShippedItemIds()).ToArray();
    private static MaterialPack? pack;
    internal static MaterialPack Pack => pack ??= Load();
    internal static DataPackSource Source => new(Text.Owner, ModFolder, Schema, typeof(AgricultureMaterials).Assembly, Resource);
    internal static MaterialPack Load()
    {
        pack = DataPacks.Load<MaterialPack>(Source, p =>
        {
            MaterialSchema.Validate(p, new MaterialContext(Ids) { Kinds = Kinds });
            // Masses and prices the crop, recovery and workup models are written for.
            foreach (var bound in new[] {
                (Definitions.Irrigation, Definitions.IrrigationKg, (double?)null), (Definitions.Nutrient, Definitions.NutrientKg, null),
                (BulkDefinitions.Nutrients, BulkDefinitions.NutrientKg, null), (Service.RecoveryCartridge, DrainageRecovery.CartridgeKg, TreatmentCartridge.FullPrice),
                (WorkupDefinitions.Makeup, NutrientRecovery.MakeupKg, NutrientRecovery.MakeupPrice) })
            {
                var m = p.materials[bound.Item1];
                if (Math.Abs(m.kg - bound.Item2) > 1e-9 || (bound.Item3 is double price && Math.Abs(m.price - price) > 1e-9))
                    throw new ArgumentException(Text.Get("materials_bound", bound.Item1, bound.Item2, bound.Item3 ?? m.price));
            }
        });
        return pack;
    }
    internal static MaterialEntry Entry(string id) => Pack.materials.TryGetValue(id, out var e) ? e : throw new InvalidOperationException("No materials entry for " + id);
    internal static double Price(string id) => Entry(id).price;
    /// <summary>A material's unit mass, for crop and recipe checks; null for an id that is not ours.</summary>
    internal static double? KgOf(string? id) => id != null && Pack.materials.TryGetValue(id, out var e) ? e.kg : null;
}
