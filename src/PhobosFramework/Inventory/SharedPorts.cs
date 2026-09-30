using System;
using System.Collections.Generic;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Inventory;

/// <summary>Stores and vessels shared by several machines (the owner's decision of 30 September 2026: machines feed
/// each other through shared stores, and the one-machine-per-vessel limit is lifted). Each vessel-side port becomes
/// a bank of <see cref="Slots"/>; slot zero keeps the old port id, so every link saved before reads unchanged and
/// nothing is rewritten. The machine side keeps one port per link and remembers which slot it holds.</summary>
public static class SharedPorts
{
    public const int Slots = 8;
    public static PortBank Bank(CondOwner vessel, string primary, PortRole role = PortRole.Receiver) =>
        new(vessel.strID, primary, vessel.mapGUIPropMaps, Slots, role);
    /// <summary>The vessel-side port <paramref name="ours"/> is saved against: its slot when that is a slot of the
    /// bank on this vessel, otherwise the primary port (unlinked machines and links saved before banks).</summary>
    public static MaterialPort Peer(MaterialPort ours, CondOwner vessel, string primary)
    {
        var link = PortPairing.Read(ours);
        string id = link.State == PortLinkState.Linked && link.PeerObjectId == vessel.strID && PortBank.IsSlot(link.PeerPortId, primary, Slots) ? link.PeerPortId : primary;
        return new MaterialPort(vessel.strID, id, vessel.mapGUIPropMaps);
    }
    /// <summary>Links a machine port to a slot of the vessel's bank. <paramref name="machineSends"/> says which way
    /// the pairing runs (a machine that fills the vessel sends). A slot is reclaimed only when its saved peer resolves
    /// on the same ship and no longer points back.</summary>
    public static bool TryLink(MaterialPort ours, CondOwner vessel, string primary, bool machineSends, Func<string, CondOwner?> resolve, out string problem)
    {
        var bank = Bank(vessel, primary, machineSends ? PortRole.Receiver : PortRole.Sender);
        if (bank.TryLinkPeer(ours, link => Stale(vessel, link, resolve), out problem)) return true;
        if (problem == Text.Get("PortBank.full")) problem = Text.Get("SharedPorts.full", Slots);
        return false;
    }
    /// <summary>Clears a machine port and its vessel slot, when that slot still points back.</summary>
    public static void Unlink(MaterialPort ours, CondOwner? vessel, string primary)
    {
        PortPairing.Unlink(ours, vessel == null ? null : Peer(ours, vessel, primary));
    }
    /// <summary>Every object a vessel's saved links name, across all its ports and slots (for its panel).</summary>
    public static IEnumerable<string> Peers(CondOwner vessel)
    {
        if (vessel?.mapGUIPropMaps == null) yield break;
        foreach (var entry in vessel.mapGUIPropMaps.ToArray())
        {
            if (!entry.Key.StartsWith(MaterialPort.Prefix, StringComparison.Ordinal)) continue;
            string port = entry.Key.Substring(MaterialPort.Prefix.Length);
            if (!MaterialPort.SafePortId(port)) continue;
            var link = PortPairing.Read(new MaterialPort(vessel.strID, port, vessel.mapGUIPropMaps));
            if (link.State == PortLinkState.Linked) yield return link.PeerObjectId;
        }
    }
    private static bool Stale(CondOwner vessel, PortLink link, Func<string, CondOwner?> resolve)
    {
        var peer = resolve(link.PeerObjectId);
        if (peer == null || peer.bDestroyed || peer.ship != vessel.ship) return false; // cannot judge: keep it
        var theirs = PortPairing.Read(new MaterialPort(peer.strID, link.PeerPortId, peer.mapGUIPropMaps));
        return theirs.State != PortLinkState.Linked || theirs.PeerObjectId != vessel.strID || theirs.PairId != link.PairId;
    }
}
