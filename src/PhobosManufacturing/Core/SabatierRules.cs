using System;
using Phobos.Ostranauts.Framework.Processing;
using Phobos.Ostranauts.Framework.Registration;

namespace PhobosManufacturing.Core;

/// <summary>The K2 Sabatier Reactor: a 2 x 2 catalytic methanation reactor, CO2 + 4 H2 -> CH4 + 2 H2O, the
/// reaction NASA's ISS Carbon Dioxide Reduction Assembly runs on crew CO2 and electrolysis hydrogen. Each
/// one-hour cycle takes 0.125 kg of hydrogen (one X2 cycle's output) from a linked store and a quarter as many
/// moles of CO2 from a linked native CO2 canister, and makes methane into a linked methane store and water into a
/// linked water vessel. Masses use the game's own molar masses; water is the remainder, so every cycle conserves
/// mass exactly and stays within 0.01% of the stoichiometric water. Complete conversion of the limiting hydrogen
/// is an authored simplification (real reactors convert most, not all).</summary>
public static class SabatierRules
{
    public const string Prefix = "PhobosSabatierReactor", Installed = Prefix + "Installed";
    public const string Record = "ManufacturingSabatier", Guard = "ManufacturingSabatierTransfer";
    public const string WaterOutPort = "PhobosManufacturing.SabatierWaterOut", VesselInPort = "PhobosManufacturing.SabatierVesselIn";
    public const string HydrogenInPort = "PhobosManufacturing.SabatierHydrogenIn", StoreOutPort = "PhobosManufacturing.StoreOut";
    public const string MethaneOutPort = "PhobosManufacturing.SabatierMethaneOut", MethaneStoreInPort = ProcessorRules.StoreInPort;
    public const string CarbonDioxide = "CO2", MethaneSpecies = "CH4", CanisterTrigger = "TIsRTACO2Installed";
    public const int Footprint = 2;
    public const double MachineKg = 150;
    /// <summary>Compressor, catalyst bed heaters and condenser fan: authored, all of it ends as room heat.</summary>
    public const double WorkingKW = 1.2, IdleKW = 0.02, CycleHours = 1, CycleKWh = WorkingKW * CycleHours;
    public const double HydrogenKgPerCycle = ProcessorRules.HydrogenKgPerCycle;
    /// <summary>Standard enthalpies of formation, kJ/mol (NIST Chemistry WebBook): CO2(g), CH4(g), H2O(l).</summary>
    public const double FormationCO2 = -393.51, FormationCH4 = -74.87, FormationWaterLiquid = -285.83;
    /// <summary>Heat released per mole of CO2 reduced, with the water condensed as it leaves the reactor:
    /// reactants minus products, -393.51 - (-74.87 + 2 x -285.83) = 253.02 kJ/mol (exothermic).</summary>
    public const double ReactionKJPerMol = FormationCO2 - (FormationCH4 + 2 * FormationWaterLiquid);
    private static double KgPerMol(string species) => NativeGasCanister.KgPerMol[species];
    public static double HydrogenMolesPerCycle => HydrogenKgPerCycle / KgPerMol("H2");
    public static double CarbonDioxideMolesPerCycle => HydrogenMolesPerCycle / 4;
    public static double CarbonDioxideKgPerCycle => CarbonDioxideMolesPerCycle * KgPerMol(CarbonDioxide);
    public static double MethaneKgPerCycle => CarbonDioxideMolesPerCycle * KgPerMol(MethaneSpecies);
    /// <summary>The remainder, so reactants and products balance exactly.</summary>
    public static double WaterKgPerCycle => HydrogenKgPerCycle + CarbonDioxideKgPerCycle - MethaneKgPerCycle;
    public static double StoichiometricWaterKg => 2 * CarbonDioxideMolesPerCycle * KgPerMol("H2O");
    public static double ReactionKWhPerCycle => CarbonDioxideMolesPerCycle * ReactionKJPerMol / 3600;
    public static double ReactionKW => ReactionKWhPerCycle / CycleHours;
    /// <summary>Everything the working reactor puts into its room: its electricity and the reaction heat.</summary>
    public static double RoomHeatKW(bool working) => working ? WorkingKW + ReactionKW : IdleKW;
    /// <summary>The second mode (Manufacturing 0.53.0; owner decision, 5 October 2026): methanation of carbon monoxide,
    /// CO + 3 H2 -> CH4 + H2O, on the same catalyst bed and the same one-cycle hydrogen charge. The reactor works in this
    /// mode whenever its carbon source is a carbon monoxide store. Formation enthalpy of CO(g): -110.53 kJ/mol (NIST);
    /// with the water condensed the reaction releases -110.53 - (-74.87 + -285.83) = 250.17 kJ per mole of CO.</summary>
    public const string CarbonMonoxide = "CO";
    public const double FormationCO = -110.53;
    public const double MonoxideReactionKJPerMol = FormationCO - (FormationCH4 + FormationWaterLiquid);
    public static double MonoxideMolesPerCycle => HydrogenMolesPerCycle / 3;
    public static double MonoxideKgPerCycle => MonoxideMolesPerCycle * KgPerMol(CarbonMonoxide);
    public static double MonoxideMethaneKgPerCycle => MonoxideMolesPerCycle * KgPerMol(MethaneSpecies);
    /// <summary>The remainder, so reactants and products balance exactly.</summary>
    public static double MonoxideWaterKgPerCycle => HydrogenKgPerCycle + MonoxideKgPerCycle - MonoxideMethaneKgPerCycle;
    public static double MonoxideReactionKWhPerCycle => MonoxideMolesPerCycle * MonoxideReactionKJPerMol / 3600;
    /// <summary>One cycle's figures in either mode: the carbon gas taken, the methane and water made, the heat released.</summary>
    public static double CarbonKg(bool monoxide) => monoxide ? MonoxideKgPerCycle : CarbonDioxideKgPerCycle;
    public static double MethaneKg(bool monoxide) => monoxide ? MonoxideMethaneKgPerCycle : MethaneKgPerCycle;
    public static double WaterKg(bool monoxide) => monoxide ? MonoxideWaterKgPerCycle : WaterKgPerCycle;
    public static double ReactionKWh(bool monoxide) => monoxide ? MonoxideReactionKWhPerCycle : ReactionKWhPerCycle;
    public static double RoomHeatKW(bool working, bool monoxide) => working ? WorkingKW + ReactionKWh(monoxide) / CycleHours : IdleKW;
    public static bool MonoxideBalanced() => Math.Abs(MonoxideWaterKgPerCycle - MonoxideMolesPerCycle * KgPerMol("H2O")) / MonoxideWaterKgPerCycle < 1e-4;
    public static bool IsFamily(string? id) => EquipmentIdentity.IsFamily(id, Prefix);
    public static bool Balanced() => Math.Abs(HydrogenKgPerCycle + CarbonDioxideKgPerCycle - MethaneKgPerCycle - WaterKgPerCycle) < Phobos.Ostranauts.Framework.Units.MassToleranceKg &&
        Math.Abs(WaterKgPerCycle - StoichiometricWaterKg) / StoichiometricWaterKg < 1e-4;
    public static (double CycleKWh, bool Complete) Advance(double cycleKWh, double suppliedKWh) => ManufacturingRules.AdvanceCycle(cycleKWh, suppliedKWh, CycleKWh);
}
