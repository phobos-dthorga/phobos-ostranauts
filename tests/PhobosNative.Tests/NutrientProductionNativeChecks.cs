using System;
using System.IO;
using System.Linq;
using System.Reflection;
using PhobosAgriculture;
using Phobos.Ostranauts.Framework.Registration;

internal static class NutrientProductionNativeChecks
{
    internal static void Run(NativeDefinitions d,string game,Action<bool,string> check)
    {
        foreach(string id in new[]{Definitions.Nutrient,WorkupDefinitions.Makeup,WorkupDefinitions.Mixture,WorkupDefinitions.Concentrate,Service.RecoveryCartridge})
        {
            check(!d.Installables.Values.Any(j=>(j.strJobType=="repair" || j.strJobType=="restore") && j.strActionCO==id),"Consumable has no repair/restore definition: "+id);
            check(!d.Objects[id].aStartingConds.Any(c=>c.StartsWith("IsMechanical=")||c.StartsWith("StatDamageMax=")),"Consumable depletion is not repairable mechanical durability: "+id);
            check(d.Objects[id].nStackLimit==1 && d.Objects[id].aTickers.Length==0,"Finite charge cannot merge or regenerate on an offline ticker: "+id);
        }
        foreach(string id in new[]{WorkupDefinitions.Residue,WorkupDefinitions.Concentrate,WorkupDefinitions.Spent,WorkupDefinitions.Mixture,RecyclerCapture.Wet})
            check(!d.Objects[id].aStartingConds.Any(c=>c.StartsWith("IsFood=")||c.StartsWith("IsHydrator=")),"Recovery supplies cannot impersonate food/potable water: "+id);
        foreach(string form in new[]{"Installed","Loose","InstalledDmg","LooseDmg"})
            check(d.Items[WorkupDefinitions.Bench+form].nCols==2,"B2 keeps registered two-tile equipment footprint");
        string provider=Path.Combine(game,"BepInEx","plugins","Workshop","3757331189","ShipsWater","ShipsWater.dll");
        if(!File.Exists(provider))return; // Optional provider is not required for native/core checks.
        var assembly=Assembly.LoadFrom(provider);var type=assembly.GetType("ShipsWater.Recycler");
        var method=type?.GetMethod("ProcessOne",BindingFlags.NonPublic|BindingFlags.Static);
        check(method!=null && method.ReturnType==typeof(void),"Installed ShipsWater recycler settlement hook resolves");
        var parameters=method!.GetParameters();
        check(parameters.Length==8 && parameters[0].ParameterType==typeof(CondOwner) && parameters[1].ParameterType==typeof(double) && parameters.Skip(2).Take(3).All(p=>p.ParameterType==typeof(float)),"Inspected recycler positional timing/recovery hook matches installed provider");
        check(parameters[5].ParameterType==typeof(System.Collections.Generic.List<CondOwner>) && parameters[6].ParameterType==parameters[5].ParameterType && parameters[7].ParameterType==typeof(CondTrigger),"Inspected recycler tank/filter signature matches installed provider");
    }
}
