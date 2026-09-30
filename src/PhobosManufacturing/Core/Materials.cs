using System;
using System.Collections.Generic;

namespace PhobosManufacturing.Core;

/// <summary>One Manufacturing-owned material: its definition id, unit mass, stack limit, base price and the
/// native market category it carries. Terminal remainders are trash at the technical minimum price and are
/// never processed again.</summary>
public sealed class Material
{
    public string Id { get; }
    public double Kg { get; }
    public int Stack { get; }
    public double Price { get; }
    public string Category { get; }
    public bool Terminal { get; }
    public int Side { get; }
    public string Art { get; }
    /// <summary>A mined chunk: it clones the game's hydrates so it mines, breaks, stacks and sells like ore.</summary>
    public bool Mined { get; }
    public Material(string id, double kg, int stack, double price, string category, bool terminal, int side, string art, bool mined = false)
    {
        if (string.IsNullOrWhiteSpace(id) || !ManufacturingRules.Finite(kg) || kg <= 0 || stack < 1 || !ManufacturingRules.Finite(price) || price <= 0 || side < 1)
            throw new ArgumentException("Invalid material.");
        Id = id; Kg = kg; Stack = stack; Price = price; Category = category; Terminal = terminal; Side = side; Art = art; Mined = mined;
    }
}

/// <summary>The materials Manufacturing adds (owner decisions, 29 September 2026): cast nickel-iron and carbon
/// stock with real consumers, two terminal remainders, and one minable CI-type clay hydrate chunk. Every charge
/// loses value (the recovery rule), checked natively against live prices: four nickel-iron ingots ($96) are
/// worth less than the $450 meteoric iron block, five carbon units ($50) plus water less than the $99 carbide
/// ore, and four Rivetline steel ingots ($100) less than the four nickel-iron ingots and carbon they come from
/// ($106). Stock is ordinary-priced raw material (owner, 29 September 2026: only machinery is late-game); the
/// vanilla ore prices are its ceiling. The clay chunk sells like the game's hydrates.</summary>
public static class Materials
{
    public const string NickelIronIngot = "PhobosNickelIronIngot", CarbonStock = "PhobosCarbonStock", RefinerySlag = "PhobosRefinerySlag",
        AnhydrousResidue = "PhobosAnhydrousResidue", ClayHydrates = "PhobosClayHydrates", AmmoniumSaltCrust = "PhobosAmmoniumSaltCrust", SpentSaltCake = "PhobosSpentSaltCake";
    public const double IngotKg = 4, CarbonKg = 1, SlagKg = 1, ResidueKg = 8, ClayKg = 10, CrustKg = 10, SaltCakeKg = 7.305;
    public const double TerminalPrice = .01;
    public const double IngotPrice = 24, CarbonPrice = 10;
    public static readonly Material Ingot = new(NickelIronIngot, IngotKg, 10, IngotPrice, "IsCategoryMetals", false, 1, "StockNickelIronIngot");
    public static readonly Material Carbon = new(CarbonStock, CarbonKg, 10, CarbonPrice, "IsCategoryIndustrialProducts", false, 1, "StockCarbon");
    public static readonly Material Slag = new(RefinerySlag, SlagKg, 10, TerminalPrice, "IsCategoryTrash", true, 1, "StockRefinerySlag");
    public static readonly Material Residue = new(AnhydrousResidue, ResidueKg, 1, TerminalPrice, "IsCategoryTrash", true, 2, "StockAnhydrousResidue");
    /// <summary>Mined, never sold: a chunk the dark-regolith rock tables and C-class deposits can yield.</summary>
    public static readonly Material Clay = new(ClayHydrates, ClayKg, 6, 180, "IsCategoryOre", false, 1, "", mined: true);
    /// <summary>Mined, never sold: an ammonium chloride and sodium carbonate salt crust of the kind NASA's Dawn mission found in
    /// Ceres' bright areas (Manufacturing 0.9.0). It sells like the game's hydrates.</summary>
    public static readonly Material Crust = new(AmmoniumSaltCrust, CrustKg, 6, 150, "IsCategoryOre", false, 1, "StockAmmoniumSaltCrust", mined: true);
    /// <summary>What the V4 leaves of a salt crust: sodium chloride and the crust's clay, terminal.</summary>
    public static readonly Material SaltCake = new(SpentSaltCake, SaltCakeKg, 1, TerminalPrice, "IsCategoryTrash", true, 1, "StockSpentSaltCake");
    public static readonly IReadOnlyList<Material> All = Array.AsReadOnly(new[] { Ingot, Carbon, Slag, Residue, Clay, Crust, SaltCake });
    public static Material? ById(string? id) { foreach (var m in All) if (m.Id == id) return m; return null; }
    public static bool IsStock(string? id) => id == NickelIronIngot || id == CarbonStock;
    public static bool IsTerminal(string? id) => ById(id)?.Terminal == true;
}
