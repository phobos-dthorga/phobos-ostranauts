using System;

namespace PhobosManufacturing.Core;

/// <summary>The Lixivar acid line (Manufacturing 0.24.0; owner decision, 30 September 2026: an acid line, with leak and
/// mist on damage). Ordinary 1 x 1 segments on Framework's shared pattern, in the acid lane, joining the LC-3, the SA-3
/// and the AT acid tanks at their acid ports (the +X side, one row below the gas port). Like every Phobos line it holds
/// nothing between transfers in the accounts; a segment that joins a tank to a machine linked to it counts as wetted,
/// and a damaged or destroyed wetted segment spills its hold-up.</summary>
public static class AcidLineRules
{
    public const string FamilyId = "PhobosManufacturing.AcidLine", Prefix = "PhobosAcidLine";
    public const string Present = Prefix + "Present", Intact = Prefix + "Intact", Installed = Prefix + "Installed";
    public const string Art = "AcidPipe";
    public const double SegmentKg = 1;
    /// <summary>The acid a wetted segment holds, authored: one metre of 25 mm bore line holds 0.49 L, about 0.9 kg of
    /// 98% sulfuric acid at <see cref="LiquidStores.AcidDensityKgPerM3"/>; rounded to 1 kg a segment.</summary>
    public const double HoldUpKg = 1;
    public static bool IsFamily(string? definition) => definition != null && definition.StartsWith(Prefix, StringComparison.Ordinal);

    /// <summary>A wetted segment's spill, drawn from the tank that feeds it: up to the hold-up, of which the tank-damage
    /// mist fraction reaches the room and the rest is held in the tank's bund. Mass is conserved:
    /// <c>Mist + Bund == Released &lt;= serviceKg</c>.</summary>
    public static (double Released, double Mist, double Bund) Spill(double serviceKg)
    {
        double released = !ManufacturingRules.Finite(serviceKg) || serviceKg <= 0 ? 0 : Math.Min(HoldUpKg, serviceKg);
        double mist = LiquidStores.MistKg(released);
        return (released, mist, released - mist);
    }
}
