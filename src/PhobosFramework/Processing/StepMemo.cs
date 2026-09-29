using System;
using System.Collections.Generic;

namespace Phobos.Ostranauts.Framework.Processing;

/// <summary>Values that hold for one simulation step and no longer: a contact reading, a hardware check, the RCS
/// input list. The memo empties itself the moment the step number changes, so nothing from an earlier step can be
/// reused, and <see cref="Invalidate"/> empties it early when the caller changed what it measures (for example by
/// switching a sensor). Steps are counted, never compared by exact epoch (vanilla-precedence rule).</summary>
public sealed class StepMemo<TKey, TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> values;
    private long step = long.MinValue;
    public StepMemo(IEqualityComparer<TKey>? comparer = null) { values = new Dictionary<TKey, TValue>(comparer); }
    public long Step => step;
    public int Count => values.Count;
    public bool TryGet(long currentStep, TKey key, out TValue value) { Roll(currentStep); return values.TryGetValue(key, out value!); }
    public void Set(long currentStep, TKey key, TValue value) { Roll(currentStep); values[key] = value; }
    public TValue GetOrAdd(long currentStep, TKey key, Func<TKey, TValue> compute)
    {
        Roll(currentStep);
        if (values.TryGetValue(key, out var found)) return found;
        var value = compute(key); values[key] = value; return value;
    }
    public void Invalidate() { values.Clear(); step = long.MinValue; }
    private void Roll(long currentStep) { if (currentStep != step) { values.Clear(); step = currentStep; } }
}

/// <summary>The engine's step counter for main-thread memos: Unity's rendered frame count, which every physics
/// boundary and power update in this game advances with.</summary>
public static class NativeSteps
{
    public static long Frame => UnityEngine.Time.frameCount;
}
