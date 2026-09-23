using System;
using System.Linq;
using System.Text.Json;
using PhobosShipbreaker.Core;

internal static class CollectorChecks
{
    internal static void Run(Action<bool,string> check, JsonElement[] recipes)
    {
        check(CollectorRules.Accepts(ProcessRules.Residue,13,true,true,true),"Only a real unstacked empty 13 kg residue packet is accepted");
        foreach(string id in new[]{"ItmScrapTrash","ItmScrapSteel","PhobosShipbreakerResidueDmg","phobosshipbreakerresidue",""})
            check(!CollectorRules.Accepts(id,13,true,true,true),"Unknown, valuable or similarly named cargo is retained: "+id);
        foreach(double kg in new[]{0.0,-1,12,14,double.NaN,double.PositiveInfinity})
            check(!CollectorRules.Accepts(ProcessRules.Residue,kg,true,true,true),"Altered payload mass is refused");
        check(!CollectorRules.Accepts(ProcessRules.Residue,13,false,true,true),"Installed payload rejected");
        check(!CollectorRules.Accepts(ProcessRules.Residue,13,true,false,true),"Nested contents cannot be silently discarded");
        check(!CollectorRules.Accepts(ProcessRules.Residue,13,true,true,false),"Stacks are not silently split");
        var recipe = recipes.Single(r=>r.GetProperty("id").GetString()=="PhobosBuildResidueCollector");
        double Mass(string side)=>recipe.GetProperty(side).EnumerateArray().Sum(i=>i.GetProperty("count").GetInt32()*i.GetProperty("unitMassKg").GetDouble());
        check(Mass("ingredients")==20 && Mass("outputs")==20 && CollectorRules.MachineKg==20,"Collector fabrication conserves its 20 kg bill");
        check(CollectorCommand.Parse("PHOBOSCOLLECTOR link port source").Action==CollectorAction.Link,"Console links explicit endpoints");
        foreach(string command in new[]{"phoboscollector link","phoboscollector link port","phoboscollector start port extra","phoboscollector eject","phoboscollector help port"})
            check(CollectorCommand.Parse(command).Action==CollectorAction.Invalid,"No ambiguous or unsupported command: "+command);
        check(CollectorCommand.Parse("phobosshipbreaker status").Action==CollectorAction.Foreign,"Other console namespaces pass through");
        foreach(string action in new[]{"status","start","pause","controls","unlink"})
            check(CollectorCommand.Parse("phoboscollector "+action+" port").PortId=="port","Target preserved: "+action);
    }
}
