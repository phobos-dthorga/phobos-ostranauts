using System;

namespace Phobos.Ostranauts.Framework.Processing;

/// <summary>Native gas species entering or leaving the air of a room, in kilograms: a working machine's
/// off-gas, oxygen a process releases, oxygen a fire consumes. Only the game's own room species are ever
/// added; removals are clamped to what the room holds, so the game's internal mole count never goes
/// negative. The game's alarms, poisoning, scrubbers and fires respond to the result by themselves.</summary>
public static class RoomGas
{
    /// <summary>The moles a removal may take: never more than the room holds of that species.</summary>
    public static double Clamped(double requestedMoles, double availableMoles)
    {
        if (double.IsNaN(requestedMoles) || double.IsInfinity(requestedMoles) || requestedMoles < 0 ||
            double.IsNaN(availableMoles) || double.IsInfinity(availableMoles)) throw new ArgumentException("Invalid gas amount.");
        return Math.Max(0, Math.Min(requestedMoles, availableMoles));
    }
    /// <summary>Releases <paramref name="kg"/> of a native species into the air and returns the kilograms added.</summary>
    public static double Emit(RoomHeat.Air air, string species, double kg)
    {
        if (air == null) throw new ArgumentNullException(nameof(air));
        double moles = NativeGasCanister.Moles(species, kg);
        if (moles <= 0) return 0;
        air.Gas.AddGasMols(species, moles, false);
        air.Gas.Run();
        return kg;
    }
    /// <summary>Takes up to <paramref name="kg"/> of a native species from the air and returns the kilograms taken.</summary>
    public static double Consume(RoomHeat.Air air, string species, double kg)
    {
        if (air == null) throw new ArgumentNullException(nameof(air));
        double taken = Clamped(NativeGasCanister.Moles(species, kg), NativeGasCanister.HeldMoles(air.Gas, species));
        if (taken <= 0) return 0;
        air.Gas.AddGasMols(species, -taken, false);
        air.Gas.Run();
        return NativeGasCanister.Kilograms(species, taken);
    }
    /// <summary>Kilograms of a species the air holds (committed plus pending).</summary>
    public static double HeldKg(RoomHeat.Air air, string species) =>
        air == null ? 0 : NativeGasCanister.Kilograms(species, NativeGasCanister.HeldMoles(air.Gas, species));
}
