using System;

namespace PhobosShipbreaker.Core;

public static class IndustrialRules
{
    public const string Prefix = "PhobosIndustrialConsole", Controls = "PhobosIndustrialControls";
    public const string LocalControls = "PhobosEquipmentControls";
    public const int Footprint = 3;
    public const double MassKg = 40, PowerKW = 0.08, AccessTiles = 2.5, RefreshSeconds = 0.5;
    public const int CompactWidth = 900;
    public static bool Console(string? id) => id == Prefix + "Installed" || id == Prefix + "InstalledDmg" || id == Prefix + "Loose" || id == Prefix + "LooseDmg";
    public static string Group(string? id) => Console(id) ? "console" :
        IntakeRules.IsHardware(id) && id!.StartsWith(IntakeRules.Grabber, StringComparison.Ordinal) ? "grabber" :
        IntakeRules.IsHardware(id) && id!.StartsWith(IntakeRules.Chute, StringComparison.Ordinal) ? "chute" :
        ReclaimerRules.IsFamily(id) ? "reclaimer" : CollectorRules.IsFamily(id) ? "collector" :
        id == "PhobosShipbreakerInstalled" || id == "PhobosShipbreakerInstalledDmg" ? "fixture" : "";
    public static bool Equipment(string? id) => Group(id) != "" && !Console(id);
}
