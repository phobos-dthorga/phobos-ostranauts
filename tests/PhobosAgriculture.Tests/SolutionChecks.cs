using System;
using PhobosAgriculture.Core;
using Phobos.Ostranauts.Framework.Liquids;

internal static class SolutionChecks
{
    internal static void Run(Action<bool,string> check)
    {
        void Near(double a,double b,string message)=>check(Math.Abs(a-b)<1e-8,message);
        foreach(var pair in new[]{(Crop.Potato,NutrientSolution.Potato),(Crop.Lettuce,NutrientSolution.Lettuce)})
        {
            var c=pair.Item1; var stock=new CropState{Water=c.Water,Nutrients=c.Nutrient};
            var mix=new NutrientSolution{Profile=pair.Item2};
            double start=stock.ContentsMass;
            Near(mix.Blend(stock,c.Water+c.Nutrient),start,"Finite water and dry stock form one complete profile");
            Near(stock.ContentsMass+mix.TotalKg,start,"Mixing conserves total mass");
            Near(stock.Water,0,"Source water consumed"); Near(stock.Nutrients,0,"Source nutrient consumed");
            var reloaded=NutrientSolution.Read(mix.Save(stock),CropState.Read(stock.Save()));
            check(MixtureTransfer.Same(mix.Quantity,reloaded.Quantity),"Both components survive save/load");
            check(!mix.CanPlant(c==Crop.Potato?"lettuce":"potato"),"Incompatible remaining solution prevents crop switch");
            var grown=new CropState(); grown.Plant(c,1);
            double carbon=0,oxygen=0,vapour=0;
            for(int hour=0;hour<c.Hours;hour++)
            {
                var e=grown.Step(1,c.KW,100,100,true,mix); carbon+=e.CO2Kg; oxygen+=e.OxygenKg; vapour+=e.VapourKg;
            }
            Near(grown.Progress,1,"Mixed feed reaches existing full-growth target");
            Near(grown.Biomass,c.Final,"Mixture does not multiply crop yields");
            Near(mix.TotalKg,0,"Whole formulation consumed without leftover invented matter");
            Near(grown.ContentsMass+mix.TotalKg+carbon+oxygen+vapour,c.Seed+start,"Full crop gas/liquid/matter balance remains closed");
        }
        var legacy=new CropState{Water=10,Nutrients=.1};
        var oldFields=legacy.Save(); var copy=CropState.Read(oldFields); var absent=new NutrientSolution();
        Near(copy.Water,10,"Old water field unchanged"); Near(copy.Nutrients,.1,"Old dry nutrient stock stays dry");
        Near(absent.TotalKg,0,"Absent additive schema initializes no solution"); absent.Validate(copy);
        var solution=new NutrientSolution{Profile=NutrientSolution.Potato};
        double before=copy.ContentsMass;
        Near(solution.Blend(copy,.05),.05,"Mixing respects measured interval budget");
        Near(copy.ContentsMass+solution.TotalKg,before,"Partial mix retains unused inputs");
        var held = solution.Quantity;
        Near(solution.Blend(copy, LiquidDeliveryBudget.Kilograms(10,0,.05,.001)),0,"No received electricity cannot mix stock");
        check(MixtureTransfer.Same(held, solution.Quantity),"Unpowered mixing preserves both components");
        var starved=new CropState{Water=5,Nutrients=0};
        Near(new NutrientSolution{Profile=NutrientSolution.Potato}.Blend(starved,1),0,"Water alone cannot manufacture nutrients");
        var scarce=new CropState{Water=5,Nutrients=.02};
        var limited=new NutrientSolution{Profile=NutrientSolution.Potato};
        Near(limited.Blend(scarce,20),2.332,"Finite nutrients limit mixing to half a potato feed");
        Near(scarce.Nutrients,0,"Nutrient-limited mixing consumes only available stock");
        var dark=new CropState(); dark.Plant(Crop.Potato,1);
        held=limited.Quantity;
        dark.Step(.1,0,100,100,true,limited);
        check(MixtureTransfer.Same(held,limited.Quantity),"Unpowered cultivation cannot consume mixed feed for growth");
        var full=new CropState{Water=20,Nutrients=.04};
        Near(new NutrientSolution{Profile=NutrientSolution.Potato}.Blend(full,1),0,"Full water tank cannot create extra solution capacity");
        bool rejected=false; var bad=solution.Save(copy); bad["profile"]="unknown";
        try { NutrientSolution.Read(bad,copy); } catch {rejected=true;}
        check(rejected,"Unknown formulation is protected, not converted");
        rejected=false; bad=solution.Save(copy); bad["nutrients"]="0.4";
        try { NutrientSolution.Read(bad,copy); } catch {rejected=true;}
        check(rejected,"Composition drift cannot masquerade as a certified fresh profile");
        var boundary=new NutrientSolution{Profile=NutrientSolution.Potato,Quantity=NutrientSolution.Ratio(NutrientSolution.Potato).Scale((20+1e-12)/4.664)};
        boundary.Validate(new CropState());
        Near(boundary.PlainWaterCapacity,0,"Accepted floating-point boundary never exposes negative reservoir capacity");
    }
}
