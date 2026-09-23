using System;
using System.Collections.Generic;
using System.Text.Json;
using Phobos.Ostranauts.Framework.Inventory;

internal static class PairingChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var mapsA = new Dictionary<string, Dictionary<string, string>> { ["Electrical"] = new() { ["outputConnections"] = "untouched" } };
        var mapsB = new Dictionary<string, Dictionary<string, string>>();
        var a = new MaterialPort("machine-a", "Example.Output", mapsA);
        var b = new MaterialPort("machine-b", "Example.Input", mapsB);
        var c = new MaterialPort("machine-c", "Example.Input", new());
        check(PortPairing.Read(a).State == PortLinkState.Unlinked, "Fresh equipment has no invented partner");
        check(!PortPairing.TryLink(a, a, out _), "Self routes rejected");
        check(PortPairing.TryLink(a, b, out _), "Explicit pair created");
        string first = PortPairing.Read(a).PairId;
        check(PortPairing.Matches(a,b) && !PortPairing.Matches(b,a), "Direction is checked");
        check(PortPairing.TryLink(a,b,out _) && PortPairing.Read(a).PairId == first, "Repeated link preserves pair identity");
        check(!PortPairing.TryLink(a,c,out _) && PortPairing.Read(c).State == PortLinkState.Unlinked && PortPairing.Matches(a,b), "Occupied sender cannot acquire another receiver");
        check(!PortPairing.TryLink(c,b,out _) && PortPairing.Read(c).State == PortLinkState.Unlinked, "Occupied receiver cannot acquire another sender");
        check(mapsA["Electrical"]["outputConnections"] == "untouched", "Native signal wiring remains untouched");
        // Fresh wrappers and fresh dictionaries model the consumer rebuilding all runtime state after load.
        Dictionary<string, Dictionary<string, string>> Copy(Dictionary<string, Dictionary<string, string>> m) =>
            JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(JsonSerializer.Serialize(m))!;
        var loadedA = new MaterialPort(a.ObjectId, a.PortId, Copy(mapsA));
        var loadedB = new MaterialPort(b.ObjectId, b.PortId, Copy(mapsB));
        check(PortPairing.Matches(loadedA, loadedB), "Persistence needs no session registry or creation order");
        var clone = new MaterialPort("new-machine", a.PortId, Copy(mapsA));
        check(PortPairing.Read(clone).State == PortLinkState.Invalid, "Copied metadata with a different owner ID cannot steal a pair");
        check(!PortPairing.Matches(a,new MaterialPort(b.ObjectId,"Example.OtherInput",mapsB)), "Logical port identity matters");
        var extra = new MaterialPort(a.ObjectId,"Example.OtherOutput",mapsA);
        check(PortPairing.TryLink(extra,c,out _), "Distinct logical ports may have separate pairs");
        PortPairing.Unlink(b,a);
        check(PortPairing.Read(a).State == PortLinkState.Unlinked && PortPairing.Read(b).State == PortLinkState.Unlinked, "Unlink is symmetric");
        check(PortPairing.Matches(extra,c), "Unlink does not touch other logical ports");
        PortPairing.Unlink(b,a);
        check(PortPairing.Matches(extra,c), "Repeated unlink is harmless");
        check(PortPairing.TryLink(a,b,out _) && PortPairing.Read(a).PairId != first, "A new pairing gets a fresh identity");
        PortPairing.Unlink(a); // Other endpoint unloaded/missing; clear only the reachable side.
        check(!PortPairing.Matches(a,b), "Half links cannot route material");
        var d = new MaterialPort("machine-d","Example.Input",new());
        check(PortPairing.TryLink(a,d,out _), "Reachable endpoint can be paired elsewhere after explicit unlink");
        PortPairing.Unlink(b,a);
        check(PortPairing.Matches(a,d), "Clearing an old orphan cannot sever the sender's new pair");
        // Both peers remember each other, but a stale pair token still must not authorize work.
        var tampered = Copy(mapsA);
        tampered["PhobosMaterialPort.Example.Output"]["pair"] = Guid.NewGuid().ToString("N");
        check(!PortPairing.Matches(new MaterialPort(a.ObjectId,a.PortId,tampered),d), "Mismatched pair tokens block stale jobs");
        foreach (var field in new[] { "schema", "owner", "port", "role", "peer", "peerPort", "pair" })
        {
            var broken = Copy(mapsA); broken["PhobosMaterialPort.Example.Output"].Remove(field);
            var invalid = new MaterialPort(a.ObjectId,a.PortId,broken);
            check(PortPairing.Read(invalid).State == PortLinkState.Invalid && !PortPairing.TryLink(invalid,b,out _), "Missing metadata is not auto-repaired: " + field);
            PortPairing.Unlink(invalid);
            check(PortPairing.Read(invalid).State == PortLinkState.Unlinked, "Player may explicitly clear malformed record: " + field);
        }
        foreach (var entry in new[] { ("schema","2"), ("role","send-all"), ("peer",a.ObjectId), ("peerPort","not a port"), ("pair","not-a-guid") })
        {
            var broken = Copy(mapsA); broken["PhobosMaterialPort.Example.Output"][entry.Item1] = entry.Item2;
            check(PortPairing.Read(new MaterialPort(a.ObjectId,a.PortId,broken)).State == PortLinkState.Invalid, "Unsupported or malformed record refused: " + entry.Item1);
        }
        // Display abbreviations must never become routing identities.
        var similar = new MaterialPort("machine-d-other","Example.Input",new());
        check(PortPairing.ShortId(d.ObjectId)==PortPairing.ShortId(similar.ObjectId) && !PortPairing.Matches(a,similar), "Short ID collisions never match full addresses");
    }
}
