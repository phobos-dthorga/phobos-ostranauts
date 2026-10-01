using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Inventory;
using Phobos.Ostranauts.Framework.Processing;
using Phobos.Ostranauts.Framework.Registration;
using PhobosShipbreaker;
using PhobosShipbreaker.Core;

/// <summary>Framework 0.71.0 stacked delivery against the real recipes: every product tray holds two batches of each of
/// its machine's recipes when the products are delivered into stacks at the game's own stack limits and cell sizes
/// (owner direction, 1 October 2026: a tray holds about one to two batches of the largest recipe). A new recipe that
/// outgrows its tray fails here, with the cells it needs.</summary>
internal static class TrayFitNativeChecks
{
    internal static void Run(IEnumerable<NativeDefinitions> definitions, Action<bool, string> check)
    {
        var sets = definitions.ToArray();
        JsonCondOwner Definition(string id) => sets.Select(d => d.Objects.TryGetValue(id, out var co) ? co : null).FirstOrDefault(c => c != null) ?? DataHandler.dictCOs[id];
        JsonItemDef Item(string id) => sets.Select(d => d.Items.TryGetValue(id, out var item) ? item : null).FirstOrDefault(i => i != null) ?? DataHandler.dictItemDefs[id];
        StackItem[] Batch(IEnumerable<ProductSpec> products) => products.SelectMany(p =>
        {
            var co = Definition(p.Id); var item = Item(co.strItemDef);
            int width = co.inventoryWidth != 0 ? co.inventoryWidth : item.nCols, height = co.inventoryHeight != 0 ? co.inventoryHeight : item.aSocketAdds.Length / item.nCols;
            return Enumerable.Repeat(new StackItem(co.nStackLimit > 1 ? p.Id : null, new ItemSize(width, height), co.nStackLimit), p.Count);
        }).ToArray();
        // Delivers a batch into a model tray the way TrayDelivery does; false when it finds no room.
        bool Deliver(bool[,] cells, List<(string Kind, int Count, int Limit)> stacks, StackItem[] batch)
        {
            var plan = BatchPlacement.PlanStacked(cells, stacks.Select(s => new StackRoom(s.Kind, s.Limit - s.Count)).ToArray(), batch);
            if (plan == null) return false;
            for (int i = 0; i < batch.Length; i++) if (plan.Existing[i] >= 0) { var s = stacks[plan.Existing[i]]; stacks[plan.Existing[i]] = (s.Kind, s.Count + 1, s.Limit); }
            for (int n = 0; n < plan.NewStacks; n++)
            {
                var first = batch[Array.IndexOf(plan.New, n)];
                for (int x = 0; x < first.Size.Width; x++) for (int y = 0; y < first.Size.Height; y++) cells[plan.Positions[n].X + x, plan.Positions[n].Y + y] = true;
                if (first.Stacks) stacks.Add((first.Kind!, plan.New.Count(v => v == n), first.Limit));
            }
            return true;
        }
        void Holds(string machine, string recipe, IEnumerable<ProductSpec> products, int batches)
        {
            var spec = EquipmentInventory.Of(machine + "Installed");
            check(spec != null && spec.Value.Role == InventoryRole.ProductTray, "The machine has a declared product tray: " + machine);
            if (spec == null) return;
            var cells = new bool[spec.Value.Width, spec.Value.Height]; var stacks = new List<(string, int, int)>(); var batch = Batch(products);
            bool fits = true;
            for (int n = 0; n < batches && fits; n++) fits = Deliver(cells, stacks, batch);
            check(fits, $"{machine} tray ({spec.Value.Width} x {spec.Value.Height}) holds {batches} batch(es) of {recipe}: {batch.Length} products each");
        }

        // The game's own calls that stacked delivery and the load-time fit are built from still have the shape they were
        // read with (game 1.0.1.5). Their behaviour needs the running game; these only catch a renamed or changed member.
        const System.Reflection.BindingFlags any = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;
        var stackFromList = typeof(CondOwner).GetMethod("StackFromList", any);
        check(stackFromList != null && stackFromList.IsStatic && stackFromList.ReturnType == typeof(CondOwner) &&
              stackFromList.GetParameters().Select(p => p.ParameterType).SequenceEqual(new[] { typeof(List<CondOwner>) }), "The game builds a stack from a list of units");
        check(typeof(CondOwner).GetProperty("StackAsList", any)?.PropertyType == typeof(List<CondOwner>) && typeof(CondOwner).GetProperty("StackCount", any)?.PropertyType == typeof(int) &&
              typeof(CondOwner).GetField("aStack", any) != null && typeof(CondOwner).GetField("coStackHead", any)?.FieldType == typeof(CondOwner), "A stack lists its units with its head last and counts them");
        check(typeof(Container).GetMethod("AddCOSimple", any)?.GetParameters().Select(p => p.ParameterType).SequenceEqual(new[] { typeof(CondOwner), typeof(PairXY) }) == true &&
              typeof(Container).GetField("bAllowStacking", any)?.FieldType == typeof(bool) && typeof(Container).GetField("gridLayout", any) != null, "A container adds a stack head at a cell");
        check(typeof(CondOwner).GetMethod("RemoveFromCurrentHome", any)?.GetParameters().Select(p => p.ParameterType).SequenceEqual(new[] { typeof(bool) }) == true &&
              typeof(CondOwner).GetMethod("PopHeadFromStack", any)?.ReturnType == typeof(CondOwner), "A unit or a whole stack leaves its container through the game's own removal");
        check(typeof(Ship).GetMethods(any).Any(m => m.Name == "DropCO" && m.GetParameters().Select(p => p.ParameterType).SequenceEqual(new[] { typeof(CondOwner), typeof(UnityEngine.Vector2) })),
            "The game drops an item on the deck near a point");

        // D4: every accepted wall mass, and both ends of every other feed family. Four standard walls are a full feed.
        for (double kg = ProcessRules.MinimumWallKg; kg <= ProcessRules.MaximumWallKg; kg++)
            Holds(Content.Prefix, "a " + kg + " kg wall", ProcessRecipes.ForWallMass(kg).Current.Products, 2);
        Holds(Content.Prefix, "a full four-panel feed of 24 kg walls", ProcessRecipes.ForWallMass(24).Current.Products, ProcessRules.FeedCapacity);
        foreach (var family in FeedFamilies.All.Where(f => f.Key != "wall"))
            foreach (double kg in new[] { family.MinKg, family.MaxKg })
                Holds(Content.Prefix, family.Key + " " + kg + " kg", family.Catalog(kg).Current.Products, 2);
        // R4: a full four-packet feed.
        Holds(ReclaimerRules.Prefix, "a full four-packet feed", ReclaimerRules.Recipes.Current.Products, ReclaimerRules.FeedCapacity);
        // F6: two releases of each recipe.
        foreach (var recipe in FurnaceRecipes.All) Holds(FurnaceRules.Prefix, recipe.Id, recipe.Products, 2);
        // V4, LC-3, SA-3: two charges of each recipe, and of what a frozen melt leaves.
        foreach (var machine in PhobosManufacturing.ChargeMachines.All)
            foreach (var recipe in PhobosManufacturing.Core.ChargeCatalog.For(machine.Spec.MachineKey).All)
            {
                // The evaporite leach leaves two 2 x 2 residues and two salts, ten cells: the LC-3's twelve hold one such
                // charge (and two of every other recipe), which the sizing rule allows for a machine's largest recipe.
                Holds(machine.Spec.Prefix, recipe.Id, recipe.Solids(recipe.Products), recipe.Id == "evaporite-leach" ? 1 : 2);
                if (machine.Spec.SpoiledProducts == null) continue;
                IReadOnlyList<ProductSpec>? slag = null;
                try { slag = machine.Spec.SpoiledProducts(recipe); } catch (ArgumentException) { /* only a melt can spoil */ }
                if (slag != null) Holds(machine.Spec.Prefix, recipe.Id + " (spoiled)", recipe.Solids(slag), 2);
            }
    }
}
