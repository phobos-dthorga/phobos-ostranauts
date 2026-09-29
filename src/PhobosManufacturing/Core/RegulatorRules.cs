using System;
using System.Collections.Generic;
using System.Globalization;
using Phobos.Ostranauts.Framework.Registration;

namespace PhobosManufacturing.Core;

/// <summary>The Fennmark A2 Cabin Air Regulator: a 2 x 2 valve and sensor unit that holds its room's oxygen partial
/// pressure at a set point from a linked bulk oxygen store, then tops up total pressure with nitrogen from a linked
/// nitrogen store. It adds gas only: it never vents, scrubs or cools. At one temperature and volume a species'
/// partial pressure is the total pressure times its mole fraction (Dalton's law), so the moles to add follow from the
/// room's own total moles and pressure without needing its volume.</summary>
public static class RegulatorRules
{
    public const string Prefix = "PhobosCabinAirRegulator", Installed = Prefix + "Installed", Record = "ManufacturingRegulator";
    public const string Inlet = "PhobosGasLineIn";
    public const int Footprint = 2;
    public const double MachineKg = 60, Price = 23000, WorkingKW = 0.1;
    /// <summary>Oxygen set points in kPa. The game counts 20 kPa and above as adequate oxygen (its DcGasPpO2 rule);
    /// Earth sea level is about 21.2 kPa.</summary>
    public static readonly IReadOnlyList<double> OxygenTargets = new[] { 19d, 21d, 23d };
    public const double DefaultOxygenKPa = 21, MaxOxygenKPa = 24;
    /// <summary>Total pressure set points in kPa (0 leaves pressure alone); never past a standard atmosphere and a bit.</summary>
    public static readonly IReadOnlyList<double> PressureTargets = new[] { 0d, 80d, 90d, 101d };
    public const double MaxPressureKPa = 105;
    /// <summary>Oxygen never takes more than this share of the room's gas: richer air makes every fire worse (authored).</summary>
    public const double MaxOxygenFraction = 0.30;
    /// <summary>A room below this pressure is breached or evacuated: the regulator stops rather than feed the leak.</summary>
    public const double MinRoomKPa = 10;
    /// <summary>Valve flow limits per gas (authored). A crew member uses under a kilogram of oxygen a day (about 0.8 kg:
    /// NASA Johnson Space Center, Anderson, Ewert, Keener and Wagner, Life Support Baseline Values and Assumptions
    /// Document, NASA/TP-2015-218570, March 2015; the exact table value was not rechecked), so this refills a room far
    /// faster than the crew use it.</summary>
    public const double OxygenKgPerHour = 6, NitrogenKgPerHour = 12;
    public const double TickSeconds = 2;
    public static bool IsFamily(string? id) => EquipmentIdentity.IsFamily(id, Prefix);

    /// <summary>Moles of oxygen that bring the room to <paramref name="targetKPa"/>, never more than keeps oxygen within
    /// <see cref="MaxOxygenFraction"/> of the gas. p_O2 = P n_O2 / N at fixed T and V, so after adding x moles of oxygen
    /// p_O2' = P (n_O2 + x) / N, and x = target N / P - n_O2.</summary>
    public static double OxygenMoles(double totalMoles, double pressureKPa, double oxygenMoles, double targetKPa)
    {
        if (!Valid(totalMoles, pressureKPa, oxygenMoles, targetKPa) || pressureKPa < MinRoomKPa || totalMoles <= 0) return 0;
        double wanted = Math.Max(0, Math.Min(targetKPa, MaxOxygenKPa) * totalMoles / pressureKPa - oxygenMoles);
        // Fraction cap: (n_O2 + x) / (N + x) <= f, so x <= (f N - n_O2) / (1 - f).
        double capped = Math.Max(0, (MaxOxygenFraction * totalMoles - oxygenMoles) / (1 - MaxOxygenFraction));
        return Math.Min(wanted, capped);
    }
    /// <summary>Moles of nitrogen that bring total pressure to <paramref name="targetKPa"/>: P' = P (N + x) / N.</summary>
    public static double NitrogenMoles(double totalMoles, double pressureKPa, double targetKPa)
    {
        if (!Valid(totalMoles, pressureKPa, 0, targetKPa) || targetKPa <= 0 || pressureKPa < MinRoomKPa || totalMoles <= 0) return 0;
        return Math.Max(0, totalMoles * (Math.Min(targetKPa, MaxPressureKPa) - pressureKPa) / pressureKPa);
    }
    private static bool Valid(params double[] values)
    {
        foreach (double v in values) if (!ManufacturingRules.Finite(v) || v < 0) return false;
        return true;
    }
}

/// <summary>The A2's saved choices. It keeps working after a reload, like the game's own air pumps.</summary>
public sealed class RegulatorState
{
    public bool On { get; set; }
    public double OxygenKPa { get; set; } = RegulatorRules.DefaultOxygenKPa;
    public double PressureKPa { get; set; }
    public string OxygenStore { get; set; } = "";
    public string NitrogenStore { get; set; } = "";
    public double AddedOxygenKg { get; set; }
    public double AddedNitrogenKg { get; set; }
    public Dictionary<string, string> Save() => new()
    {
        ["on"] = On ? "1" : "0", ["o2"] = N(OxygenKPa), ["pressure"] = N(PressureKPa), ["o2store"] = OxygenStore, ["n2store"] = NitrogenStore,
        ["o2kg"] = N(AddedOxygenKg), ["n2kg"] = N(AddedNitrogenKg)
    };
    public static RegulatorState Read(IReadOnlyDictionary<string, string> f)
    {
        if (f.Count != 7) throw new FormatException("Invalid regulator record.");
        var s = new RegulatorState
        {
            On = Flag(f, "on"), OxygenKPa = Amount(f, "o2"), PressureKPa = Amount(f, "pressure"), OxygenStore = Id(f, "o2store"), NitrogenStore = Id(f, "n2store"),
            AddedOxygenKg = Amount(f, "o2kg"), AddedNitrogenKg = Amount(f, "n2kg")
        };
        if (s.OxygenKPa > RegulatorRules.MaxOxygenKPa || s.PressureKPa > RegulatorRules.MaxPressureKPa) throw new FormatException("Regulator set point out of range.");
        return s;
    }
    private static string N(double v) => v.ToString("R", CultureInfo.InvariantCulture);
    private static bool Flag(IReadOnlyDictionary<string, string> f, string k) => f.TryGetValue(k, out var v) && (v == "1" || (v == "0" ? false : throw new FormatException("Invalid regulator flag.")));
    private static string Id(IReadOnlyDictionary<string, string> f, string k) =>
        f.TryGetValue(k, out var v) && (v.Length == 0 || ProcessorState.SafeId(v)) ? v : throw new FormatException("Invalid regulator link.");
    private static double Amount(IReadOnlyDictionary<string, string> f, string k) =>
        f.TryGetValue(k, out var t) && double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) && ManufacturingRules.Finite(v) && v >= 0 ? v : throw new FormatException("Invalid regulator amount.");
}
