using System;
using Phobos.Ostranauts.Framework.Processing;
using System.Collections.Generic;
using System.Linq;

namespace PhobosShipbreaker.Core;

public static class ProcessRules
{
    public const string Wall = "ItmWall1x1Loose";
    public const string Residue = "PhobosShipbreakerResidue";
    public const string Progress = "PhobosShipbreakerProgress";
    public const string Revision = "PhobosShipbreakerRecipeRevision";
    public const string Duration = "PhobosShipbreakerJobSeconds";
    public const string Working = "PhobosShipbreakerWorking";
    public const double InputKg = 24;
    public const double CycleSeconds = 60;
    public const double ActiveKW = 30;
    public const double IdleKW = 0.12;
    public const int FeedCapacity = 4;
    public const int Footprint = 4;
    public const int OutputSize = 8;
    public const double MachineKg = 160;
    public const string AssemblySection = "PhobosShipbreakerSection";
    public const string AssemblySectionCondition = "PhobosShipbreakerIsSection";
    public const double AssemblySectionKg = MachineKg / 2;
    public const double MassTolerance = Phobos.Ostranauts.Framework.Units.MassToleranceKg;
    public const double MinJobSeconds = ProcessJob.MinSeconds, MaxJobSeconds = ProcessJob.MaxSeconds;
    public const double MinimumConfiguredCycleSeconds = 10;
    public const int AccessRangeTiles = 4;
    public const double LegacyResidueKg = 13;

    public static bool MassMatches(double actual, double expected) => ProcessMaterial.MassMatches(actual, expected);
    public static bool Balanced(double input, IEnumerable<double> outputs) => ProcessMaterial.Balanced(input, outputs);
}
