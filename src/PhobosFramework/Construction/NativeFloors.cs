using System;
using System.Collections.Generic;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Construction;

/// <summary>How the game makes a floor (Framework 0.73.0). <see cref="TileMark"/> (IsFloor) is a tile property: the
/// tile gets it from the socket loot of the object installed on it (TILFloor, TILFloorFixture). The floor object itself
/// carries no IsFloor. Most floors are grates marked IsFloorGrate (the game finds a tile's floor object by that mark),
/// but the 4 x 4 aero grate and asteroid rock floors carry neither, and floor labels are decals that make no floor.
/// So an object is a floor when its definition gives its tiles IsFloor, as the game's own data says.
///
/// Testing a floor object for IsFloor never matches. That mistake kept every Phobos pipe and conduit off the floor
/// from Framework 0.18.0 to 0.72.0, so no line ever carried anything, and kept G4 reclamation from finding the floor
/// beside a wall. Tile checks keep reading IsFloor on the tile; object checks go through here.</summary>
public static class NativeFloors
{
    public const string ObjectMark = "IsFloorGrate", TileMark = "IsFloor";
    private static readonly Dictionary<string, bool> definitions = new(StringComparer.Ordinal);
    internal static void Reset() => definitions.Clear();

    /// <summary>Whether a definition, installed, makes the tiles under it floor (pure: given the conditions it adds to
    /// its tiles and its own starting conditions).</summary>
    public static bool MakesFloor(IEnumerable<string>? tileConditions, Func<string, bool>? has = null) =>
        tileConditions?.Contains(TileMark) == true || has != null && (has(ObjectMark) || has(TileMark));
    /// <summary>Whether a definition by name is a floor, read once from the game's data and remembered.</summary>
    public static bool IsFloorDefinition(string? name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        if (definitions.TryGetValue(name!, out bool known)) return known;
        var starting = NativePlaceholders.StartingConditions(name!);
        bool floor = MakesFloor(NativePlaceholders.TileConditions(name!), c => starting.Contains(c));
        if (DataHandler.dictCOs != null) definitions[name!] = floor;
        return floor;
    }
    public static bool IsFloorObject(CondOwner? co) => co != null && (co.HasCond(ObjectMark) || co.HasCond(TileMark) || IsFloorDefinition(co.strCODef));
    /// <summary>A live floor that can carry a line or support work: installed, intact and not destroyed.</summary>
    public static bool IsSoundFloorObject(CondOwner? co) =>
        co != null && !co.bDestroyed && co.HasCond("IsInstalled") && !co.HasCond("IsDamaged") && IsFloorObject(co);
}
