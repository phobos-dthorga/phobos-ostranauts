using System;
using Phobos.Ostranauts.Framework.Processing;
using Phobos.Ostranauts.Framework.Registration;

namespace PhobosManufacturing.Core;

/// <summary>The Tolvane AX-2 Ammonia Cracker: a 2 x 2 catalytic cracker, 2 NH3 -> N2 + 3 H2, the reverse of the
/// Haber-Bosch synthesis. Each one-hour cycle takes 1 kg of ammonia from a linked ammonia store over a heated
/// catalyst bed and sends the nitrogen to a linked nitrogen store and the hydrogen to a linked hydrogen store.
/// Masses use the game's own molar masses; nitrogen is the remainder, so every cycle conserves mass exactly and
/// stays within 0.01% of the stoichiometric nitrogen (the game's molar masses differ in the sixth figure). Complete conversion is an authored simplification (a real
/// cracker near 600 C converts almost all of it and leaves a trace of ammonia in the gas).</summary>
public static class CrackerRules
{
    public const string Prefix = "PhobosAmmoniaCracker", Installed = Prefix + "Installed";
    public const string Record = "ManufacturingCracker", Guard = "ManufacturingCrackerTransfer";
    /// <summary>The cracker draws from an ammonia store's ordinary outlet and delivers into each product store's own
    /// cracker inlet, so a store can take from an X2 or a V4 and a cracker at once.</summary>
    public const string AmmoniaInPort = "PhobosManufacturing.CrackerAmmoniaIn", StoreOutPort = SabatierRules.StoreOutPort;
    public const string NitrogenOutPort = "PhobosManufacturing.CrackerNitrogenOut", HydrogenOutPort = "PhobosManufacturing.CrackerHydrogenOut";
    public const string StoreInPort = "PhobosManufacturing.CrackerIn";
    public const string AmmoniaSpecies = "NH3", NitrogenSpecies = "N2";
    public const int Footprint = 2;
    public const double MachineKg = 150;
    /// <summary>Catalyst bed heaters, recuperator fan and controls: authored. The part the reaction absorbs leaves as
    /// chemical energy in the products; the rest ends as room heat.</summary>
    public const double WorkingKW = 2.0, IdleKW = 0.02, CycleHours = 1, CycleKWh = WorkingKW * CycleHours;
    public const double AmmoniaKgPerCycle = 1.0;
    /// <summary>Standard enthalpy of formation of ammonia gas, kJ/mol (NIST Chemistry WebBook); N2 and H2 are zero.</summary>
    public const double FormationNH3 = -45.94;
    /// <summary>Heat absorbed per mole of ammonia cracked: products minus reactants, 0 - (-45.94) = 45.94 kJ/mol.</summary>
    public const double ReactionKJPerMol = -FormationNH3;
    private static double KgPerMol(string species) => NativeGasCanister.KgPerMol[species];
    public static double AmmoniaMolesPerCycle => AmmoniaKgPerCycle / KgPerMol(AmmoniaSpecies);
    public static double HydrogenKgPerCycle => AmmoniaMolesPerCycle * 1.5 * KgPerMol("H2");
    /// <summary>The remainder, so reactant and products balance exactly.</summary>
    public static double NitrogenKgPerCycle => AmmoniaKgPerCycle - HydrogenKgPerCycle;
    public static double StoichiometricNitrogenKg => AmmoniaMolesPerCycle / 2 * KgPerMol(NitrogenSpecies);
    public static double ReactionKWhPerCycle => AmmoniaMolesPerCycle * ReactionKJPerMol / 3600;
    public static double ReactionKW => ReactionKWhPerCycle / CycleHours;
    /// <summary>Everything the working cracker puts into its room: its electricity less what the reaction stores in
    /// the products.</summary>
    public static double RoomHeatKW(bool working) => working ? WorkingKW - ReactionKW : IdleKW;
    public static bool IsFamily(string? id) => EquipmentIdentity.IsFamily(id, Prefix);
    public static bool Balanced() => Math.Abs(AmmoniaKgPerCycle - NitrogenKgPerCycle - HydrogenKgPerCycle) < Phobos.Ostranauts.Framework.Units.MassToleranceKg &&
        Math.Abs(NitrogenKgPerCycle - StoichiometricNitrogenKg) / StoichiometricNitrogenKg < 1e-4 && ReactionKWhPerCycle < CycleKWh;
    public static (double CycleKWh, bool Complete) Advance(double cycleKWh, double suppliedKWh) => ManufacturingRules.AdvanceCycle(cycleKWh, suppliedKWh, CycleKWh);
    /// <summary>The reaction energy for credited cycle progress: it is taken out of the electricity's room heat.</summary>
    public static double AbsorbedKWh(double creditedKWh) => Math.Max(0, creditedKWh) / CycleKWh * ReactionKWhPerCycle;
}
