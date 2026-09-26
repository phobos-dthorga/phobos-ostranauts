using System;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Persistence;

namespace PhobosAgriculture.Core;

public static class DosingBinding
{
    public static Dictionary<string,string> Save(string source)
    {
        if (source == null || source == "none" || source.Length > 200 || source.Length > 0 && !ObjectStateStore.SafeValue(source))
            throw new ArgumentException("Invalid dosing binding.");
        return new() { ["source"] = source.Length == 0 ? "none" : source };
    }

    public static string Read(IReadOnlyDictionary<string,string> fields)
    {
        if (fields.Count != 1 || !fields.TryGetValue("source", out var stored) || !ObjectStateStore.SafeValue(stored))
            throw new ArgumentException("Unknown dosing binding.");
        var source = stored == "none" ? "" : stored;
        _ = Save(source);
        return source;
    }
}
