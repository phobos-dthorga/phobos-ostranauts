using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;

namespace PhobosManufacturing.Core;

/// <summary>Manufacturing's <c>vessels</c> data pack (<c>framework/vessels.json</c>, Manufacturing 0.13.0): the small
/// size of every gas store family, its capacity, dry housing mass and damaged leak rate. Identities, record names,
/// species and the size ladder stay in code; the pack is checked against the families the code builds.</summary>
public static class Vessels
{
    public const string Schema = VesselSchema.Name, ModFolder = "PhobosManufacturing", Resource = "PhobosManufacturing.vessels.json";
    public const string GasStoreKind = "gas-store";
    public static readonly IReadOnlyList<string> Kinds = new[] { GasStoreKind };
    private static VesselPack? pack;
    public static VesselPack Pack => pack ??= Load();
    public static DataPackSource Source => new(ManufacturingRules.Owner, ModFolder, Schema, typeof(Vessels).Assembly, Resource);
    public static VesselPack Load()
    {
        var families = GasStores.Families;
        pack = DataPacks.Load<VesselPack>(Source, p =>
        {
            VesselSchema.Validate(p, new VesselContext(families.Select(f => f.SmallPrefix).ToArray()) { Kinds = Kinds });
            foreach (var family in families)
                if (p.families[family.SmallPrefix].commodity != family.Commodity)
                    throw new ArgumentException(family.SmallPrefix + " holds " + family.Commodity + " in code; the file says " + p.families[family.SmallPrefix].commodity + ".");
        });
        return pack;
    }
    public static VesselFamilyEntry Entry(string prefix) => Pack.families.TryGetValue(prefix, out var e) ? e : throw new InvalidOperationException("No vessels entry for " + prefix);
}
