using System;
using System.Collections.Generic;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Inventory;

/// <summary>Bounded reciprocal fan-out or fan-in. Slot zero retains the original single-port identity, so a link
/// saved before the bank existed reads as slot zero with no rewrite; further slots are <c>primary.1</c> to
/// <c>primary.(count-1)</c>. A sender bank (the default, Agriculture's W2 feeding racks) sends to many receivers; a
/// receiver bank (Framework 0.56.0, a store shared by several machines) receives from many senders.</summary>
public sealed class PortBank
{
    public IReadOnlyList<MaterialPort> Ports { get; }
    public PortRole Role { get; }
    public PortBank(string owner, string primary, Dictionary<string, Dictionary<string,string>> maps, int count) : this(owner, primary, maps, count, PortRole.Sender) { }
    public PortBank(string owner, string primary, Dictionary<string, Dictionary<string,string>> maps, int count, PortRole role)
    {
        if(count < 1 || count > 64) throw new ArgumentOutOfRangeException(nameof(count));
        Role = role;
        Ports = Enumerable.Range(0,count).Select(n => new MaterialPort(owner,SlotId(primary,n),maps)).ToArray();
    }
    public static string SlotId(string primary, int slot) => slot == 0 ? primary : primary + "." + slot;
    /// <summary>Whether a saved peer port id names a slot of a bank on <paramref name="primary"/> with
    /// <paramref name="count"/> slots.</summary>
    public static bool IsSlot(string? portId, string primary, int count)
    {
        if (portId == primary) return true;
        if (portId == null || !portId.StartsWith(primary + ".", StringComparison.Ordinal)) return false;
        string tail = portId.Substring(primary.Length + 1);
        return tail.Length > 0 && tail.All(char.IsDigit) && tail[0] != '0' && int.TryParse(tail, out int n) && n >= 1 && n < count;
    }
    public bool Occupied => Ports.Any(p=>PortPairing.Read(p).State!=PortLinkState.Unlinked);
    public MaterialPort? ForReceiver(MaterialPort receiver) => Ports.FirstOrDefault(p=>PortPairing.Matches(p,receiver));
    /// <summary>The slot linked to <paramref name="peer"/>, for either role.</summary>
    public MaterialPort? For(MaterialPort peer) => Ports.FirstOrDefault(p => Role == PortRole.Sender ? PortPairing.Matches(p, peer) : PortPairing.Matches(peer, p));
    public bool TryLink(MaterialPort receiver,out string problem) => TryLinkPeer(receiver, null, out problem);
    /// <summary>Links <paramref name="peer"/> to its existing slot, the first free one, or, when every slot is
    /// taken, the first slot whose saved peer <paramref name="stale"/> says no longer points back (a machine that was
    /// destroyed or relinked elsewhere). Slots whose peer cannot be judged stay taken.</summary>
    public bool TryLinkPeer(MaterialPort peer, Func<PortLink, bool>? stale, out string problem)
    {
        var port = For(peer) ?? Ports.FirstOrDefault(p => PortPairing.Read(p).State == PortLinkState.Unlinked);
        if (port == null && stale != null)
        {
            port = Ports.FirstOrDefault(p => PortPairing.Read(p) is var link && link.State == PortLinkState.Linked && stale(link));
            if (port != null) PortPairing.Unlink(port);
        }
        if(port==null) { problem=Text.Get("PortBank.full"); return false; }
        return Role == PortRole.Sender ? PortPairing.TryLink(port, peer, out problem) : PortPairing.TryLink(peer, port, out problem);
    }
}
