namespace PhobosAgriculture;

// Content-owned storage limits. Processing still admits individual physical units.
internal static class StackLimits
{
    internal const int Pipes = 10;
    internal const int SeedPotatoes = 10;
    internal const int LettuceSeeds = 50;
    internal const int Nutrients = 25;
    internal const int MakeupSalts = 25;
    internal const int FoodPortions = 10;
    internal const int BulkySupplies = 3;

    internal static int Stock(string id) => id switch
    {
        Definitions.PotatoSeed => SeedPotatoes,
        Definitions.LettuceSeed => LettuceSeeds,
        Definitions.Nutrient => Nutrients,
        WorkupDefinitions.Makeup => MakeupSalts,
        Definitions.Raw => FoodPortions,
        Definitions.Meal => FoodPortions,
        Definitions.Leaves => FoodPortions,
        Service.RecoveryCartridge => BulkySupplies,
        BulkDefinitions.Nutrients => BulkySupplies,
        Definitions.Irrigation => BulkySupplies,
        _ => 1
    };
}
