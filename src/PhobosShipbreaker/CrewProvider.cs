using System;
using System.Collections.Generic;
using System.Linq;
using PhobosShipbreaker.Core;
using Phobos.Ostranauts.Framework.Crew;

namespace PhobosShipbreaker;

internal sealed class IndustrialCrewProvider : ICrewWorkProvider,ICrewSkipProvider,ICrewBoundProvider,ICrewOrderPresentation
{
    public OrderFields Fields(CondOwner co)=>ProcessingService.IsGrabber(co)?OrderFields.Target|OrderFields.Hazardous:
        co.strCODef.StartsWith(CollectorRules.Installed,StringComparison.Ordinal)?OrderFields.Destination|OrderFields.Routine:
        OrderFields.Stock|OrderFields.Source|OrderFields.Destination|(FurnaceRules.Machine(co.strCODef)?OrderFields.Hazardous:OrderFields.None);
    public IEnumerable<Ship> Targets(CondOwner co)=>ProcessingService.IsGrabber(co)&&CaptureService.Read(co,out var c)&&CrewSim.system.GetShipByRegID(c["target"]) is Ship s?new[]{s}:Array.Empty<Ship>();
    public OrderState Activity(CondOwner co,StandingOrder order)
    {
        if(co.HasCond("IsDamaged")||co.HasCond("IsLocked"))return OrderState.Blocked;
        bool working=ProcessingService.IsGrabber(co)?ReclamationService.CrewActive(co):FurnaceRules.Machine(co.strCODef)?FurnaceService.Get(co).State.Batch.Armed:co.HasCond(ProcessRules.Working);
        return working?(co.HasCond("IsPowered")?OrderState.Running:OrderState.Blocked):OrderState.Waiting;
    }
    public bool Validate(CondOwner co,StandingOrder draft,out string reason)
    {reason=Text.Get("Crew.bound_target");return !ProcessingService.IsGrabber(co)||draft.Target=="none"||Targets(co).Any(s=>s.strRegID==draft.Target);}
    public bool RelevantStore(CondOwner co,StandingOrder draft,CondOwner store,bool output)=>CrewLogistics.Contents(store).Any(c=>output?ProcessingService.CrewProduct(co,c):
        FurnaceRules.Machine(co.strCODef)?FurnaceService.CrewFeed(c):ProcessingService.CrewFeed(co,c));
    public string Id=>Plugin.Id;
    public bool Supports(CondOwner c)=>ProcessingService.IsProcessor(c.strCODef) || c.strCODef==CollectorRules.Installed || c.strCODef==CollectorRules.Installed+"Dmg" ||
        FurnaceRules.Machine(c.strCODef) || ProcessingService.IsGrabber(c);
    public bool RoutineResume(CondOwner c)=>c.strCODef==CollectorRules.Installed;
    public IReadOnlyList<string> Recipes(CondOwner c)=>ProcessingService.IsGrabber(c)?new[]{"reclaim"}:FurnaceRules.Machine(c.strCODef)?new[]{"housing"}:
        c.strCODef==CollectorRules.Installed?new[]{"collect"}:new[]{"process"};
    public string RecipeLabel(string recipe)=>Text.Get("Crew.recipe_"+recipe);
    public string CaptureBinding(CondOwner co,StandingOrder order)=>ProcessingService.IsGrabber(co)&&CaptureService.Read(co,out var capture)?
        CrewBalance.Binding(new[]{"owner","ship","target","console","module","g4","chute","processor","permission","mount"}.Select(k=>capture[k])):"";
    public CrewWorkOffer? Next(CondOwner co,StandingOrder order,out string reason)
    {
        reason=CrewWork.Message("waiting");
        if(!Content.Ready || co.HasCond("IsDamaged") || co.HasCond("IsLocked") || !co.HasCond("IsInstalled")){reason=Text.Get("Crew.repair");return null;}
        if(!Recipes(co).Contains(order.Recipe)){reason=Text.Get("Crew.select_recipe");return null;}
        CrewWorkOffer Act(string action)=>new(action,Text.Get("Crew.action_"+action),CrewRole.Industry,co,10,"IndustrialProcessing");
        if(ProcessingService.IsGrabber(co))
        {
            if(!order.Hazardous || !CaptureService.Read(co,out var capture) || order.Target!=capture["target"] ||
                order.Binding.Length==0 || order.Binding!=CaptureBinding(co,order)){reason=Text.Get("Crew.bound_target");return null;}
            if(ReclamationService.CrewActive(co))return null;
            string action=ReclamationService.CrewHasMission(co)?"reclaim-resume":"reclaim-start";
            return new(action,Text.Get("Crew.action_reclaim"),CrewRole.Exterior,co,30,"SkillOpsEnvSuit",skipSupported:false);
        }
        if(FurnaceRules.Machine(co.strCODef))
        {
            var s=FurnaceService.Get(co);var b=s.State.Batch;
            if(s.Protected){reason=Text.Get("Furnace.protected");return null;}
            if(b.Phase==FurnacePhase.Idle && b.SafeOpen)
            {
                var products=CrewLogistics.Output(co,order,co,c=>c.strCODef==FurnaceRules.Blank || c.strCODef==FurnaceRules.Remainder || c.strCODef==FurnaceService.CoolantWaste,CrewRole.Industry);
                if(products!=null)return products;
                if(FurnaceService.CrewNeedsCoolant(co))
                {
                    if(CrewLogistics.Contents(co).Any(c=>c.strCODef==FurnaceService.CoolantStock&&c.coStackHead==null&&c.aStack.Count==0))return Act("coolant-fill");
                    return CrewLogistics.Supply(co,order,co,c=>c.strCODef==FurnaceService.CoolantStock,CrewRole.Industry)??Blocked(out reason);
                }
                int stock=CrewLogistics.Contents(co).Concat(CrewLogistics.Contents(CrewWork.Resolve(order.Destination))).Count(c=>c.strCODef==FurnaceRules.Blank);
                if(stock>=order.Stock){reason=Text.Get("Crew.stock_met");return null;}
                var feed=FurnaceService.Feed(co);if(feed==null)return null;
                if(CrewLogistics.Contents(feed).Count()<FurnaceRules.ChargeUnits)return CrewLogistics.Supply(co,order,feed,FurnaceService.CrewFeed,CrewRole.Industry)??Blocked(out reason);
                if(!order.Hazardous){reason=Text.Get("Crew.hot_permission");return null;}
                return Act("seal");
            }
            if(!order.Hazardous){reason=Text.Get("Crew.hot_permission");return null;}
            if(b.Phase==FurnacePhase.Equalize && b.SafeOpen)return Act("equalize");
            if(b.Phase==FurnacePhase.Ready && b.SafeOpen)return Act("release");
            if(b.Phase>FurnacePhase.Idle && b.Phase<FurnacePhase.Equalize && !b.Armed)return Act("auto-run");
            return null;
        }
        var output=CrewLogistics.Output(co,order,co,_=>true,CrewRole.Industry);
        if(output!=null)return output;
        if(co.strCODef==CollectorRules.Installed)return Plugin.Collectors.ReceivingEnabled(co)?null:Act("collect");
        var bin=ProcessingService.Feed(co);if(bin==null)return null;
        if(co.HasCond(ProcessRules.Working))return null;
        int count=CrewLogistics.Contents(co).Concat(CrewLogistics.Contents(CrewWork.Resolve(order.Destination))).Count(c=>ProcessingService.CrewProduct(co,c));
        if(count>=order.Stock){reason=Text.Get("Crew.stock_met");return null;}
        if(CrewLogistics.Contents(bin).Any(c=>ProcessingService.ValidInput(co,c)))return Act("process");
        return CrewLogistics.Supply(co,order,bin,c=>ProcessingService.CrewFeed(co,c),CrewRole.Industry)??Blocked(out reason);
    }
    private static CrewWorkOffer? Blocked(out string reason){reason=Text.Get("Crew.supplies");return null;}
    public bool Complete(CrewWorkContext context,CrewWorkOffer offer,out string reason)
    {
        var next=Next(context.Equipment,context.Order,out reason);if(next?.Action!=offer.Action)return false;
        var co=context.Equipment;
        if(offer.Role==CrewRole.Exterior)
        {
            bool started=ReclamationService.Command(null,co,offer.Action,out reason);
            if(!CrewWork.CompleteOnce(co)) { Suspend(co); return false; }
            return started;
        }
        if(FurnaceRules.Machine(co.strCODef))return FurnaceService.Command(null,co,offer.Action,null,out reason);
        bool done=co.strCODef==CollectorRules.Installed?Plugin.Collectors.Start(co):Plugin.Service.CrewStart(co);
        reason=done?CrewWork.Message("done"):Plugin.Service.Describe(co);return done;
    }
    public void Suspend(CondOwner co)
    {
        if(ProcessingService.IsGrabber(co))ReclamationService.CrewSuspend(co);
        else if(FurnaceRules.Machine(co.strCODef))FurnaceService.CrewSuspend(co);
        else if(ProcessingService.IsProcessor(co.strCODef))Plugin.Service.Block(co,Text.Get("Crew.stopped"));
        else Plugin.Collectors.CrewSuspend(co);
    }
    public bool CanAdvance(CondOwner co,out string reason)
    { reason=Text.Get("Crew.skip_exterior");return !ProcessingService.IsGrabber(co) && Content.Ready; }
    public void BeforeSkip()
    {
        foreach(var co in DataHandler.mapCOs.Values.Where(c=>c!=null&&!c.bDestroyed&&ProcessingService.IsGrabber(c)).ToArray())
        {ReclamationService.CrewSuspend(co);if(CrewWork.Order(co).Permission==WorkPermission.Enabled)CrewWork.SetPermission(co,WorkPermission.Suspended,"skip");}
    }
    public void AfterSkip() { }
}

internal sealed partial class ProcessingService
{
    internal static bool CrewProduct(CondOwner machine,CondOwner item)=>Recipes(machine).Recipes.Any(r=>r.Products.Any(p=>p.Id==item.strCODef));
    internal static bool CrewFeed(CondOwner machine,CondOwner input)=>CrewLogistics.Loose(input) &&
        input.strCODef==InputDefinition(machine) && ProcessRules.MassMatches(input.GetCondAmount("StatMass"),Recipes(machine).Current.InputKg);
    internal bool CrewStart(CondOwner machine)
    {
        if(AccessProblem(machine)!=null || MachineProblem(machine)!=null)return false;
        var state=sessions.GetValue(machine,_=>new Session());
        if(state.Intake?.Mission==true&&state.Intake.Armed)return false;
        state.CrewManaged=true;
        // A standing order authorises this stocked batch; it does not arm an unrelated intake chain.
        return state.Job?.Running==true || StartNext(machine,state);
    }
}
internal sealed partial class CollectorService
{
    internal void CrewSuspend(CondOwner co)
    {
        var s=sessions.GetValue(co,_=>new Session());
        Disarm(co,s); s.Status=Text.Get("Crew.stopped");
    }
}
internal static partial class ReclamationService
{
    static partial void CrewManualStop(CondOwner co)=>CrewWork.ManualStop(co);
    internal static bool CrewActive(CondOwner co)=>sessions.TryGetValue(co.strID,out var s)&&s.Authorized;
    internal static bool CrewHasMission(CondOwner co)=>Read(co,out _);
    internal static void CrewSuspend(CondOwner co){if(sessions.TryGetValue(co.strID,out var s))Suspend(s,Text.Get("Crew.skip_exterior"));}
}
internal static partial class FurnaceService
{
    internal static bool CrewNeedsCoolant(CondOwner co)=>Get(co).Coolant.Enabled&&Get(co).Coolant.TotalKg+1<=CoolantCharge.CapacityKg;
    internal static void CrewSuspend(CondOwner co){var s=Get(co);s.State.Batch.Stop();Save(s);}
}
