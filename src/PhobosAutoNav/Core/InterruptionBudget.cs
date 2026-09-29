using System.Collections.Generic;

namespace PhobosAutoNav.Core;

/// <summary>How many failed native ship updates Auto Nav rides out before it suspends. A single failure (the game
/// throwing while it spawns an NPC ship, 30 September 2026) is held for one step; repeated failures inside a short real
/// window mean the physics cannot be trusted and the flight is suspended with its intent kept.</summary>
internal sealed class InterruptionBudget
{
    internal const int MaximumHolds = 3;
    internal const double WindowSeconds = 10;
    private readonly Queue<double> recent = new();

    /// <summary>Records a failure at <paramref name="now"/> (real seconds); true while it may be ridden out.</summary>
    internal bool TryHold(double now)
    {
        while (recent.Count > 0 && (now - recent.Peek() > WindowSeconds || now < recent.Peek())) recent.Dequeue();
        if (recent.Count >= MaximumHolds) return false;
        recent.Enqueue(now);
        return true;
    }
    internal int Recent => recent.Count;
    internal void Reset() => recent.Clear();
}
