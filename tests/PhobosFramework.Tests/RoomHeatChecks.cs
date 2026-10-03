using System;
using Phobos.Ostranauts.Framework.Processing;

/// <summary>The shared air-cooled operating rule, on numbers alone: the reclaimer's cases, so promoting the
/// arithmetic into Framework changed nothing.</summary>
internal static class RoomHeatChecks
{
    internal static void Run(Action<bool, string> check)
    {
        void Reject(Action action, string label) { bool failed = false; try { action(); } catch { failed = true; } check(failed, label); }
        check(RoomHeat.GasHeatCapacityJPerMolK == 20.7 && Math.Abs(RoomHeat.MaxRoomKelvin - 313.15) < 1e-9 && RoomHeat.MinPressureKPa == 10,
            "The rule keeps the game's 20.7 J per mol K, the 40 C ceiling and the 10 kPa floor");
        check(RoomHeat.Budget(10000, 290, 0, 100, 12, 120, out double rise) && Math.Abs(rise * 10000 * RoomHeat.GasHeatCapacityJPerMolK - 1440000) < .0001,
            "A 12 kW, 120 s step becomes 1.44 MJ of room heat");
        check(!RoomHeat.Budget(10000, 310, 0, 100, 12, 120, out _), "A hot room blocks the step before electricity is requested");
        check(!RoomHeat.Budget(10000, 290, 20, 100, 12, 120, out _), "Pending heat from another machine counts toward the ceiling");
        check(!RoomHeat.Budget(0, 290, 0, 0, 12, 120, out _), "Vacuum is not a free heat sink");
        check(!RoomHeat.Budget(10000, 290, 0, 9.99, 12, 120, out _), "Low pressure stops air-cooled operation");
        foreach (double value in new[] { double.NaN, double.PositiveInfinity, -1d, 3601d })
            check(!RoomHeat.Budget(10000, 290, 0, 100, 12, value, out _), "An invalid power interval is refused");
        check(!RoomHeat.Admit(null, 12, 120, out double none) && none == 0, "No room means no admission");
        check(Math.Abs(RoomHeat.RiseKelvin(10000, 0.4) - 1440000 / (10000 * 20.7)) < 1e-9, "A deposit of 0.4 kWh warms 10,000 mol by the same 1.44 MJ");
        check(RoomHeat.RiseKelvin(10000, 0) == 0, "Nothing supplied warms nothing");
        Reject(() => RoomHeat.RiseKelvin(0, 1), "A deposit into no gas is refused");
        Reject(() => RoomHeat.RiseKelvin(10000, -1), "A negative deposit is refused");
        Reject(() => RoomHeat.RiseKelvin(10000, double.NaN), "An invalid deposit is refused");

        // Framework 0.78.0: the game's own painted jobs, field for field as the PDA builds them.
        var haulTask = Phobos.Ostranauts.Framework.Crew.NativeJobs.HaulTask("abc");
        check(haulTask == ("Haul", "ACTHaulItem", "abc", "HaulJobabc"), "A Haul job is the PDA's: Haul duty, the game's haul action, the item, the PDA's task name");
        var mineTask = Phobos.Ostranauts.Framework.Crew.NativeJobs.MineDepositTask("abc");
        check(mineTask == ("Demolish", "ACTMineDeposit", "abc", "MineJobabc"), "A deposit job is the PDA's: Demolish duty, the Mine Deposit action, the deposit, the PDA's task name");

        // Framework 0.76.0 (owner decision, 3 October 2026): our machines do not work in vacuum, and say so.
        var air = RoomHeat.Decide(true, 10000, 290, 0, 100, 12, 120);
        check(air.Admitted && air.PressureKPa == 100, "Air of 10 kPa or more under 40 C takes the heat");
        var warm = RoomHeat.Decide(true, 10000, 310, 0, 100, 12, 120);
        check(warm.Problem == RoomHeat.HeatProblem.RoomTooWarm && Math.Abs(warm.RoomCelsius - 36.85) < 0.01, "A warm room waits, with its temperature");
        var vacuum = RoomHeat.Decide(false, 0, 0, 0, 0, 1.0, 60);
        check(vacuum.Problem == RoomHeat.HeatProblem.NoAir && vacuum.PressureKPa == 0, "No room or vacuum: no air, whatever the load");
        var thin = RoomHeat.Decide(true, 50, 290, 0, 5, 1.0, 60);
        check(thin.Problem == RoomHeat.HeatProblem.NoAir && thin.PressureKPa == 5, "Air under 10 kPa counts as no air, with its pressure");
        check(RoomHeat.Check(null, 1.0, 60).Problem == RoomHeat.HeatProblem.NoAir, "A machine with no room has no air");
        foreach (double kw in new[] { 0d, -1d, double.NaN })
            check(RoomHeat.Decide(false, 0, 0, 0, 0, kw, 60).Problem == RoomHeat.HeatProblem.Invalid, "An invalid load is refused");
    }
}
