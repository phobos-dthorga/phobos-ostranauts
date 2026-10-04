namespace PhobosAgriculture.Core;

/// <summary>Beet sugar at the B2 (Agriculture 0.46.0). One 0.5 kg sugar beet is authored as 75% water, 17% sucrose, 5% pulp
/// fibre and 3% other solubles and minerals, within the 75 to 80% water and 15 to 20% sucrose reported for sugar beet roots
/// (University of California, Davis, Sugar Beet as a Biofuel Feedstock; ScienceDirect sugar beet overview). Diffusion takes
/// about 98% of the sucrose and some 10 to 15% of it stays in molasses, so the bench crystallises 70 g of white sugar, 82%
/// of the root's sucrose. The pulp, the molasses sugar, the other solubles and all the water leave as one recorded residue
/// the straw press takes. Mass is conserved to the gram; evaporating the diffusion juice is folded into the bench's work.</summary>
public static class SugarExtraction
{
    public const string Beet = "PhobosVerdemorrowSugarBeets", Sugar = "PhobosVerdemorrowBeetSugar";
    public const double BeetKg = .5, SugarKg = .07;
    /// <summary>The beet's make-up (authored within the cited ranges).</summary>
    public const double BeetWaterKg = .375, BeetSucroseKg = .085, BeetMineralsKg = .004;
    /// <summary>The pulp: everything but the sugar, with all the water and minerals; its organic matter is the fibre, the
    /// sucrose left in molasses and the other solubles.</summary>
    public const double PulpKg = BeetKg - SugarKg, PulpMineralsKg = BeetMineralsKg, PulpOrganicKg = BeetKg - BeetWaterKg - BeetMineralsKg - SugarKg;
    /// <summary>Electricity per beet (authored): slicing, hot diffusion and boiling the juice down, about eighteen minutes at
    /// the bench's 0.5 kW.</summary>
    public const double KWh = .15;
}
