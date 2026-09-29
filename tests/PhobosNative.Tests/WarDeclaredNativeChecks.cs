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
        // 29 September 2026 pass (FF7): facts are built once per part until content reloads.
        check(ReferenceEquals(WarService.Facts("ItmWall1x1"), WarService.Facts("ItmWall1x1")), "A part's schematic facts are remembered");
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 10000; i++) WarService.Facts("ItmWall1x1");
        check(GC.GetAllocatedBytesForCurrentThread() == allocated, "Remembered facts allocate nothing");
        WarService.Reset();
        check(!ReferenceEquals(WarService.Facts("ItmWall1x1"), WarService.Facts("ItmFloorGrate01")) && WarService.Facts("ItmWall1x1").Conditions.Contains("IsWall"), "A content reload rebuilds the facts");
        check(WarRules.RetrySeconds > WarRules.PollSeconds, "Blocked build sites are retried less often than the poll runs");

        // safe-walls: walls join what safe lays; doors, hatches and docking ports stay held. The sweep covers every
        // HULL part carrying the wall tile condition, so a future game-data change to a passage is noticed here.
        var safeWalls = Schematic.Parse("safe-walls", File.ReadAllText(Path.Combine(FindRepo(), "mods", "PhobosWarDeclared", "schematics", "safe-walls.json")));
        var laid = new System.Collections.Generic.List<string>();
        foreach (var part in Installables.dictJobBuildOptions.Values.SelectMany(menu => menu.Keys).Distinct().OrderBy(k => k, StringComparer.Ordinal))
        {
            var facts = WarService.Facts(part);
            if (facts.Menu != "HULL" || !facts.Conditions.Contains("IsWall")) continue;
            bool passage = facts.Conditions.Contains("IsPortal") || facts.Conditions.Contains("IsDockSys") || part.StartsWith("ItmHatch", StringComparison.Ordinal);
            var action = safeWalls.Evaluate(facts, out _);
            check(action == (passage ? SchematicAction.Hold : SchematicAction.Lay), $"safe-walls {(passage ? "holds the passage" : "lays the wall")}: {part}");
            if (action == SchematicAction.Lay) laid.Add(part);
        }
        check(new[] { "ItmWall1x1", "ItmWallWindow1x1", "ItmSealTemp01" }.All(laid.Contains), "safe-walls lays the ordinary wall, window wall and blister seal");
        check(laid.Count >= 8, $"safe-walls lays every plain hull wall variant ({laid.Count})");
        foreach (var held in new[] { "ItmDoor01Open", "ItmHatch01Closed", "ItmDockSys03Closed", "ItmCargoPod01", "ItmTowingBrace01" })
            check(safeWalls.Evaluate(WarService.Facts(held), out _) == SchematicAction.Hold, "safe-walls holds " + held);
        foreach (var free in new[] { "ItmFloorGrate01", "ItmConduit01" })
            check(safeWalls.Evaluate(WarService.Facts(free), out _) == safe.Evaluate(WarService.Facts(free), out _) &&
                  safeWalls.Evaluate(WarService.Facts(free), out _) == SchematicAction.Lay, "safe-walls agrees with safe on " + free);

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
