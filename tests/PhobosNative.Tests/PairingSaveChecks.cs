using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Phobos.Ostranauts.Framework.Inventory;

internal static class PairingSaveChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var a = new Dictionary<string, Dictionary<string, string>>();
        var b = new Dictionary<string, Dictionary<string, string>>();
        var sender = new MaterialPort("sender-id", "PhobosShipbreaker.ResidueOut", a);
        var receiver = new MaterialPort("receiver-id", "PhobosShipbreaker.ResidueIn", b);
        check(PortPairing.TryLink(sender,receiver,out _), "Native save fixture paired");
        // Exercise the actual game's data conversion + JsonItem save schema. This is not a Unity play test.
        Dictionary<string, Dictionary<string, string>> RoundTrip(Dictionary<string, Dictionary<string, string>> maps)
        {
            var saved = new JsonItem { aGPMSettings = maps.Select(m => new JsonGUIPropMap
                { strName = m.Key, dictGUIPropMap = DataHandler.ConvertDictToStringArray(m.Value) }).ToArray() };
            var loaded = JsonConvert.DeserializeObject<JsonItem>(JsonConvert.SerializeObject(saved))!;
            return loaded.aGPMSettings.ToDictionary(m=>m.strName,m=>DataHandler.ConvertStringArrayToDict(m.dictGUIPropMap));
        }
        var loadedA = RoundTrip(a); var loadedB = RoundTrip(b);
        SavedPortFilter.Set(receiver, new[] { "PhobosPanelRejectR2" });
        var savedFilter = SavedPortFilter.Read(new MaterialPort(receiver.ObjectId, receiver.PortId, RoundTrip(b)));
        check(savedFilter.State == PortFilterState.Configured && savedFilter.DefinitionIds.SequenceEqual(new[] { "PhobosPanelRejectR2" }),
            "Exact filter IDs survive the game's actual property-map serialization");
        var input = new MaterialPort(sender.ObjectId, "PhobosShipbreaker.ReclaimerFeed", a);
        var upstream = new MaterialPort("upstream-id", "PhobosShipbreaker.ResidueOut", new());
        check(PortPairing.TryLink(upstream, input, out _) && PortPairing.Matches(new MaterialPort(input.ObjectId, input.PortId, RoundTrip(a)),
            upstream) == false && PortPairing.Matches(upstream, new MaterialPort(input.ObjectId, input.PortId, RoundTrip(a))),
            "Native maps retain independent incoming/outgoing port direction on one machine");
        check(PortPairing.Matches(new MaterialPort(sender.ObjectId,sender.PortId,loadedA),
            new MaterialPort(receiver.ObjectId,receiver.PortId,loadedB)), "Real native property-map save format preserves both endpoints and pair token");
        // Native ship cloning remaps whole property values equal to an object ID.
        foreach (var map in loadedA.Values.Concat(loadedB.Values))
        foreach (string key in map.Keys.ToArray())
        {
            if (map[key] == sender.ObjectId) map[key] = "cloned-sender-id";
            else if (map[key] == receiver.ObjectId) map[key] = "cloned-receiver-id";
        }
        var clonedSender = new MaterialPort("cloned-sender-id", sender.PortId, loadedA);
        var clonedReceiver = new MaterialPort("cloned-receiver-id", receiver.PortId, loadedB);
        check(PortPairing.Matches(clonedSender,clonedReceiver), "Native whole-ID remapping preserves the cloned pair internally");
        check(!PortPairing.Matches(sender,clonedReceiver) && !PortPairing.Matches(clonedSender,receiver), "Shared clone pair token cannot cross original and cloned equipment");
        PortPairing.Unlink(clonedSender,clonedReceiver);
        check(PortPairing.Read(new MaterialPort(clonedSender.ObjectId,clonedSender.PortId,RoundTrip(loadedA))).State == PortLinkState.Unlinked,
            "Explicit unlink survives native save round trip");
    }
}
