using System;

namespace PhobosShipbreaker.Core;

public static class IndustrialRules
{
    public const string Prefix = "PhobosIndustrialConsole", Controls = "PhobosIndustrialControls";
    public const string LocalControls = "PhobosEquipmentControls";
    /// <summary>Right-click toggle: crew keep this machine's feed loaded until switched off.</summary>
    public const string FeedOrder = "PhobosEquipmentFeedOrder";
    public const int Footprint = 3;
    public const double MassKg = 40, PowerKW = 0.08, AccessTiles = 2.5, RefreshSeconds = 0.5;
    public const int CompactWidth = 900;
    public static bool Console(string? id) => id == Prefix + "Installed" || id == Prefix + "InstalledDmg" || id == Prefix + "Loose" || id == Prefix + "LooseDmg";
    public static string Group(string? id) => Console(id) ? "console" :
        FurnaceRules.Machine(id) ? "furnace" : FurnaceRules.Cooling(id) ? "radiator" :
        IntakeRules.IsHardware(id) && id!.StartsWith(IntakeRules.Grabber, StringComparison.Ordinal) ? "grabber" :
        IntakeRules.IsHardware(id) && id!.StartsWith(IntakeRules.Chute, StringComparison.Ordinal) ? "chute" :
        ReclaimerRules.IsFamily(id) ? "reclaimer" : CollectorRules.IsFamily(id) ? "collector" :
        ThawRules.IsFamily(id) ? "thaw" : LaserRules.IsFamily(id) ? "laser" :
        id == "PhobosShipbreakerInstalled" || id == "PhobosShipbreakerInstalledDmg" ? "fixture" : "";
    public static bool Equipment(string? id) => Group(id) != "" && !Console(id);
    /// <summary>Equipment whose own Control Panel is Framework's shared panel (0.63.0): the T2 thaw unit and the ML-2
    /// laser, which are presented entirely by their provider. Everything else keeps the industrial panel, with its
    /// routing, furnace and capture pages.</summary>
    public static bool SharedPanel(string? id) => ThawRules.IsFamily(id) || LaserRules.IsFamily(id);
}
