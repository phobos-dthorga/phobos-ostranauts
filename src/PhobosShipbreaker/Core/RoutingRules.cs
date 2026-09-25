using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Inventory;

namespace PhobosShipbreaker.Core;

public static class RoutingRules
{
    // Published addresses keep their meaning. A reclaimer's input is a distinct port.
    public const string SendPort = "PhobosShipbreaker.ResidueOut", CollectorIn = "PhobosShipbreaker.ResidueIn";
    public const string ReclaimerIn = "PhobosShipbreaker.ReclaimerFeed", Feeding = "PhobosReclaimerFeeding";
    public const string MetalsOut = "PhobosShipbreaker.MetalsOut", FurnaceIn = "PhobosFurnace.MaterialIn", FurnaceOut = "PhobosFurnace.MaterialOut";
    public const string Processor = "PhobosShipbreakerInstalled";
    public const double FeedSeconds = 2, FeedKW = 2;
    // Address resolution/unlink must still work after damage or uninstallation.
    // The service separately requires healthy installed endpoints before linking/running.
    private static string? Family(string? id) => FurnaceRules.Machine(id) ? FurnaceRules.Prefix : ReclaimerRules.IsFamily(id) ? ReclaimerRules.Installed :
        CollectorRules.IsFamily(id) ? CollectorRules.Installed :
        new[] { Processor, Processor + "Dmg", "PhobosShipbreakerLoose", "PhobosShipbreakerLooseDmg" }.Contains(id) ? Processor : null;
    public static bool IsSender(string? id) => Family(id) != null;
    public static bool IsReceiver(string? id) => FurnaceRules.Machine(id) || Family(id) == CollectorRules.Installed || Family(id) == ReclaimerRules.Installed;
    public static string OutputPort(string? id, bool metals = false) => FurnaceRules.Machine(id) ? FurnaceOut : metals && ReclaimerRules.IsFamily(id) ? MetalsOut : SendPort;
    public static string InputPort(string? id) => FurnaceRules.Machine(id) ? FurnaceIn : ReclaimerRules.IsFamily(id) ? ReclaimerIn : CollectorIn;
    public static string SourcePort(string? source, string? receiver) => OutputPort(source, FurnaceRules.Machine(receiver));
    public static bool CanConnect(string? sender, string port, string? receiver) => CanConnect(sender, receiver) && port == SourcePort(sender, receiver);
    public static bool CanConnect(string? sender, string? receiver) =>
        FurnaceRules.Machine(receiver) ? ReclaimerRules.IsFamily(sender) :
        Family(receiver) == ReclaimerRules.Installed ? Family(sender) == Processor || Family(sender) == CollectorRules.Installed :
        Family(receiver) == CollectorRules.Installed && (Family(sender) == Processor || Family(sender) == ReclaimerRules.Installed || FurnaceRules.Machine(sender));
    public static string[] FilterIds(string choice) => choice switch
    {
        "all" => new[] { ProcessRules.Residue, ReclaimerRules.Feedstock, ReclaimerRules.Reject },
        "feed" => new[] { ReclaimerRules.Feedstock },
        "rejects" => new[] { ReclaimerRules.Reject },
        "legacy" => new[] { ProcessRules.Residue },
        "aluminium" => new[] { FurnaceMaterialRules.Aluminium },
        "furnace-products" => new[] { FurnaceRules.Blank, FurnaceRules.Remainder },
        _ => Array.Empty<string>()
    };
    public static string[] Choices(string? id) => FurnaceRules.Machine(id) ? new[] { "aluminium" } : ReclaimerRules.IsFamily(id) ? new[] { "feed" } : new[] { "all", "feed", "rejects", "legacy", "furnace-products" };
    public static string DefaultFilter(string? id) => FurnaceRules.Machine(id) ? "aluminium" : ReclaimerRules.IsFamily(id) ? "feed" : "all";
    public static bool ValidChoice(bool reclaimer, string choice) => Choices(reclaimer ? ReclaimerRules.Installed : CollectorRules.Installed).Contains(choice);
    public static bool CompatibleFilter(string? id, PortFilterSnapshot filter) => filter.State == PortFilterState.Default ||
        filter.State == PortFilterState.Configured && filter.DefinitionIds.Count > 0 && Choices(id).Any(choice =>
            filter.DefinitionIds.All(new ItemDefinitionFilter(FilterIds(choice)).Allows));
    public static bool CompatibleFilter(bool reclaimer, PortFilterSnapshot filter) =>
        CompatibleFilter(reclaimer ? ReclaimerRules.Installed : CollectorRules.Installed, filter);
    public static double DemandKW(bool processing, bool feeding, double workKW, double feedKW) =>
        (processing ? workKW : ReclaimerRules.IdleKW) + (feeding ? feedKW : 0);
}
