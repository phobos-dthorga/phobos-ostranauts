using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Registration;
using PhobosWarDeclared.Core;

namespace PhobosWarDeclared;

/// <summary>Registration and readiness. The mod adds no items: only three orders on the game's own
/// navigation stations (Battle stations, Stand down, Lay held build sites).</summary>
internal static class Content
{
    internal const string ModName = "Phobos' War Has Been Declared";
    internal static readonly string[] Orders = { WarRules.BattleStations, WarRules.StandDown, WarRules.LayHeld };
    internal static bool Ready { get; private set; }

    internal static void Register(Action<string> log)
    {
        Ready = false;
        try
        {
            var mod = DataHandler.dictModInfos?.Values.FirstOrDefault(m => m.strName == ModName && !m.GetIsDisabled());
            if (mod == null) throw new InvalidOperationException(Text.Get("Content.missing_package"));
            Prepare().Publish();
            int stations = AmendNavigationStations();
            Ready = true;
            log(Text.Get("Content.ready", stations));
        }
        catch (Exception e) { Ready = false; log(Text.Get("Content.failed")); log(e.ToString()); }
    }

    internal static NativeDefinitions Prepare()
    {
        var d = new NativeDefinitions();
        foreach (var name in Orders)
        {
            // Like the game's own Toggle Power: a short action taken at the station, with no window of its own.
            var order = NativeDefinitions.Clone(DataHandler.dictInteractions["Inventory"]);
            string key = name.Substring(WarRules.Prefix.Length);
            order.strName = name; order.strTitle = Text.Get("Order." + key + ".title");
            order.strDesc = Text.Get("Order." + key + ".action"); order.strTooltip = Text.Get("Order." + key + ".tooltip");
            order.strRaiseUI = null; order.fTargetPointRange = 2;
            d.Interactions[name] = order;
        }
        return d;
    }

    /// <summary>Appends the orders to every installed navigation station definition, vanilla or modded,
    /// in place (never republished by name). Returns how many definitions carry them.</summary>
    internal static int AmendNavigationStations()
    {
        int count = 0;
        foreach (var definition in DataHandler.dictCOs.Values.Where(IsInstalledStation))
        {
            DefinitionAmendments.AppendInteractions(definition, Orders);
            count++;
        }
        return count;
    }

    internal static bool IsInstalledStation(JsonCondOwner? definition)
    {
        var conditions = definition?.aStartingConds;
        return conditions != null && conditions.Any(c => c.StartsWith("IsNavStation=", StringComparison.Ordinal)) &&
            conditions.Any(c => c.StartsWith("IsInstalled=", StringComparison.Ordinal));
    }

    /// <summary>An order is offered only on an intact installed station of a ship the player owns, and only when it would do something.</summary>
    internal static bool Offered(string order, CondOwner? station)
    {
        if (!Ready || station == null || station.bDestroyed || !station.HasCond("IsNavStation") || !station.HasCond("IsInstalled") || station.HasCond("IsDamaged")) return false;
        return order switch
        {
            WarRules.BattleStations => WarService.CanDeclare(station.ship),
            WarRules.StandDown => WarService.CanStandDown(station.ship),
            WarRules.LayHeld => WarService.CanLayHeld(station.ship),
            _ => false
        };
    }

    internal static bool Carry(string order, CondOwner station, out string message) => order switch
    {
        WarRules.BattleStations => WarService.Declare(station.ship, out message),
        WarRules.StandDown => WarService.StandDown(station.ship, out message),
        WarRules.LayHeld => WarService.LayHeld(station.ship, out message),
        _ => throw new ArgumentOutOfRangeException(nameof(order))
    };
}
