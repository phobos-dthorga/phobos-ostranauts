using Phobos.Ostranauts.Framework.Inventory;

namespace PhobosShipbreaker.Core;

public static class CollectorRules
{
    public const string Prefix = "PhobosResidueCollector";
    public const string Installed = Prefix + "Installed", Working = Prefix + "Working";
    public const string Controls = Prefix + "Controls";
    public const int Width = 2, Depth = 1, Capacity = 4, StorageSide = 2;
    public const double MachineKg = 20, PayloadKg = 13, CycleSeconds = 5, WorkingKW = 2, IdleKW = 0.05;
    public static readonly ItemDefinitionFilter Filter = new ItemDefinitionFilter(new[] { ProcessRules.Residue });
    public static bool IsFamily(string? id) => id == Installed || id == Prefix + "Loose" ||
        id == Installed + "Dmg" || id == Prefix + "LooseDmg";
    public static bool Accepts(string? id, double kg, bool detached, bool empty, bool unstacked) =>
        Filter.Allows(id) && ProcessRules.MassMatches(kg, PayloadKg) && detached && empty && unstacked;
}
