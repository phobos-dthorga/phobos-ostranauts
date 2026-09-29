using System;
using System.Collections.Generic;
using PhobosManufacturing.Core;

/// <summary>The X2 electrolysis contract on numbers alone: the mass split, the energy budget, one cycle per
/// step at most, and the saved record.</summary>
internal static class ProcessorChecks
{
    internal static void Run(Action<bool, string> check, Action<Action, string> throws)
    {
        check(ProcessorRules.Balanced() && ProcessorRules.WaterKgPerCycle == 1.125 && ProcessorRules.OxygenKgPerCycle == 1 && ProcessorRules.HydrogenKgPerCycle == .125,
            "1.125 kg of water becomes 1.000 kg of oxygen and 0.125 kg of hydrogen");
        check(Math.Abs(ProcessorRules.OxygenMolesPerCycle - 31.25) < .01, "One kilogram of oxygen is 31.25 mol by the game's molar mass");
        check(ProcessorRules.MinimumKWhPerCycle > 4.9 && ProcessorRules.MinimumKWhPerCycle < 5.0 && ProcessorRules.CycleKWh == 6,
            "The higher heating value of 1.125 kg of water is 4.9 kWh; the cycle supplies 6 kWh");
        check(ProcessorRules.RoomHeatFraction > .15 && ProcessorRules.RoomHeatFraction < .2 && Math.Abs(ProcessorRules.RoomHeatKW(true) - ProcessorRules.WorkingKW * ProcessorRules.RoomHeatFraction) < 1e-12 && ProcessorRules.RoomHeatKW(false) == ProcessorRules.IdleKW,
            "The share above the chemical minimum, about a sixth, warms the room");
        var (credited, complete) = ProcessorRules.Advance(0, 2.5);
        check(credited == 2.5 && !complete, "Energy is credited to the cycle");
        (credited, complete) = ProcessorRules.Advance(5.5, .5);
        check(credited == 6 && complete, "The cycle completes at 6 kWh");
        (credited, complete) = ProcessorRules.Advance(5.5, 20);
        check(credited == 6 && complete, "Energy beyond a completion is not banked: never two cycles in one step");
        throws(() => ProcessorRules.Advance(6, 1), "A completed cycle must be settled before more energy is credited");
        throws(() => ProcessorRules.Advance(double.NaN, 1), "Invalid energy is refused");
        throws(() => ProcessorRules.Advance(1, -1), "Negative energy is refused");
        var state = new ProcessorState { HoldKg = 1.125, CycleKWh = 3.25, ProducedO2Kg = 12, ProducedH2Kg = 1.5, CabinO2Kg = 2, Cycles = 12, Canister = "can-7" };
        var read = ProcessorState.Read(state.Save());
        check(read.HoldKg == 1.125 && read.CycleKWh == 3.25 && read.ProducedO2Kg == 12 && read.ProducedH2Kg == 1.5 && read.CabinO2Kg == 2 && read.Cycles == 12 && read.Canister == "can-7", "The processor record round-trips");
        check(ProcessorState.Read(new ProcessorState().Save()).Canister == "", "An empty record has no canister");
        throws(() => ProcessorState.Read(new Dictionary<string, string> { ["hold"] = "2" }), "A hold beyond one cycle's water is refused");
        throws(() => ProcessorState.Read(new Dictionary<string, string> { ["cycle"] = "6" }), "A completed cycle is never saved unsettled");
        throws(() => ProcessorState.Read(new Dictionary<string, string> { ["o2"] = "-1" }), "Negative totals are refused");
        throws(() => ProcessorState.Read(new Dictionary<string, string> { ["mystery"] = "1" }), "An unknown field is refused");
        check(ProcessorState.SafeId("RTA-12") && !ProcessorState.SafeId("a,b"), "A canister id must be safe for the record");
    }
}
