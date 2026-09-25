using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Phobos.Ostranauts.Framework.Registration;
using PhobosAutoNav.Core;

internal static class SalvageChecks
{
    internal static void Run(Action<bool, string> check, Action<Action,string> throws)
    {
        List<LootUnit> ParsedChoice(Loot loot) => ((List<List<LootUnit>>)typeof(Loot)
            .GetField("aCOLootUnits", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(loot)!).Single();
        string parent = "ItmNavStationModsRandomAll", branch = "PhobosAutoNavSalvage_" + parent;
        var original = DataHandler.dictLoot[parent];
        string[] originalItems = original.aCOs.ToArray();
        original.aLoots = original.aLoots.Concat(new[] { "ForeignNavSalvage=0.5x1" }).ToArray();
        var definitions = PhobosAutoNav.EquipmentContent.Prepare();
        check(ReferenceEquals(original, DataHandler.dictLoot[parent]) && original.aCOs.SequenceEqual(originalItems),
            "Preparing loot additions leaves live definitions untouched until publication");
        definitions.Publish();
        foreach (string table in EquipmentRules.SalvageTables)
        {
            string ownBranch = "PhobosAutoNavSalvage_" + table;
            check(DataHandler.dictLoot[table].aLoots.Count(s => s == ownBranch + "=1x1") == 1,
                "Exactly one additive branch in native navigation leaf table: " + table);
            var choice = definitions.Loot[ownBranch];
            var units = ParsedChoice(choice);
            check(units.All(unit => unit.fMin == 1 && unit.fMax == 1) &&
                Math.Abs(units.Sum(unit => unit.fChance) - EquipmentRules.SalvageChance) < 1e-7,
                "Actual native parser sees a bounded single choice with the authored total chance");
            check(units.All(unit => unit.strName == "PhobosNavModAutoNav" || unit.strName == "PhobosNavModAutoNavDmg"),
                "Native loot targets saved module identities");
            if (table.EndsWith("Dmg", StringComparison.Ordinal))
                check(units.Count == 1 && units[0].strName == "PhobosNavModAutoNavDmg", "Damaged pools cannot create pristine modules");
        }
        check(DataHandler.dictLoot[parent].aCOs.SequenceEqual(originalItems) &&
            DataHandler.dictLoot[parent].aLoots.Contains("ForeignNavSalvage=0.5x1"), "Native and foreign choices survive publication");
        string[] previous = DataHandler.dictLoot[parent].aLoots.ToArray();
        PhobosAutoNav.EquipmentContent.Prepare().Publish();
        check(DataHandler.dictLoot[parent].aLoots.SequenceEqual(previous), "Repeated salvage registration is idempotent");
        foreach (string table in new[] { "ItmNavStationModsRandomTorch", "ItmNavStationModsRandomAtmoCombat" })
            check(!DataHandler.dictLoot[table].aLoots.Any(s => s.StartsWith("PhobosAutoNavSalvage_", StringComparison.Ordinal)),
                "Random parent pools never receive a second nested bonus roll");
        PhobosAutoNav.EquipmentContent.Prepare(false).Publish();
        check(EquipmentRules.SalvageTables.All(table => !DataHandler.dictLoot[table].aLoots.Any(s =>
            s.StartsWith("PhobosAutoNavSalvage_", StringComparison.Ordinal))), "Disabling salvage removes only its own future-roll branches");
        check(DataHandler.dictLoot[parent].aCOs.SequenceEqual(originalItems) &&
            DataHandler.dictLoot[parent].aLoots.Contains("ForeignNavSalvage=0.5x1"), "Disabling retains native and foreign loot");
        PhobosAutoNav.EquipmentContent.Prepare(true, 0).Publish();
        check(DataHandler.dictLoot[branch].aCOs.Length == 0, "Zero chance produces no native zero-probability entry");
        PhobosAutoNav.EquipmentContent.Prepare(true, 1).Publish();
        check(Math.Abs(ParsedChoice(DataHandler.dictLoot[branch]).Sum(unit => unit.fChance) - 1) < 1e-7,
            "Maximum configured chance still yields a single mutually exclusive module");
        throws(() => AdditiveLoot.SetItemChoice(new NativeDefinitions(), parent, "ForeignBranch",
            new Dictionary<string,double> { ["PhobosNavModAutoNav"] = .1 }), "Foreign branch ownership is rejected");
        foreach (double bad in new[] { -.1, double.NaN, double.PositiveInfinity, 1.1 })
            throws(() => AdditiveLoot.SetItemChoice(new NativeDefinitions(), parent, "PhobosInvalid",
                new Dictionary<string,double> { ["PhobosNavModAutoNav"] = bad }), "Invalid probability rejected");
        throws(() => AdditiveLoot.SetItemChoice(new NativeDefinitions(), parent, "PhobosInvalid",
            new Dictionary<string,double> { ["PhobosNavModAutoNav"] = .8, ["PhobosNavModAutoNavDmg"] = .8 }), "Choice total cannot exceed one");
        throws(() => AdditiveLoot.SetItemChoice(new NativeDefinitions(), "PhobosParent", "PhobosParent",
            new Dictionary<string,double> { ["PhobosNavModAutoNav"] = .1 }), "A branch cannot replace its parent table");
        var wrongType = new NativeDefinitions();
        wrongType.Loot["PhobosInteraction"] = new Loot { strName = "PhobosInteraction", strType = "interaction" };
        throws(() => AdditiveLoot.SetItemChoice(wrongType, "PhobosInteraction", "PhobosChoice",
            new Dictionary<string,double> { ["PhobosNavModAutoNav"] = .1 }), "Staged non-item tables are protected");
        PhobosAutoNav.EquipmentContent.Prepare().Publish();
    }
}
