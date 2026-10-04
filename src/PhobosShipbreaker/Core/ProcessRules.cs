using System;
using Phobos.Ostranauts.Framework.Processing;
using System.Collections.Generic;
using System.Linq;

namespace PhobosShipbreaker.Core;

public static class ProcessRules
{
    public const string Wall = "ItmWall1x1Loose";
    public const string InstalledWall = "ItmWall1x1";
    public const string Residue = "PhobosShipbreakerResidue";
    public const string Progress = "PhobosShipbreakerProgress";
    public const string Revision = "PhobosShipbreakerRecipeRevision";
    public const string Duration = "PhobosShipbreakerJobSeconds";
    public const string Working = "PhobosShipbreakerWorking";
    /// <summary>The plain ordinary wall's mass and the reference for revision 1.</summary>
    public const double StandardWallKg = 24;
    // The game's ordinary walls are cosmetic variants of one base with their own masses (14 to 48 kg
    // in the shipped data). A wall must carry the residue packet plus at least the parts.
    public const double MinimumWallKg = LegacyResidueKg + 1;
    // Heaviest ordinary wall the game ships (Langdon-Phillips "Glory Series", 48 kg).
    public const double MaximumWallKg = 48;
    public const double CycleSeconds = 60;
    /// <summary>Real seconds between an armed, idle machine's looks into its own inventory for feed put there by hand.</summary>
    public const double OwnFeedRecheckSeconds = 2;
    public const double ActiveKW = 30;
    public const double IdleKW = 0.12;
    public const int FeedCapacity = 4;
    public const int Footprint = 4;
    /// <summary>The D4 product tray in cells (Shipbreaker 0.65.0; it was 8 x 8): two of the heaviest wall's batches, or a
    /// full four-panel feed of standard walls, delivered into stacks.</summary>
    public const int TrayWidth = 4, TrayHeight = 3;
    public const double MachineKg = 160;
    /// <summary>The D4 definition prefix (Content.Prefix), named here for the economy pack.</summary>
    public const string Prefix = "PhobosShipbreaker";
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
    /// <summary>A whole-kilogram wall mass the panel recipe can account for.</summary>
    public static bool AcceptedWallKg(double mass)
    {
        if (double.IsNaN(mass) || double.IsInfinity(mass)) return false;
        double rounded = Math.Round(mass);
        return MassMatches(mass, rounded) && rounded >= MinimumWallKg && rounded <= MaximumWallKg;
    }
}
