using System;
using Phobos.Ostranauts.Framework.Processing;

namespace PhobosShipbreaker.Core;

public static class ReclaimerRules
{
    public const string Prefix = "PhobosScrapReclaimer", Installed = Prefix + "Installed";
    public const string InputSlot = Prefix + "Input", InputBin = Prefix + "InputBin", Controls = Prefix + "Controls";
    public const string Section = Prefix + "Section", SectionCondition = Prefix + "IsSection", SectionTrigger = Prefix + "TSection";
    public const string Feedstock = "PhobosPanelResidueR2", Reject = "PhobosPanelRejectR2", FeedCondition = "PhobosIsPanelResidueR2";
    public const int Footprint = 4, FeedCapacity = 4, OutputSize = 8;
    public const double InputKg = 13, RejectKg = 9, MachineKg = 180, SectionKg = MachineKg / 2;
    public const double CycleSeconds = 120, WorkingKW = 12, IdleKW = .1;
    // Authored operating bounds. Native Heater uses 20.7 J/(mol K) for room gas.
    public const double GasHeatCapacity = 20.7, MaxRoomKelvin = 40 + Phobos.Ostranauts.Framework.Units.CelsiusToKelvin, MinPressureKPa = 10;
    public const double JoulesPerKilojoule = 1000;
    public static bool IsFamily(string? id) => id == Installed || id == Installed + "Dmg" ||
        id == Prefix + "Loose" || id == Prefix + "LooseDmg";
    public static readonly ProcessRecipeCatalog Recipes = new ProcessRecipeCatalog(1, new[] {
        new ProcessRecipe(1, InputKg, new[] { new ProductSpec("ItmScrapSteel", 3, 1),
            new ProductSpec("ItmScrapAluminum", 1, 1), new ProductSpec(Reject, 1, RejectKg) })
    });
    public static bool CoolingBudget(double mols, double kelvin, double pendingKelvin, double pressure,
        double kw, double seconds, out double rise)
    {
        rise = 0;
        foreach (double value in new[] { mols, kelvin, pendingKelvin, pressure, kw, seconds })
            if (double.IsNaN(value) || double.IsInfinity(value)) return false;
        if (mols <= 0 || pressure < MinPressureKPa || kelvin <= 0 || kelvin + pendingKelvin <= 0 ||
            kw <= 0 || seconds < 0 || seconds > ProcessJob.MaxSeconds) return false;
        rise = kw * JoulesPerKilojoule * seconds / (mols * GasHeatCapacity);
        return kelvin + pendingKelvin + rise < MaxRoomKelvin;
    }
}
