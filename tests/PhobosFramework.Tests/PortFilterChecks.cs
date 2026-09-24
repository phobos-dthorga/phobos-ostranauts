using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Phobos.Ostranauts.Framework.Inventory;

internal static class PortFilterChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var maps = new Dictionary<string, Dictionary<string, string>> { ["Electrical"] = new() { ["wire"] = "native" } };
        var input = new MaterialPort("receiver", "Example.Input", maps);
        var output = new MaterialPort("receiver", "Example.Output", maps);
        var sender = new MaterialPort("sender", "Example.Output", new());
        var defaults = new ItemDefinitionFilter(new[] { "Feed", "Reject" });
        check(PortPairing.TryLink(sender, input, out _), "Pair established before filter settings");
        var originalPair = PortPairing.Read(input).PairId;
        check(SavedPortFilter.Read(input).Allows("Feed", defaults) && !SavedPortFilter.Read(input).Allows("Unknown", defaults), "Old saves use only the consumer's explicit default filter");
        SavedPortFilter.Set(input, new[] { "Feed" });
        check(SavedPortFilter.Read(input).Allows("Feed", defaults) && !SavedPortFilter.Read(input).Allows("Reject", defaults), "Saved filter narrows eligible cargo");
        check(PortPairing.Matches(sender, input) && PortPairing.Read(input).PairId == originalPair && maps["Electrical"]["wire"] == "native", "Filter changes preserve pair IDs and native wiring");
        SavedPortFilter.Set(output, new[] { "Reject" });
        check(!SavedPortFilter.Read(input).Allows("Reject", defaults) && SavedPortFilter.Read(output).Allows("Reject", defaults), "Input and output settings are independent");
        Dictionary<string, Dictionary<string, string>> Copy() => JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(JsonSerializer.Serialize(maps))!;
        check(SavedPortFilter.Read(new MaterialPort("receiver", "Example.Input", Copy())).Allows("Feed", defaults), "Filter survives a fresh object wrapper and serialization");
        check(SavedPortFilter.Read(new MaterialPort("clone", "Example.Input", Copy())).State == PortFilterState.Invalid, "Unremapped copies cannot inherit another object's settings");
        foreach (var edit in new[] { ("schema", "2"), ("count", "65"), ("count", "-1"), ("count", "garbage"), ("owner", "someone-else"), ("port", "wrong"), ("id0", "") })
        {
            var broken = Copy(); broken["PhobosMaterialFilter.Example.Input"][edit.Item1] = edit.Item2;
            var snapshot = SavedPortFilter.Read(new MaterialPort("receiver", "Example.Input", broken));
            check(snapshot.State == PortFilterState.Invalid && !snapshot.Allows("Feed", defaults), "Malformed/future saved filters fail closed: " + edit.Item1);
        }
        var duplicate = Copy(); duplicate["PhobosMaterialFilter.Example.Input"]["count"] = "2"; duplicate["PhobosMaterialFilter.Example.Input"]["id1"] = "Feed";
        check(SavedPortFilter.Read(new MaterialPort("receiver", "Example.Input", duplicate)).State == PortFilterState.Invalid, "Duplicate saved IDs rejected");
        foreach (var ids in new[] { new[] { "Feed", "Feed" }, new[] { "bad,id" }, new[] { "bad=id" }, Enumerable.Range(0, 65).Select(i => "ID" + i).ToArray() })
        {
            bool threw = false; try { SavedPortFilter.Set(input, ids); } catch (ArgumentException) { threw = true; }
            check(threw && SavedPortFilter.Read(input).Allows("Feed", defaults), "Invalid edits retain the last valid filter");
        }
        SavedPortFilter.Set(input, Array.Empty<string>());
        check(!SavedPortFilter.Read(input).Allows("Feed", defaults), "Explicit empty allowlist blocks all cargo rather than falling back");
        PortPairing.Unlink(input, sender);
        check(SavedPortFilter.Read(output).Allows("Reject", defaults), "Unlink never clears another port's settings");
    }
}
