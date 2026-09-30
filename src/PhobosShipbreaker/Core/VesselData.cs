using System;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Data;

namespace PhobosShipbreaker.Core;

/// <summary>Shipbreaker's <c>vessels</c> data pack (<c>framework/vessels.json</c>, Shipbreaker 0.47.0): the Y2 material
/// bin (dry mass and grid density). The S3 silo's ratings moved to Framework's own pack with the tanks (Shipbreaker
/// 0.54.0). Identities, record names and the size ladder stay in code.</summary>
public static class ShipbreakerVessels
{
    public const string Schema = VesselSchema.Name, ModFolder = "PhobosShipbreaker", Resource = "PhobosShipbreaker.vessels.json";
    public const string BinKind = "bin";
    public static readonly IReadOnlyList<string> Kinds = new[] { BinKind };
    public static IReadOnlyList<string> Ids => new[] { BinRules.Prefix };
    private static VesselPack? pack;
    public static VesselPack Pack => pack ??= Load();
    public static DataPackSource Source => new(Text.Owner, ModFolder, Schema, typeof(ShipbreakerVessels).Assembly, Resource);
    public static VesselPack Load()
    {
        pack = DataPacks.Load<VesselPack>(Source, p =>
        {
            VesselSchema.Validate(p, new VesselContext(Ids) { Kinds = Kinds });
            if (p.families[BinRules.Prefix].kind != BinKind) throw new ArgumentException(Text.Get("Vessels.bin_kind", BinRules.Prefix, BinKind));
        });
        return pack;
    }
    public static VesselFamilyEntry Entry(string prefix) => Pack.families.TryGetValue(prefix, out var e) ? e : throw new InvalidOperationException("No vessels entry for " + prefix);
}
