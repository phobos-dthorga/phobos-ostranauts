using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ostranauts.Trading;
using PhobosAgriculture;
using Phobos.Ostranauts.Framework.Registration;

internal static class AgricultureLootChecks
{
    internal static void Run(NativeDefinitions content, Action<bool, string> check, Action<Action, string> throws)
    {
        var parents = new[] { LootContent.FridgeTable, LootContent.CrateTable, "ItmLootSpawnEngineering" };
        var branches = new[] { LootContent.FridgeBranch, LootContent.CrateBranch, "PhobosAgricultureMachinerySalvage" };
        var originals = parents.ToDictionary(id => id, id => DataHandler.dictLoot[id]);
        List<LootUnit> Choices(Loot loot) => ((List<List<LootUnit>>)typeof(Loot)
            .GetField("aCOLootUnits", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(loot)!).Single();
        try
        {
            var foreignTables = new Dictionary<string, Loot>(StringComparer.Ordinal);
            foreach (string parent in parents)
            {
                var foreign = NativeDefinitions.Clone(originals[parent]);
                foreign.aLoots = foreign.aLoots.Concat(new[] { "ForeignAgricultureSupplies=0.1x1" }).ToArray();
                DataHandler.dictLoot[parent] = foreignTables[parent] = foreign;
            }
            var before = parents.ToDictionary(id => id, id => DataHandler.dictLoot[id].aLoots.ToArray());
            var prepared = new NativeDefinitions();
            LootContent.Add(prepared, true, 1);
            check(prepared.Loot.Count == 3 && parents.All(p => !prepared.Loot.ContainsKey(p)),
                "Agriculture adds three contents/engineering branches and republishes no native table by name");
            for (int n = 0; n < parents.Length; n++)
                check(!DataHandler.dictLoot[parents[n]].aLoots.Contains(branches[n] + "=1x1"), "Loot preparation does not mutate live native tables");
            prepared.Publish();
            for (int n = 0; n < parents.Length; n++)
            {
                string parent = parents[n], branch = branches[n];
                var live = DataHandler.dictLoot[parent];
                check(ReferenceEquals(live, foreignTables[parent]), "The game's own table object stays after publication: " + parent);
                check(live.aCOs.SequenceEqual(originals[parent].aCOs) && live.aLoots.SequenceEqual(before[parent].Concat(new[] { branch + "=1x1" })),
                    "Native and foreign contents survive additive Agriculture registration");
                var units = Choices(prepared.Loot[branch]);
                // The machinery roll is the pack's own (0.25 since the reservoirs retired in Agriculture 0.31.0).
                double expected = n == 0 ? .22 : n == 1 ? .30 : PhobosAgriculture.AgricultureEconomy.Pack.worldLoot[2].chance;
                check(units.All(u => u.fMin == 1 && u.fMax == 1) && Math.Abs(units.Sum(u => u.fChance) - expected) < 1e-7, "Native parser sees one bounded Agriculture item choice per contents roll");
                foreach (var unit in units)
                {
                    check(content.Objects.TryGetValue(unit.strName, out var co) && (n == 2 ? !co.aStartingConds.Any(c => c.StartsWith("IsInstalled=", StringComparison.Ordinal)) : co.inventoryWidth == 1 && co.inventoryHeight == 1), "Container loot fits one slot; engineering machinery is loose, never installed");
                    check(!new[] { Service.CharacterizedDrainage, Service.RecoveryReject, Definitions.Drainage, Definitions.Residue }.Contains(unit.strName), "Loot cannot manufacture measured process records or spent waste");
                    if (n == 1) check(DataHandler.dictCTs["TIsFitCrate"].TriggeredDataCO(new DataCO(content.Objects[unit.strName]), false), "Supply satisfies the real native locked-crate filter");
                }
            }
            var linked = parents.ToDictionary(id => id, id => DataHandler.dictLoot[id].aLoots.ToArray());
            var repeated = new NativeDefinitions();
            LootContent.Add(repeated, true, 1); repeated.Publish();
            for (int n = 0; n < parents.Length; n++)
                check(DataHandler.dictLoot[parents[n]].aLoots.SequenceEqual(linked[parents[n]]), "Repeated registration cannot multiply Agriculture loot branches");
            foreach (var setting in new[] { (false, 1d), (true, 0d) })
            {
                var off = new NativeDefinitions(); LootContent.Add(off, setting.Item1, setting.Item2); off.Publish();
                for (int n = 0; n < parents.Length; n++)
                {
                    var live = DataHandler.dictLoot[parents[n]];
                    check(!live.aLoots.Contains(branches[n] + "=1x1") && off.Loot[branches[n]].aCOs.Length == 0, "Disabled/zero loot removes its previous future-roll branch");
                    check(live.aLoots.Contains("ForeignAgricultureSupplies=0.1x1") && live.aLoots.SequenceEqual(before[parents[n]]), "Disabled loot preserves other providers");
                }
            }
            var maximum = new NativeDefinitions(); LootContent.Add(maximum, true, LootContent.MaximumMultiplier);
            foreach (string branch in branches)
                check(Choices(maximum.Loot[branch]).Sum(u => u.fChance) <= 1, "Maximum multiplier still leaves a bounded mutually exclusive choice");
            foreach (double invalid in new[] { -.01, 3.01, double.NaN, double.PositiveInfinity })
                throws(() => LootContent.Add(new NativeDefinitions(), true, invalid), "Invalid loot multiplier is rejected before touching definitions");
        }
        finally { foreach (var pair in originals) DataHandler.dictLoot[pair.Key] = pair.Value; }
    }
}
