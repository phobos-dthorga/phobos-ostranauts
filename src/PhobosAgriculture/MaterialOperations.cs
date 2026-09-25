using System;
using System.Collections.Generic;
using System.Linq;
using PhobosAgriculture.Core;
using Phobos.Ostranauts.Framework.Inventory;
using Phobos.Ostranauts.Framework.Liquids;
using Ostranauts.Inventory;

namespace PhobosAgriculture;

internal static partial class Service
{
    internal static CondOwner? Input(CondOwner co, string id, double kg) => co.objContainer?.ContainedCOs.FirstOrDefault(x =>
        x.strCODef == id && !x.bDestroyed && x.coStackHead == null && x.aStack.Count == 0 && !x.HasCond("IsInstalled") && x.GetCOsSafe(true).Count == 0 && x.GetLotCOs(true).Count == 0 && Math.Abs(x.GetTotalMass() - kg) < 1e-7);
    internal static bool Work(CondOwner co, CondOwner actor, string action)
    {
        var s = Get(co);
        if (s.Protected || WaterGuard(co).Protected || Access(co, null, actor) != null || !co.HasCond("IsInstalled") || co.HasCond("IsDamaged") || !Definitions.Ready) return false;
        if (IrrigationDefinitions.IsSupply(co) && action != "load-water" && action != "load-irrigation" && action != "load-nutrients" && action != "drain" && action != "recover-solution") return false;
        try
        {
            if(action=="recover-solution") return QueueRecovery(s);
            if (action == "harvest" || action == "clear") return Harvest(s, action == "clear");
            if (action == "drain")
            {
                double kg = s.State.Water + s.State.Nutrients + s.Solution.TotalKg + s.Line.TotalKg;
                if (kg <= 0) return false;
                var drained = s.State.Copy(); drained.Water = drained.Nutrients = 0; drained.Running = drained.Receiving = false;
                var solution = s.Solution.Copy(); solution.Quantity = default;
                var line=s.Line.Copy(); line.SetQuantity(default);
                return Deliver(s, new List<(string, double)> { (CharacterizedDrainage, kg) }, null, drained, solution,line);
            }
            var next = s.State.Copy(); CondOwner? input;
            if (action == "plant-potato" || action == "plant-lettuce")
            {
                var crop = action == "plant-potato" ? Crop.Potato : Crop.Lettuce;
                if (next.CropId.Length != 0) return false;
                if (!s.Solution.CanPlant(crop.Id)) { s.Notice = Text.Get("solution_incompatible"); return false; }
                input = Input(co, crop == Crop.Potato ? Definitions.PotatoSeed : Definitions.LettuceSeed, crop.Seed);
                next.Plant(crop, Plugin.Pace.Value);
            }
            else if (action == "load-water")
            { if (next.Water > s.Solution.PlainWaterCapacity - .25) return false; input = Input(co, "LiquidWater", .25); next.Water += .25; }
            else if (action == "load-irrigation")
            { if (next.Water > s.Solution.PlainWaterCapacity - Definitions.IrrigationKg) { s.Notice = Text.Get("irrigation_full", Definitions.IrrigationKg); return false; } input = Input(co, Definitions.Irrigation, Definitions.IrrigationKg); next.Water += Definitions.IrrigationKg; }
            else if (action == "load-nutrients")
            { if (next.Nutrients > s.Solution.DryCapacity - .04) return false; input = Input(co, Definitions.Nutrient, .04); next.Nutrients += .04; }
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
    private static bool Deliver(Session s, List<(string Id, double Kg)> specs, CondOwner? input, CropState next, NutrientSolution? nextSolution = null, FluidLine? nextLine=null, CondOwner? additionalInput=null)
    {
        var products = new List<CondOwner>(); bool committed = false;
        try
        {
            if (s.Object.objContainer == null || s.Object.objContainer.Locked) return false;
            double sourceMass = (input?.GetTotalMass() ?? 0) + (additionalInput?.GetTotalMass() ?? 0) + s.State.ContentsMass - next.ContentsMass + s.Solution.TotalKg - (nextSolution ?? s.Solution).TotalKg + s.Line.TotalKg - (nextLine ?? s.Line).TotalKg;
            if (Math.Abs(specs.Sum(x => x.Kg) - sourceMass) > 1e-7) throw new InvalidOperationException("Unbalanced agriculture delivery.");
            foreach (var spec in specs)
            {
                var product = DataHandler.GetCondOwner(spec.Id); products.Add(product);
                if (spec.Id == Definitions.Residue || spec.Id == Definitions.Drainage || spec.Id == CharacterizedDrainage || spec.Id == RecoveryReject) product.SetCondAmount("StatMass", spec.Kg);
                if(spec.Id==CharacterizedDrainage) WriteDrainage(product,new(s.State.Water+s.Solution.Quantity.CarrierKg+s.Line.Quantity.CarrierKg,s.State.Nutrients+s.Solution.Quantity.SoluteKg+s.Line.Quantity.SoluteKg));
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
            if(additionalInput!=null) { additionalInput.RemoveFromCurrentHome(true); if(additionalInput.objCOParent!=null||additionalInput.ship!=null) throw new InvalidOperationException("Recovery input did not detach."); committed=true; }
            s.State = next; s.Solution = nextSolution ?? s.Solution; s.Line=nextLine??s.Line; Save(s); committed = true;
            if (input != null) input.Destroy();
            if(additionalInput!=null) additionalInput.Destroy();
            s.Object.objContainer.Redraw(); s.Notice = Text.Get("done"); return true;
        }
        finally
        {
            if (!committed) foreach (var p in products)
            { if (p == null || p.bDestroyed) continue; if (p.ship != null || p.objCOParent != null) p.RemoveFromCurrentHome(true); p.Destroy(); }
        }
    }
}
