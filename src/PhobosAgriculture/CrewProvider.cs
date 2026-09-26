using System;
using System.Collections.Generic;
using System.Linq;
using PhobosAgriculture.Core;
using Phobos.Ostranauts.Framework.Crew;

namespace PhobosAgriculture;

internal sealed class AgricultureCrewProvider : ICrewWorkProvider, ICrewSkipProvider, ICrewOrderPresentation
{
    public OrderFields Fields(CondOwner co)=>OrderFields.Stock|OrderFields.Source|OrderFields.Destination|OrderFields.Routine|
        (Definitions.IsCooker(co)||WorkupDefinitions.IsBench(co)?OrderFields.None:IrrigationDefinitions.IsSupply(co)?OrderFields.Drain:OrderFields.ClearCrops|OrderFields.Drain);
    public IEnumerable<Ship> Targets(CondOwner co)=>Array.Empty<Ship>();
    public OrderState Activity(CondOwner co,StandingOrder order)
    {var s=Service.Get(co);return s.Protected||co.HasCond("IsDamaged")||co.HasCond("IsLocked")||s.State.Running&&!co.HasCond("IsPowered")?OrderState.Blocked:s.State.Running?OrderState.Running:OrderState.Waiting;}
    public bool Validate(CondOwner co,StandingOrder draft,out string reason){reason="";return true;}
    public bool RelevantStore(CondOwner co,StandingOrder draft,CondOwner store,bool output)=>CrewLogistics.Contents(store).Any(c=>output?IsOutput(co,c,draft):
        Definitions.IsCooker(co)?c.strCODef==Definitions.Raw:WorkupDefinitions.IsBench(co)?
        c.strCODef==(draft.Recipe=="recover-crop"?WorkupDefinitions.Residue:WorkupDefinitions.Concentrate)||c.strCODef==WorkupDefinitions.Makeup:
        c.strCODef==Definitions.Irrigation||c.strCODef=="LiquidWater"||c.strCODef.StartsWith("PhobosVerdemorrow",StringComparison.Ordinal));
    public string Id => Plugin.Id;
    public bool Supports(CondOwner equipment) => Definitions.Machine(equipment);
    public bool RoutineResume(CondOwner equipment) => true;
    public IReadOnlyList<string> Recipes(CondOwner co) => Definitions.IsCooker(co)?new[]{"cook"}:WorkupDefinitions.IsBench(co)?new[]{"recover-crop","formulate-nutrients"}:
        IrrigationDefinitions.IsSupply(co)?new[]{"supply","recover-solution"}:new[]{"potato","lettuce","lettuce-seed"};
    public string RecipeLabel(string recipe) => Text.Get("crew_recipe_"+recipe);
    public CrewWorkOffer? Next(CondOwner co,StandingOrder order,out string reason)
    {
        reason=CrewWork.Message("waiting");
        var s=Service.Get(co); var b=s.State;
        if(!Definitions.Ready || s.Protected || !co.HasCond("IsInstalled") || co.HasCond("IsDamaged") || co.HasCond("IsLocked")) {reason=Text.Get("protected");return null;}
        if(!Recipes(co).Contains(order.Recipe)) {reason=Text.Get("crew_select_recipe");return null;}
        CrewRole role=Definitions.IsCooker(co)?CrewRole.Cooking:CrewRole.Agriculture;
        string skill=role==CrewRole.Cooking?"Cooking":"Agriculture";
        CrewWorkOffer Act(string action,double seconds=10)=>new(action,Text.Get(action),role,co,seconds,skill);
        CrewWorkOffer? Supply(string id,int retain=0)=>CrewLogistics.Supply(co,order,co,c=>c.strCODef==id,role,retain);
        var output=CrewLogistics.Output(co,order,co,c=>IsOutput(co,c,order),role);
        if(output!=null)return output;
        if(order.Drain && b.CropId.Length==0 && !b.Running && b.Water+b.Nutrients+s.Solution.TotalKg>0)return Act("drain",900);
        if(Definitions.IsCooker(co))
        {
            if(b.Running)return null;
            if(Stock(co,order,Definitions.Meal)>=order.Stock) {reason=Text.Get("crew_stock_met");return null;}
            if(Service.Input(co,Definitions.Raw,.4)==null)return Supply(Definitions.Raw)??Blocked(out reason);
            return Act("start");
        }
        if(WorkupDefinitions.IsBench(co))
        {
            if(s.Workup.Mode.Length>0)return b.Running?null:Act("start");
            if(Stock(co,order,order.Recipe=="recover-crop"?WorkupDefinitions.Concentrate:WorkupDefinitions.Mixture)>=order.Stock) {reason=Text.Get("crew_stock_met");return null;}
            string input=order.Recipe=="recover-crop"?WorkupDefinitions.Residue:WorkupDefinitions.Concentrate;
            if(!CrewLogistics.Contents(co).Any(c=>c.strCODef==input))return Supply(input)??Blocked(out reason);
            if(order.Recipe=="formulate-nutrients" && !CrewLogistics.Contents(co).Any(c=>c.strCODef==WorkupDefinitions.Makeup))return Supply(WorkupDefinitions.Makeup)??Blocked(out reason);
            return Act(order.Recipe,60);
        }
        if(IrrigationDefinitions.IsSupply(co) && order.Recipe=="recover-solution")
        {
            if(s.RecoveryInput.Length>0)return b.Running?null:Act("start");
            if(Stock(co,order,Definitions.Irrigation)>=order.Stock){reason=Text.Get("crew_stock_met");return null;}
            if(!CrewLogistics.Contents(co).Any(c=>c.strCODef==Service.CharacterizedDrainage))return Supply(Service.CharacterizedDrainage)??Blocked(out reason);
            if(!CrewLogistics.Contents(co).Any(c=>c.strCODef==Service.RecoveryCartridge))return Supply(Service.RecoveryCartridge)??Blocked(out reason);
            return Act("recover-solution",900);
        }
        if(b.Ready)return Act("harvest",1800);
        if(!IrrigationDefinitions.IsSupply(co) && b.CropId.Length>0 && b.CropId!=order.Recipe && order.ClearCrops)return Act("clear",900);
        if(b.CropId.Length>0 && b.Health<=0) return order.ClearCrops?Act("clear",900):Blocked(out reason);
        if(b.Water<Math.Min(4.7,s.Solution.PlainWaterCapacity))
        {
            if(Service.Input(co,Definitions.Irrigation,Definitions.IrrigationKg)!=null && b.Water<=s.Solution.PlainWaterCapacity-Definitions.IrrigationKg)return Act("load-irrigation");
            if(Service.Input(co,"LiquidWater",.25)!=null)return Act("load-water");
            var refill=b.Water<=s.Solution.PlainWaterCapacity-Definitions.IrrigationKg?Supply(Definitions.Irrigation):null;if(refill!=null)return refill;
            // Only explicitly designated stores are eligible; retain the same crew-water reserve as the provider adapter.
            int reserve=(int)Math.Ceiling(Plugin.ReserveLitres.Value/.25);
            refill=Supply("LiquidWater",reserve); if(refill!=null)return refill;
        }
        if(b.Nutrients<.04 && !s.Solution.Enabled)
        {
            if(Service.Input(co,Definitions.Nutrient,.04)!=null)return Act("load-nutrients");
            var nutrients=Supply(Definitions.Nutrient);if(nutrients!=null)return nutrients;
        }
        if(IrrigationDefinitions.IsSupply(co))return b.Running?null:Act("start");
        if(s.Routed && !b.Receiving)return Act("receive");
        if(b.CropId.Length==0)
        {
            string product=order.Recipe=="potato"?Definitions.Raw:order.Recipe=="lettuce"?Definitions.Leaves:Definitions.LettuceSeed;
            if(Stock(co,order,product)>=order.Stock){reason=Text.Get("crew_stock_met");return null;}
            string seed=order.Recipe=="potato"?Definitions.PotatoSeed:Definitions.LettuceSeed;
            double kg=Crop.Get(order.Recipe).Seed;
            if(Service.Input(co,seed,kg)==null)return Supply(seed,1)??Blocked(out reason);
            return Act("plant-"+order.Recipe,900);
        }
        return b.Running?null:Act("start");
    }
    private static CrewWorkOffer? Blocked(out string reason){reason=Text.Get("crew_supplies");return null;}
    private static int Stock(CondOwner co,StandingOrder order,string id)=>CrewLogistics.Contents(co).Concat(CrewLogistics.Contents(CrewWork.Resolve(order.Destination))).Count(c=>c.strCODef==id);
    private static bool IsOutput(CondOwner co,CondOwner item,StandingOrder order)
    {
        string id=item.strCODef;
        if(Definitions.IsCooker(co))return id==Definitions.Meal;
        if(WorkupDefinitions.IsBench(co))return id==WorkupDefinitions.Spent || id==WorkupDefinitions.Mixture || order.Recipe=="recover-crop" && id==WorkupDefinitions.Concentrate;
        if(IrrigationDefinitions.IsSupply(co))return id==Service.RecoveryReject || id==Definitions.Irrigation;
        return id==Definitions.Raw || id==Definitions.Leaves || id==Definitions.Residue || id==WorkupDefinitions.Residue ||
            id==Definitions.PotatoSeed && CrewLogistics.Contents(co).Count(c=>c.strCODef==id)>1 || id==Definitions.LettuceSeed && CrewLogistics.Contents(co).Count(c=>c.strCODef==id)>1;
    }
    public bool Complete(CrewWorkContext context,CrewWorkOffer offer,out string reason)
    {
        var next=Next(context.Equipment,context.Order,out reason);
        if(next?.Action!=offer.Action)return false;
        bool done=Definitions.Work.Contains(offer.Action)?Service.Work(context.Equipment,context.Actor,offer.Action):Service.Command(context.Equipment,null,offer.Action,out reason);
        if(done && offer.Action=="drain" && !CrewWork.CompleteOnce(context.Equipment))
        { Suspend(context.Equipment); return false; }
        if(done)reason=CrewWork.Message("done");return done;
    }
    public void Suspend(CondOwner equipment)=>Service.CrewSuspend(equipment);
    public bool CanAdvance(CondOwner equipment,out string reason)
    { reason=Text.Get("gap");return Supports(equipment) && Definitions.Ready && !Service.Get(equipment).Protected; }
    public void BeforeSkip() { }
    public void AfterSkip() { }
}

internal static partial class Service
{
    internal static void CrewSuspend(CondOwner co) { var s=Get(co);s.State.Running=s.State.Receiving=false;s.Watch.Cancel();Save(s); }
}
