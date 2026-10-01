using System;
using System.Collections.Generic;
using System.Linq;
using PhobosManufacturing.Core;

/// <summary>The A2 cabin air regulator's gas arithmetic (Dalton's law at fixed temperature and volume) and its saved record.</summary>
internal static class RegulatorChecks
{
    internal static void Run(Action<bool, string> check, Action<Action, string> throws)
    {
        static bool Near(double a, double b) => Math.Abs(a - b) < 1e-9;
        // A room of 1,000 mol at 100 kPa holding 150 mol of oxygen: oxygen is at 15 kPa.
        double add = RegulatorRules.OxygenMoles(1000, 100, 150, 21);
        check(Near(add, 60), "Oxygen to add is target x N / P minus what is there: 21 x 1000 / 100 - 150 = 60 mol");
        check(Near((150 + add) / (1000 + add) * 100 * (1000 + add) / 1000, 21), "After adding it, the oxygen partial pressure reaches the set point");
        check(RegulatorRules.OxygenMoles(1000, 100, 250, 21) == 0, "A room already above its oxygen set point gets none");
        // Thin air: 20 kPa of pure-ish nitrogen with no oxygen would need 1,050 mol for 21 kPa; the 30% cap limits it.
        double capped = RegulatorRules.OxygenMoles(1000, 20, 0, 21);
        check(Near(capped / (1000 + capped), RegulatorRules.MaxOxygenFraction), "Oxygen never passes the fire-safety share of the air");
        check(RegulatorRules.OxygenMoles(1000, RegulatorRules.MinRoomKPa - 1, 0, 21) == 0 && RegulatorRules.NitrogenMoles(1000, RegulatorRules.MinRoomKPa - 1, 101) == 0,
            "A room below the breach limit is never fed");
        check(RegulatorRules.OxygenMoles(1000, 100, 150, 99) == RegulatorRules.OxygenMoles(1000, 100, 150, RegulatorRules.MaxOxygenKPa), "Set points above the maximum are clamped");
        check(Near(RegulatorRules.NitrogenMoles(1000, 80, 101), 262.5), "Nitrogen to add is N x (target - P) / P: 1000 x 21 / 80 = 262.5 mol");
        check(RegulatorRules.NitrogenMoles(1000, 100, 0) == 0 && RegulatorRules.NitrogenMoles(1000, 102, 101) == 0, "Pressure left alone, or already above target, adds no nitrogen");
        check(RegulatorRules.OxygenMoles(double.NaN, 100, 0, 21) == 0 && RegulatorRules.NitrogenMoles(-1, 100, 101) == 0, "Invalid readings add nothing");
        check(RegulatorRules.OxygenTargets.All(t => t <= RegulatorRules.MaxOxygenKPa) && RegulatorRules.PressureTargets.All(t => t <= RegulatorRules.MaxPressureKPa) &&
            RegulatorRules.OxygenTargets.Contains(RegulatorRules.DefaultOxygenKPa), "Every offered set point is within the limits and the default is offered");
        check(RegulatorRules.IsFamily(RegulatorRules.Installed) && RegulatorRules.IsFamily(RegulatorRules.Prefix + "LooseDmg") && !RegulatorRules.IsFamily(FillerRules.Installed),
            "Every A2 form, and only those, belongs to the family");

        // The record: round trip, and anything malformed is refused rather than guessed.
        var state = new RegulatorState { On = true, OxygenKPa = 23, PressureKPa = 90, OxygenStore = "abc-123", NitrogenStore = "", AddedOxygenKg = 1.25, AddedNitrogenKg = 4 };
        var saved = state.Save();
        var back = RegulatorState.Read(saved);
        check(back.On && back.OxygenKPa == 23 && back.PressureKPa == 90 && back.OxygenStore == "abc-123" && back.NitrogenStore == "" && back.AddedOxygenKg == 1.25 && back.AddedNitrogenKg == 4,
            "The regulator record survives a save and load");
        var fresh = new RegulatorState();
        check(!fresh.On && fresh.OxygenKPa == RegulatorRules.DefaultOxygenKPa && fresh.PressureKPa == 0, "A new regulator starts switched off at the default oxygen set point, pressure left alone");
        throws(() => RegulatorState.Read(new Dictionary<string, string>(saved) { ["o2"] = "40" }), "An oxygen set point above the maximum is refused");
        throws(() => RegulatorState.Read(new Dictionary<string, string>(saved) { ["on"] = "yes" }), "A malformed switch is refused");
        throws(() => RegulatorState.Read(new Dictionary<string, string>(saved) { ["o2kg"] = "-1" }), "A negative total is refused");
        var extra = new Dictionary<string, string>(saved) { ["extra"] = "1" };
        throws(() => RegulatorState.Read(extra), "A record with unknown fields is refused");

        // Carbon dioxide for grow rooms (Manufacturing 0.27.0): the same Dalton arithmetic, below the game's warning band.
        check(Near(RegulatorRules.CarbonDioxideMoles(1000, 100, 0.2, 0.1), 0.8), "Carbon dioxide to add is target x N / P minus what is there: 0.1 x 1000 / 100 - 0.2 = 0.8 mol");
        check(RegulatorRules.CarbonDioxideMoles(1000, 100, 0, 0) == 0 && RegulatorRules.CarbonDioxideMoles(1000, 100, 5, 0.2) == 0 && RegulatorRules.CarbonDioxideMoles(1000, 5, 0, 0.2) == 0,
            "Left alone, already above its set point, or in a breached room, a room gets no carbon dioxide");
        check(Near(RegulatorRules.CarbonDioxideMoles(1000, 100, 0, 3), RegulatorRules.MaxCarbonDioxideKPa * 10) && RegulatorRules.MaxCarbonDioxideKPa < 0.3 &&
              RegulatorRules.CarbonDioxideTargets.All(t => t <= RegulatorRules.MaxCarbonDioxideKPa) && RegulatorRules.CarbonDioxideTargets.Contains(0),
            "Carbon dioxide set points, and any clamped request, stay under the game's 0.3 kPa warning band; 0 leaves it alone");
        var dosing = new RegulatorState { On = true, CarbonDioxideKPa = 0.1, CarbonDioxideStore = "c-1", AddedCarbonDioxideKg = 0.5 };
        var dosed = RegulatorState.Read(dosing.Save());
        check(dosing.Save().Count == RegulatorState.Fields && dosed.CarbonDioxideKPa == 0.1 && dosed.CarbonDioxideStore == "c-1" && dosed.AddedCarbonDioxideKg == 0.5,
            "The carbon dioxide set point, store and total survive a save and load");
        var legacy = new Dictionary<string, string>(saved); legacy.Remove("co2"); legacy.Remove("co2store"); legacy.Remove("co2kg");
        var old = RegulatorState.Read(legacy);
        check(legacy.Count == RegulatorState.LegacyFields && old.OxygenKPa == 23 && old.CarbonDioxideKPa == 0 && old.CarbonDioxideStore == "" && old.AddedCarbonDioxideKg == 0,
            "A regulator record saved before 0.27.0 reads with carbon dioxide left alone");
        var partial = new Dictionary<string, string>(saved); partial.Remove("co2kg");
        throws(() => RegulatorState.Read(partial), "A record with only part of the carbon dioxide fields is refused");
        throws(() => RegulatorState.Read(new Dictionary<string, string>(saved) { ["co2"] = "1" }), "A carbon dioxide set point above the maximum is refused");
    }
}
