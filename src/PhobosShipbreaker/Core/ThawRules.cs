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
    /// <summary>Exact identities: methane ice (ItmIce02) shares IsIce and is refused; gangue is not feed.</summary>
    public const string Ice = "ItmIce01", Gangue = "ItmIceTrash01", Commodity = SiloRules.Commodity;
    /// <summary>The thaw unit's outlet and the port any registered water vessel offers it.</summary>
    public const string OutPort = "PhobosShipbreaker.ThawOut", VesselPort = "PhobosShipbreaker.VesselIn";
    public const int Footprint = 2, FeedCapacity = 2, TrayCells = 2;
    // Native masses (items_mining.json): water ice 24.7 kg, ice gangue 2.0 kg. The water yield is the remainder.
    public const double IceKg = 24.7, GangueKg = 2.0, WaterKg = IceKg - GangueKg, MachineKg = 120, Price = 3200;
    // Authored operation: 6 kW for 40 minutes a block. The thermal need below is 3.60 kWh; the rest covers the
    // drive, controls and the share that warms the room.
    public const double WorkingKW = 6, IdleKW = 0.1, CycleSeconds = 2400, RoomHeatFraction = 0.15;
    /// <summary>Authored thermal need per kilogram of ice: warming the block from cold storage plus melting. The
    /// enthalpy of fusion of water is 6.01 kJ/mol (333.6 kJ/kg) in the NIST Chemistry WebBook; the warming
    /// allowance is our simplification, not a measured property of the game's ice.</summary>
    public const double MeltKJPerKg = 525, FusionKJPerKg = 333.6;
    public const double ThermalNeedKWh = IceKg * MeltKJPerKg / 3600;
    public const double CycleEnergyKWh = WorkingKW * CycleSeconds / 3600;
    public static readonly ProcessRecipeCatalog Recipes = new(1, new[] {
        new ProcessRecipe(1, IceKg, new[] { new ProductSpec(Commodity, 1, WaterKg), new ProductSpec(Gangue, 1, GangueKg) }) });
    public static bool IsFamily(string? id) => EquipmentIdentity.IsFamily(id, Prefix);
    public static bool ValidIce(string? id, double kg, bool detached, bool empty, bool unstacked) =>
        id == Ice && detached && empty && unstacked && ProcessMaterial.MassMatches(kg, IceKg);
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
