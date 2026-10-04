namespace PhobosManufacturing.Core;

/// <summary>Identities of the regolith floor (Manufacturing 0.51.0): the Phobos twin of the game's Polished Regolith
/// Floor, laid from one sintered paver. Saved games hold the twin's id, so it never changes.</summary>
public static class RegolithFloor
{
    /// <summary>The installed twin, and the hidden condition only it carries.</summary>
    public const string Installed = "PhobosRegolithFloor", Identity = "PhobosRegolithFloorIdentity";
    public const string PaverTrigger = "PhobosRegolithPaverTLoose", InstalledTrigger = "PhobosRegolithFloorTInstalled";
    public const string InstallJob = "PhobosRegolithFloorInstall", UninstallJob = "PhobosRegolithFloorUninstall";
    /// <summary>The game's own definitions this is built from, by reference: the tile, the loose floor plate whose
    /// picture the paver shows until it has its own.</summary>
    public const string NativeTile = "ItmFloorGrate02", LooseDonor = "ItmFloorGrate01Loose";
    /// <summary>One paver lays one tile: both are 6.5 kg.</summary>
    public const double TileKg = 6.5;
}
