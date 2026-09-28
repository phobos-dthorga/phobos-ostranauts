using System;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Persistence;

namespace Phobos.Ostranauts.Framework.Sensors;

/// <summary>Leased: automation switched this sensor type on and may switch it off again.
/// Declined: someone else switched it off while that automation was working.</summary>
public enum SensorLeaseState { Leased, Declined }

/// <summary>Saved note on a sensor unit about one of its sensor types. It lives in the unit's own
/// property maps, so it follows native power mode switches, repairs and saves.</summary>
public sealed class SensorLeaseRecord
{
    public const int Schema = 1;
    private const string StorePrefix = "SensorLease.";
    public string Automation { get; }
    public string Holder { get; }
    public SensorLeaseState State { get; }

    public SensorLeaseRecord(string automation, string holder, SensorLeaseState state)
    {
        if (!ObjectStateStore.SafeValue(automation) || !ObjectStateStore.SafeValue(holder) || !Enum.IsDefined(typeof(SensorLeaseState), state))
            throw new ArgumentException("Invalid sensor lease identity.");
        Automation = automation; Holder = holder; State = state;
    }

    public bool BelongsTo(string automation, string holder) => Automation == automation && Holder == holder;
    public SensorLeaseRecord With(SensorLeaseState state) => new(Automation, Holder, state);

    public IReadOnlyDictionary<string, string> Encode() => new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["automation"] = Automation, ["holder"] = Holder, ["state"] = State.ToString()
    };

    /// <summary>Unknown or malformed fields are not a lease; callers leave such records untouched.</summary>
    public static bool TryDecode(IReadOnlyDictionary<string, string> fields, out SensorLeaseRecord record)
    {
        record = null!;
        if (fields == null || fields.Count != 3 || !fields.TryGetValue("automation", out var automation) ||
            !fields.TryGetValue("holder", out var holder) || !fields.TryGetValue("state", out var text) ||
            (text != nameof(SensorLeaseState.Leased) && text != nameof(SensorLeaseState.Declined)) ||
            !ObjectStateStore.SafeValue(automation) || !ObjectStateStore.SafeValue(holder)) return false;
        record = new SensorLeaseRecord(automation, holder, text == nameof(SensorLeaseState.Leased) ? SensorLeaseState.Leased : SensorLeaseState.Declined);
        return true;
    }

    /// <summary>One record per sensor type per unit. The unit ID guards against copied property maps.</summary>
    public static ObjectStateStore Store(Dictionary<string, Dictionary<string, string>> maps, string unitId, string sensorCondition) =>
        new(maps, StorePrefix + sensorCondition, unitId, Schema);

    /// <summary>Ready and decodable, or null. Other states are someone else's data and stay untouched.</summary>
    public static SensorLeaseRecord? Read(ObjectStateStore store) =>
        store.Read(out var fields) == SavedStateStatus.Ready && TryDecode(fields, out var record) ? record : null;
}
