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
    // The game stacks matching supplies dropped into a machine; a stack member is one unit like any other.
    private static bool IsInput(CondOwner owner, CondOwner item, string id, double kg) =>
        StackUnits.Inside(item, owner) && item.strCODef == id && !item.bDestroyed && !item.HasCond("IsInstalled") &&
        StackUnits.Empty(item) && Math.Abs(StackUnits.UnitMass(item) - kg) < 1e-7;
    internal static CondOwner? Input(CondOwner co, string id, double kg) => StackUnits.All(co).FirstOrDefault(x => IsInput(co, x, id, kg));
    private static readonly string[] SupplyWork = { "load-water", "load-irrigation", "load-nutrients", "drain", "recover-solution" };
    /// <summary>What would stop this work right now, or null. Checked when the action is offered, so crew are
    /// not sent to fail half an hour later, and again when it runs.</summary>
    internal static string? WorkProblem(CondOwner co, CondOwner? actor, string action)
    {
        if (!Definitions.Ready) return Text.Get("protected");
        var s = Get(co);
        if (s.Protected || WaterGuard(co).Protected) return Text.Get("protected");
        var access = Access(co, null, actor); if (access != null) return access;
        if (!co.HasCond("IsInstalled") || co.HasCond("IsDamaged")) return Text.Get("repair");
        if (IrrigationDefinitions.IsSupply(co) && !SupplyWork.Contains(action)) return Text.Get("help");
        if (WorkupDefinitions.IsBench(co)) return action == "recover-crop" || action == "formulate-nutrients" ? null : Text.Get("help");
        if (action == "recover-crop" || action == "formulate-nutrients") return Text.Get("help");
        var b = s.State;
        if (action == "drain") return b.Water + b.Nutrients + s.Solution.TotalKg + s.Line.TotalKg > 0 ? null : Text.Get("empty");
        if (action == "harvest") return b.CropId.Length > 0 && b.Ready ? null : Text.Get("not_ready");
        if (action == "clear") return b.CropId.Length > 0 ? null : Text.Get("not_ready");
        if (action.StartsWith("plant-", StringComparison.Ordinal))
        {
            var crop = Definitions.PlantCrop(action);
            if (crop == null) return Text.Get("help");
            if (b.CropId.Length != 0) return Text.Get("crop_present");
            if (!s.Solution.CanPlant(crop.Id)) return Text.Get("solution_incompatible");
            return Input(co, crop.Stock, crop.Seed) != null ? null : Text.Get("missing_input");
        }
        if (action == "load-water") return b.Water > s.Solution.PlainWaterCapacity - .25 ? Text.Get("full") : Input(co, "LiquidWater", .25) != null ? null : Text.Get("missing_input");
        if (action == "load-irrigation") return b.Water > s.Solution.PlainWaterCapacity - Definitions.IrrigationKg ? Text.Get("irrigation_full", Definitions.IrrigationKg) : Input(co, Definitions.Irrigation, Definitions.IrrigationKg) != null ? null : Text.Get("missing_input");
        if (action == "load-nutrients") return b.Nutrients > s.Solution.DryCapacity - .04 ? Text.Get("full") : Input(co, Definitions.Nutrient, .04) != null ? null : Text.Get("missing_input");
        return null;
    }
    internal static bool Work(CondOwner co, CondOwner actor, string action)
    {
        var s = Get(co);
        if (WorkProblem(co, actor, action) is string problem) { s.Notice = problem; return false; }
        try
        {
            if (WorkupDefinitions.IsBench(co)) return action == "recover-crop" ? QueueWorkup(s,"recover") : action == "formulate-nutrients" && QueueWorkup(s,"formulate");
            if (action == "recover-crop" || action == "formulate-nutrients") return false;
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
            if (Definitions.PlantCrop(action) is Crop crop)
            {
                if (next.CropId.Length != 0) return false;
                if (!s.Solution.CanPlant(crop.Id)) { s.Notice = Text.Get("solution_incompatible"); return false; }
                input = Input(co, crop.Stock, crop.Seed);
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
        var specs = new List<(string Id, double Kg)>(); var harvest = b.Harvest(clear); var grown = Crop.Get(b.CropId);
        if (harvest.SeedKg > 0) specs.Add((grown.Stock, harvest.SeedKg));
        for (int n = 0; n < harvest.Portions; n++) specs.Add((grown.Produce, harvest.PortionKg));
        if (harvest.ResidueKg > 1e-8) specs.Add((b.RecoveryRevision == 1 ? WorkupDefinitions.Residue : Definitions.Residue, harvest.ResidueKg));
        var next = b.Copy(); next.ClearCrop();
        double nutrient = NutrientRecovery.Allocation(b, harvest.ResidueKg);
        return Deliver(s, specs, null, next, initialize:(p,id)=> { if(id==WorkupDefinitions.Residue) WriteResidue(p,nutrient); });
    }
    private static void Cook(Session s)
    {
        var raw = CookerInput(s); var recipe = HearthRecipes.ForInput(raw?.strCODef); if (raw == null || recipe == null) { s.State.Running = false; return; }
        var next = s.State.Copy(); next.CookerProgress = 0; next.CookerInput = ""; next.Running = false;
        s.MealCommitted = Deliver(s, new List<(string, double)> { (recipe.Product, recipe.Kg) }, raw, next);
        if (!s.MealCommitted) { s.Watch.Cancel(); s.State.Running = false; }
    }
    private static CondOwner? CookerInput(Session s)
    {
        var input = Resolve(s.State.CookerInput);
        return input != null && HearthRecipes.ForInput(input.strCODef) is HearthRecipe r && IsInput(s.Object, input, r.Input, r.Kg) ? input : null;
    }
    /// <summary>The recipe the bound portion is cooked by, or null when nothing cookable is bound.</summary>
    private static HearthRecipe? CookerRecipe(Session s) => HearthRecipes.ForInput(CookerInput(s)?.strCODef);
    /// <summary>The energy the bound portion takes; with nothing bound, the most any recipe takes.</summary>
    private static double CookerKWh(Session s) => CookerRecipe(s)?.KWh ?? HearthRecipes.MaxKWh;
    /// <summary>The first portion in the cooker that some recipe takes, in the recipes' order.</summary>
    private static CondOwner? Cookable(CondOwner co) => HearthRecipes.All.Select(r => Input(co, r.Input, r.Kg)).FirstOrDefault(x => x != null);
    private static bool Deliver(Session s, List<(string Id, double Kg)> specs, CondOwner? input, CropState next, NutrientSolution? nextSolution = null, FluidLine? nextLine=null, CondOwner? additionalInput=null, double cartridgeRemaining=0, Action<CondOwner,string>? initialize=null)
    {
        var products = new List<CondOwner>(); bool committed = false; TrayDelivery? delivery = null;
        try
        {
            if (s.Object.objContainer == null || s.Object.objContainer.Locked) return false;
            double sourceMass = (input?.GetTotalMass() ?? 0) + (additionalInput?.GetTotalMass() ?? 0) + s.State.ContentsMass - next.ContentsMass + s.Solution.TotalKg - (nextSolution ?? s.Solution).TotalKg + s.Line.TotalKg - (nextLine ?? s.Line).TotalKg;
            if (Math.Abs(specs.Sum(x => x.Kg) - sourceMass) > 1e-7) throw new InvalidOperationException("Unbalanced agriculture delivery.");
            foreach (var spec in specs)
            {
                var product = DataHandler.GetCondOwner(spec.Id); products.Add(product);
                if (spec.Id == Definitions.Residue || spec.Id == Definitions.Drainage || spec.Id == CharacterizedDrainage || spec.Id == RecoveryReject || spec.Id == WorkupDefinitions.Residue || spec.Id == WorkupDefinitions.Concentrate || spec.Id == WorkupDefinitions.Spent || spec.Id == WorkupDefinitions.Mixture || spec.Id == WorkupDefinitions.Makeup) product.SetCondAmount("StatMass", spec.Kg);
                if(spec.Id==RecoveryCartridge) WriteCartridge(product,cartridgeRemaining);
                if(spec.Id==CharacterizedDrainage) WriteDrainage(product,new(s.State.Water+s.Solution.Quantity.CarrierKg+s.Line.Quantity.CarrierKg,s.State.Nutrients+s.Solution.Quantity.SoluteKg+s.Line.Quantity.SoluteKg));
                initialize?.Invoke(product,spec.Id);
                if (Math.Abs(product.GetTotalMass() - spec.Kg) > 1e-7 || !s.Object.objContainer.AllowedCO(product)) throw new InvalidOperationException("Invalid agriculture product.");
            }
            // Portions and seed go into the inventory as stacks since Agriculture 0.37.0 (Framework TrayDelivery); recorded
            // products (residue, solution, mixtures) keep their own cells.
            delivery = TrayDelivery.Plan(s.Object.objContainer, products);
            if (delivery == null) { s.Notice = Text.Get("full"); return false; }
            if (!DeliveryStore(s.Object).TryWrite(new Dictionary<string,string>{["state"]="pending"})) throw new InvalidOperationException("Protected material delivery.");
            delivery.Place();
            if (products.Any(p => !StackUnits.Inside(p, s.Object))) throw new InvalidOperationException("Agriculture placement failed.");
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
            if (!DeliveryStore(s.Object).TryWrite(new Dictionary<string,string>{["state"]="clear"})) throw new InvalidOperationException("Material delivery journal failed.");
            s.Object.objContainer.Redraw(); s.Notice = Text.Get("done"); return true;
        }
        finally
        {
            if (!committed)
            {
                delivery?.Rollback();
                foreach (var p in products)
                { if (p == null || p.bDestroyed) continue; if (p.ship != null || p.objCOParent != null) p.RemoveFromCurrentHome(true); p.Destroy(); }
            }
        }
    }
}
