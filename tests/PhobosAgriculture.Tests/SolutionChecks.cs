using System;
using System.Collections.Generic;
using System.Linq;
using PhobosAgriculture.Core;
using Phobos.Ostranauts.Framework.Liquids;

/// <summary>One nutrient for every crop (Agriculture 0.55.0): how a W2 feeds, what the growth section says, and how
/// the per-crop feeds of 0.5.0 to 0.54.0 convert.</summary>
internal static class SolutionChecks
{
    internal static void Run(Action<bool,string> check)
    {
        void Near(double a,double b,string message)=>check(Math.Abs(a-b)<1e-9,message+$": {a} vs {b}");
        void Reject(Action action,string message){bool refused=false;try{action();}catch(ArgumentException){refused=true;}catch(FormatException){refused=true;}check(refused,message);}

        // The W2's feeding rule.
        Near(NutrientFeed.Need(0,.1),.1,"An empty rack wants the whole target");
        Near(NutrientFeed.Need(.04,.1),.06,"A part-fed rack wants the rest");
        Near(NutrientFeed.Need(.2,.1),0,"A rack above the target wants nothing");
        Near(NutrientFeed.Need(.45,2),.05,"The target never passes what a rack holds");
        Near(NutrientFeed.Send(.1,.5,2,.01),.02,"Two kilograms of water carry twenty grams");
        Near(NutrientFeed.Send(.1,.5,50,.01),.1,"The stream carries no more than the rack needs");
        Near(NutrientFeed.Send(.1,.03,50,.01),.03,"The stream carries no more than the W2 holds");
        Near(NutrientFeed.Send(.1,.5,0,.01),0,"Water that did not move carries nothing");
        Near(NutrientFeed.Send(0,.5,5,.01),0,"A fed rack takes nothing");
        // Recirculation: a full rack still gets its nutrients, the pump paying for the water it turns over.
        Near(NutrientFeed.Recirculate(.1,.5,0,.01,100),10,"Topping a full rack by 0.1 kg turns over 10 kg");
        Near(NutrientFeed.Recirculate(.1,.5,0,.01,3),3,"Recirculation stays inside what the pump may still move");
        Near(NutrientFeed.Recirculate(.1,.5,4,.01,100),6,"Fresh water already delivered counts toward the dose");
        Near(NutrientFeed.Recirculate(.1,.5,10,.01,100),0,"Fresh water that carried the whole dose needs no recirculation");
        Near(NutrientFeed.Recirculate(0,.5,0,.01,100),0,"A fed rack is not recirculated, so a fed farm costs nothing extra");
        Near(NutrientFeed.Recirculate(.1,0,0,.01,100),0,"A W2 with no nutrients does not recirculate");
        Near(NutrientFeed.Recirculate(.1,.5,0,.01,0),0,"No pump budget moves nothing");
        Near(NutrientFeed.Recirculate(.1,.5,0,.01,double.NaN),0,"A bad budget moves nothing");
        {
            // A step: 1.5 kg may move; the rack takes 0.5 kg of fresh water and the rest is recirculated.
            double w2=.5,fed=0,budget=1.5,fresh=.5,need=NutrientFeed.Need(fed,.1);
            double round=NutrientFeed.Recirculate(need,w2,fresh,.01,budget-fresh),sent=NutrientFeed.Send(need,w2,fresh+round,.01);
            Near(round,1,"The rest of the step's budget recirculates");Near(sent,.015,"The step feeds what the moved water carries");
            w2-=sent;fed+=sent;Near(w2+fed,.5,"Feeding moves nutrients; it makes and loses none");
        }
        // The W2 stocking itself from a charge or hopper.
        Near(NutrientFeed.Dose(0,.5,1),.5,"An empty W2 takes a whole bulk charge");
        Near(NutrientFeed.Dose(.3,.5,1),.2,"A W2 takes only what it has room for");
        Near(NutrientFeed.Dose(.5,.5,1),0,"A full W2 takes nothing");
        Near(NutrientFeed.Dose(0,.04,1),.04,"A W2 takes no more than its source holds");
        Near(NutrientFeed.Dose(0,.5,.01),.01,"A W2 takes no more than the step allows");
        Near(NutrientFeed.Dose(0,.5,0),0,"No budget stocks nothing");

        // The growth section: defaults, the shipped file, and one crop's own room.
        var plain=Growth.Room(null,null);
        check(plain.MinC==18&&plain.MaxC==31&&plain.MinKPa==70&&plain.MaxKPa==110,"With no growth section a crop's room is 18 to 31 C and 70 to 110 kPa");
        var live=GrowthRoom.For("wheat");
        check(live.Suits(299.55,97.7)&&live.Suits(291.15,70)&&live.Suits(304.15,110)&&!live.Suits(304.3,100)&&!live.Suits(290,100)&&!live.Suits(295,60)&&!live.Suits(295,111),
            "The shipped room: 18 to 31 C and 70 to 110 kPa, the owner's 26.4 C ship included");
        check(GrowthRoom.For(null).MaxC==31&&GrowthRoom.For("no-such-crop").MaxC==31,"An empty rack and an unknown crop use the shared room");
        check(Crops.All.All(c=>Crops.Pack.growth!.crops.ContainsKey(c.Id)),"Every shipped crop has a growth entry to tune");
        var tuned=new GrowthEntry{room=new RoomEntry{maxC=28}};tuned.crops["potato"]=new CropGrowthEntry{room=new RoomEntry{minC=10,maxKPa=120}};
        var potato=Growth.Room(tuned,"potato");var other=Growth.Room(tuned,"lettuce");
        check(potato.MinC==10&&potato.MaxC==28&&potato.MinKPa==70&&potato.MaxKPa==120,"A crop's own room replaces only what it gives");
        check(other.MinC==18&&other.MaxC==28&&other.MaxKPa==110,"Other crops keep the shared room");
        var ids=new HashSet<string>{"potato","lettuce"};Growth.Validate(tuned,ids);Growth.Validate(null,ids);
        Reject(()=>Growth.Validate(new GrowthEntry{room=new RoomEntry{minC=31}},ids),"A lower limit must be below the upper one");
        Reject(()=>Growth.Validate(new GrowthEntry{room=new RoomEntry{maxC=140}},ids),"A room hotter than 100 C is refused");
        Reject(()=>Growth.Validate(new GrowthEntry{room=new RoomEntry{minKPa=0}},ids),"A room with no pressure is refused");
        Reject(()=>Growth.Validate(new GrowthEntry{room=new RoomEntry{maxC=double.NaN}},ids),"A limit that is not a number is refused");
        {var g=new GrowthEntry();g.crops["rye"]=new CropGrowthEntry();Reject(()=>Growth.Validate(g,ids),"A room for a crop that is not in the file is refused");}
        {var g=new GrowthEntry();g.crops["potato"]=new CropGrowthEntry{room=new RoomEntry{maxC=12}};Reject(()=>Growth.Validate(g,ids),"A crop's own room must still leave a range");}
        Reject(()=>Growth.Validate(new GrowthEntry{stress=new StressEntry{healthLossPerHour=-.1}},ids),"A negative stress rate is refused");
        Reject(()=>Growth.Validate(new GrowthEntry{stress=new StressEntry{healthLossPerHourOutside=2}},ids),"A stress rate above 1 an hour is refused");
        Reject(()=>Growth.Validate(new GrowthEntry{stress=new StressEntry{plantWaterKg=25}},ids),"Planting water beyond what a rack holds is refused");
        Reject(()=>Growth.Validate(new GrowthEntry{nutrientTargetKg=0},ids),"A zero nutrient target is refused");
        Reject(()=>Growth.Validate(new GrowthEntry{nutrientTargetKg=.6},ids),"A target above what a rack holds is refused");
        Reject(()=>Growth.Validate(new GrowthEntry{feedStrengthKgPerKg=0},ids),"A zero feed strength is refused");
        Near(Growth.NutrientTargetKg,.1,"The shipped nutrient target");Near(Growth.FeedStrength,.01,"The shipped feed strength");
        var rules=Growth.Stress;
        check(rules.GraceHours==2&&rules.HealthLossPerHour==.01&&rules.HealthLossPerHourOutside==.1&&rules.HealthLossPerHourNoAir==.1&&rules.PlantWaterKg==.25,"The shipped stress rules are the figures the code held");
        {
            // Stress follows the rules it is given: no grace and a heavy rate cost health in the first dark hour.
            var kind=new CropState{Water=20,Nutrients=.5};kind.Plant(Crop.Get("potato"),1);
            var harsh=new CropState{Water=20,Nutrients=.5};harsh.Plant(Crop.Get("potato"),1);
            kind.Step(1,0,10,10,true);harsh.Step(1,0,10,10,true,1,new StressRules{GraceHours=0,HealthLossPerHour=.2});
            Near(kind.Health,1,"The shipped rules shrug off a first dark hour");Near(harsh.Health,.8,"Stress rules from the pack decide the health lost");
        }

        // Every crop grows from the plain stores alone, in balance.
        foreach(var c in Crops.All)
        {
            var grown=new CropState{Water=c.Water,Nutrients=c.Nutrient};double start=grown.ContentsMass;grown.Plant(c,1);
            double carbon=0,oxygen=0,vapour=0;
            for(int hour=0;hour<c.Hours*4&&!grown.Ready;hour++)
            {
                double h=Math.Min(1,(1-grown.Progress)*c.Hours);var e=grown.Step(h,h*c.KW,100,100,true);carbon+=e.CO2Kg;oxygen+=e.OxygenKg;vapour+=e.VapourKg;
            }
            check(grown.Ready,"Plain water and nutrients grow the crop: "+c.Id);
            check(Math.Abs(grown.ContentsMass+carbon+oxygen+vapour-c.Seed-start)<1e-7,"The crop's gas, water and matter balance stays closed: "+c.Id);
            check(grown.Nutrients<1e-7,"A crop uses its own nutrient budget and no more: "+c.Id);
        }

        // Old saves: the solution record of 0.5.0 to 0.54.0 folds into the plain stores, mass unchanged.
        var record=new Dictionary<string,string>{["profile"]="potato-v1",["water"]="4.624",["nutrients"]="0.04"};
        var held=LegacyFeed.ReadSolution(record);Near(held.CarrierKg,4.624,"An old solution's water is read");Near(held.SoluteKg,.04,"An old solution's nutrients are read");
        var rack=new CropState{Water=3,Nutrients=.02};double before=rack.ContentsMass+held.CarrierKg+held.SoluteKg;
        check(LegacyFeed.Fold(rack,held),"An old solution folds into the stores");
        Near(rack.Water,7.624,"Its water joins the water store");Near(rack.Nutrients,.06,"Its nutrients join the nutrient store");Near(rack.ContentsMass,before,"Folding changes no mass");
        var none=LegacyFeed.ReadSolution(new Dictionary<string,string>{["profile"]="water",["water"]="0",["nutrients"]="0"});
        check(none.CarrierKg==0&&none.SoluteKg==0,"A record of plain water holds nothing to fold");
        var brim=new CropState{Water=19,Nutrients=.49};
        check(!LegacyFeed.Fold(brim,held)&&brim.Water==19&&brim.Nutrients==.49,"A solution that cannot fit is left alone, changing nothing");
        var unknown=LegacyFeed.ReadSolution(new Dictionary<string,string>{["profile"]="rye-v1",["water"]="1",["nutrients"]="0.01"});
        Near(unknown.CarrierKg+unknown.SoluteKg,1.01,"An old solution under a name no crop carries is still water and nutrients");
        var empty=LegacyFeed.EmptySolution();
        check(empty["profile"]=="water"&&LegacyFeed.ReadSolution(empty).CarrierKg==0,"Machines now save an empty record old code can still read");
        Reject(()=>LegacyFeed.ReadSolution(new Dictionary<string,string>{["profile"]="water",["water"]="-1",["nutrients"]="0"}),"A negative solution is refused");
        Reject(()=>LegacyFeed.ReadSolution(new Dictionary<string,string>{["profile"]="water",["water"]="x",["nutrients"]="0"}),"A solution that is not a number is refused");
        Reject(()=>LegacyFeed.ReadSolution(new Dictionary<string,string>{["profile"]="water",["water"]="0",["nutrients"]="0",["extra"]="1"}),"Unknown solution fields are refused");

        // Old feed in parcels, pipes and canisters: its crop's own proportion of water and nutrients.
        foreach(var c in Crops.All)
        {
            var byFeed=LegacyFeed.Split(c.Feed,c.Water+c.Nutrient);var byPipe=LegacyFeed.Split(c.FeedCommodity,c.Water+c.Nutrient);
            check(byFeed is LiquidMixture a&&Math.Abs(a.CarrierKg-c.Water)<1e-9&&Math.Abs(a.SoluteKg-c.Nutrient)<1e-9,"An old feed splits into its crop's water and nutrients: "+c.Id);
            check(byPipe is LiquidMixture b&&Math.Abs(b.CarrierKg-c.Water)<1e-9&&Math.Abs(b.SoluteKg-c.Nutrient)<1e-9,"An old pipe feed splits the same way: "+c.Id);
        }
        var part=LegacyFeed.Split("potato feed",1.166)!.Value;Near(part.CarrierKg+part.SoluteKg,1.166,"Splitting part of a feed conserves its mass");
        Near(part.SoluteKg,1.166*.04/4.664,"Part of a feed keeps its proportion");
        check(LegacyFeed.Split("rye feed",1)==null&&LegacyFeed.Split("",1)==null&&LegacyFeed.Split(null,1)==null&&LegacyFeed.Split("potato feed",-1)==null&&LegacyFeed.Split("water",1)==null,
            "A name no crop carries, or a bad amount, is not converted");
        check(LegacyFeed.Commodities().SequenceEqual(Crops.All.Select(c=>c.FeedCommodity)),"Pipes still know every old feed name, so they can be drained");
    }
}
