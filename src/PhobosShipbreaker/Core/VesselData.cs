using System;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Data;

namespace PhobosShipbreaker.Core;

/// <summary>Shipbreaker's <c>vessels</c> data pack (<c>framework/vessels.json</c>, Shipbreaker 0.47.0): the S3
/// process water silo (capacity and dry mass) and the Y2 material bin (dry mass and grid density). Identities,
/// record names, the water commodity and the size ladder stay in code.</summary>
public static class ShipbreakerVessels
{
    public const string Schema = VesselSchema.Name, ModFolder = "PhobosShipbreaker", Resource = "PhobosShipbreaker.vessels.json";
    public const string SiloKind = "silo", BinKind = "bin";
    public static readonly IReadOnlyList<string> Kinds = new[] { SiloKind, BinKind };
    public static IReadOnlyList<string> Ids => new[] { SiloRules.Prefix, BinRules.Prefix };
    private static VesselPack? pack;
    public static VesselPack Pack => pack ??= Load();
    public static DataPackSource Source => new(Text.Owner, ModFolder, Schema, typeof(ShipbreakerVessels).Assembly, Resource);
    public static VesselPack Load()
    {
        pack = DataPacks.Load<VesselPack>(Source, p =>
        {
            VesselSchema.Validate(p, new VesselContext(Ids) { Kinds = Kinds });
            if (p.families[SiloRules.Prefix].kind != SiloKind || p.families[SiloRules.Prefix].commodity != SiloRules.Commodity)
                throw new ArgumentException(Text.Get("Vessels.silo_kind", SiloRules.Prefix, SiloKind, SiloRules.Commodity));
            if (p.families[BinRules.Prefix].kind != BinKind) throw new ArgumentException(Text.Get("Vessels.bin_kind", BinRules.Prefix, BinKind));
        });
        return pack;
    }
    public static VesselFamilyEntry Entry(string prefix) => Pack.families.TryGetValue(prefix, out var e) ? e : throw new InvalidOperationException("No vessels entry for " + prefix);
}
