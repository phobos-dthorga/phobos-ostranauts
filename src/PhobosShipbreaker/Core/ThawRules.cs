using System;
using Phobos.Ostranauts.Framework.Processing;
using Phobos.Ostranauts.Framework.Registration;

namespace PhobosShipbreaker.Core;

/// <summary>The T2 ice thaw unit: one whole block of the game's water ice becomes process water in a linked
/// vessel plus one native gangue packet in the unit's tray. The recipe is immutable; the water product is
/// delivered as vessel contents, never as an item.</summary>
public static class ThawRules
{
    public const string Prefix = "PhobosIceThaw", Installed = Prefix + "Installed";
    public const string InputBin = Prefix + "InputBin", InputSlot = Prefix + "Input";
    /// <summary>Exact identities: water ice here; methane ice below. Gangue is not feed.</summary>
    public const string Ice = "ItmIce01", Gangue = "ItmIceTrash01", Commodity = SiloRules.Commodity;
    /// <summary>The thaw unit's outlet and the port any registered water vessel offers it.</summary>
    public const string OutPort = "PhobosShipbreaker.ThawOut", VesselPort = "PhobosShipbreaker.VesselIn";
    public const int Footprint = 2, FeedCapacity = 2, TrayCells = 2;
    // Native masses (items_mining.json): water ice 24.7 kg, ice gangue 2.0 kg. The water yield is the remainder.
    public const double IceKg = 24.7, GangueKg = 2.0, WaterKg = IceKg - GangueKg, MachineKg = 120;
    // Authored operation: 6 kW for 40 minutes a block. The thermal need below is 3.60 kWh; the rest covers the
    // drive, controls and the share that warms the room.
    public const double WorkingKW = 6, IdleKW = 0.1, RoomHeatFraction = 0.15;
    /// <summary>The water thaw's powered duration, from the recipe pack (2,400 s authored).</summary>
    public static double CycleSeconds => ShipbreakerRecipes.Entry("thaw-water").seconds!.Value;
    /// <summary>Authored thermal need per kilogram of ice: warming the block from cold storage plus melting. The
    /// enthalpy of fusion of water is 6.01 kJ/mol (333.6 kJ/kg) in the NIST Chemistry WebBook; the warming
    /// allowance is our simplification, not a measured property of the game's ice.</summary>
    public const double MeltKJPerKg = 525, FusionKJPerKg = 333.6;
    public const double ThermalNeedKWh = IceKg * MeltKJPerKg / 3600;
    public static double CycleEnergyKWh => WorkingKW * CycleSeconds / 3600;
    /// <summary>The water thaw catalog from the recipe pack.</summary>
    public static ProcessRecipeCatalog Recipes => ShipbreakerRecipes.Catalog(ShipbreakerRecipes.ThawWater);

    // Methane ice (Shipbreaker 0.45.0). The game calls ItmIce02 "Methane Ice" and "cold, wet": read as methane
    // clathrate (structure I hydrate), CH4.nH2O. Full cage occupancy is n = 5.75 (USGS Fact Sheet 2017-3080);
    // natural and laboratory sI hydrate measures n = 6.0 to 6.2 (Circone et al. 2005, USGS). Authored n = 6.0.
    // The block carries the same 2.0 kg of ice gangue as water ice; the 22.84 kg of hydrate is 183.99 mol:
    // 2.95 kg of methane and 19.89 kg of water. The clathrate reading and the gangue share are ours.
    public const string MethaneIce = "ItmIce02", MethaneCommodity = "methane";
    public const double MethaneIceKg = 24.84, HydrationNumber = 6.0, MethaneKg = 2.95, ClathrateWaterKg = 19.89;
    public const double MethaneMolarKg = 0.016043, WaterMolarKg = 0.018015;
    /// <summary>Methane hydrate to gas and liquid water: 54.2 kJ per mol of methane (Handa 1986, J. Chem.
    /// Thermodynamics 18, heat-flow calorimetry, NRC Canada). The warming allowance per kilogram of block is
    /// the water-ice rule's (525 minus 333.6 kJ/kg), our simplification.</summary>
    public const double DissociationKJPerMol = 54.2;
    public const double ClathrateMol = (MethaneIceKg - GangueKg) / (MethaneMolarKg + HydrationNumber * WaterMolarKg);
    public const double ClathrateThermalNeedKWh = (ClathrateMol * DissociationKJPerMol + MethaneIceKg * (MeltKJPerKg - FusionKJPerKg)) / 3600;
    /// <summary>50 minutes at the same 6 kW: 5 kWh a block, 4.25 kWh of it into the block after the room's share.</summary>
    public static double MethaneCycleSeconds => ShipbreakerRecipes.Entry("thaw-methane").seconds!.Value;
    /// <summary>The game prices methane ice at 20 for 24.84 kg, below the water inside it (owner decision,
    /// 30 September 2026: correct the native price in place so processing still loses value). At full occupancy
    /// its products are worth 3.33 kg x 2.2 (the game's methane price) + 21.51 kg x 10 = 222.</summary>
    public const double MethaneIcePrice = 250;
    /// <summary>The thaw unit's methane outlet and the port it pairs on a methane store, distinct from the K2's.</summary>
    public const string MethaneOutPort = "PhobosShipbreaker.ThawMethaneOut", MethaneInPort = "PhobosShipbreaker.ThawMethaneIn";
    /// <summary>The methane ice catalog from the recipe pack.</summary>
    public static ProcessRecipeCatalog MethaneRecipes => ShipbreakerRecipes.Catalog(ShipbreakerRecipes.ThawMethane);

    public static bool IsFamily(string? id) => EquipmentIdentity.IsFamily(id, Prefix);
    public static bool IsFeed(string? id) => id == Ice || id == MethaneIce;
    public static double FeedKg(string? id) => id == MethaneIce ? MethaneIceKg : IceKg;
    public static ProcessRecipeCatalog RecipesFor(string? id) => id == MethaneIce ? MethaneRecipes : Recipes;
    public static double CycleSecondsFor(string? id) => id == MethaneIce ? MethaneCycleSeconds : CycleSeconds;
    /// <summary>A single, empty, detached block of water ice or methane ice at the game's own mass.</summary>
    public static bool ValidIce(string? id, double kg, bool detached, bool empty, bool unstacked) =>
        IsFeed(id) && detached && empty && unstacked && ProcessMaterial.MassMatches(kg, FeedKg(id));
    /// <summary>Electricity that warms the room: the authored fraction while thawing, all of the idle draw.</summary>
    public static double RoomHeatKW(bool working) => working ? WorkingKW * RoomHeatFraction : IdleKW;
    /// <summary>The vessel lies within one tile of the thaw unit: the distance between centres, on the longer
    /// axis, is at most half of both footprints plus one tile, and the two do not overlap. Rotation does not
    /// matter for square footprints. Coordinates are in tiles.</summary>
    public static bool Adjacent(double thawX, double thawY, double vesselX, double vesselY, int vesselFootprint)
    {
        foreach (double v in new[] { thawX, thawY, vesselX, vesselY }) if (double.IsNaN(v) || double.IsInfinity(v)) return false;
        if (vesselFootprint < 1) return false;
        double reach = (Footprint + vesselFootprint) / 2.0, distance = Math.Max(Math.Abs(thawX - vesselX), Math.Abs(thawY - vesselY));
        return distance + 1e-6 >= reach && distance <= reach + 1 + 1e-6;
    }
}
