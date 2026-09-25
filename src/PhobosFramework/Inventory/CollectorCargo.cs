using System;
using System.Collections.Generic;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Inventory;

/// <summary>Content-owned admission of additional physical cargo to the existing residue collector.
/// Does not authorize routing, pairing, bypassing capacity or resource creation.</summary>
public static class CollectorCargo
{
    private static readonly Dictionary<string,Func<CondOwner,bool>> providers=new(StringComparer.Ordinal);
    private static Func<CondOwner,bool>? endpoint;
    public static void SetEndpointValidator(Func<CondOwner,bool>? validate)=>endpoint=validate;
    public static bool EndpointReady(CondOwner collector)=>endpoint?.Invoke(collector)==true;
    public static void Register(string owner,Func<CondOwner,bool> accepts)=>providers.Add(owner,accepts);
    public static void Unregister(string owner)=>providers.Remove(owner);
    public static bool Accepts(CondOwner item)=>providers.Values.Any(p=>p(item));
}
