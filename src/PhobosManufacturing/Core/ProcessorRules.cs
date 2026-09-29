using System;
using Phobos.Ostranauts.Framework.Processing;
using Phobos.Ostranauts.Framework.Registration;

namespace PhobosManufacturing.Core;

/// <summary>The X2 Chemical Processor: a 2 x 2 water electrolysis cell. Each one-hour cycle splits 1.125 kg of
/// water from a linked vessel into 1.000 kg of oxygen (into a linked native O2 canister, or the cabin air when
/// none is linked) and 0.125 kg of hydrogen into a linked hydrogen store. 2 H2O -> 2 H2 + O2: the 9:8:1 mass
/// split is within 0.1% of the real 9.008:7.999:1.008. The cycle's 6.0 kWh covers the 4.9 kWh higher heating
/// value of that water (285.8 kJ/mol, NIST Chemistry WebBook) with the rest as room heat.</summary>
public static class ProcessorRules
{
    public const string Prefix = "PhobosChemicalProcessor", Installed = Prefix + "Installed";
    public const string Record = "ManufacturingProcessor", Guard = "ManufacturingProcessorTransfer";
    public const string WaterInPort = "PhobosManufacturing.ProcessorWaterIn", VesselOutPort = "PhobosManufacturing.VesselOut";
    public const string HydrogenOutPort = "PhobosManufacturing.ProcessorHydrogenOut", StoreInPort = "PhobosManufacturing.StoreIn";
    public const int Footprint = 2;
    public const double MachineKg = 130, Price = 38000;
    public const double WorkingKW = 6, IdleKW = 0.02, CycleHours = 1, CycleKWh = WorkingKW * CycleHours;
    public const double WaterKgPerCycle = 1.125, OxygenKgPerCycle = 1.000, HydrogenKgPerCycle = 0.125;
    /// <summary>Enthalpy of formation of liquid water, 285.83 kJ/mol (NIST): the electrical minimum per cycle.</summary>
    public const double WaterHHVKJPerMol = 285.83, WaterKgPerMol = 0.0180153;
    public static double MinimumKWhPerCycle => WaterKgPerCycle / WaterKgPerMol * WaterHHVKJPerMol / 3600;
    public static double RoomHeatFraction => 1 - MinimumKWhPerCycle / CycleKWh;
    public const string OxygenSpecies = "O2", CanisterTrigger = "TIsRTAO2Installed";
    public static double OxygenMolesPerCycle => NativeGasCanister.Moles(OxygenSpecies, OxygenKgPerCycle);
    public static bool IsFamily(string? id) => EquipmentIdentity.IsFamily(id, Prefix);
    public static bool Balanced() => Math.Abs(WaterKgPerCycle - OxygenKgPerCycle - HydrogenKgPerCycle) < Phobos.Ostranauts.Framework.Units.MassToleranceKg;
    /// <summary>Electricity that warms the room: the share above the chemical minimum while running, all of the idle draw.</summary>
    public static double RoomHeatKW(bool working) => working ? WorkingKW * RoomHeatFraction : IdleKW;
    /// <summary>Credits supplied energy to the current cycle (the shared one-cycle-per-step rule).</summary>
    public static (double CycleKWh, bool Complete) Advance(double cycleKWh, double suppliedKWh) => ManufacturingRules.AdvanceCycle(cycleKWh, suppliedKWh, CycleKWh);
}
