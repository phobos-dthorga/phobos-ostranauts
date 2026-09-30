using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Phobos.Ostranauts.Framework.Registration;

/// <summary>Carved loot shares against the game's own tables: preparation leaves live tables alone,
/// publication keeps every other unit's cumulative band and the table total, the asteroid-field ship
/// picker accepts a carved cluster, republishing is idempotent, a zero share restores the native table,
/// and a table another mod rewrote afterwards is left alone.</summary>
internal static class LootCarveNativeChecks
{
    private static List<List<LootUnit>> Parsed(Loot loot) => (List<List<LootUnit>>)typeof(Loot)
        .GetField("aCOLootUnits", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(loot)!;

    // Every positive unit's cumulative band, in roll order, from the game's own parse.
    private static List<(string Name, double Start, double End)> Bands(Loot loot)
    {
        var bands = new List<(string, double, double)>();
        foreach (var expression in Parsed(loot))
        {
            double at = 0;
            foreach (var unit in expression) { bands.Add((unit.strName, at, at + unit.fChance)); at += unit.fChance; }
        }
        return bands;
    }

    internal static void Run(Action<bool, string> check, Action<Action, string> throws)
    {
        const string item = "ItmRock05SalvageOutput", donor = "ItmMineralStone01", choice = "ItmMineral02";
        const string ship = "RandomAsteroidC", cluster = "ClusterI01", field = "ClusterC02";
        var itemTable = DataHandler.dictLoot[item]; var shipTable = DataHandler.dictLoot[ship];
        string[] itemOriginal = itemTable.aCOs.ToArray(), shipOriginal = shipTable.aCOs.ToArray();
        var itemBands = Bands(itemTable); var shipBands = Bands(shipTable);
        try
        {
            var d = new NativeDefinitions();
            AdditiveLoot.CarveChoice(d, item, donor, choice, 0.1);
            AdditiveLoot.CarveChoice(d, ship, field, cluster, 0.05);
            check(itemTable.aCOs.SequenceEqual(itemOriginal) && shipTable.aCOs.SequenceEqual(shipOriginal), "Preparing a carve leaves live tables untouched until publication");
            d.Publish();
            foreach (var (table, before, carved, taken) in new[] { (itemTable, itemBands, donor, choice), (shipTable, shipBands, field, cluster) })
            {
                var after = Bands(table);
                foreach (var b in before.Where(b => b.Name != carved))
                {
                    var a = after.Single(x => x.Name == b.Name);
                    check(Math.Abs(a.Start - b.Start) < 1e-6 && Math.Abs(a.End - b.End) < 1e-6, $"{table.strName}: {b.Name} keeps its band");
                }
                check(Math.Abs(after.Max(b => b.End) - before.Max(b => b.End)) < 1e-6, table.strName + ": the total probability is unchanged");
                int donorAt = after.FindIndex(b => b.Name == carved);
                check(donorAt >= 0 && after[donorAt + 1].Name == taken, table.strName + ": the carved choice sits right after its donor");
            }
            check(shipTable.aCOs.Single().Contains("ClusterC02=0.15x1|ClusterI01=0.05x1"), "The asteroid-field picker carries a carved cluster in the game's own format");
            check(Parsed(shipTable).Single().Any(u => u.strName == cluster && Math.Abs(u.fChance - 0.05) < 1e-6), "The game parses the carved cluster as a positive choice");

            string[] once = itemTable.aCOs.ToArray();
            d.Publish();
            check(itemTable.aCOs.SequenceEqual(once), "Republishing the same carve changes nothing");

            // A foreign rewrite after our carve is reported and preserved, never overwritten.
            string[] foreign = { "ItmMiningTrash=1x1" };
            itemTable.aCOs = foreign;
            var later = new NativeDefinitions();
            AdditiveLoot.CarveChoice(later, item, "ItmMineral04", choice, 0.05);
            later.Publish();
            check(itemTable.aCOs.SequenceEqual(foreign), "A table another mod rewrote after carving is left as that mod wrote it");
            itemTable.aCOs = once;

            // Invalid carves fail at preparation.
            throws(() => AdditiveLoot.CarveChoice(new NativeDefinitions(), item, donor, donor, 0.1), "A donor cannot carve itself");
            throws(() => AdditiveLoot.CarveChoice(new NativeDefinitions(), item, donor, choice, 1.5), "A share above one is refused");
            throws(() => AdditiveLoot.CarveChoice(new NativeDefinitions(), item, "ItmNothing", choice, 0.1), "A missing donor is refused at preparation");
            throws(() => AdditiveLoot.CarveChoice(new NativeDefinitions(), item, donor, choice, 0.9), "A share larger than the donor is refused at preparation");
            throws(() => AdditiveLoot.CarveChoice(new NativeDefinitions(), ship, field, "PhobosNoSuchCluster", 0.05), "A ship table only takes a known ship or asteroid cluster");
            throws(() => AdditiveLoot.CarveChoice(new NativeDefinitions(), "PhobosNoSuchTable", donor, choice, 0.1), "A missing table is refused");

            // A zero share restores the native tables exactly.
            var restore = new NativeDefinitions();
            AdditiveLoot.CarveChoice(restore, item, donor, choice, 0);
            AdditiveLoot.CarveChoice(restore, ship, field, cluster, 0);
            restore.Publish();
            check(itemTable.aCOs.SequenceEqual(itemOriginal) && shipTable.aCOs.SequenceEqual(shipOriginal), "A zero share restores each native table exactly");
        }
        finally
        {
            itemTable.aCOs = itemOriginal; shipTable.aCOs = shipOriginal;
            // Forget these test tables only; the published mods' carves (the clay chunk) stay registered.
            var tables = (System.Collections.IDictionary)typeof(LootCarveRegistry).GetField("tables", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            tables.Remove(item); tables.Remove(ship);
        }
    }
}
