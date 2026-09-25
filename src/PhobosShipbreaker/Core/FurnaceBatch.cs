using System;
using Phobos.Ostranauts.Framework.Processing;

namespace PhobosShipbreaker.Core;

public enum FurnacePhase { Idle, Sealed, Evacuating, Preheat, Melt, Hold, Solidify, Cool, Equalize, Ready, Delivering }

/// <summary>Content-owned closed-batch physics. No Unity, inventory mutations or UI dependencies.</summary>
public sealed class FurnaceBatch
{
    public FurnacePhase Phase;
    public double HotKJ, SinkKJ, Hold, PumpSeconds;
    public double HeatCapKW = FurnaceRules.HeatLimitKW, RampKPerSecond = 2, CoolingCapKW = FurnaceRules.CoolingKW;
    public bool Qualified, Armed, StepMode, StepWaiting;
    public readonly GasParcel Chamber = new(FurnaceRules.GasCv), Receiver = new(FurnaceRules.GasCv);
    public double TemperatureK => Phase == FurnacePhase.Idle ? FurnaceRules.ReferenceK + HotKJ / FurnaceRules.LiningCapacity : FurnaceRules.Temperature(HotKJ, Chamber.Moles, out _);
    public double LiquidFraction { get { FurnaceRules.Temperature(HotKJ, Chamber.Moles, out double liquid); return liquid; } }
    public double SinkK => FurnaceRules.ReferenceK + SinkKJ / FurnaceRules.SinkCapacity;
    public double PressureKPa => Chamber.Moles * FurnaceRules.GasR * TemperatureK / FurnaceRules.ChamberM3;
    public double ReceiverKPa => Receiver.Moles * FurnaceRules.GasR * Receiver.TemperatureK / FurnaceRules.ReceiverM3;
    public double TotalKJ => HotKJ + SinkKJ + Chamber.Moles * FurnaceRules.GasCv * FurnaceRules.ReferenceK + Receiver.EnergyKJ;
    public bool Heating => Phase == FurnacePhase.Preheat || Phase == FurnacePhase.Melt || Phase == FurnacePhase.Hold;
    public bool SafeOpen => TemperatureK <= FurnaceRules.ReleaseK && !double.IsNaN(TemperatureK);
    public void Seal(GasParcel captured, double feedKelvin = FurnaceRules.ReferenceK)
    {
        if (Phase != FurnacePhase.Idle || !SafeOpen || Chamber.Moles != 0) throw new InvalidOperationException("Batch cannot seal.");
        Chamber.Add(captured);
        HotKJ += FurnaceRules.ChargeUnits * FurnaceRules.SolidCp * (feedKelvin - FurnaceRules.ReferenceK);
        HotKJ += captured.EnergyKJ - captured.Moles * FurnaceRules.GasCv * FurnaceRules.ReferenceK;
        Phase = FurnacePhase.Sealed; Hold = 0; Qualified = false; PumpSeconds = 0;
        Chamber.SetTemperature(TemperatureK);
    }
    public void Stop() { Armed = false; Hold = 0; StepWaiting = false; if (Phase != FurnacePhase.Idle && Phase < FurnacePhase.Solidify) Phase = FurnacePhase.Solidify; }
    public void Resume()
    {
        if (Phase == FurnacePhase.Idle || Phase >= FurnacePhase.Equalize) return;
        Armed = true; StepWaiting = false;
        if (!Qualified && Phase >= FurnacePhase.Solidify) Phase = PressureKPa <= FurnaceRules.HotPressureKPa ? FurnacePhase.Preheat : FurnacePhase.Evacuating;
        if (Phase == FurnacePhase.Sealed) Phase = FurnacePhase.Evacuating;
    }
    public void AdvanceStep() { StepWaiting = false; }
    public double RequestedKJ(double dt, bool coolingConnected, double reservedSinkKJ = 0)
    {
        if (!ThermalMath.Finite(dt) || dt <= 0 || dt > FurnaceRules.MaxIntervalSeconds) return 0;
        if (!Armed || StepWaiting || !coolingConnected || SinkK >= FurnaceRules.SinkMaxK || Phase >= FurnacePhase.Equalize) return 0;
        double headroom = Math.Max(0, FurnaceRules.SinkCapacity * (FurnaceRules.SinkMaxK - SinkK) - reservedSinkKJ);
        double aux = Math.Min(headroom, (Heating || Phase == FurnacePhase.Evacuating ? FurnaceRules.HeatAuxKW : FurnaceRules.CoolAuxKW) * dt);
        if (!Heating || PressureKPa > FurnaceRules.HotPressureKPa) return aux;
        double need = Math.Max(0, FurnaceRules.Enthalpy(FurnaceRules.TargetK, 1, Chamber.Moles) - HotKJ);
        double rate = Math.Min(HeatCapKW, TemperatureK < FurnaceRules.MeltK - .1 ? RampKPerSecond * FurnaceRules.HeatCapacity(Chamber.Moles) : HeatCapKW);
        double heat = Math.Min(need, rate * dt);
        heat = Math.Min(heat, (headroom - aux) * FurnaceRules.Efficiency / (1 - FurnaceRules.Efficiency));
        return aux + heat / FurnaceRules.Efficiency;
    }
    /// <summary>Sealed-loop circulation, bounded by actual incremental pump energy.
    /// Its electricity is retained in the cold node; no credit survives this interval.</summary>
    public double Circulate(double seconds, double pumpKJ) => Circulate(seconds,pumpKJ,1);
    public double Circulate(double seconds, double pumpKJ, double hydraulicFraction)
    {
        if(!ThermalMath.Finite(hydraulicFraction)||hydraulicFraction<0||hydraulicFraction>1) throw new ArgumentOutOfRangeException(nameof(hydraulicFraction));
        if (!ThermalMath.Finite(seconds) || seconds <= 0 || seconds > FurnaceRules.MaxIntervalSeconds ||
            !ThermalMath.Finite(pumpKJ) || pumpKJ < 0 || pumpKJ > FurnaceCooling.PumpKW * seconds + 1e-7)
            throw new ArgumentOutOfRangeException(nameof(pumpKJ));
        SinkKJ += pumpKJ;
        double fraction = Math.Min(1, pumpKJ / (FurnaceCooling.PumpKW * seconds)), moved = 0;
        while (seconds > 1e-9)
        {
            double dt = Math.Min(seconds, FurnaceRules.MaxStepSeconds); seconds -= dt;
            moved += CoolingStep(dt * fraction * hydraulicFraction);
        }
        if (SinkK > FurnaceRules.SinkMaxK) Armed = false;
        Chamber.SetTemperature(TemperatureK);
        return moved;
    }
    private double CoolingStep(double seconds, double extraHeadroom = 0)
    {
        double cool = !Heating || !Armed ? Math.Max(0, Math.Min(CoolingCapKW, FurnaceRules.ConductanceKW * (TemperatureK - SinkK))) * seconds : 0;
        double equilibrium = Phase == FurnacePhase.Idle ? FurnaceRules.LiningCapacity * (SinkK - FurnaceRules.ReferenceK) : FurnaceRules.Enthalpy(SinkK, 0, Chamber.Moles);
        cool = Math.Min(cool, Math.Max(0, HotKJ - equilibrium));
        cool = Math.Min(cool, Math.Max(0, FurnaceRules.SinkCapacity * (FurnaceRules.SinkMaxK - SinkK) + extraHeadroom));
        HotKJ -= cool; SinkKJ += cool;
        return cool;
    }
    /// <summary>Apply exactly a measured receipt after reserving capacity. Any unexpected excess
    /// remains stored and trips heating; energy is never silently clamped away.</summary>
    public void Receive(double energyKJ, double dt)
    {
        if (!ThermalMath.Finite(energyKJ) || energyKJ < 0 || !ThermalMath.Finite(dt) || dt <= 0) throw new ArgumentOutOfRangeException(nameof(energyKJ));
        double aux = Math.Min(energyKJ, (Heating || Phase == FurnacePhase.Evacuating ? FurnaceRules.HeatAuxKW : FurnaceRules.CoolAuxKW) * dt);
        double heater = Heating && Armed && !StepWaiting ? energyKJ - aux : 0;
        HotKJ += heater * FurnaceRules.Efficiency;
        SinkKJ += energyKJ - heater * FurnaceRules.Efficiency;
        if (Phase == FurnacePhase.Evacuating && Armed && !StepWaiting)
        {
            double moles = Math.Min(FurnaceRules.PumpMolesPerSecond * aux / FurnaceRules.HeatAuxKW,
                Math.Max(0, Chamber.Moles - FurnaceRules.VacuumKPa * FurnaceRules.ChamberM3 / (FurnaceRules.GasR * TemperatureK)));
            double capacity = Math.Max(0, (FurnaceRules.ReceiverLimitKPa - ReceiverKPa) * FurnaceRules.ReceiverM3 / (FurnaceRules.GasR * TemperatureK));
            moles = Math.Min(moles, capacity);
            Chamber.SetTemperature(TemperatureK);
            var gas = Chamber.Take(moles);
            HotKJ -= gas.EnergyKJ - gas.Moles * FurnaceRules.GasCv * FurnaceRules.ReferenceK;
            Receiver.Add(gas); PumpSeconds += dt;
            if (PressureKPa <= FurnaceRules.VacuumKPa + 1e-7) Transition(FurnacePhase.Preheat);
            else if (PumpSeconds >= FurnaceRules.PumpTimeoutSeconds || moles == 0) { Armed = false; }
        }
        if (SinkK > FurnaceRules.SinkMaxK + 1e-6 || TemperatureK > FurnaceRules.TargetK + FurnaceRules.OvershootTripK) Armed = false;
        Chamber.SetTemperature(TemperatureK);
    }
    /// <summary>Passive time always advances independently of heating permission. Room transfer
    /// is bounded by the caller's actual accepting room budget, and reported for native debit.</summary>
    public (double Radiated, double Room) Passive(double seconds, bool exterior, bool connected, double roomK, double roomCapacityKJ, bool probeValid)
    {
        if (!ThermalMath.Finite(seconds) || seconds < 0 || seconds > FurnaceRules.MaxIntervalSeconds ||
            !ThermalMath.Finite(roomK) || !ThermalMath.Finite(roomCapacityKJ) || roomCapacityKJ < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        double radiated = 0, room = 0;
        if (!probeValid) { Armed = false; Hold = 0; }
        while (seconds > 1e-9)
        {
            double dt = Math.Min(seconds, FurnaceRules.MaxStepSeconds); seconds -= dt;
            double t = TemperatureK, sink = SinkK;
            double loss = Math.Min(Math.Max(0, FurnaceRules.RoomLeakKW * (t - roomK)) * dt, roomCapacityKJ - room);
            double rad = exterior ? FurnaceRules.Radiation(sink) * dt : 0;
            if (connected) CoolingStep(dt, rad);
            HotKJ -= loss; SinkKJ -= rad; radiated += rad; room += loss;
            if (Heating && Armed && !StepWaiting && probeValid && PressureKPa <= FurnaceRules.HotPressureKPa && Math.Abs(TemperatureK - FurnaceRules.TargetK) <= FurnaceRules.HoldToleranceK)
            {
                if (Phase != FurnacePhase.Hold) Transition(FurnacePhase.Hold);
                if (!StepWaiting) Hold += dt;
                if (Hold >= FurnaceRules.HoldSeconds) { Qualified = true; Transition(FurnacePhase.Solidify); }
            }
            else if (Heating) Hold = 0;
            if (Phase == FurnacePhase.Preheat && TemperatureK >= FurnaceRules.MeltK) Transition(FurnacePhase.Melt);
            if (Phase == FurnacePhase.Solidify && LiquidFraction == 0) Transition(FurnacePhase.Cool);
            if (Phase == FurnacePhase.Cool && SafeOpen) Transition(FurnacePhase.Equalize);
        }
        Chamber.SetTemperature(TemperatureK);
        return (radiated, room);
    }
    private void Transition(FurnacePhase phase) { Phase = phase; if (StepMode && phase != FurnacePhase.Solidify && phase != FurnacePhase.Cool) StepWaiting = true; }
}
