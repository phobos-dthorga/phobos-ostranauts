using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Registration;

/// <summary>Framework 0.79.0 (owner request, 4 October 2026): every bulk vessel shows what it holds on the game's
/// right-click card. Every registered commodity has its row, each row is the kind the game's number module draws
/// (as Ship's Water's tank stat and the game's own liquid D2O stat are), and every row is Phobos' own condition.</summary>
internal static class VesselContentsNativeChecks
{
    internal static void Run(IEnumerable<NativeDefinitions> definitions, string game, Action<bool, string> check)
    {
        var sets = definitions.ToArray();
        // The game's named colours, from its own data (the test host does not load the colour table).
        var colours = new HashSet<string>(Directory.GetFiles(Path.Combine(game, "Ostranauts_Data", "StreamingAssets", "data", "colors"), "*.json")
            .SelectMany(f => JArray.Parse(File.ReadAllText(f)).Select(c => (string?)c["strName"] ?? "")), StringComparer.Ordinal);
        var commodities = BulkVessels.All.Select(s => s.Commodity).Distinct(StringComparer.Ordinal).OrderBy(c => c, StringComparer.Ordinal).ToArray();
        check(commodities.Length >= 9, "Every Phobos mod's vessel families are registered (" + string.Join(", ", commodities) + ")");
        var undeclared = commodities.Where(c => !VesselContentsDisplay.IsDeclared(c)).ToArray();
        check(undeclared.Length == 0, "Every stored commodity shows on the card: " + string.Join(", ", undeclared));
        var native = DataHandler.dictConds["StatLiqD2O"];
        var rows = commodities.Select(VesselContentsDisplay.ConditionName).Append(VesselContentsDisplay.Trapped).ToArray();
        check(rows.Distinct(StringComparer.Ordinal).Count() == rows.Length, "No two commodities share a row");
        foreach (string name in rows)
        {
            var owners = sets.Where(d => d.Conditions.ContainsKey(name)).ToArray();
            check(owners.Length >= 1 && name.StartsWith(VesselContentsDisplay.Prefix, StringComparison.Ordinal), "The row is Phobos' own condition, declared with its content: " + name);
            check(DataHandler.dictConds.TryGetValue(name, out var row) && row.nDisplayType == native.nDisplayType && row.nDisplayType == VesselContentsDisplay.NumberDisplay &&
                  row.nDisplayOther == VesselContentsDisplay.CardOnly && row.nDisplaySelf == VesselContentsDisplay.CardOnly && row.fConversionFactor == native.fConversionFactor &&
                  row.strDisplayBonus == VesselContentsDisplay.Unit && row.bPersists && !string.IsNullOrWhiteSpace(row.strNameFriendly),
                "The row is a card number in kilograms that follows the vessel through damage and installation: " + name);
            check(row != null && colours.Contains(row.strColor), "The row uses one of the game's named colours: " + name + " (" + row?.strColor + ")");
        }
        check(!rows.Any(r => DataHandler.dictCOs.Values.Any(c => (c.aStartingConds ?? Array.Empty<string>()).Any(s => s.StartsWith(r + "=", StringComparison.Ordinal)))),
            "No definition starts with a contents row: it is written from the record, never from a definition");
    }
}
