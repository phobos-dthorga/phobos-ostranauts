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
    public Material(string id, double kg, int stack, double price, string category, bool terminal, int side, string art)
    {
        if (string.IsNullOrWhiteSpace(id) || !ManufacturingRules.Finite(kg) || kg <= 0 || stack < 1 || !ManufacturingRules.Finite(price) || price <= 0 || side < 1)
            throw new ArgumentException("Invalid material.");
        Id = id; Kg = kg; Stack = stack; Price = price; Category = category; Terminal = terminal; Side = side; Art = art;
    }
}

/// <summary>The materials Manufacturing adds (owner decisions, 29 September 2026): cast nickel-iron and carbon
/// stock with real consumers, two terminal remainders, and one minable CI-type clay hydrate chunk. Prices keep
/// the recovery rule: four nickel-iron ingots ($80) are worth less than the $450 meteoric iron block, five
/// carbon units ($50) plus water less than the $99 carbide ore; the clay chunk sells like the game's hydrates.</summary>
public static class Materials
{
    public const string NickelIronIngot = "PhobosNickelIronIngot", CarbonStock = "PhobosCarbonStock", RefinerySlag = "PhobosRefinerySlag",
        AnhydrousResidue = "PhobosAnhydrousResidue", ClayHydrates = "PhobosClayHydrates";
    public const double IngotKg = 4, CarbonKg = 1, SlagKg = 1, ResidueKg = 8, ClayKg = 10;
    public const double TerminalPrice = .01;
    public static readonly Material Ingot = new(NickelIronIngot, IngotKg, 10, 20, "IsCategoryMetals", false, 1, "StockNickelIronIngot");
    public static readonly Material Carbon = new(CarbonStock, CarbonKg, 10, 10, "IsCategoryIndustrialProducts", false, 1, "StockCarbon");
    public static readonly Material Slag = new(RefinerySlag, SlagKg, 10, TerminalPrice, "IsCategoryTrash", true, 1, "StockRefinerySlag");
    public static readonly Material Residue = new(AnhydrousResidue, ResidueKg, 1, TerminalPrice, "IsCategoryTrash", true, 2, "StockAnhydrousResidue");
    /// <summary>Mined, never sold: a chunk the dark-regolith rock tables and C-class deposits can yield.</summary>
    public static readonly Material Clay = new(ClayHydrates, ClayKg, 6, 180, "IsCategoryOre", false, 1, "");
    public static readonly IReadOnlyList<Material> All = Array.AsReadOnly(new[] { Ingot, Carbon, Slag, Residue, Clay });
    public static Material? ById(string? id) { foreach (var m in All) if (m.Id == id) return m; return null; }
    public static bool IsStock(string? id) => id == NickelIronIngot || id == CarbonStock;
    public static bool IsTerminal(string? id) => id == RefinerySlag || id == AnhydrousResidue;
}
