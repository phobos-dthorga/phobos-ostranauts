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
        var parents = new[] { LootContent.FridgeTable, LootContent.CrateTable };
        var branches = new[] { LootContent.FridgeBranch, LootContent.CrateBranch };
        var originals = parents.ToDictionary(id => id, id => DataHandler.dictLoot[id]);
        List<LootUnit> Choices(Loot loot) => ((List<List<LootUnit>>)typeof(Loot)
            .GetField("aCOLootUnits", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(loot)!).Single();
        try
        {
            foreach (string parent in parents)
            {
                var foreign = NativeDefinitions.Clone(originals[parent]);
                foreign.aLoots = foreign.aLoots.Concat(new[] { "ForeignAgricultureSupplies=0.1x1" }).ToArray();
                DataHandler.dictLoot[parent] = foreign;
            }
            var prepared = new NativeDefinitions();
            LootContent.Add(prepared, true, 1);
            check(prepared.Loot.Count == 4, "Agriculture adds only two contents branches, not repeated parent/leaf bonuses");
            for (int n = 0; n < parents.Length; n++)
            {
                string parent = parents[n], branch = branches[n];
                var live = DataHandler.dictLoot[parent];
                var changed = prepared.Loot[parent];
                check(!live.aLoots.Contains(branch + "=1x1"), "Loot preparation does not mutate live native tables");
                check(changed.aCOs.SequenceEqual(live.aCOs) && changed.aLoots.SequenceEqual(live.aLoots.Concat(new[] { branch + "=1x1" })), "Native and foreign contents survive additive Agriculture registration");
                var units = Choices(prepared.Loot[branch]);
                check(units.All(u => u.fMin == 1 && u.fMax == 1) && Math.Abs(units.Sum(u => u.fChance) - (n == 0 ? .22 : .30)) < 1e-7, "Native parser sees one bounded Agriculture item choice per contents roll");
                foreach (var unit in units)
                {
                    check(content.Objects.TryGetValue(unit.strName, out var co) && co.inventoryWidth == 1 && co.inventoryHeight == 1, "Loot uses defined single-slot portable supplies, never oversized machinery");
                    check(!new[] { Service.CharacterizedDrainage, Service.RecoveryReject, Definitions.Drainage, Definitions.Residue }.Contains(unit.strName), "Loot cannot manufacture measured process records or spent waste");
                    if (n == 1) check(DataHandler.dictCTs["TIsFitCrate"].TriggeredDataCO(new DataCO(content.Objects[unit.strName]), false), "Supply satisfies the real native locked-crate filter");
                }
                DataHandler.dictLoot[parent] = changed;
            }
            var repeated = new NativeDefinitions();
            LootContent.Add(repeated, true, 1);
            for (int n = 0; n < parents.Length; n++)
                check(repeated.Loot[parents[n]].aLoots.SequenceEqual(prepared.Loot[parents[n]].aLoots), "Repeated registration cannot multiply Agriculture loot branches");
            foreach (var setting in new[] { (false, 1d), (true, 0d) })
            {
                var off = new NativeDefinitions(); LootContent.Add(off, setting.Item1, setting.Item2);
                for (int n = 0; n < parents.Length; n++)
                {
                    check(!off.Loot[parents[n]].aLoots.Contains(branches[n] + "=1x1") && off.Loot[branches[n]].aCOs.Length == 0, "Disabled/zero loot removes its previous future-roll branch");
                    check(off.Loot[parents[n]].aLoots.Contains("ForeignAgricultureSupplies=0.1x1"), "Disabled loot preserves other providers");
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
