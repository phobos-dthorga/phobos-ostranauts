using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PhobosWarDeclared.Core;

int checks = 0;
void Check(bool condition, string message) { if (!condition) throw new Exception(message); checks++; }
void Throws(Action action, string message) { bool failed = false; try { action(); } catch { failed = true; } Check(failed, message); }

const double Quiet = 300;

// ---- Combat window -------------------------------------------------------------------------------
{
    var w = new CombatWindow();
    Check(w.Observe(0, false, null, Quiet) == WindowChange.None && !w.Open, "a quiet ship stays at ease");
    Check(w.Observe(10, true, null, Quiet) == WindowChange.Opened && w.Open && !w.Manual, "a hostile lock opens the window");
    Check(w.Observe(200, false, null, Quiet) == WindowChange.None && w.Open, "the window holds inside the quiet period");
    Check(w.Observe(309, false, null, Quiet) == WindowChange.None, "quiet counts from the last activity, not the opening");
    Check(w.Observe(310, false, null, Quiet) == WindowChange.Closed && !w.Open, "the window closes after the quiet period");
    Check(w.Observe(320, false, 100, Quiet) == WindowChange.None, "damage from before the stand-down does not reopen it");
    Check(w.Observe(400, false, 395, Quiet) == WindowChange.Opened, "fresh damage reopens it");
    Check(w.Observe(694, false, 395, Quiet) == WindowChange.None && w.Observe(695, false, 395, Quiet) == WindowChange.Closed, "damage keeps the window open for one quiet period");
    Throws(() => w.Observe(700, false, null, 0), "a zero quiet period is refused");
}
{
    var w = new CombatWindow();
    Check(w.Declare(5) == WindowChange.Opened && w.Manual, "Battle stations opens a manual window");
    Check(w.Declare(6) == WindowChange.None, "a second order changes nothing");
    Check(w.Observe(5000, false, null, Quiet) == WindowChange.None && w.Open, "a manual window never times out");
    Check(w.Stand(5001) == WindowChange.Closed && !w.Open && !w.Manual, "Stand down closes it");
    Check(w.Stand(5002) == WindowChange.None, "standing down twice changes nothing");
    Check(w.Observe(5003, true, null, Quiet) == WindowChange.None, "a fight still listed by the game does not reopen after Stand down");
    Check(w.Observe(5004, false, null, Quiet) == WindowChange.None && !w.SuppressOngoing, "the listing clearing lifts the suppression");
    Check(w.Observe(5005, true, null, Quiet) == WindowChange.Opened, "a new lock after that reopens the window");
    Check(w.Stand(5006) == WindowChange.Closed && w.Observe(5007, true, 5006.5, Quiet) == WindowChange.Opened, "fresh damage reopens despite a lingering lock");
}
{
    var w = new CombatWindow();
    w.Declare(1); w.Stand(2);
    Check(w.Declare(3) == WindowChange.Opened && !w.SuppressOngoing, "Battle stations clears a previous stand-down suppression");
}

// ---- Schematics ----------------------------------------------------------------------------------
string repo = FindRepo();
var shipped = Directory.GetFiles(Path.Combine(repo, "mods", "PhobosWarDeclared", "schematics"), "*.json")
    .ToDictionary(p => Schematic.KeyFromFileName(Path.GetFileName(p))!, p => Schematic.Parse(Schematic.KeyFromFileName(Path.GetFileName(p))!, File.ReadAllText(p)));
Check(shipped.ContainsKey(WarRules.DefaultSchematic) && shipped.ContainsKey("everything") && shipped.ContainsKey("hull-only"), "the three shipped schematics parse");

PartFacts Part(string id, PartFootprint footprint, string? menu = "HULL", params string[] conditions) => new(id, conditions, menu, footprint);
var floor = Part("ItmFloorGrate01", PartFootprint.Walkable, "HULL", "IsFloor", "IsFloorGrate");
var wall = Part("ItmWall1x1", PartFootprint.Blocks, "HULL", "IsWall", "IsObstruction");
var door = Part("ItmDoor01Closed", PartFootprint.Blocks, "HULL", "IsWall", "IsPortal");
var reactor = Part("ItmFusionReactor", PartFootprint.Blocks, "POWR", "IsObstruction");
var conduit = Part("ItmConduitPower", PartFootprint.Walkable, "POWR");
var mystery = Part("ItmMystery", PartFootprint.Unknown, null);

var safe = shipped["safe"];
Check(safe.Evaluate(floor, out _) == SchematicAction.Lay && safe.Evaluate(conduit, out _) == SchematicAction.Lay, "safe lays walk-over parts");
Check(safe.Evaluate(wall, out _) == SchematicAction.Hold && safe.Evaluate(door, out _) == SchematicAction.Hold && safe.Evaluate(reactor, out _) == SchematicAction.Hold, "safe holds blocking parts");
Check(safe.Evaluate(mystery, out var none) == SchematicAction.Hold && none == null, "safe holds a part it cannot classify, by default");
var everything = shipped["everything"];
Check(new[] { floor, wall, door, reactor, conduit, mystery }.All(p => everything.Evaluate(p, out _) == SchematicAction.Lay), "everything lays every part");
var hull = shipped["hull-only"];
Check(hull.Evaluate(floor, out _) == SchematicAction.Lay && hull.Evaluate(wall, out _) == SchematicAction.Hold &&
      hull.Evaluate(reactor, out _) == SchematicAction.Ignore && hull.Evaluate(conduit, out _) == SchematicAction.Ignore, "hull-only lays floors, holds hull and ignores the rest");

var custom = Schematic.Parse("mine", @"{ ""title"": ""Mine"", ""default"": ""lay"", ""rules"": [
    { ""action"": ""ignore"", ""parts"": [ ""ItmFusion*"" ] },
    { ""action"": ""hold"", ""conditions"": [ ""IsPortal"" ], ""footprint"": ""blocks"" },
    { ""action"": ""hold"", ""menus"": [ ""powr"" ] } ] }");
Check(custom.Evaluate(reactor, out var first) == SchematicAction.Ignore && first == custom.Rules[0], "the first matching rule wins");
Check(custom.Evaluate(door, out _) == SchematicAction.Hold && custom.Evaluate(wall, out _) == SchematicAction.Lay, "all stated fields must match");
Check(custom.Evaluate(conduit, out _) == SchematicAction.Hold, "menu names match without regard to case");
Check(custom.Title == "Mine" && custom.Key == "mine" && Schematic.Parse("bare", "{}").Default == SchematicAction.Hold, "missing default means hold");

Throws(() => Schematic.Parse("bad", "{ not json"), "broken JSON is refused");
Throws(() => Schematic.Parse("bad", @"{ ""defualt"": ""lay"" }"), "a misspelt field is refused rather than ignored");
Throws(() => Schematic.Parse("bad", @"{ ""rules"": [ { ""parts"": [ ""X"" ] } ] }"), "a rule without an action is refused");
Throws(() => Schematic.Parse("bad", @"{ ""rules"": [ { ""action"": ""build"" } ] }"), "an unknown action is refused");
Throws(() => Schematic.Parse("bad", @"{ ""rules"": [ { ""action"": ""lay"", ""footprint"": ""open"" } ] }"), "an unknown footprint is refused");
Throws(() => Schematic.Parse("bad", @"{ ""rules"": [ { ""action"": ""lay"", ""parts"": [] } ] }"), "an empty list is refused");
Throws(() => Schematic.Parse("Bad Name", "{}"), "keys are lower-case identifiers");
Check(Schematic.KeyFromFileName("My-Ship_2.JSON") == "my-ship_2" && Schematic.KeyFromFileName("notes.txt") == null && Schematic.KeyFromFileName("with space.json") == null, "file names map to keys");

// ---- Ledger --------------------------------------------------------------------------------------
{
    var ledger = new WarLedger();
    var a = new LedgerEntry("co-1", "ItmWall1x1", 1.5, -2.5, 90);
    var b = new LedgerEntry("co-2", "ItmFloorGrate01", 1.5, -2.5, 0);
    var c = new LedgerEntry("co-3", "ItmConduitPower", 3, 4, 180);
    Check(ledger.Record(a) && ledger.Record(b) && ledger.Record(c), "parts are recorded");
    Check(!ledger.Record(new LedgerEntry("co-1", "ItmWall1x1", 9, 9, 0)), "one ID is recorded once, across its damage stages");
    int Rank(LedgerEntry e) => e.Part.Contains("Floor") ? 0 : e.Part.Contains("Wall") ? 1 : 2;
    Check(ledger.LayOrder(EntryState.Pending, Rank).Select(e => e.Id).SequenceEqual(new[] { "co-2", "co-1", "co-3" }), "floors, then walls, then the rest, oldest first");

    Check(ledger.Apply(c, LayResult.PlayerBusy) == LayDisposition.Retry && c.State == EntryState.Pending && c.Attempts == 0, "a busy player means try again later, not a failure");
    Check(ledger.Apply(c, LayResult.Failed) == LayDisposition.Retry && ledger.Apply(c, LayResult.Failed) == LayDisposition.Retry &&
          ledger.Apply(c, LayResult.Failed) == LayDisposition.Held && c.State == EntryState.Held, "repeated unexpected failures hold the part for the player");
    Check(ledger.Apply(a, LayResult.DoesNotFit) == LayDisposition.Held && a.Reason == "DoesNotFit", "a part that does not fit is held");
    Check(ledger.Apply(b, LayResult.Laid) == LayDisposition.Laid && !ledger.Entries.Contains(b), "a laid part leaves the ledger");
    Check(ledger.Count(EntryState.Held) == 2 && ledger.Count(EntryState.Pending) == 0, "counts by state");

    ledger.Window.Declare(50); ledger.LayDue = true;
    var fields = ledger.Save();
    Check(fields.All(p => p.Value.Length <= 512 && !p.Value.Any(ch => ch == '=' || ch == ',' || char.IsControl(ch))), "saved values fit the Framework state store");
    var back = WarLedger.Load(fields);
    Check(back.Window.Open && back.Window.Manual && back.Window.OpenedAt == 50 && back.LayDue, "the window round-trips");
    Check(back.Entries.Count == 2 && back.Entries[0].Id == "co-1" && back.Entries[0].X == 1.5 && back.Entries[0].Y == -2.5 && back.Entries[0].Rotation == 90 &&
          back.Entries[0].State == EntryState.Held && back.Entries[1].Attempts == 3, "entries round-trip exactly");

    var broken = new Dictionary<string, string>(fields) { ["entry.0"] = "co-1|ItmWall1x1|oops|0|0|held|0|-" };
    Throws(() => WarLedger.Load(broken), "a malformed saved entry is refused so the record can be kept intact");
    var missing = new Dictionary<string, string>(fields); missing.Remove("window.open");
    Throws(() => WarLedger.Load(missing), "a missing field is refused");
    var repeated = new Dictionary<string, string>(fields) { ["entry.1"] = fields["entry.0"] };
    Throws(() => WarLedger.Load(repeated), "a repeated part ID is refused");
    Throws(() => new LedgerEntry("co|4", "ItmWall1x1", 0, 0, 0), "IDs may not contain the field separator");
    Throws(() => new LedgerEntry("co-4", "ItmWall1x1", double.NaN, 0, 0), "positions must be finite");

    var fresh = new WarLedger();
    Check(fresh.Empty, "a new ledger has nothing to save");
    for (int i = 0; i < WarRules.MaximumEntries; i++) fresh.Record(new LedgerEntry("id-" + i, "ItmWall1x1", i, 0, 0));
    Check(!fresh.Record(new LedgerEntry("over", "ItmWall1x1", 0, 0, 0)) && fresh.Overflow == 1, "a full ledger counts overflow instead of growing");
    Check(WarLedger.Load(fresh.Save()).Entries.Count == WarRules.MaximumEntries, "a full ledger still round-trips");
}

Console.WriteLine($"PASS: {checks} battle-window, schematic and ledger checks on data alone. No game session was run.");

static string FindRepo()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir != null && !File.Exists(Path.Combine(dir.FullName, "AGENTS.md"))) dir = dir.Parent;
    return dir?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
}
