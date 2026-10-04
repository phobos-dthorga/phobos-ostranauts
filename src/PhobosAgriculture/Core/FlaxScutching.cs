namespace PhobosAgriculture.Core;

/// <summary>Flax scutching at the B2 (Agriculture 0.45.0). One retted 0.25 kg flax straw bundle is authored as 88% organic
/// matter, 2% minerals and 10% water. Breaking, scutching and hackling free its long fibre, which the bench works into two
/// of the game's own clean scrap cloth (25 g each: 20% of the straw, between the about 15% after industrial hackling and
/// 25% after scutching reported for flax in Industrial Crops and Products, 2021). The woody shives, short tow, minerals and
/// water leave as one recorded residue the straw press takes. Mass is conserved to the gram; the cloth stands in for
/// spinning and weaving, which the game has no items for.</summary>
public static class FlaxScutching
{
    public const string Straw = "PhobosVerdemorrowFlaxStraw", Cloth = "ItmScrapClothClean";
    public const double StrawKg = .25, ClothKg = .025;
    public const int ClothCount = 2;
    /// <summary>The shives: everything but the cloth, with all the straw's minerals and water.</summary>
    public const double ShivesKg = StrawKg - ClothCount * ClothKg, ShivesMineralsKg = .005, ShivesOrganicKg = .17;
    /// <summary>Electricity per bundle (authored): a small breaker and scutcher, a few minutes at the bench's 0.5 kW.</summary>
    public const double KWh = .05;
}
