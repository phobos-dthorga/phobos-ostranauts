using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Registration;

namespace PhobosShipbreaker.Core;

/// <summary>One size of the material bin: Y2 (2 x 2), Y3 (3 x 3) or Y4 (4 x 4), scaled from the Y2 by Framework's
/// shared size ladder for footprint, housing mass and price. Its grid keeps four cells per floor tile, as the D4
/// tray and the game's largest equipment truck do.</summary>
public sealed class BinSize
{
    public VesselSize Size { get; }
    public string Prefix { get; }
    public string Installed => Prefix + "Installed";
    public int Footprint { get; }
    /// <summary>The square inventory grid's side, in cells.</summary>
    public int Grid => Footprint * BinRules.CellsPerTileSide;
    /// <summary>Scaled from the Y2's housing in the vessels data pack (Shipbreaker 0.47.0) through the Framework ladder.</summary>
    public double DryKg => BulkVesselSizes.Scale(BinRules.DryKg, BulkVesselSizes.DryFactor(BinRules.Footprint, Size));
    public double Price { get; }
    /// <summary>The translation key of this size's name (its own Rivetline model in the naming map).</summary>
    public string NameKey => "Bin.name" + (Size == VesselSize.Small ? "" : "_" + Size.ToString().ToLowerInvariant());
    public string Art => Prefix;
    internal BinSize(VesselSize size)
    {
        Size = size; Prefix = BulkVesselSizes.Prefix(BinRules.Prefix, size); Footprint = BulkVesselSizes.Footprint(BinRules.Footprint, size);
        Price = BulkVesselSizes.Scale(BinRules.Price, BulkVesselSizes.PriceFactor(BinRules.Footprint, size));
    }
    public bool IsFamily(string? id) => EquipmentIdentity.IsFamily(id, Prefix);
}

/// <summary>The Rivetline material bins: passive, sealed, unpowered stores for mined material (ore, loose regolith,
/// gangue, water and methane ice, ice gangue and mined chunks such as clay hydrates). Unlike the silos they hold
/// real items in an ordinary inventory grid, so every block keeps its own identity and mass; crew hauling, loading
/// orders and ship-wide searches use them like any unlocked container. Owner decision, 30 September 2026.</summary>
public static class BinRules
{
    public const string Prefix = "PhobosMaterialBin", Installed = Prefix + "Installed";
    /// <summary>What a bin admits at the game level: the game's own solid-container rule and its own
    /// mined-material rule (IsOre, IsMineral or IsIce), the one the government kiosks buy by.</summary>
    public const string Trigger = "PhobosMaterialBinTFit", NativeMiningOutput = "TIsMiningOutput", NativeSolid = "TIsFitContainerSolid";
    public const int Footprint = 2;
    // Authored: a 2 x 2 sealed bin, four cells per tile, in a 60 kg housing, about half the game's Storage Bay per cell (framework/vessels.json).
    public static int CellsPerTileSide => ShipbreakerVessels.Entry(Prefix).cellsPerTileSide ?? 1;
    public static double DryKg => ShipbreakerVessels.Entry(Prefix).dryKg;
    public const double Price = 2400;
    /// <summary>Any size of the bin family.</summary>
    public static bool IsFamily(string? id) => BulkVesselSizes.InLadder(id, Prefix);
    /// <summary>The Y2, Y3 and Y4, smallest first.</summary>
    public static readonly IReadOnlyList<BinSize> Sizes = BulkVesselSizes.All.Select(s => new BinSize(s)).ToArray();
    public static BinSize? For(string? id) => Sizes.FirstOrDefault(s => s.IsFamily(id));
}
