using System;
using System.Collections.Generic;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Processing;

/// <summary>Immutable mass-balanced processing outputs. Published revisions must never be rewritten.</summary>
public sealed class ProcessRecipe
{
    public double InputKg { get; }
    public int Revision { get; }
    public IReadOnlyList<ProductSpec> Products { get; }
    // Only revisions actually shipped without a saved duration may supply this fallback.
    public double? LegacySeconds { get; }

    public ProcessRecipe(int revision, double inputKg, IEnumerable<ProductSpec> products, double? legacySeconds = null)
    {
        var copy = products.ToArray();
        if (revision < 1 || copy.Length == 0 || copy.Any(p => p == null ||
            string.IsNullOrWhiteSpace(p.Id) || p.Count <= 0 || p.Kg <= 0 || double.IsNaN(p.Kg) || double.IsInfinity(p.Kg)) ||
            copy.Select(p => p.Id).Distinct(StringComparer.Ordinal).Count() != copy.Length ||
            !ProcessMaterial.Balanced(inputKg, copy.Select(p => p.Kg * p.Count)) ||
            (legacySeconds.HasValue && (double.IsNaN(legacySeconds.Value) ||
                double.IsInfinity(legacySeconds.Value) || legacySeconds < ProcessJob.MinSeconds || legacySeconds > ProcessJob.MaxSeconds)))
            throw new ArgumentException(Text.Get("ProcessRecipes.invalid_wall_recipe_or_material_balance"));
        InputKg = inputKg; Revision = revision; Products = Array.AsReadOnly(copy); LegacySeconds = legacySeconds;
    }
}

/// <summary>Current selection is for fresh inputs; saved revisions resolve independently.</summary>
public sealed class ProcessRecipeCatalog
{
    private readonly Dictionary<double, ProcessRecipe> revisions;
    public ProcessRecipe Current { get; }
    public IReadOnlyList<ProcessRecipe> Recipes { get; }

    public ProcessRecipeCatalog(int currentRevision, IEnumerable<ProcessRecipe> recipes)
    {
        var copy = recipes.ToArray();
        revisions = copy.ToDictionary(r => (double)r.Revision);
        if (!revisions.TryGetValue(currentRevision, out var current))
            throw new ArgumentException(Text.Get("ProcessRecipes.current_wall_recipe_is_not_registered"));
        Current = current; Recipes = Array.AsReadOnly(copy);
    }

    public bool TryGet(double revision, out ProcessRecipe? recipe) => revisions.TryGetValue(revision, out recipe);
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
    public const double MinSeconds = 1, MaxSeconds = 3600;
    public string InputId { get; }
    public double Progress { get; private set; }
    public bool Running { get; private set; } = true;
    public double Duration { get; }
    public ProcessRecipe Recipe { get; }
    public bool Complete => Progress >= Duration;

    public ProcessJob(string inputId, double progress, ProcessRecipe recipe, double duration)
    {
        if (string.IsNullOrWhiteSpace(inputId) || recipe == null ||
            double.IsNaN(progress) || double.IsInfinity(progress) || progress < 0 || progress > duration ||
            double.IsNaN(duration) || double.IsInfinity(duration) || duration < MinSeconds || duration > MaxSeconds)
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

public static class ProcessMaterial
{
    public static bool MassMatches(double actual, double expected) => !double.IsNaN(actual) && !double.IsInfinity(actual) &&
        !double.IsNaN(expected) && !double.IsInfinity(expected) && Math.Abs(actual - expected) <= Units.MassToleranceKg;
    public static bool Balanced(double input, IEnumerable<double> outputs) => input > 0 && !double.IsInfinity(input) &&
        outputs.All(m => m > 0 && !double.IsInfinity(m)) && MassMatches(outputs.Sum(), input);
}
