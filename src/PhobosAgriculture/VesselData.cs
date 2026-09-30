using System;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Data;

namespace PhobosAgriculture;

/// <summary>Agriculture's <c>vessels</c> data pack (<c>framework/vessels.json</c>, Agriculture 0.23.0): the R3
/// reservoir's and (0.27.0) the E2 nutrient hopper's capacity and dry mass. The W2 supply is not in it: its 20 kg is the rack reservoir the crop model
/// is written for (<c>CropState.ReservoirKg</c>), not a free vessel rating. Identities and record names stay in code.</summary>
internal static class AgricultureVessels
{
    internal const string Schema = VesselSchema.Name, ModFolder = "PhobosAgriculture", Resource = "PhobosAgriculture.vessels.json";
    internal const string ReservoirKind = "reservoir";
    internal static readonly IReadOnlyList<string> Kinds = new[] { ReservoirKind, Core.HopperRules.Kind };
    internal static IReadOnlyList<string> Ids => new[] { BulkDefinitions.Tank, Core.HopperRules.Prefix };
    private static VesselPack? pack;
    internal static VesselPack Pack => pack ??= Load();
    internal static DataPackSource Source => new(Text.Owner, ModFolder, Schema, typeof(AgricultureVessels).Assembly, Resource);
    internal static VesselPack Load()
    {
        pack = DataPacks.Load<VesselPack>(Source, p =>
        {
            VesselSchema.Validate(p, new VesselContext(Ids) { Kinds = Kinds });
            if (p.families[BulkDefinitions.Tank].commodity != BulkDefinitions.Commodity) throw new ArgumentException(Text.Get("vessels_commodity", BulkDefinitions.Tank, BulkDefinitions.Commodity));
            if (p.families[Core.HopperRules.Prefix].commodity != Core.HopperRules.Commodity) throw new ArgumentException(Text.Get("vessels_commodity", Core.HopperRules.Prefix, Core.HopperRules.Commodity));
        });
        return pack;
    }
    internal static VesselFamilyEntry Entry(string prefix) => Pack.families.TryGetValue(prefix, out var e) ? e : throw new InvalidOperationException("No vessels entry for " + prefix);
}
