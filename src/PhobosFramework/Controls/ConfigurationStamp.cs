using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Crew;

namespace Phobos.Ostranauts.Framework.Controls;

public static class ConfigurationStamp
{
    /// <summary>Snapshot only the content-selected maps, never elapsed machinery time or inventories.</summary>
    public static string For(CondOwner co,params string[] prefixes)=>CrewBalance.Binding(co.mapGUIPropMaps
        .Where(p=>prefixes.Any(prefix=>p.Key.StartsWith(prefix,StringComparison.Ordinal))).OrderBy(p=>p.Key,StringComparer.Ordinal)
        .SelectMany(p=>new[]{p.Key}.Concat(p.Value==null?new[]{"invalid"}:p.Value.OrderBy(v=>v.Key,StringComparer.Ordinal).SelectMany(v=>new[]{v.Key,v.Value??"invalid"}))));
    public static void SuspendChangedOrder(CondOwner co)
    {if(CrewWork.Provider(co)!=null&&CrewWork.Order(co).Permission==WorkPermission.Enabled)CrewWork.SetPermission(co,WorkPermission.Suspended,"changed");}
}
