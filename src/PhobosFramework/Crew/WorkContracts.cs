using System;
using System.Collections.Generic;

namespace Phobos.Ostranauts.Framework.Crew;

public sealed class CrewWorkContext
{
    public CondOwner Actor { get; }
    public CondOwner Equipment { get; }
    public StandingOrder Order { get; }
    public bool Skipping { get; }
    internal CrewWorkContext(CondOwner actor, CondOwner equipment, StandingOrder order, bool skipping)
    { Actor = actor; Equipment = equipment; Order = order; Skipping = skipping; }
}

public sealed class CrewWorkOffer
{
    public string Action { get; }
    public string Label { get; }
    public CrewRole Role { get; }
    public string Duty { get; }
    public string Skill { get; }
    public double Seconds { get; }
    public CondOwner Target { get; }
    public CondOwner? Cargo { get; }
    public CondOwner? Destination { get; }
    public CondOwner? Origin { get; }
    public bool SkipSupported { get; }
    public CrewWorkOffer(string action, string label, CrewRole role, CondOwner target, double seconds,
        string skill = "", string duty = "Operate", bool skipSupported = true, CondOwner? cargo = null, CondOwner? destination = null)
    { Action = action; Label = label; Role = role; Target = target; Seconds = seconds; Skill = skill; Duty = duty; SkipSupported = skipSupported; Cargo = cargo; Destination = destination; Origin = cargo?.objCOParent; }
}

public interface ICrewWorkProvider
{
    string Id { get; }
    bool Supports(CondOwner equipment);
    bool RoutineResume(CondOwner equipment);
    IReadOnlyList<string> Recipes(CondOwner equipment);
    string RecipeLabel(string recipe);
    CrewWorkOffer? Next(CondOwner equipment, StandingOrder order, out string reason);
    bool Complete(CrewWorkContext context, CrewWorkOffer offer, out string reason);
    void Suspend(CondOwner equipment);
}

/// <summary>Optional exact equipment-chain and mission-settings binding.</summary>
public interface ICrewBoundProvider
{
    /// <summary>Immutable mission scope captured only when the owner explicitly enables an order.</summary>
    string CaptureBinding(CondOwner equipment, StandingOrder order);
}

/// <summary>Only providers with measured power and material/thermal accounting opt into skipped machine ticks.</summary>
public interface ICrewSkipProvider
{
    bool CanAdvance(CondOwner equipment, out string reason);
    void BeforeSkip();
    void AfterSkip();
}

public sealed class CrewSpeciality
{
    public string Id { get; }
    public string Label { get; }
    public string Owner { get; }
    public CrewRole Role { get; }
    public CrewSpeciality(string id, string label, string owner, CrewRole role)
    { Id = id; Label = label; Owner = owner; Role = role; }
}
