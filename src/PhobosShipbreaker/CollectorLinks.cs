using Phobos.Ostranauts.Framework.Inventory;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

internal sealed partial class CollectorService
{
    // Saved logical port names; don't rename when equipment art or labels change.
    internal const string SendPort = "PhobosShipbreaker.ResidueOut";
    internal const string ReceivePort = "PhobosShipbreaker.ResidueIn";
    private static MaterialPort Sender(CondOwner co) => new MaterialPort(co.strID, SendPort, co.mapGUIPropMaps);
    private static MaterialPort Receiver(CondOwner co) => new MaterialPort(co.strID, ReceivePort, co.mapGUIPropMaps);
    private static bool IsSource(CondOwner co) => Content.IsMachine(co.strCODef);
    private static MaterialPort Endpoint(CondOwner co) => IsSource(co) ? Sender(co) : Receiver(co);
    private static CondOwner? Resolve(string id) => DataHandler.mapCOs != null && DataHandler.mapCOs.TryGetValue(id, out var co) &&
        co != null && !co.bDestroyed ? co : null;
    internal static string Label(CondOwner co) => co.strNameFriendly + " [" + PortPairing.ShortId(co.strID) + "]";
    private static CondOwner? Peer(CondOwner co, PortLink link)
    {
        if (link.State != PortLinkState.Linked || link.PeerPortId != (IsSource(co) ? ReceivePort : SendPort)) return null;
        var peer = Resolve(link.PeerObjectId);
        return peer != null && (IsSource(co) ? CollectorRules.IsFamily(peer.strCODef) : IsSource(peer)) ? peer : null;
    }
    private static string? PairProblem(CondOwner receiver, out CondOwner? source, out PortLink link)
    {
        link = PortPairing.Read(Receiver(receiver)); source = Peer(receiver, link);
        if (link.State == PortLinkState.Unlinked) return "No sender linked. Choose a processor.";
        if (link.State == PortLinkState.Invalid) return "Saved link is invalid or from a newer version. Unlink before making a new pair.";
        if (source == null) return "Saved sender is unavailable. Restore that equipment or unlink this endpoint.";
        return PortPairing.Matches(Sender(source), Receiver(receiver)) ? null :
            "Sender no longer reciprocates this pair. Unlink before selecting a destination.";
    }
    internal static string DescribeLink(CondOwner endpoint)
    {
        var link = PortPairing.Read(Endpoint(endpoint));
        string direction = IsSource(endpoint) ? "Receiver: " : "Sender: ";
        if (link.State == PortLinkState.Unlinked) return direction + "not linked.";
        if (link.State == PortLinkState.Invalid) return direction + "invalid saved link; use Unlink.";
        var peer = Peer(endpoint, link);
        bool matched = peer != null && (IsSource(endpoint) ? PortPairing.Matches(Sender(endpoint), Receiver(peer)) :
            PortPairing.Matches(Sender(peer), Receiver(endpoint)));
        return direction + (peer == null ? link.PeerObjectId + " (unavailable)" : Label(peer)) +
            " | Pair " + PortPairing.ShortId(link.PairId) + (matched ? " (saved)." : " (broken; no transfer).");
    }
    internal static string LinkIds(CondOwner endpoint)
    {
        var link = PortPairing.Read(Endpoint(endpoint));
        return "Object ID: " + endpoint.strID + "\nPort: " + Endpoint(endpoint).PortId +
            (link.State == PortLinkState.Linked ? "\nPeer object ID: " + link.PeerObjectId + "\nPeer port: " + link.PeerPortId + "\nPair: " + link.PairId : "");
    }
    internal bool Unlink(CondOwner endpoint, out string message)
    {
        if (!IsSource(endpoint) && !CollectorRules.IsFamily(endpoint.strCODef)) { message = "Not a material endpoint."; return false; }
        string? problem = IsSource(endpoint) ? ProcessingService.AccessProblem(endpoint) : AccessProblem(endpoint);
        if (problem != null || endpoint.HasCond("IsLocked")) { message = problem ?? "Unlock this endpoint."; return false; }
        var link = PortPairing.Read(Endpoint(endpoint));
        var peer = Peer(endpoint, link);
        bool reciprocates = peer != null && (IsSource(endpoint) ? PortPairing.Matches(Sender(endpoint), Receiver(peer)) :
            PortPairing.Matches(Sender(peer), Receiver(endpoint)));
        // Clear only the reciprocal peer. An old or damaged record cannot steal another pair.
        PortPairing.Unlink(Endpoint(endpoint), peer == null ? null : Endpoint(peer));
        var receiver = IsSource(endpoint) ? (reciprocates ? peer : null) : endpoint;
        if (receiver != null && sessions.TryGetValue(receiver, out var s))
        {
            Disarm(receiver, s); s.Source = null; s.PairId = ""; s.Route = null; s.Clock = null; s.Item = null;
            s.Status = "Unlinked and paused; all cargo retained.";
        }
        message = "Unlinked; cargo retained. Choose a new pair when ready.";
        return true;
    }
}
