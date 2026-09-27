namespace PhobosAutoNav;

internal static class StockQuantities
{
    internal const double EquipmentChance = .85;
    internal static double Chance(string item, double original) => System.Math.Min(1, System.Math.Max(EquipmentChance, original));

    // Each successful merchant offer, in every region; rare derelict salvage stays separate.
    internal const int Boards = 16;
}
