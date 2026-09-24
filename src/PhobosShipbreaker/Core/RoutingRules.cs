using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Inventory;

namespace PhobosShipbreaker.Core;

public static class RoutingRules
{
    // Published addresses keep their meaning. A reclaimer's input is a distinct port.
    public const string SendPort = "PhobosShipbreaker.ResidueOut", CollectorIn = "PhobosShipbreaker.ResidueIn";
    public const string ReclaimerIn = "PhobosShipbreaker.ReclaimerFeed", Feeding = "PhobosReclaimerFeeding";
    public const string Processor = "PhobosShipbreakerInstalled";
    public const double FeedSeconds = 2, FeedKW = 2;
    // Address resolution/unlink must still work after damage or uninstallation.
    // The service separately requires healthy installed endpoints before linking/running.
    private static string? Family(string? id) => ReclaimerRules.IsFamily(id) ? ReclaimerRules.Installed :
        CollectorRules.IsFamily(id) ? CollectorRules.Installed :
        new[] { Processor, Processor + "Dmg", "PhobosShipbreakerLoose", "PhobosShipbreakerLooseDmg" }.Contains(id) ? Processor : null;
    public static bool IsSender(string? id) => Family(id) != null;
    public static bool IsReceiver(string? id) => Family(id) == CollectorRules.Installed || Family(id) == ReclaimerRules.Installed;
    public static bool CanConnect(string? sender, string? receiver) =>
        Family(receiver) == ReclaimerRules.Installed ? Family(sender) == Processor || Family(sender) == CollectorRules.Installed :
        Family(receiver) == CollectorRules.Installed && (Family(sender) == Processor || Family(sender) == ReclaimerRules.Installed);
    public static string[] FilterIds(string choice) => choice switch
    {
        "all" => new[] { ProcessRules.Residue, ReclaimerRules.Feedstock, ReclaimerRules.Reject },
        "feed" => new[] { ReclaimerRules.Feedstock },
        "rejects" => new[] { ReclaimerRules.Reject },
        "legacy" => new[] { ProcessRules.Residue },
        _ => Array.Empty<string>()
    };
    public static bool ValidChoice(bool reclaimer, string choice) => reclaimer ? choice == "feed" : FilterIds(choice).Length > 0;
    public static bool CompatibleFilter(bool reclaimer, PortFilterSnapshot filter) => filter.State == PortFilterState.Default ||
        filter.State == PortFilterState.Configured && filter.DefinitionIds.Count > 0 &&
        filter.DefinitionIds.All(new ItemDefinitionFilter(FilterIds(reclaimer ? "feed" : "all")).Allows);
    public static double DemandKW(bool processing, bool feeding, double workKW, double feedKW) =>
        (processing ? workKW : ReclaimerRules.IdleKW) + (feeding ? feedKW : 0);
}
