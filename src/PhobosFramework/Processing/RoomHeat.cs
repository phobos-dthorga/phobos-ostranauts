using System;

namespace Phobos.Ostranauts.Framework.Processing;

/// <summary>Heat a working machine pays into the air of its room, under the shared air-cooled operating
/// rule: the room must hold gas above 10 kPa and stay below 40 C after the step, and a vacuum is never a
/// free heat sink. The arithmetic is the reclaimer's (20.7 J per mol K, the game's own Heater constant);
/// the native <c>GasContainer</c> mixing and cooling follow once the pending temperature is applied.</summary>
public static class RoomHeat
{
    public const double GasHeatCapacityJPerMolK = 20.7, MaxRoomKelvin = 40 + Units.CelsiusToKelvin, MinPressureKPa = 10;
    public const double JoulesPerKilojoule = 1000, JoulesPerKilowattHour = 3600 * 1000;

    /// <summary>Whether a step of <paramref name="kw"/> for <paramref name="seconds"/> keeps the room below the
    /// ceiling, and the temperature rise it would cause. Refuses vacuum, low pressure, invalid inputs and
    /// steps longer than one processing hour.</summary>
    public static bool Budget(double mols, double kelvin, double pendingKelvin, double pressureKPa, double kw, double seconds, out double riseKelvin)
    {
        riseKelvin = 0;
        foreach (double value in new[] { mols, kelvin, pendingKelvin, pressureKPa, kw, seconds })
            if (double.IsNaN(value) || double.IsInfinity(value)) return false;
        if (mols <= 0 || pressureKPa < MinPressureKPa || kelvin <= 0 || kelvin + pendingKelvin <= 0 ||
            kw <= 0 || seconds < 0 || seconds > ProcessJob.MaxSeconds) return false;
        riseKelvin = kw * JoulesPerKilojoule * seconds / (mols * GasHeatCapacityJPerMolK);
        return kelvin + pendingKelvin + riseKelvin < MaxRoomKelvin;
    }

    /// <summary>The temperature rise of <paramref name="mols"/> of room gas receiving <paramref name="kWh"/>.</summary>
    public static double RiseKelvin(double mols, double kWh)
    {
        if (double.IsNaN(mols) || double.IsInfinity(mols) || mols <= 0 || double.IsNaN(kWh) || double.IsInfinity(kWh) || kWh < 0)
            throw new ArgumentException("Invalid room heat deposit.");
        return kWh * JoulesPerKilowattHour / (mols * GasHeatCapacityJPerMolK);
    }

    /// <summary>The air a machine can heat: its room's gas container and the values the budget needs.</summary>
    public sealed class Air
    {
        public CondOwner Room { get; }
        public GasContainer Gas { get; }
        public double Mols { get; }
        public double Kelvin => Room.GetCondAmount("StatGasTemp");
        public double PendingKelvin => Gas.fDGasTemp;
        public double PressureKPa => Room.GetCondAmount("StatGasPressure");
        internal Air(CondOwner room, GasContainer gas, double mols) { Room = room; Gas = gas; Mols = mols; }
    }

    /// <summary>The room around a machine's named point, or null when it stands in no room or the room holds
    /// no committed gas total (a vacuum, or a container not yet initialised).</summary>
    public static Air? Read(CondOwner machine, string point = "use")
    {
        var room = machine?.ship?.GetRoomAtWorldCoords1(machine.GetPos(point), false)?.CO;
        var gas = room?.GasContainer;
        if (gas == null || !gas.mapGasMols1.TryGetValue("StatGasMolTotal", out double mols)) return null;
        return new Air(room!, gas, mols);
    }

    /// <summary>Whether the air can take this step's heat; null air is never free cooling.</summary>
    public static bool Admit(Air? air, double kw, double seconds, out double riseKelvin)
    {
        riseKelvin = 0;
        return air != null && Budget(air.Mols, air.Kelvin, air.PendingKelvin, air.PressureKPa, kw, seconds, out riseKelvin);
    }

    /// <summary>Pays the declared fraction of supplied energy into the air as a pending temperature change.</summary>
    public static void Deposit(Air air, double suppliedKWh, double fraction = 1)
    {
        if (air == null) throw new ArgumentNullException(nameof(air));
        if (double.IsNaN(fraction) || fraction < 0 || fraction > 1) throw new ArgumentException("Invalid heat fraction.");
        air.Gas.fDGasTemp += RiseKelvin(air.Mols, suppliedKWh * fraction);
    }
}
