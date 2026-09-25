using System;
using Phobos.Ostranauts.Framework.Processing;

namespace PhobosShipbreaker.Core;

public static class FurnaceRules
{
    public const string Prefix = "PhobosFurnace", Radiator = "PhobosFurnaceRadiator", Feed = Prefix + "InputBin", Slot = Prefix + "Input";
    public const string Blank = "PhobosFurnaceHousingBlank", Housing = "PhobosFurnaceHousing", Remainder = "PhobosFurnaceMeltRemainder";
    public const string Section = Prefix + "Section";
    public const string ThermalPort = Prefix + "ThermalPort";
    public const int Footprint = 6, RadiatorDepth = 4, ChargeUnits = 20, RecipeRevision = 1;
    public const double FeedUnitKg = 1, RemainderKg = 1, BlankKg = ChargeUnits * FeedUnitKg - RemainderKg;
    public const double RatingKg = 50, MachineKg = 240, RadiatorKg = 100, SectionKg = 80;
    public const double ReferenceK = 298.15, MeltK = 933.45, TargetK = 973.15, ReleaseK = 323.15;
    public const double SolidCp = 1.05, LiquidCp = 1.177, LatentKJ = 397, LiningCapacity = 30;
    public const double SinkCapacity = 80, SinkMaxK = 523.15, RadiatorArea = 12, BackgroundK = 200, Emissivity = .85;
    public const double Efficiency = .9, HeatLimitKW = 250, HeatAuxKW = 2, CoolAuxKW = 1;
    public const double HoldSeconds = 60, GasCv = .0208, GasR = .008314462618;
    public const double ChamberM3 = .08, ReceiverM3 = .05, VacuumKPa = .1, ReceiverLimitKPa = 200, PumpMolesPerSecond = .05;
    public const double ConductanceKW = .30, CoolingKW = 100, RoomLeakKW = .001, MaxStepSeconds = .25, MaxIntervalSeconds = 60;
    public const double InstrumentKW = .05, ProbeFreshSeconds = 5, MaxRoomK = 333.15, MinSealRoomK = 250;
    public const double DamagedRadiatorFraction = .25;
    public const double PumpTimeoutSeconds = 180, HotPressureKPa = .5, HoldToleranceK = .5, OvershootTripK = 5;
    public const double MinRamp = .1, MaxRamp = 5, MinPowerSettingKW = 1;
    public const double PairSpacingTiles = Footprint / 2.0 + 1 + RadiatorDepth / 2.0;
    public static bool Machine(string? id) => Family(id, Prefix);
    public static bool Cooling(string? id) => Family(id, Radiator) || Underside(id);
    public static bool Underside(string? id) => Family(id, ThermalPort);
    private static bool Family(string? id, string prefix) => id == prefix + "Installed" || id == prefix + "InstalledDmg" || id == prefix + "Loose" || id == prefix + "LooseDmg";
    public static double HeatCapacity(double gasMoles = 0) => LiningCapacity + ChargeUnits * SolidCp + gasMoles * GasCv;
    public static double Enthalpy(double kelvin, double liquid = 0, double gasMoles = 0)
    {
        double solid = HeatCapacity(gasMoles), melt = solid * (MeltK - ReferenceK);
        if (kelvin < MeltK) return solid * (kelvin - ReferenceK);
        if (kelvin == MeltK) return melt + ChargeUnits * LatentKJ * liquid;
        return melt + ChargeUnits * LatentKJ + (LiningCapacity + ChargeUnits * LiquidCp + gasMoles * GasCv) * (kelvin - MeltK);
    }
    public static double Temperature(double kJ, double gasMoles, out double liquid)
    {
        double melt = Enthalpy(MeltK, 0, gasMoles), fusion = ChargeUnits * LatentKJ;
        liquid = Math.Max(0, Math.Min(1, (kJ - melt) / fusion));
        return kJ < melt ? ReferenceK + kJ / HeatCapacity(gasMoles) :
            kJ <= melt + fusion ? MeltK : MeltK + (kJ - melt - fusion) / (LiningCapacity + ChargeUnits * LiquidCp + gasMoles * GasCv);
    }
    public static double Radiation(double kelvin) => ThermalMath.RadiationKW(kelvin, BackgroundK, RadiatorArea, Emissivity);
}
