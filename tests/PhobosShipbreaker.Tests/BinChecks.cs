using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Liquids;
using PhobosShipbreaker.Core;

/// <summary>The Rivetline material bin ladder on numbers alone: Y2, Y3 and Y4 footprints, grids, housings and prices.</summary>
internal static class BinChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var sizes = BinRules.Sizes;
        check(sizes.Count == 3 && sizes.Select(s => s.Prefix).SequenceEqual(new[] { "PhobosMaterialBin", "PhobosMaterialBinMedium", "PhobosMaterialBinLarge" }),
            "Three bin sizes; the small keeps the family prefix");
        check(sizes.Select(s => s.Footprint).SequenceEqual(new[] { 2, 3, 4 }) && sizes.Select(s => s.Grid).SequenceEqual(new[] { 4, 6, 8 }),
            "Y2, Y3 and Y4 are 2, 3 and 4 tiles wide with 4, 6 and 8 cell grids");
        check(sizes.All(s => s.Grid * s.Grid == 4 * s.Footprint * s.Footprint), "Every bin keeps four grid cells per floor tile");
        check(sizes.Select(s => s.DryKg).SequenceEqual(new[] { 60d, 115, 170 }) && sizes.Select(s => s.Price).SequenceEqual(new[] { 2400d, 3900, 5510 }),
            "Housings and prices follow the shared size ladder from the Y2's 60 kg and 2,400");
        check(sizes.Zip(sizes.Skip(1), (a, b) => b.Price / (b.Grid * b.Grid) < a.Price / (a.Grid * a.Grid)).All(x => x), "Bigger bins cost less per cell");
        check(sizes.Select(s => s.NameKey).SequenceEqual(new[] { "Bin.name", "Bin.name_medium", "Bin.name_large" }), "Each size has its own name key");
        check(BinRules.IsFamily("PhobosMaterialBinLargeInstalledDmg") && BinRules.For("PhobosMaterialBinMediumLoose")?.Size == VesselSize.Medium &&
              !BinRules.IsFamily("PhobosProcessSiloInstalled") && BinRules.For(null) == null, "Definitions resolve to their bin size by exact prefix");
    }
}
