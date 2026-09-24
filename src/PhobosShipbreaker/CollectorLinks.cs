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
        if (link.State == PortLinkState.Unlinked) return Text.Get("CollectorLinks.no_sender_linked_choose_a_processor");
        if (link.State == PortLinkState.Invalid) return Text.Get("CollectorLinks.saved_link_is_invalid_or_from_a");
        if (source == null) return Text.Get("CollectorLinks.saved_sender_is_unavailable_restore_that_equipment");
        return PortPairing.Matches(Sender(source), Receiver(receiver)) ? null :
            Text.Get("CollectorLinks.sender_no_longer_reciprocates_this_pair_unlink");
    }
    internal static string DescribeLink(CondOwner endpoint)
    {
        var link = PortPairing.Read(Endpoint(endpoint));
        string direction = IsSource(endpoint) ? Text.Get("CollectorLinks.receiver") : Text.Get("CollectorLinks.sender");
        if (link.State == PortLinkState.Unlinked) return Text.Get("CollectorLinks.not_linked", direction);
        if (link.State == PortLinkState.Invalid) return Text.Get("CollectorLinks.invalid_saved_link_use_unlink", direction);
        var peer = Peer(endpoint, link);
        bool matched = peer != null && (IsSource(endpoint) ? PortPairing.Matches(Sender(endpoint), Receiver(peer)) :
            PortPairing.Matches(Sender(peer), Receiver(endpoint)));
        return Text.Get("CollectorLinks.pair", direction, (peer == null ? Text.Get("CollectorLinks.unavailable", link.PeerObjectId) : Label(peer)), PortPairing.ShortId(link.PairId), (matched ? Text.Get("CollectorLinks.saved") : Text.Get("CollectorLinks.broken_no_transfer")));
    }
    internal static string LinkIds(CondOwner endpoint)
    {
        var link = PortPairing.Read(Endpoint(endpoint));
        return Text.Get("CollectorLinks.object_id_port", endpoint.strID, Endpoint(endpoint).PortId, (link.State == PortLinkState.Linked ? Text.Get("CollectorLinks.peer_object_id_peer_port_pair", link.PeerObjectId, link.PeerPortId, link.PairId) : ""));
    }
    internal bool Unlink(CondOwner endpoint, out string message)
    {
        if (!IsSource(endpoint) && !CollectorRules.IsFamily(endpoint.strCODef)) { message = Text.Get("CollectorLinks.not_a_material_endpoint"); return false; }
        string? problem = IsSource(endpoint) ? ProcessingService.AccessProblem(endpoint) : AccessProblem(endpoint);
        if (problem != null || endpoint.HasCond("IsLocked")) { message = problem ?? Text.Get("CollectorLinks.unlock_this_endpoint"); return false; }
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
            s.Status = Text.Get("CollectorLinks.unlinked_and_paused_all_cargo_retained");
        }
        message = Text.Get("CollectorLinks.unlinked_cargo_retained_choose_a_new_pair");
        return true;
    }
}
