namespace PhobosShipbreaker.Core;

/// <summary>The crew loading order behind the right-click "Load feed by crew" toggle: which standing
/// work each machine family runs and how far it goes. The game's own PDA Reload job keeps a container
/// stocked until it is cancelled; this order does the same through Framework's standing orders.</summary>
public static class CrewOrderRules
{
    /// <summary>The standing-order maximum: crew keep loading until the products have nowhere to go.</summary>
    public const int LoadStock = 256;
    public static string? FeedRecipe(string? id) => FurnaceRules.Machine(id) ? "housing" :
        ReclaimerRules.IsFamily(id) || RoutingRules.IsProcessorFamily(id) ? "process" : null;
}
