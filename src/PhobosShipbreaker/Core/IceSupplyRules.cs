namespace PhobosShipbreaker.Core;

/// <summary>Where the T2's water ice comes from (owner decisions, 30 September 2026). The game ships an ice
/// asteroid cluster that no star-system field references, so in ordinary play water ice turned up only in
/// C-class deposit pulls. Shipbreaker carves small shares for it, never adding rolls:
/// the game's own ice cluster joins the C- and S-class field pickers (newly generated asteroids only), and
/// C-class deposits give a little more water ice in place of some silicates (existing saves too).
/// These shares are named constants, not player tuning knobs: the silicates share is a budget other mods
/// carve too (Manufacturing's clay hydrates).</summary>
public static class IceSupplyRules
{
    public const string IceCluster = "ClusterI01";
    public const string CFields = "RandomAsteroidC", SFields = "RandomAsteroidS";
    /// <summary>Donors: the smallest core-less pure dark cluster (C03 has the same content), and the S picker's
    /// dominant cluster.</summary>
    public const string CFieldDonor = "ClusterC02", SFieldDonor = "ClusterS01";
    public const double FieldShare = 0.05;
    public const string DepositTable = "ItmRandomMineralCClass", DepositDonor = "ItmMineral04", WaterIce = "ItmIce01";
    public const double DepositIceShare = 0.05;
}
