using System;
using Phobos.Ostranauts.Framework.Liquids;

namespace PhobosManufacturing.Core;

/// <summary>The Lixivar acid line (Manufacturing 0.24.0; owner decision, 30 September 2026: an acid line, with leak and
/// mist on damage). Ordinary 1 x 1 segments on Framework's shared pattern, in the acid lane, joining the LC-3, the SA-3
/// and the AT acid tanks at their acid ports (the +X side, one row below the gas port). Since Manufacturing 0.25.0
/// (owner decision, 1 October 2026: lines hold their contents until drained) each segment holds its own acid through
/// Framework's <see cref="LineContents"/>: filled from the tanks on its network, drained into a drain canister, and a
/// damaged or destroyed segment releases the tank-damage mist fraction of what it holds into the room.</summary>
public static class AcidLineRules
{
    public const string FamilyId = "PhobosManufacturing.AcidLine", Prefix = "PhobosAcidLine";
    public const string Present = Prefix + "Present", Intact = Prefix + "Intact", Installed = Prefix + "Installed";
    public const string Art = "AcidPipe";
    public const double SegmentKg = 1;
    /// <summary>What one segment holds: Framework's shared 25 mm bore, one metre a tile (0.49 L), of 98% sulfuric acid at
    /// <see cref="LiquidStores.AcidDensityKgPerM3"/>, about 0.90 kg; the mist on damage is the tanks' own
    /// <see cref="LiquidStores.MistFraction"/> as the game's H2SO4. A drain canister holds 20 L, about 36.7 kg.</summary>
    public static LineCommodity HeldAcid() =>
        LineCommodity.Liquid(LiquidStores.SulfuricAcid, LiquidStores.AcidDensityKgPerM3, mistSpecies: LiquidStores.AcidFamily.MistSpecies, mistFraction: LiquidStores.MistFraction);
    public static bool IsFamily(string? definition) => definition != null && definition.StartsWith(Prefix, StringComparison.Ordinal);
}
