using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Crew;

namespace Phobos.Ostranauts.Framework.Controls;

public static class ConfigurationStamp
{
    /// <summary>Snapshot only the content-selected maps, never elapsed machinery time or inventories.</summary>
    public static string For(CondOwner co,params string[] prefixes)=>CrewBalance.Binding(co.mapGUIPropMaps
        .Where(p=>prefixes.Any(prefix=>p.Key.StartsWith(prefix,StringComparison.Ordinal))).OrderBy(p=>p.Key,StringComparer.Ordinal)
        .SelectMany(p=>new[]{p.Key}.Concat(p.Value==null?new[]{"invalid"}:p.Value.OrderBy(v=>v.Key,StringComparer.Ordinal).SelectMany(v=>new[]{v.Key,v.Value??"invalid"}))));
    /// <summary>The settings a panel shows (Framework 0.127.2): each field's current choice, plus the maps named by
    /// <paramref name="prefixes"/>, which must hold settings only (links, crew orders). A machine's own working record
    /// also holds its progress, contents and counters, which change every power step or transfer; a stamp over it went
    /// stale before an Apply could land, so a running machine's settings could never be changed from its panel.</summary>
    public static string Of(CondOwner co,IEnumerable<EquipmentField> fields,params string[] prefixes)=>For(co,prefixes)+"|"+Settings(fields);
    /// <summary>The fields' current choices in order, hashed. A field with no current choice (a one-off command such as
    /// a vent or a transfer, offered only while something is in reach) is left out, so its coming and going changes
    /// nothing. Pure.</summary>
    public static string Settings(IEnumerable<EquipmentField> fields)=>CrewBalance.Binding(fields.Where(f=>f.Current.Length>0).Select(f=>f.Current));
    public static void SuspendChangedOrder(CondOwner co)
    {if(CrewWork.Provider(co)!=null&&CrewWork.Order(co).Permission==WorkPermission.Enabled)CrewWork.SetPermission(co,WorkPermission.Suspended,"changed");}
}
