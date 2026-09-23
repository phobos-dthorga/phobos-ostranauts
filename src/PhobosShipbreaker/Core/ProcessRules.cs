using System;
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
    public const int RecipeRevision = 1;
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
    public const double MassTolerance = 0.000001;

    public static readonly IReadOnlyList<ProductSpec> Products = new[] {
        new ProductSpec("ItmPartsMechSmall01", 2, 0.5),
        new ProductSpec("ItmScrapAluminum", 2, 1),
        new ProductSpec("ItmScrapCarbonFiber", 2, 1),
        new ProductSpec("ItmScrapSteel", 6, 1),
        new ProductSpec(Residue, 1, 13)
    };

    public static bool MassMatches(double actual, double expected) =>
        !double.IsNaN(actual) && !double.IsInfinity(actual) &&
        Math.Abs(actual - expected) <= MassTolerance;

    public static bool Balanced(double input, IEnumerable<double> outputs) =>
        outputs.All(m => m > 0 && !double.IsInfinity(m)) && MassMatches(outputs.Sum(), input);
}

public sealed class ProductSpec
{
    public string Id { get; }
    public int Count { get; }
    public double Kg { get; }
    public ProductSpec(string id, int count, double kg) { Id = id; Count = count; Kg = kg; }
}

/// <summary>Session binding; saved progress belongs to the actual input object.</summary>
public sealed class ProcessJob
{
    public string InputId { get; }
    public double Progress { get; private set; }
    public bool Running { get; private set; } = true;
    public double Duration { get; }
    public bool Complete => Progress >= Duration;

    public ProcessJob(string inputId, double progress, int revision, double duration = ProcessRules.CycleSeconds)
    {
        if (string.IsNullOrWhiteSpace(inputId) || revision != ProcessRules.RecipeRevision ||
            double.IsNaN(progress) || double.IsInfinity(progress) || progress < 0 || progress > duration ||
            double.IsNaN(duration) || double.IsInfinity(duration) || duration < 1 || duration > 3600)
            throw new ArgumentException("Unsupported saved job");
        InputId = inputId; Progress = progress; Duration = duration;
    }

    public double Advance(string currentInputId, double seconds, bool powered, bool ready)
    {
        if (!Running) return 0;
        if (currentInputId != InputId || !ready || double.IsNaN(seconds) || double.IsInfinity(seconds) ||
            seconds < 0 || seconds > Duration)
        { Running = false; return 0; }
        if (!powered) return 0;
        double credited = Math.Min(seconds, Duration - Progress);
        Progress += credited;
        return credited;
    }

    public void Pause() => Running = false;
}
