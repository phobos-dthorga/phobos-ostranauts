using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Processing;

/// <summary>The native canister arithmetic and the room-gas clamps, on numbers alone. The live canister
/// reads and writes are exercised by the native suite against the game's own definitions.</summary>
internal static class GasCanisterChecks
{
    internal static void Run(Action<bool, string> check)
    {
        void Reject(Action action, string label) { bool failed = false; try { action(); } catch { failed = true; } check(failed, label); }
        // The game's shipped RTA canister: 0.787 m3 at 41,400 kPa and 293 K holds 13,373 mol of O2 when full.
        double capacity = NativeGasCanister.CapacityMoles(0.787, 41400, 293);
        check(Math.Abs(capacity - 13373) / 13373 < 0.001, "The rated capacity matches the game's full O2 canister within 0.1%");
        check(Math.Abs(NativeGasCanister.Moles("O2", 1.0) - 31.25) < 0.01, "One kilogram of oxygen is 31.25 mol by the game's molar mass");
        check(Math.Abs(NativeGasCanister.Kilograms("O2", NativeGasCanister.Moles("O2", 2.5)) - 2.5) < 1e-12, "Moles and kilograms round-trip");
        check(Math.Abs(NativeGasCanister.Kilograms("CO2", 1) - 0.04401) < 1e-12 && Math.Abs(NativeGasCanister.Kilograms("CO", 1) - 0.02801) < 1e-12,
            "Carbon dioxide and monoxide use the game's own constants");
        check(NativeGasCanister.RoomSpecies.SequenceEqual(new[] { "CH4", "CO", "CO2", "H2SO4", "N2", "NH3", "O2", "Smoke" }), "Only the game's eight room species may be added anywhere");
        check(!NativeGasCanister.IsRoomSpecies("H2") && !NativeGasCanister.IsRoomSpecies("H2O") && !NativeGasCanister.IsRoomSpecies("He2"),
            "Hydrogen, water vapour and helium are never room species, although the game has their masses");
        Reject(() => NativeGasCanister.Moles("H2", 1), "Hydrogen cannot become room or canister gas");
        Reject(() => NativeGasCanister.Moles("Plasma", 1), "An unknown species is refused, never created");
        Reject(() => NativeGasCanister.Moles("O2", -1), "A negative amount is refused");
        Reject(() => NativeGasCanister.CapacityMoles(0, 41400, 293), "A canister without volume has no capacity");
        Reject(() => NativeGasCanister.CapacityMoles(0.787, 41400, 0), "A canister at zero kelvin has no capacity");
        var reading = new CanisterReading("O2", 13000, capacity, 40245, 41400, 0.787, 293);
        check(Math.Abs(reading.HeadroomMoles - (capacity - 13000)) < 1e-9, "Headroom is capacity less contents");
        check(new CanisterReading("O2", capacity + 5, capacity, 41420, 41400, 0.787, 293).HeadroomMoles == 0, "An overfilled canister offers no headroom");
        // Room-gas removals never exceed what the room holds: the game's own count would go negative.
        check(RoomGas.Clamped(10, 25) == 10 && RoomGas.Clamped(30, 25) == 25 && RoomGas.Clamped(30, 0) == 0 && RoomGas.Clamped(30, -1) == 0,
            "A removal is clamped to the moles present");
        Reject(() => RoomGas.Clamped(-1, 25), "A negative removal is refused");
        Reject(() => RoomGas.Clamped(double.NaN, 25), "An invalid removal is refused");
    }
}
