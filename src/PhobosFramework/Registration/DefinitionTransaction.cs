using System;
using System.Collections.Generic;

namespace Phobos.Ostranauts.Framework.Registration;

/// <summary>Publish prepared definitions synchronously, restoring prior entries on failure.</summary>
public sealed class DefinitionTransaction
{
    private readonly List<Action<Stack<Action>>> writes = new List<Action<Stack<Action>>>();
    private bool attempted;

    public void Stage<T>(IDictionary<string, T> target, IEnumerable<KeyValuePair<string, T>> prepared)
    {
        if (attempted) throw new InvalidOperationException(Text.Get("DefinitionTransaction.registration_already_attempted"));
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

    public void Commit()
    {
        if (attempted) throw new InvalidOperationException(Text.Get("DefinitionTransaction.registration_already_attempted"));
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
                throw new AggregateException(Text.Get("DefinitionTransaction.definition_rollback_failed_restart_with_compatible_dependencies"), errors);
            throw;
        }
        finally { writes.Clear(); }
    }
}
