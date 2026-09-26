using System;
using System.Collections.Generic;
using System.Linq;
using PhobosAutoNav.Core;
using Phobos.Ostranauts.Framework.Crew;

namespace PhobosAutoNav;

/// <summary>Only resumes an explicitly bound, already recorded flight; never acquires a target.</summary>
internal sealed class NavigationCrewProvider : ICrewWorkProvider,ICrewBoundProvider
{
    public string Id => Plugin.Id;
    public bool Supports(CondOwner co) => co.HasCond("IsNavStation") &&
        co.GetCOsSafe(true).Any(c => c.strCODef == NavigationService.ModuleId || c.strCODef == NavigationService.PursuitId);
    public bool RoutineResume(CondOwner co) => false;
    public IReadOnlyList<string> Recipes(CondOwner co) => new[] { "resume-flight" };
    public string RecipeLabel(string recipe) => Text.Get("Crew.recipe");
    public string CaptureBinding(CondOwner co,StandingOrder order)=>Plugin.Service.CrewFlightBinding(co);
    public CrewWorkOffer? Next(CondOwner co, StandingOrder order, out string reason)
    {
        reason = Text.Get("Crew.bind");
        if (order.Recipe != "resume-flight" || !order.Hazardous || AutoNavCore.Engaged ||
            !Plugin.Service.CrewFlightMatches(co,order.Target) || order.Binding.Length==0 || order.Binding!=CaptureBinding(co,order)) return null;
        return new CrewWorkOffer("resume-flight",Text.Get("Crew.recipe"),CrewRole.Exterior,co,30,"SkillOpsSpaceship",skipSupported:false);
    }
    public bool Complete(CrewWorkContext context, CrewWorkOffer offer, out string reason)
    {
        reason = Text.Get("Crew.bind");
        if (!CrewWork.LocalAccess(context.Actor,context.Equipment,2) || Next(context.Equipment,context.Order,out reason)==null) return false;
        Plugin.Service.ResumeSaved(context.Equipment);
        bool done = AutoNavCore.Engaged;
        // One permission admits one attempt. Tracking loss or a later stop needs a new explicit Resume.
        if(!CrewWork.CompleteOnce(context.Equipment)) { Plugin.Service.CrewSuspend(context.Equipment); return false; }
        reason = done ? CrewWork.Message("done") : reason;
        return done;
    }
    public void Suspend(CondOwner co) => Plugin.Service.CrewSuspend(co);
}

internal sealed partial class NavigationService
{
    partial void CrewResumePolicy(CondOwner co,ref bool permitted)
    { if(CrewWork.Order(co).Recipe=="resume-flight")permitted=false; }
    partial void CrewManualStop(CondOwner co)=>CrewWork.ManualStop(co);
    internal string CrewFlightBinding(CondOwner co)
    {
        var flight=DisplaySnapshot(co); if(flight==null)return "";
        var fields=flight.Encode(); fields.Remove("elapsedSeconds");fields.Remove("coasting");
        fields["mode"]=flight.ActiveMode.ToString();
        return CrewBalance.Binding(fields.OrderBy(p=>p.Key,StringComparer.Ordinal).SelectMany(p=>new[]{p.Key,p.Value}));
    }
    internal bool CrewFlightMatches(CondOwner co,string target) =>
        DisplaySnapshot(co) is FlightSnapshot saved && saved.IsResumable && saved.TargetId==target &&
        HardwareProblem(co)==null;
    internal void CrewSuspend(CondOwner co) { if (console==co) SuspendForSkip(); }
    internal void SuspendForSkip()
    {
        string reason=Text.Get("Crew.skip");
        StopExtended(reason); EndIndustrial(reason); CeaseFire();
        if (AutoNavCore.Engaged) FinishSavedFlight(savedFlight?.SuspendedMode??SavedFlightMode.Suspended);
        issuing=true;
        try { if(AutoNavCore.Engaged)AutoNavCore.EndFlight(AutoNavCore.EngagedPlayer,reason); }
        finally { AutoNavCore.ResetStatics(); Torch.Release(); issuing=false; status=reason; }
    }
}
