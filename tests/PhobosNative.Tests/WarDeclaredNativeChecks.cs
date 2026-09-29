using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Phobos.Ostranauts.Framework.Construction;
using PhobosWarDeclared;
using PhobosWarDeclared.Core;

/// <summary>War Has Been Declared against the installed game's own definitions: install jobs created by the
/// game's Installables.Create, damage chains, state variants, footprints and the navigation-station orders.</summary>
internal static class WarDeclaredNativeChecks
{
    internal static void Run(string game, Action<bool, string> check)
    {
        string native = Path.Combine(game, "Ostranauts_Data", "StreamingAssets", "data");
        var jobs = Directory.GetFiles(Path.Combine(native, "installables"), "*.json", SearchOption.AllDirectories)
            .SelectMany(f => JsonConvert.DeserializeObject<JsonInstallable[]>(File.ReadAllText(f))!).ToArray();
        foreach (var job in jobs) DataHandler.dictInstallables[job.strName] = job;
        foreach (var job in jobs.Where(j => j.strJobType == "install"))
        {
            try { Installables.Create(job); } catch { /* a few native jobs need runtime state; they are not used below */ }
        }
        NativePlaceholders.Reset();

        check(NativePlaceholders.IntactFormOf("ItmWall1x1Dmg") == "ItmWall1x1", "A damaged wall resolves to the intact wall");
        check(NativePlaceholders.IntactFormOf("ItmWall1x1") == "ItmWall1x1", "An intact part resolves to itself");
        check(NativePlaceholders.IntactFormOf("ItmScrapTrash") == "ItmScrapTrash", "Scrap is never traced back to a part");
        var wallJob = NativePlaceholders.InstallJobFor("ItmWall1x1");
        check(wallJob?.strActionCO == "ItmWall1x1Loose" && NativePlaceholders.MenuFor("ItmWall1x1") == "HULL", "The wall's own INSTALL job takes a loose wall");
        check(NativePlaceholders.RebuildTarget("ItmWall1x1Dmg") == "ItmWall1x1", "A destroyed damaged wall is rebuilt as a wall");
        check(NativePlaceholders.RebuildTarget("ItmDoor01Closed") == "ItmDoor01Open", "A closed door is rebuilt through its uninstall job's loose door");
        check(NativePlaceholders.RebuildTarget("ItmDoor01ClosedLocked") == "ItmDoor01Open", "A locked door is rebuilt the same way");
        check(NativePlaceholders.RebuildTarget("ItmConduit01Dmg") == "ItmConduit01", "Damaged conduit is rebuilt as conduit, not re-laid damaged");
        check(NativePlaceholders.InstallJobFor("ItmConduit01Dmg") == null, "The game's untabbed damaged re-install job is not offered");
        check(NativePlaceholders.RebuildTarget("ItmScrapTrash") == null, "Scrap has nothing to rebuild");

        // Coverage over every installed form the game can uninstall: how many a build site can bring back.
        var installed = jobs.Where(j => j.strJobType == "uninstall" && j.strActionCO != null && DataHandler.dictCOs.ContainsKey(j.strActionCO))
            .Select(j => j.strActionCO).Distinct().ToArray();
        int rebuildable = installed.Count(d => NativePlaceholders.RebuildTarget(d) != null);
        check(rebuildable * 10 >= installed.Length * 9, $"At least nine in ten uninstallable native parts resolve to a build site ({rebuildable}/{installed.Length})");
        foreach (var d in installed)
        {
            var target = NativePlaceholders.RebuildTarget(d);
            if (target != null) check(NativePlaceholders.InstallJobFor(target) != null && !target.Contains("Dmg"), "Rebuild targets are tabbed, intact installs: " + d + " -> " + target);
        }

        check(NativePlaceholders.Footprint("ItmWall1x1") == PlaceholderFootprint.Blocks, "A wall build site blocks walking");
        check(NativePlaceholders.Footprint("ItmDoor01Open") == PlaceholderFootprint.Blocks, "A door build site blocks walking (it is not a working door)");
        check(NativePlaceholders.Footprint("ItmFloorGrate01") == PlaceholderFootprint.Walkable, "A floor build site can be walked over");
        check(NativePlaceholders.Footprint("ItmConduit01") == PlaceholderFootprint.Walkable, "A conduit build site can be walked over");
        check(NativePlaceholders.Footprint("NoSuchPart") == PlaceholderFootprint.Unknown, "An unknown part has an unknown footprint");
        check(NativePlaceholders.TileConditions("ItmFloorGrate01")!.Contains("IsFloor"), "Floors mark their tiles as floor");

        // Phobos four-form families published earlier in this run resolve too (damage stays installed).
        var families = DataHandler.dictCOs.Keys.Where(k => k.StartsWith("Phobos", StringComparison.Ordinal) && k.EndsWith("InstalledDmg", StringComparison.Ordinal) &&
            DataHandler.dictCOs.ContainsKey(k.Substring(0, k.Length - 3))).ToArray();
        check(families.Length > 0, "Phobos equipment families are available to check");
        foreach (var damaged in families)
            check(NativePlaceholders.IntactFormOf(damaged) == damaged.Substring(0, damaged.Length - 3), "Phobos damaged form resolves to its intact form: " + damaged);

        // Schematic facts on real parts: safe lays floors and conduit, holds walls and doors.
        var safe = Schematic.Parse("safe", File.ReadAllText(Path.Combine(FindRepo(), "mods", "PhobosWarDeclared", "schematics", "safe.json")));
        check(safe.Evaluate(WarService.Facts("ItmFloorGrate01"), out _) == SchematicAction.Lay, "Safe lays a real floor");
        check(safe.Evaluate(WarService.Facts("ItmConduit01"), out _) == SchematicAction.Lay, "Safe lays real conduit");
        check(safe.Evaluate(WarService.Facts("ItmWall1x1"), out _) == SchematicAction.Hold, "Safe holds a real wall");
        check(safe.Evaluate(WarService.Facts("ItmDoor01Open"), out _) == SchematicAction.Hold, "Safe holds a real door");
        check(WarService.Facts("ItmWall1x1").Conditions.Contains("IsWall") && WarService.Facts("ItmWall1x1").Menu == "HULL", "Schematic facts carry tile conditions and the INSTALL tab");

        // Orders on the game's own navigation stations, amended in place and only once.
        var prepared = Content.Prepare();
        check(prepared.Interactions.Count == 3 && Content.Orders.All(prepared.Interactions.ContainsKey), "Three battle orders are prepared");
        foreach (var order in prepared.Interactions.Values)
            check(order.strRaiseUI == null && !string.IsNullOrEmpty(order.strTitle) && order.strTitle != order.strName, "Order has a player title and no window: " + order.strName);
        prepared.Publish();
        int stations = Content.AmendNavigationStations();
        check(stations > 0, "Installed navigation stations exist to carry the orders");
        Content.AmendNavigationStations();
        foreach (var station in DataHandler.dictCOs.Values.Where(Content.IsInstalledStation))
            check(Content.Orders.All(o => station.aInteractions.Count(x => x == o) == 1), "Each order appears once on " + station.strName);
        check(!DataHandler.dictCOs.Values.Where(d => !Content.IsInstalledStation(d)).Any(d => d.aInteractions != null && d.aInteractions.Contains(WarRules.BattleStations)),
            "Only installed navigation stations carry battle orders");
    }

    private static string FindRepo()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "AGENTS.md"))) dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
