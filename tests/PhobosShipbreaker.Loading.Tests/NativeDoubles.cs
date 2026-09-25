using System;
using System.Collections.Generic;

// Controlled loader boundary, not a Unity simulation. Native signature/order
// checks separately inspect the installed game assembly.
namespace HarmonyLib { [AttributeUsage(AttributeTargets.Class)] public sealed class HarmonyPatch : Attribute { public HarmonyPatch(Type type, string name) { } } }
namespace UnityEngine { public sealed class GameObject { } }
public struct Point { public float x, y; public Point(float x, float y) { this.x = x; this.y = y; } }
public sealed class Marker { public string strName = "marker", strInstalledCO = "Owned"; }
public sealed class SavedShip
{
    public Marker[] aPlaceholders = { new Marker() };
    public object[] aRooms = { new object() };
    public int nCols = 63, nRows = 44;
    public Point vShipPos = new Point(-32, -14);
}
public sealed class Ship
{
    public enum Loaded { None, Shallow, Edit, Full }
    public Loaded LoadState;
    public SavedShip json = new SavedShip();
    public int nCols = 62, nRows = 44;
    public string strRegID = "test";
    public Point vShipPos = new Point(-32, -14);
    public List<int> aTiles = new List<int>(new int[62 * 44]);
}
public sealed class CondOwner { public bool Placeholder = true; public bool HasCond(string name) => name == "IsPlaceholder" && Placeholder; }
public sealed class OwnerDefinition { public string strItemDef = "Owned"; }
public sealed class ItemDefinition { public int nCols = 4; public string[] aSocketAdds = new string[12]; }
public static class DataHandler
{
    public static Dictionary<string, OwnerDefinition> dictCOs = new Dictionary<string, OwnerDefinition> { ["Owned"] = new OwnerDefinition() };
    public static Dictionary<string, ItemDefinition> dictItemDefs = new Dictionary<string, ItemDefinition> { ["Owned"] = new ItemDefinition() };
}
public static class TileUtils
{
    public static int Calls;
    public static bool PadTilemap(Ship ship, UnityEngine.GameObject parent, int left, int right, int top, int bottom)
    {
        Calls++;
        ship.nCols += left + right; ship.nRows += top + bottom;
        ship.vShipPos = new Point(ship.vShipPos.x - left, ship.vShipPos.y + top);
        ship.aTiles = new List<int>(new int[ship.nCols * ship.nRows]);
        return true;
    }
}
namespace PhobosShipbreaker
{
    internal static class Content { internal static bool Ready = true; internal static bool OwnsInstalledDefinition(string? id) => id == "Owned"; }
    internal static class Plugin { internal static List<string> Messages = new List<string>(); internal static Action<string> Log = Messages.Add; }
    internal static class Text { internal static string Get(string key, params object[] values) => key; }
}
