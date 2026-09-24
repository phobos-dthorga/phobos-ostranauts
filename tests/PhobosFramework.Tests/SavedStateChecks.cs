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
        check(new ObjectStateStore(secondWorld, "Tests.Flight", "console-1", 1).Read(out _) == SavedStateStatus.Missing,
            "Identical IDs in another save cannot leak state through globals");
    }
}
