using System;
using Phobos.Ostranauts.Framework.Processing;

namespace PhobosShipbreaker.Core;

public static class ReclaimerRules
{
    public const string Prefix = "PhobosScrapReclaimer", Installed = Prefix + "Installed";
    public const string InputSlot = Prefix + "Input", InputBin = Prefix + "InputBin", Controls = Prefix + "Controls";
    public const string Section = Prefix + "Section", SectionCondition = Prefix + "IsSection", SectionTrigger = Prefix + "TSection";
    public const string Feedstock = "PhobosPanelResidueR2", Reject = "PhobosPanelRejectR2", FeedCondition = "PhobosIsPanelResidueR2";
    public const int Footprint = 4, FeedCapacity = 4;
    /// <summary>The R4 product tray in cells (Shipbreaker 0.65.0; it was 8 x 8): a full four-packet feed delivered into
    /// stacks is one steel stack, one aluminium stack and four rejects.</summary>
    public const int TrayWidth = 3, TrayHeight = 2;
    public const double InputKg = 13, RejectKg = 9, MachineKg = 180, SectionKg = MachineKg / 2;
    public const double CycleSeconds = 120, WorkingKW = 12, IdleKW = .1;
    // Authored operating bounds. Native Heater uses 20.7 J/(mol K) for room gas.
    public const double GasHeatCapacity = 20.7, MaxRoomKelvin = 40 + Phobos.Ostranauts.Framework.Units.CelsiusToKelvin, MinPressureKPa = 10;
    public const double JoulesPerKilojoule = 1000;
    public static bool IsFamily(string? id) => id == Installed || id == Installed + "Dmg" ||
        id == Prefix + "Loose" || id == Prefix + "LooseDmg";
    /// <summary>The reclaimer catalog from the recipe pack (revision 1: 3 kg steel, 1 kg aluminium, a 9 kg reject).</summary>
    public static ProcessRecipeCatalog Recipes => ShipbreakerRecipes.Catalog(ShipbreakerRecipes.Reclaimer);
    /// <summary>The air budget: Framework's shared rule, which this used to repeat (Shipbreaker 0.69.0).</summary>
    public static bool CoolingBudget(double mols, double kelvin, double pendingKelvin, double pressure,
        double kw, double seconds, out double rise) =>
        Phobos.Ostranauts.Framework.Processing.RoomHeat.Budget(mols, kelvin, pendingKelvin, pressure, kw, seconds, out rise);
}
