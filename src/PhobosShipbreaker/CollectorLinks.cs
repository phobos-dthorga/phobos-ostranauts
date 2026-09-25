using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Inventory;
using Phobos.Ostranauts.Framework.Controls;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

internal sealed partial class CollectorService
{
    internal const string SendPort = RoutingRules.SendPort, ReceivePort = RoutingRules.CollectorIn;
    private static bool DefaultSender(CondOwner co) => !CollectorRules.IsFamily(co.strCODef);
    internal static MaterialPort Sender(CondOwner co, string? port = null) => new MaterialPort(co.strID, port ?? RoutingRules.OutputPort(co.strCODef), co.mapGUIPropMaps);
    internal static MaterialPort Receiver(CondOwner co) => new MaterialPort(co.strID,
        RoutingRules.InputPort(co.strCODef), co.mapGUIPropMaps);
    private static MaterialPort Endpoint(CondOwner co, bool sender, bool metals = false) => sender ? Sender(co, RoutingRules.OutputPort(co.strCODef, metals)) : Receiver(co);
    internal static CondOwner? Resolve(string id) => DataHandler.mapCOs != null && DataHandler.mapCOs.TryGetValue(id, out var co) &&
        co != null && !co.bDestroyed ? co : null;
    internal static string Label(CondOwner co) => co.strNameFriendly + " [" + PortPairing.ShortId(co.strID) + "]";
    private static MaterialPort SourcePort(CondOwner source, CondOwner receiver) => Sender(source, RoutingRules.SourcePort(source.strCODef, receiver.strCODef));
    private static CondOwner? Peer(CondOwner co, PortLink link, bool sender, bool metals = false)
    {
        if (link.State != PortLinkState.Linked) return null;
        var peer = Resolve(link.PeerObjectId);
        if (peer == null || !(sender ? RoutingRules.CanConnect(co.strCODef, peer.strCODef) : RoutingRules.CanConnect(peer.strCODef, co.strCODef))) return null;
        return sender ? link.PeerPortId == Receiver(peer).PortId && Endpoint(co, true, metals).PortId == SourcePort(co, peer).PortId ? peer : null :
            link.PeerPortId == SourcePort(peer, co).PortId ? peer : null;
    }
    private static string? PairProblem(CondOwner receiver, out CondOwner? source, out PortLink link)
    {
        link = PortPairing.Read(Receiver(receiver)); source = Peer(receiver, link, false);
        if (link.State == PortLinkState.Unlinked) return Text.Get("Routing.no_sender");
        if (link.State == PortLinkState.Invalid) return Text.Get("CollectorLinks.saved_link_is_invalid_or_from_a");
        if (source == null) return Text.Get("CollectorLinks.saved_sender_is_unavailable_restore_that_equipment");
        return PortPairing.Matches(SourcePort(source, receiver), Receiver(receiver)) ? null :
            Text.Get("CollectorLinks.sender_no_longer_reciprocates_this_pair_unlink");
    }
    internal static string DescribeLink(CondOwner endpoint, bool? sending = null, bool metals = false)
    {
        bool sender = sending ?? DefaultSender(endpoint);
        var link = PortPairing.Read(Endpoint(endpoint, sender, metals));
        string direction = sender ? Text.Get("CollectorLinks.receiver") : Text.Get("CollectorLinks.sender");
        if (link.State == PortLinkState.Unlinked) return Text.Get("CollectorLinks.not_linked", direction);
        if (link.State == PortLinkState.Invalid) return Text.Get("CollectorLinks.invalid_saved_link_use_unlink", direction);
        var peer = Peer(endpoint, link, sender, metals);
        bool matched = peer != null && (sender ? PortPairing.Matches(Endpoint(endpoint, true, metals), Receiver(peer)) : PortPairing.Matches(SourcePort(peer, endpoint), Receiver(endpoint)));
        return Text.Get("CollectorLinks.pair", direction, peer == null ? Text.Get("CollectorLinks.unavailable", link.PeerObjectId) : Label(peer),
            PortPairing.ShortId(link.PairId), matched ? Text.Get("CollectorLinks.saved") : Text.Get("CollectorLinks.broken_no_transfer"));
    }
    internal static string LinkIds(CondOwner endpoint, bool? sending = null, bool metals = false)
    {
        var port = Endpoint(endpoint, sending ?? DefaultSender(endpoint), metals); var link = PortPairing.Read(port);
        return Text.Get("CollectorLinks.object_id_port", endpoint.strID, port.PortId, link.State == PortLinkState.Linked ?
            Text.Get("CollectorLinks.peer_object_id_peer_port_pair", link.PeerObjectId, link.PeerPortId, link.PairId) : "");
    }
    internal bool Unlink(CondOwner endpoint, out string message, bool? sending = null, ConsoleBinding? console = null, bool metals = false)
    {
        bool sender = sending ?? DefaultSender(endpoint);
        if (metals && !ProcessingService.IsReclaimer(endpoint) || !(sender ? RoutingRules.IsSender(endpoint.strCODef) : RoutingRules.IsReceiver(endpoint.strCODef)))
        { message = Text.Get("CollectorLinks.not_a_material_endpoint"); return false; }
        string? problem = EndpointAccess(endpoint, console);
        if (problem != null) { message = problem; return false; }
        var port = Endpoint(endpoint, sender, metals); var link = PortPairing.Read(port); var peer = Peer(endpoint, link, sender, metals);
        // Moving a paired object must not let its old counterpart mutate another ship.
        if (peer != null && (peer.ship != endpoint.ship || console != null && ControlAuthority.Check(peer, console) != null)) peer = null;
        bool reciprocates = peer != null && (sender ? PortPairing.Matches(Endpoint(endpoint, true, metals), Receiver(peer)) : PortPairing.Matches(SourcePort(peer, endpoint), Receiver(endpoint)));
        PortPairing.Unlink(port, peer == null ? null : sender ? Receiver(peer) : SourcePort(peer, endpoint));
        var receiver = sender ? (reciprocates ? peer : null) : endpoint;
        if (receiver != null) ClearTransfer(receiver, Text.Get("CollectorLinks.unlinked_and_paused_all_cargo_retained"));
        message = Text.Get("CollectorLinks.unlinked_cargo_retained_choose_a_new_pair"); return true;
    }
    private static PortFilterSnapshot Filter(CondOwner port) => SavedPortFilter.Read(Receiver(port));
    private static string FilterSignature(CondOwner port)
    { var filter = Filter(port); return filter.State + ":" + string.Join(",", filter.DefinitionIds); }
    private static bool FilterAllows(CondOwner port, CondOwner item) => Filter(port).Allows(item.strCODef,
        new ItemDefinitionFilter(RoutingRules.FilterIds(RoutingRules.DefaultFilter(port.strCODef))));
    private static string? FilterProblem(CondOwner port) => RoutingRules.CompatibleFilter(port.strCODef, Filter(port))
        ? null : Text.Get("Routing.invalid_filter");
    internal static string FilterLabel(CondOwner port)
    {
        var filter = Filter(port);
        if (FilterProblem(port) != null) return Text.Get("Routing.invalid_filter");
        var ids = filter.State == PortFilterState.Default ? RoutingRules.FilterIds(RoutingRules.DefaultFilter(port.strCODef)) : filter.DefinitionIds;
        return Text.Get("Routing.filter", string.Join(", ", ids.Select(id => DataHandler.GetCondOwnerDef(id)?.strNameFriendly ?? id)));
    }
    internal bool SetFilter(CondOwner port, string choice, out string message, ConsoleBinding? console = null)
    {
        string? problem = EndpointAccess(port, console);
        if (problem != null) { message = problem; return false; }
        if (!RoutingRules.IsReceiver(port.strCODef) || !RoutingRules.Choices(port.strCODef).Contains(choice))
        { message = Text.Get("Routing.filter_choices"); return false; }
        SavedPortFilter.Set(Receiver(port), RoutingRules.FilterIds(choice));
        ClearTransfer(port, Text.Get("Routing.filter_changed")); message = FilterLabel(port); return true;
    }
    private void ClearTransfer(CondOwner port, string status)
    {
        var s = sessions.GetValue(port, _ => new Session()); Disarm(port, s);
        s.Source = null; s.PairId = ""; s.Route = null; s.Clock = null; s.Item = null; s.Status = status;
    }
}
