using System;
using System.Collections.Generic;
using System.Globalization;
using Phobos.Ostranauts.Framework.Registration;

namespace PhobosManufacturing.Core;

/// <summary>The Slingwright RM-1 Reaction Mass Feeder (Manufacturing 0.43.0; owner decisions of 4 October 2026: no
/// trash-like object is left to pile up, and remainders become reaction mass for the RCS). A powered 1 x 1 machine
/// installed where an RCS gas canister would go, beside or instead of one. Remainders put in its inventory are ground,
/// one item at a time, into a saved record of reaction mass in kilograms; the thrusters draw on that record as they
/// draw on a canister. It takes declared remainders only (Framework <see cref="Remainders"/>), never ore, scrap or a
/// product.
///
/// Authored figures, ours: reaction mass thrown at the ideal exhaust speed of cold nitrogen gas, about 700 m/s, counts
/// kilogram for kilogram as nitrogen (<see cref="ExhaustRatio"/> 1). Half of 700 squared is 245 kJ, 0.068 kWh, a
/// kilogram; with launcher and grinding losses the feeder pays <see cref="KWhPerKg"/> while it grinds, so a burn needs
/// no power. Throwing ground rock out of gas thrusters is game-like; the reaction mass idea itself is the mass driver
/// of the 1977 NASA Ames space settlement study (NASA SP-428), cited from memory.</summary>
public static class FeederRules
{
    public const string Prefix = "PhobosReactionMassFeeder", Installed = Prefix + "Installed";
    public const string Record = "ManufacturingFeeder";
    public const string MassRecord = "ManufacturingFeederMass", MassJournal = "ManufacturingFeederWork", MassGuard = "ManufacturingFeederTransfer";
    public const string Commodity = "reaction mass";
    public const int Footprint = 1, TrayWidth = 2, TrayHeight = 2;
    public const double MachineKg = 40, CapacityKg = 60, WorkingKW = 3, IdleKW = 0.02;
    /// <summary>Electricity to grind and charge one kilogram (authored: about twice the 0.068 kWh of kinetic energy).</summary>
    public const double KWhPerKg = 0.15;
    /// <summary>The share of the grinding electricity that warms the room at once; the rest leaves with the mass.</summary>
    public const double RoomHeatFraction = 0.5;
    /// <summary>Nitrogen-equivalent kilograms a kilogram of reaction mass is worth to the thrusters.</summary>
    public const double ExhaustRatio = 1;
    public const double IdealExhaustMetresPerSecond = 700;
    public const string DrawFirst = "first", DrawLast = "last", SwitchOn = "on", SwitchOff = "off";
    /// <summary>The action prefix of every setting the feeder's panel offers besides the shared feed store.</summary>
    public static readonly string[] SettingPrefixes = { "order:", "feeding:" };
    public static bool IsFamily(string? id) => EquipmentIdentity.IsFamily(id, Prefix);
    public static double IdealKWhPerKg => IdealExhaustMetresPerSecond * IdealExhaustMetresPerSecond / 2 / 3.6e6;
    /// <summary>Electricity one item of this mass takes.</summary>
    public static double GrindKWh(double itemKg) => Valid(itemKg) ? itemKg * KWhPerKg : throw new ArgumentException("Invalid item mass.");
    /// <summary>Credits supplied energy toward the item in hand; never more than the item needs.</summary>
    public static (double CreditedKWh, bool Complete) Advance(double creditedKWh, double suppliedKWh, double itemKg)
    {
        double need = GrindKWh(itemKg);
        if (!ManufacturingRules.Finite(creditedKWh) || !ManufacturingRules.Finite(suppliedKWh) || creditedKWh < 0 || suppliedKWh < 0) throw new ArgumentException("Invalid grinding energy.");
        double credited = Math.Min(creditedKWh + suppliedKWh, need);
        return (credited, credited >= need - 1e-9);
    }
    /// <summary>Whether an item of this mass fits beside what the record already holds.</summary>
    public static bool Fits(double heldKg, double itemKg) => Valid(itemKg) && ManufacturingRules.Finite(heldKg) && heldKg >= 0 && heldKg + itemKg <= CapacityKg + 1e-8;
    public static double EquivalentKg(double kg) => kg * ExhaustRatio;
    /// <summary>The kilograms to take for a request in nitrogen-equivalent kilograms, bounded by what is held.</summary>
    public static double KilogramsFor(double equivalentKg, double availableKg) =>
        !ManufacturingRules.Finite(equivalentKg) || !ManufacturingRules.Finite(availableKg) || equivalentKg <= 0 || availableKg <= 0 ? 0 : Math.Min(equivalentKg / ExhaustRatio, availableKg);
    private static bool Valid(double kg) => ManufacturingRules.Finite(kg) && kg > 0;
}

/// <summary>The feeder's saved settings and work (schema 1): whether it feeds the thrusters, whether it is drawn before
/// the regulator's own canisters, and the energy already credited to the item in hand. The mass itself is the bulk
/// vessel record beside it. It grinds whenever it has power and something to grind, like the cabin air regulator.</summary>
public sealed class FeederState
{
    public bool Feeding = true, First = true;
    public double GrindKWh;
    public Dictionary<string, string> Save()
    {
        if (!ManufacturingRules.Finite(GrindKWh) || GrindKWh < 0) throw new ArgumentException("Invalid feeder record.");
        return new(StringComparer.Ordinal) { ["feeding"] = Feeding ? "1" : "0", ["first"] = First ? "1" : "0", ["grind"] = GrindKWh.ToString("R", CultureInfo.InvariantCulture) };
    }
    public static FeederState Read(IReadOnlyDictionary<string, string> d)
    {
        if (d.Count != 3 || !Flag(d["feeding"], out bool feeding) || !Flag(d["first"], out bool first)) throw new ArgumentException("Unknown feeder record.");
        var s = new FeederState { Feeding = feeding, First = first, GrindKWh = double.Parse(d["grind"], CultureInfo.InvariantCulture) };
        s.Save(); return s;
    }
    private static bool Flag(string value, out bool flag) { flag = value == "1"; return value == "1" || value == "0"; }
}
