using System;
using System.Collections.Generic;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Inventory;

/// <summary>Bounded reciprocal fan-out. Slot zero retains the original single-port identity.</summary>
public sealed class PortBank
{
    public IReadOnlyList<MaterialPort> Ports { get; }
    public PortBank(string owner, string primary, Dictionary<string, Dictionary<string,string>> maps, int count)
    {
        if(count < 1 || count > 64) throw new ArgumentOutOfRangeException(nameof(count));
        Ports = Enumerable.Range(0,count).Select(n => new MaterialPort(owner,n==0?primary:primary+"."+n,maps)).ToArray();
    }
    public bool Occupied => Ports.Any(p=>PortPairing.Read(p).State!=PortLinkState.Unlinked);
    public MaterialPort? ForReceiver(MaterialPort receiver) => Ports.FirstOrDefault(p=>PortPairing.Matches(p,receiver));
    public bool TryLink(MaterialPort receiver,out string problem)
    {
        var port=ForReceiver(receiver) ?? Ports.FirstOrDefault(p=>PortPairing.Read(p).State==PortLinkState.Unlinked);
        if(port==null) { problem=Text.Get("PortBank.full"); return false; }
        return PortPairing.TryLink(port,receiver,out problem);
    }
}
