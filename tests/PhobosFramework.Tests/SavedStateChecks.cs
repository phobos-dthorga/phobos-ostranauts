using System;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Persistence;

internal static class SavedStateChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var maps = new Dictionary<string, Dictionary<string, string>> { ["Panel A"] = new() { ["throttle"] = "0.5" } };
        var store = new ObjectStateStore(maps, "Tests.Flight", "console-1", 1);
        check(store.Read(out _) == SavedStateStatus.Missing, "Missing state needs no migration");
        var fields = new Dictionary<string, string> { ["target"] = "ship-2", ["elapsed"] = "120.5" };
        check(store.TryWrite(fields), "Save a bounded consumer snapshot");
        fields["target"] = "changed";
        check(store.Read(out var read) == SavedStateStatus.Ready && read["target"] == "ship-2", "Write detaches caller dictionary");
        var snapshot = read;
        check(store.TryWrite(fields) && snapshot["target"] == "ship-2", "Old read snapshots cannot be changed by later writes");
        check(maps["Panel A"]["throttle"] == "0.5", "Other native state preserved");
        var wrongOwner = new ObjectStateStore(maps, "Tests.Flight", "copied-console", 1);
        check(wrongOwner.Read(out _) == SavedStateStatus.DifferentOwner && !wrongOwner.TryWrite(fields), "Cloned identities cannot adopt or overwrite original state");
        var future = new ObjectStateStore(maps, "Tests.Flight", "console-1", 2);
        check(future.Read(out _) == SavedStateStatus.UnsupportedVersion && !future.TryWrite(fields), "Unsupported schemas remain untouched");
        var prior = maps["PhobosState.Tests.Flight"];
        check(!store.TryWrite(new Dictionary<string, string> { ["value"] = "unsafe=field" }) && ReferenceEquals(prior, maps["PhobosState.Tests.Flight"]), "Failed writes publish nothing");
        maps["PhobosState.Tests.Flight"]["schema"] = "nonsense";
        check(store.Read(out _) == SavedStateStatus.Invalid && !store.TryWrite(fields), "Corrupt envelope is retained");
        store.Clear();
        check(store.Read(out _) == SavedStateStatus.Missing && maps.Count == 1, "Explicit reset removes only its own state");
        var secondWorld = new Dictionary<string, Dictionary<string, string>>();
        check(store.TryWrite(fields), "Reset record can be saved again");
        var native = maps["PhobosState.Tests.Flight"];
        native["data.bad=key"] = "value";
        check(store.Read(out var invalidFields) == SavedStateStatus.Invalid && invalidFields.Count == 0 && !store.TryWrite(fields),
            "Fresh validation rejects malformed native data without copying or overwriting it");
        native.Remove("data.bad=key"); native["data.target"] = "new-native-value";
        check(store.Read(out var changed) == SavedStateStatus.Ready && changed["target"] == "new-native-value",
            "Validation never retains a stale native snapshot");
        check(new ObjectStateStore(secondWorld, "Tests.Flight", "console-1", 1).Read(out _) == SavedStateStatus.Missing,
            "Identical IDs in another save cannot leak state through globals");

        // Changed-only writes: an identical payload leaves the native map untouched, anything else still writes or refuses.
        var settled = new ObjectStateStore(maps, "Tests.Settle", "console-1", 1);
        var payload = new Dictionary<string, string> { ["mode"] = "active", ["elapsed"] = "12" };
        check(settled.Status() == SavedStateStatus.Missing && settled.TryWriteIfChanged(payload) && settled.Status() == SavedStateStatus.Ready, "The first changed-only write creates the record");
        var written = maps["PhobosState.Tests.Settle"];
        check(settled.TryWriteIfChanged(new Dictionary<string, string> { ["mode"] = "active", ["elapsed"] = "12" }) && ReferenceEquals(written, maps["PhobosState.Tests.Settle"]),
            "An identical payload keeps the existing native map");
        payload["elapsed"] = "13";
        check(settled.TryWriteIfChanged(payload) && !ReferenceEquals(written, maps["PhobosState.Tests.Settle"]) && settled.Read(out var after) == SavedStateStatus.Ready && after["elapsed"] == "13",
            "A changed value writes a new detached map");
        check(!settled.TryWriteIfChanged(new Dictionary<string, string> { ["mode"] = "bad=value" }), "Changed-only writes still refuse unsafe values");
        check(settled.TryWriteIfChanged(new Dictionary<string, string> { ["mode"] = "active" }) && settled.Read(out var fewer) == SavedStateStatus.Ready && fewer.Count == 1,
            "A payload with fewer fields is a change");
        maps["PhobosState.Tests.Settle"]["schema"] = "nonsense";
        check(settled.Status() == SavedStateStatus.Invalid && !settled.TryWriteIfChanged(payload), "A corrupt envelope is retained by changed-only writes too");

        // Framework 0.133.0 (L105): an identical record is recognised before the full validation, so it must still be
        // ours under this schema; anything else is refused as before and left as it is.
        Dictionary<string, string> Envelope(string schema, string recordOwner) => new(StringComparer.Ordinal)
            { ["schema"] = schema, ["owner"] = recordOwner, ["data.mode"] = "active", ["data.elapsed"] = "13" };
        var same = new Dictionary<string, string> { ["mode"] = "active", ["elapsed"] = "13" };
        foreach (var (schema, recordOwner, status, what) in new[] {
            ("1", "copied-console", SavedStateStatus.DifferentOwner, "another owner's record"),
            ("2", "console-1", SavedStateStatus.UnsupportedVersion, "a newer schema"),
            (" 1", "console-1", SavedStateStatus.Invalid, "a schema written with a space") })
        {
            var envelope = Envelope(schema, recordOwner);
            maps["PhobosState.Tests.Settle"] = envelope;
            check(settled.Status() == status && !settled.TryWriteIfChanged(same) && ReferenceEquals(envelope, maps["PhobosState.Tests.Settle"]),
                "An identical payload does not overwrite " + what);
        }
        var extra = Envelope("1", "console-1"); extra["data.note"] = "bad=value";
        maps["PhobosState.Tests.Settle"] = extra;
        check(settled.Status() == SavedStateStatus.Invalid && !settled.TryWriteIfChanged(same) && ReferenceEquals(extra, maps["PhobosState.Tests.Settle"]),
            "A record holding one more, unsafe field is not mistaken for an identical one");
        var good = Envelope("1", "console-1");
        maps["PhobosState.Tests.Settle"] = good;
        check(settled.TryWriteIfChanged(same) && ReferenceEquals(good, maps["PhobosState.Tests.Settle"]), "A valid identical record is still left untouched");
    }
}
