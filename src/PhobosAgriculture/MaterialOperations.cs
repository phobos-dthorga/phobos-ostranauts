using System;
using System.Collections.Generic;
using System.Linq;
using PhobosAgriculture.Core;
using Phobos.Ostranauts.Framework.Inventory;
using Ostranauts.Inventory;

namespace PhobosAgriculture;

internal static partial class Service
{
    internal static CondOwner? Input(CondOwner co, string id, double kg) => co.objContainer?.ContainedCOs.FirstOrDefault(x =>
        x.strCODef == id && !x.bDestroyed && x.coStackHead == null && x.aStack.Count == 0 && !x.HasCond("IsInstalled") && x.GetCOsSafe(true).Count == 0 && x.GetLotCOs(true).Count == 0 && Math.Abs(x.GetTotalMass() - kg) < 1e-7);
    internal static bool Work(CondOwner co, CondOwner actor, string action)
    {
        var s = Get(co);
        if (s.Protected || Access(co, null, actor) != null || !co.HasCond("IsInstalled") || co.HasCond("IsDamaged") || !Definitions.Ready) return false;
        try
        {
            if (action == "harvest" || action == "clear") return Harvest(s, action == "clear");
            if (action == "drain")
            {
                double kg = s.State.Water + s.State.Nutrients;
                if (kg <= 0) return false;
                var drained = s.State.Copy(); drained.Water = drained.Nutrients = 0; drained.Receiving = false;
                return Deliver(s, new List<(string, double)> { (Definitions.Drainage, kg) }, null, drained);
            }
            var next = s.State.Copy(); CondOwner? input;
            if (action == "plant-potato" || action == "plant-lettuce")
            {
                var crop = action == "plant-potato" ? Crop.Potato : Crop.Lettuce;
                if (next.CropId.Length != 0) return false;
                input = Input(co, crop == Crop.Potato ? Definitions.PotatoSeed : Definitions.LettuceSeed, crop.Seed);
                next.Plant(crop, Plugin.Pace.Value);
            }
            else if (action == "load-water")
            { if (next.Water > CropState.ReservoirKg - .25) return false; input = Input(co, "LiquidWater", .25); next.Water += .25; }
            else if (action == "load-nutrients")
            { if (next.Nutrients > CropState.NutrientCapacityKg - .04) return false; input = Input(co, Definitions.Nutrient, .04); next.Nutrients += .04; }
            else return false;
            if (input == null) { s.Notice = Text.Get("missing_input"); return false; }
            // Main-thread conversion: source detaches before its quantity becomes rack contents.
            input.RemoveFromCurrentHome(true);
            if (input.objCOParent != null || input.ship != null) throw new InvalidOperationException("Agriculture input did not detach.");
            s.State = next; Save(s); input.Destroy(); co.objContainer.Redraw(); s.Notice = Text.Get("done"); return true;
        }
        catch (Exception ex) { Fault(co, ex); return false; }
    }
    private static bool Harvest(Session s, bool clear)
    {
        var b = s.State;
        if (b.CropId.Length == 0 || !clear && !b.Ready) { s.Notice = Text.Get("not_ready"); return false; }
        var specs = new List<(string Id, double Kg)>(); var harvest = b.Harvest(clear);
        if (harvest.SeedKg > 0) specs.Add((Definitions.PotatoSeed, harvest.SeedKg));
        for (int n = 0; n < harvest.Portions; n++) specs.Add((b.CropId == "potato" ? Definitions.Raw : Definitions.Leaves, harvest.PortionKg));
        if (harvest.ResidueKg > 1e-8) specs.Add((Definitions.Residue, harvest.ResidueKg));
        var next = b.Copy(); next.ClearCrop();
        return Deliver(s, specs, null, next);
    }
    private static void Cook(Session s)
    {
        var raw = CookerInput(s); if (raw == null) { s.State.Running = false; return; }
        var next = s.State.Copy(); next.CookerProgress = 0; next.CookerInput = ""; next.Running = false;
        if (!Deliver(s, new List<(string, double)> { (Definitions.Meal, .4) }, raw, next)) s.State.Running = false;
    }
    private static CondOwner? CookerInput(Session s)
    {
        var input = Resolve(s.State.CookerInput);
        return input != null && input.objCOParent == s.Object && input.strCODef == Definitions.Raw && input.coStackHead == null && input.aStack.Count == 0 &&
            input.GetCOsSafe(true).Count == 0 && Math.Abs(input.GetTotalMass() - .4) < 1e-7 ? input : null;
    }
    private static bool Deliver(Session s, List<(string Id, double Kg)> specs, CondOwner? input, CropState next)
    {
        var products = new List<CondOwner>(); bool committed = false;
        try
        {
            if (s.Object.objContainer == null || s.Object.objContainer.Locked) return false;
            double sourceMass = input != null ? input.GetTotalMass() : s.State.ContentsMass - next.ContentsMass;
            if (Math.Abs(specs.Sum(x => x.Kg) - sourceMass) > 1e-7) throw new InvalidOperationException("Unbalanced agriculture delivery.");
            foreach (var spec in specs)
            {
                var product = DataHandler.GetCondOwner(spec.Id); products.Add(product);
                if (spec.Id == Definitions.Residue || spec.Id == Definitions.Drainage) product.SetCondAmount("StatMass", spec.Kg);
                if (Math.Abs(product.GetTotalMass() - spec.Kg) > 1e-7 || !s.Object.objContainer.AllowedCO(product)) throw new InvalidOperationException("Invalid agriculture product.");
            }
            var grid = s.Object.objContainer.gridLayout; var cells = new bool[grid.gridMaxX, grid.gridMaxY];
            for (int x = 0; x < grid.gridMaxX; x++) for (int y = 0; y < grid.gridMaxY; y++) cells[x, y] = grid.gridID[x, y] != null || grid.gridInventoryItem[x, y] != null;
            var plan = BatchPlacement.Plan(cells, products.Select(p => { var size = GUIInventoryItem.GetWidthHeightForCO(p); return new ItemSize(size.x, size.y); }).ToArray());
            if (plan == null) { s.Notice = Text.Get("full"); return false; }
            for (int n = 0; n < products.Count; n++)
            { s.Object.objContainer.AddCOSimple(products[n], new PairXY(plan[n].X, plan[n].Y)); if (products[n].objCOParent != s.Object) throw new InvalidOperationException("Agriculture placement failed."); }
            if (input != null)
            {
                input.RemoveFromCurrentHome(true);
                if (input.objCOParent != null || input.ship != null) throw new InvalidOperationException("Cooker source did not detach.");
                // Once detached, replacements must survive even if native destruction throws.
                committed = true;
            }
            s.State = next; Save(s); committed = true;
            if (input != null) input.Destroy();
            s.Object.objContainer.Redraw(); s.Notice = Text.Get("done"); return true;
        }
        finally
        {
            if (!committed) foreach (var p in products)
            { if (p == null || p.bDestroyed) continue; if (p.ship != null || p.objCOParent != null) p.RemoveFromCurrentHome(true); p.Destroy(); }
        }
    }
}
