using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Phobos.Ostranauts.Framework.Registration;

/// <summary>Power points of our installed equipment, against the game's own wall-backed floor equipment (the nav
/// station, the EVA battery charger, the battery): they sit one tile beyond the back edge, in the wall row, so the
/// conduit the walls usually carry reaches them.</summary>
internal static class PowerPointNativeChecks
{
    internal static IEnumerable<(string Id, int Width, int Height, string Point, double X, double Y, double Back)> Points(IEnumerable<NativeDefinitions> definitions)
    {
        foreach (var d in definitions)
            foreach (var co in d.Objects.Values)
            {
                if (co.jsonPI == null || !(co.aStartingConds ?? Array.Empty<string>()).Any(c => c.StartsWith("IsInstalled=", StringComparison.Ordinal))) continue;
                if (!d.Items.TryGetValue(co.strItemDef ?? co.strName, out var item) || item.nCols <= 0 || item.aSocketAdds == null) continue;
                var power = d.Power.TryGetValue(co.jsonPI, out var p) ? p : DataHandler.dictPowerInfo != null && DataHandler.dictPowerInfo.TryGetValue(co.jsonPI, out var native) ? native : null;
                if (power?.aInputPts == null) continue;
                int width = item.nCols, height = item.aSocketAdds.Length / item.nCols;
                foreach (string name in power.aInputPts)
                {
                    var entry = (co.mapPoints ?? Array.Empty<string>()).FirstOrDefault(m => m.StartsWith(name + ",", StringComparison.Ordinal));
                    if (entry == null) continue;
                    var parts = entry.Split(',');
                    yield return (co.strName, width, height, name, double.Parse(parts[1], CultureInfo.InvariantCulture), double.Parse(parts[2], CultureInfo.InvariantCulture), 8.0 * height);
                }
            }
    }

    // Exterior mounts put their back against the hull on their own -y side; the C2 is set into the wall itself; the F6
    // keeps the two front power points its guide documents.
    private static readonly string[] Exterior = { "PhobosExteriorGrabberInstalled", "PhobosMiningLaserInstalled" };
    private static readonly string[] InWall = { "PhobosResidueCollectorInstalled" };
    private static readonly string[] FrontPoints = { "PhobosFurnaceInstalled" };

    internal static void Run(IEnumerable<NativeDefinitions> definitions, Action<bool, string> check)
    {
        var points = Points(definitions).ToArray();
        check(points.Length >= 20, "Powered equipment found for the power-point check (" + points.Length + ")");
        check(ApplianceDefinitions.WallRowY(2) == 24 && ApplianceDefinitions.WallRowY(3) == 32 && ApplianceDefinitions.WallRowY(4) == 40,
            "The wall row is the middle of the tile behind the back edge");
        foreach (var p in points)
        {
            string label = p.Id + " " + p.Point + " at (" + p.X + "," + p.Y + ")";
            if (FrontPoints.Contains(p.Id)) continue;
            if (InWall.Contains(p.Id)) { check(Math.Abs(p.Y) < 8 * p.Height, "A wall-set collector draws power inside its own wall tiles: " + label); continue; }
            double row = Exterior.Contains(p.Id) ? -ApplianceDefinitions.WallRowY(p.Height) : ApplianceDefinitions.WallRowY(p.Height);
            check(p.Y == row && Math.Abs(p.X) < 8 * p.Width, "Power point in the wall row behind the machine, like the game's own wall-backed equipment: " + label);
        }
    }
}
