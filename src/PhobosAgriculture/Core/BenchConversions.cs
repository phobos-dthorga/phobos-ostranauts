using System;
using System.Collections.Generic;

namespace PhobosAgriculture.Core;

/// <summary>A B2 job that turns one crop item into a product and a recorded residue for the straw press (Agriculture
/// 0.46.0): flax scutching and beet sugar. Each conserves the input's mass to the gram.</summary>
public sealed class BenchConversion
{
    public string Mode { get; }
    public string Action { get; }
    public string Input { get; }
    public double InputKg { get; }
    public string Product { get; }
    public double ProductKg { get; }
    public int ProductCount { get; }
    public double ResidueKg { get; }
    public double ResidueMineralsKg { get; }
    public double ResidueOrganicKg { get; }
    public double KWh { get; }
    public BenchConversion(string mode, string action, string input, double inputKg, string product, double productKg, int productCount,
        double residueMineralsKg, double residueOrganicKg, double kwh)
    {
        Mode = mode; Action = action; Input = input; InputKg = inputKg; Product = product; ProductKg = productKg; ProductCount = productCount;
        ResidueKg = inputKg - productCount * productKg; ResidueMineralsKg = residueMineralsKg; ResidueOrganicKg = residueOrganicKg; KWh = kwh;
        if (productCount < 1 || ResidueKg <= 0 || residueMineralsKg < 0 || residueOrganicKg <= 0 || residueMineralsKg + residueOrganicKg > ResidueKg + 1e-12 || kwh <= 0)
            throw new ArgumentException("Invalid bench conversion: " + mode);
    }
}

public static class BenchConversions
{
    public static readonly BenchConversion Scutch = new("scutch", "scutch-flax", FlaxScutching.Straw, FlaxScutching.StrawKg, FlaxScutching.Cloth, FlaxScutching.ClothKg,
        FlaxScutching.ClothCount, FlaxScutching.ShivesMineralsKg, FlaxScutching.ShivesOrganicKg, FlaxScutching.KWh);
    public static readonly BenchConversion Sugar = new("sugar", "extract-sugar", SugarExtraction.Beet, SugarExtraction.BeetKg, SugarExtraction.Sugar, SugarExtraction.SugarKg,
        1, SugarExtraction.PulpMineralsKg, SugarExtraction.PulpOrganicKg, SugarExtraction.KWh);
    public static readonly IReadOnlyList<BenchConversion> All = new[] { Scutch, Sugar };
    public static BenchConversion? ForMode(string? mode) { foreach (var c in All) if (c.Mode == mode) return c; return null; }
    public static BenchConversion? ForAction(string? action) { foreach (var c in All) if (c.Action == action) return c; return null; }
}
