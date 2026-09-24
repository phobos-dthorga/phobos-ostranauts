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
    public const double MinJobSeconds = 1, MaxJobSeconds = 3600;
    public const double MinimumConfiguredCycleSeconds = 10;
    public const int AccessRangeTiles = 4;
    public const double LegacyResidueKg = 13;

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
    public ProcessRecipe Recipe { get; }
    public bool Complete => Progress >= Duration;

    public ProcessJob(string inputId, double progress, ProcessRecipe recipe, double duration = ProcessRules.CycleSeconds)
    {
        if (string.IsNullOrWhiteSpace(inputId) || recipe == null ||
            double.IsNaN(progress) || double.IsInfinity(progress) || progress < 0 || progress > duration ||
            double.IsNaN(duration) || double.IsInfinity(duration) || duration < ProcessRules.MinJobSeconds || duration > ProcessRules.MaxJobSeconds)
            throw new ArgumentException(Text.Get("ProcessRules.invalid_saved_progress_or_duration_panel_retained"));
        InputId = inputId; Progress = progress; Duration = duration; Recipe = recipe;
    }

    // Native numeric conditions are the complete saved contract. Zero revision is
    // fresh stock only when the other fields are zero too; never round a revision
    // or reinterpret an unknown/partial record as a new job.
    public static ProcessJob CreateOrResume(ProcessRecipeCatalog recipes, string inputId,
        double progress, double revision, double duration, double newJobSeconds)
    {
        if (revision == 0)
        {
            if (progress != 0 || duration != 0)
                throw new ArgumentException(Text.Get("ProcessRules.incomplete_saved_job_panel_retained_cancel_work"));
            return new ProcessJob(inputId, 0, recipes.Current, newJobSeconds);
        }
        if (!recipes.TryGet(revision, out var recipe))
            throw new ArgumentException(Text.Get("ProcessRules.unsupported_saved_recipe_revision_panel_retained_restore", revision));
        if (duration == 0)
        {
            if (!recipe!.LegacySeconds.HasValue)
                throw new ArgumentException(Text.Get("ProcessRules.saved_duration_is_missing_panel_retained_cancel"));
            duration = recipe.LegacySeconds.Value;
        }
        return new ProcessJob(inputId, progress, recipe!, duration);
    }

    public bool MatchesSaved(string inputId, double progress, double revision, double duration) =>
        inputId == InputId && progress == Progress && revision == Recipe.Revision && duration == Duration;

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
