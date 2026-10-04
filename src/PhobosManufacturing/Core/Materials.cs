using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;

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
/// stock with real consumers, terminal remainders, and the minable clay hydrate and ammonium salt crust chunks.
/// Their masses, prices, stacks, categories and art live in the <c>materials</c> data pack
/// (<c>framework/materials.json</c>, Manufacturing 0.12.0) and are read through the Framework loader; the
/// identities and the donor each clones stay here. Stock is ordinary-priced raw material (owner, 29 September
/// 2026: only machinery is late-game); the pricing review is recorded in AGENTS.md under Refining value.</summary>
public static class Materials
{
    public const string NickelIronIngot = "PhobosNickelIronIngot", CarbonStock = "PhobosCarbonStock", RefinerySlag = "PhobosRefinerySlag",
        AnhydrousResidue = "PhobosAnhydrousResidue", ClayHydrates = "PhobosClayHydrates", AmmoniumSaltCrust = "PhobosAmmoniumSaltCrust", SpentSaltCake = "PhobosSpentSaltCake";
    /// <summary>Feedstock round three (Manufacturing 0.18.0): the mined evaporite crust, the LC-3's products and
    /// intermediates, and one terminal remainder per feed (brine salt cake, caustic remainder, calcined residue).</summary>
    public const string EvaporiteCrust = "PhobosEvaporiteCrust", PotassiumSulfate = "PhobosPotassiumSulfate", PhosphateConcentrate = "PhobosPhosphateConcentrate",
        LeachedResidue = "PhobosLeachedResidue", Struvite = "PhobosStruvite", BrineSaltCake = "PhobosBrineSaltCake", CausticRemainder = "PhobosCausticRemainder",
        CalcinedResidue = "PhobosCalcinedResidue";
    /// <summary>The acid round (Manufacturing 0.19.0): the mined sulfide-phosphide nodule, the SA-3's phosphoric acid
    /// flask and its terminal roasted calcine.</summary>
    public const string SulfideNodule = "PhobosSulfideNodule", PhosphoricAcidFlask = "PhobosPhosphoricAcidFlask", RoastedCalcine = "PhobosRoastedCalcine";
    /// <summary>The acid consumers (Manufacturing 0.20.0): Epsom salt from olivine, the acid-route struvite's ammonium
    /// sulfate and the olivine's terminal leach cake.</summary>
    public const string EpsomSalt = "PhobosEpsomSalt", AmmoniumSulfate = "PhobosAmmoniumSulfate", OlivineLeachCake = "PhobosOlivineLeachCake";
    /// <summary>The mined iron chain's own end product (Manufacturing 0.26.0; owner decision, 1 October 2026): nickel steel
    /// from the V4, separate from Shipbreaker's plain steel ingot.</summary>
    public const string NickelSteelIngot = "PhobosNickelSteelIngot";
    /// <summary>The interdependency first slice (Manufacturing 0.27.0): carbon black from methane pyrolysis, the carbon
    /// burner's feed, and the terminal remainder of cartridge reactivation.</summary>
    public const string CarbonBlack = "PhobosCarbonBlack", ExhaustedSorbent = "PhobosExhaustedSorbent";
    /// <summary>Agriculture's straw in the V4 (Manufacturing 0.37.0): the terminal ash of one burned or charred bale.</summary>
    public const string PlantAsh = "PhobosPlantAsh";
    /// <summary>The fermenter-still (Manufacturing 0.39.0): the terminal spent mash of a beet or sugar charge.</summary>
    public const string SpentMash = "PhobosSpentMash";
    /// <summary>The technical minimum price of a terminal remainder (authoring rule).</summary>
    public const double TerminalPrice = .01;
    public const string Schema = MaterialSchema.Name, Resource = "PhobosManufacturing.materials.json", Stock = "stock", MinedKind = "mined";
    /// <summary>Every material, in definition order.</summary>
    public static readonly IReadOnlyList<string> Ids = new[] { NickelIronIngot, CarbonStock, RefinerySlag, AnhydrousResidue, ClayHydrates, AmmoniumSaltCrust, SpentSaltCake,
        EvaporiteCrust, PotassiumSulfate, PhosphateConcentrate, LeachedResidue, Struvite, BrineSaltCake, CausticRemainder, CalcinedResidue,
        SulfideNodule, PhosphoricAcidFlask, RoastedCalcine, EpsomSalt, AmmoniumSulfate, OlivineLeachCake, NickelSteelIngot,
        CarbonBlack, ExhaustedSorbent, PlantAsh, SpentMash };
    public static readonly IReadOnlyList<string> Kinds = new[] { Stock, MinedKind };
    private static MaterialPack? pack; private static IReadOnlyList<Material>? all; private static MaterialPack? builtFrom;
    public static MaterialPack Pack => pack ??= Load();
    public static DataPackSource Source => new(ManufacturingRules.Owner, Economy.ModFolder, Schema, typeof(Materials).Assembly, Resource);
    /// <summary>Reads the shipped pack and any player files, validated against the ids and kinds the code knows.</summary>
    public static MaterialPack Load()
    {
        pack = DataPacks.Load<MaterialPack>(Source, p => MaterialSchema.Validate(p, new MaterialContext(Ids) { Kinds = Kinds }));
        return pack;
    }
    public static IReadOnlyList<Material> All
    {
        get
        {
            if (all != null && ReferenceEquals(builtFrom, Pack)) return all;
            builtFrom = Pack;
            return all = Array.AsReadOnly(Ids.Select(id => { var e = builtFrom.materials[id]; return new Material(id, e.kg, e.stack, e.price, e.category ?? "", e.terminal, e.side, e.art ?? "", e.kind == MinedKind); }).ToArray());
        }
    }
    public static Material Ingot => ById(NickelIronIngot)!;
    public static Material Carbon => ById(CarbonStock)!;
    public static Material Slag => ById(RefinerySlag)!;
    public static Material Residue => ById(AnhydrousResidue)!;
    public static Material Clay => ById(ClayHydrates)!;
    public static Material Crust => ById(AmmoniumSaltCrust)!;
    public static Material SaltCake => ById(SpentSaltCake)!;
    public static double IngotKg => Ingot.Kg;
    public static double CarbonKg => Carbon.Kg;
    public static double SlagKg => Slag.Kg;
    public static double ResidueKg => Residue.Kg;
    public static double ClayKg => Clay.Kg;
    public static double CrustKg => Crust.Kg;
    public static double SaltCakeKg => SaltCake.Kg;
    public static double IngotPrice => Ingot.Price;
    public static double CarbonPrice => Carbon.Price;
    public static Material? ById(string? id) { foreach (var m in All) if (m.Id == id) return m; return null; }
    /// <summary>A material's unit mass, for recipe checks; null for an id that is not ours.</summary>
    public static double? KgOf(string? id) => ById(id)?.Kg;
    /// <summary>Raw stock the V4 takes (ingots and carbon); the LC-3's salts are stock too but feed only the LC-3.</summary>
    public static bool IsStock(string? id) => id == NickelIronIngot || id == CarbonStock;
    public static bool IsTerminal(string? id) => ById(id)?.Terminal == true;
}
