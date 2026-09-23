using System;
using System.Collections.Generic;

namespace PhobosShipbreaker.Core;

/// <summary>Publish prepared definitions synchronously, restoring prior entries on failure.</summary>
internal sealed class DefinitionTransaction
{
    private readonly List<Action<Stack<Action>>> writes = new List<Action<Stack<Action>>>();
    private bool attempted;

    internal void Stage<T>(IDictionary<string, T> target, IEnumerable<KeyValuePair<string, T>> prepared)
    {
        if (attempted) throw new InvalidOperationException("Registration already attempted.");
        foreach (var entry in prepared)
        {
            string key = entry.Key;
            T value = entry.Value;
            writes.Add(undo => {
                bool existed = target.TryGetValue(key, out var previous);
                // Record recovery before assignment, including a setter that writes then throws.
                undo.Push(() => { if (existed) target[key] = previous!; else target.Remove(key); });
                target[key] = value;
            });
        }
    }

    internal void Commit()
    {
        if (attempted) throw new InvalidOperationException("Registration already attempted.");
        attempted = true;
        var undo = new Stack<Action>();
        try { foreach (var write in writes) write(undo); }
        catch (Exception failure)
        {
            var errors = new List<Exception> { failure };
            while (undo.Count > 0)
            {
                try { undo.Pop()(); }
                catch (Exception recovery) { errors.Add(recovery); }
            }
            if (errors.Count > 1)
                throw new AggregateException("Definition rollback failed. Restart with compatible dependencies before loading an affected save.", errors);
            throw;
        }
        finally { writes.Clear(); }
    }
}
