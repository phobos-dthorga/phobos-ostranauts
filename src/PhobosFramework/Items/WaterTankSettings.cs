using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Items;

/// <summary>The crew water reserve moved from Shipbreaker's settings to Framework's with the tanks (0.58.0). The first
/// time Framework's own entry is created, it starts from the value the player set under Shipbreaker's Silo section, so
/// nobody has to set it again; afterwards Framework's entry is the only one read. Files are only read, never written.</summary>
public static class WaterTankSettings
{
    public const string ShipbreakerConfig = "phobosgekko.ostranauts.shipbreaker.cfg";
    public static double InitialReserve(string frameworkConfigPath, string configRoot)
    {
        try
        {
            if (File.Exists(frameworkConfigPath) && File.ReadAllText(frameworkConfigPath).Contains("CrewWaterReserveKg")) return WaterTanks.DefaultCrewReserveKg;
            string old = Path.Combine(configRoot, ShipbreakerConfig);
            return File.Exists(old) ? Parse(File.ReadAllLines(old)) ?? WaterTanks.DefaultCrewReserveKg : WaterTanks.DefaultCrewReserveKg;
        }
        catch (Exception) { return WaterTanks.DefaultCrewReserveKg; }
    }
    /// <summary>The CrewWaterReserveKg value under a [Silo] section of a BepInEx configuration file, or null.</summary>
    public static double? Parse(string[] lines)
    {
        bool inSilo = false;
        foreach (string raw in lines)
        {
            string line = raw.Trim();
            if (line.StartsWith("[", StringComparison.Ordinal)) { inSilo = line == "[Silo]"; continue; }
            if (!inSilo || line.StartsWith("#", StringComparison.Ordinal)) continue;
            int eq = line.IndexOf('=');
            if (eq < 0 || line.Substring(0, eq).Trim() != "CrewWaterReserveKg") continue;
            return double.TryParse(line.Substring(eq + 1).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double kg) && kg >= 0 && kg <= 100000 ? kg : null;
        }
        return null;
    }
}
