using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

/// <summary>The game's ordinary walls are one base definition plus cosmetic overlay variants: an
/// overlay object carries the variant's name in strCODef and the variant's mass, while the game
/// resolves it to the base for everything else (DataHandler.GetCondOwner, GetDataCO). Uninstalling a
/// variant wall keeps the variant on the loose replacement. Identity therefore means "base is the
/// ordinary wall", never the exact name; mass is whatever the wall actually weighs.</summary>
internal static class WallIdentity
{
    internal static string? Base(string? definition) =>
        definition != null && DataHandler.dictCOOverlays != null && DataHandler.dictCOOverlays.TryGetValue(definition, out var overlay) &&
        !string.IsNullOrEmpty(overlay?.strCOBase) ? overlay!.strCOBase : definition;
    internal static bool IsLooseOrdinary(string? definition) => Base(definition) == ProcessRules.Wall;
    internal static bool IsInstalledOrdinary(string? definition) => Base(definition) == ProcessRules.InstalledWall;
    /// <summary>A detached, single, empty ordinary wall the panel recipe can take.</summary>
    internal static bool OrdinaryLooseWall(CondOwner? wall) => wall != null && !wall.bDestroyed && IsLooseOrdinary(wall.strCODef) &&
        !wall.HasCond("IsInstalled") && wall.coStackHead == null && (wall.aStack == null || wall.aStack.Count == 0) &&
        wall.GetCOsSafe(true).Count == 0 && ProcessRules.AcceptedWallKg(wall.GetTotalMass());
}
