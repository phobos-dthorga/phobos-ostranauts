using System;
using System.Collections.Generic;
using PhobosAgriculture.Core;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Persistence;

internal static class NutrientRecoveryChecks
{
    internal static void Run(Action<bool,string> check)
    {
        void Near(double a,double b,string message)=>check(Math.Abs(a-b)<1e-8,message);
        void Reject(Action action,string message){bool rejected=false;try{action();}catch(ArgumentException){rejected=true;}check(rejected,message);}
        foreach(var crop in new[]{Crop.Get("potato"),Crop.Get("lettuce"),Crop.Get("lettuce-seed")})
        {
            var state=new CropState{Water=20,Nutrients=.5};state.Plant(crop,1);
            Near(NutrientRecovery.Allocation(state,state.Biomass),0,"Clearing seed before growth cannot manufacture recovered nutrients");
            state.Step(1,crop.KW/2,10,10,true);
            Near(NutrientRecovery.Allocation(state,state.Biomass),crop.Nutrient/crop.Hours/2,"Allocation follows actual partial-power growth inputs");
            for(int n=0;n<crop.Hours+1;n++)state.Step(1,crop.KW,10,10,true);
            var harvest=state.Harvest();double allocated=NutrientRecovery.Allocation(state,harvest.ResidueKg),concentrate=allocated*NutrientRecovery.Fraction;
            check(allocated<=crop.Nutrient && allocated<=harvest.ResidueKg,"Recovery never exceeds consumed nutrients or physical residue");
            Near(concentrate+(harvest.ResidueKg-concentrate),harvest.ResidueKg,"Recovery retains all non-concentrate material");
            var supplement=new NutrientCharge(.04,.04);var leftover=supplement.Spend(concentrate);
            Near(concentrate+.04,concentrate*2+leftover.Remaining,"Formulation retains every kilogram including unused makeup");
            var record=NutrientRecovery.Record(harvest.ResidueKg,allocated);
            Near(NutrientRecovery.Read(record,harvest.ResidueKg),allocated,"Residue record survives save serialization values");
            Reject(()=>NutrientRecovery.Read(record,harvest.ResidueKg+1),"Changed physical mass cannot reuse an old nutrient allocation");
            var legacy=state.Save();legacy.Remove("recoveryRevision");
            Near(NutrientRecovery.Allocation(CropState.Read(legacy),harvest.ResidueKg),0,"Historic cohorts keep uncharacterized residue");
            check(CropState.Read(state.Save()).RecoveryRevision==1,"New cohort allocation revision survives reload");
        }
        // The straw press (Agriculture 0.44.0): residue records name their organic matter; older records still read.
        {
            var wheat=new CropState{Water=20,Nutrients=.5};wheat.Plant(Crop.Get("wheat"),1);
            for(int n=0;n<200&&!wheat.Ready;n++)wheat.Step(1,Crop.Get("wheat").KW,10,10,true);
            var h=wheat.Harvest();double nutrient=NutrientRecovery.Allocation(wheat,h.ResidueKg),organic=NutrientRecovery.Organic(wheat,h.ResidueKg);
            check(organic>0&&organic+nutrient<=h.ResidueKg+1e-9,"A harvest's residue records its organic matter within its mass");
            var full=NutrientRecovery.ReadFull(NutrientRecovery.Record(h.ResidueKg,nutrient,organic),h.ResidueKg);
            Near(full.Nutrient,nutrient,"The organic record keeps the nutrient");check(full.Organic is double o&&Math.Abs(o-organic)<1e-12,"The organic record survives reload");
            check(NutrientRecovery.ReadFull(NutrientRecovery.Record(h.ResidueKg,nutrient),h.ResidueKg).Organic==null,"A record from before 0.44.0 has no organic matter and cannot be pressed");
            Reject(()=>NutrientRecovery.Record(1,.5,.6),"Organic matter and nutrients cannot exceed the item's mass");
            var press=new StrawPress();double pressed=0;
            for(int n=0;n<3;n++){press.Absorb(h.ResidueKg,nutrient,organic);pressed+=h.ResidueKg;}
            Near(press.TotalKg,pressed,"The press holds every kilogram it takes");
            press.Absorb(2,.06,1.5);pressed+=2;
            double dried=press.Dry();double kept=press.TotalKg;Near(kept+dried,pressed,"Drying only moves water out, to the tank");
            Near(press.Water,press.Organic*StrawPress.BaleWaterKg/StrawPress.BaleOrganicKg,"Dried straw keeps the bale's share of water");
            int bales=press.Bales;check(bales>=1,"Enough dried straw makes a bale");
            press.Pack(bales);Near(press.TotalKg+bales*StrawPress.BaleKg,kept,"Bales take exactly their mass out of the press");
            check(press.Bales==0,"The press stops when no whole bale is left");
            var read=StrawPress.Read(press.Save());Near(read.TotalKg,press.TotalKg,"The press record survives reload");
            Reject(()=>new StrawPress().Absorb(StrawPress.CapacityKg+1,0,1),"The press refuses more than it holds");
            Near(StrawPress.BaleOrganicKg+StrawPress.BaleMineralsKg+StrawPress.BaleWaterKg,StrawPress.BaleKg,"A bale's parts make its kilogram");
            var wet=new StrawPress();wet.Absorb(1,.03,.2);check(wet.Bales==0&&wet.Surplus>0,"Too little straw for a bale still dries");
            var scutch=WorkupJob.Read(new WorkupJob{Mode="scutch",Input="flax1",Energy=.02}.Save());check(scutch.Mode=="scutch"&&scutch.Input=="flax1"&&Math.Abs(scutch.Energy-.02)<1e-12,"A flax scutching job survives reload");
            Reject(()=>new WorkupJob{Mode="scutch",Input="flax1",Supplement="salts"}.Save(),"Scutching takes no supplement");
            check(WorkupJob.Read(new WorkupJob{Mode="sugar",Input="beet1"}.Save()).Mode=="sugar","A sugar extraction job survives reload");
            Reject(()=>new WorkupJob{Mode="ferment",Input="beet1"}.Save(),"An unknown bench job is refused");
        }
        var charge=new NutrientCharge(.04,.04);double spent=0;
        for(int n=0;n<100;n++){charge=charge.Spend(.0001);spent+=.0001;charge=NutrientCharge.Read(charge.Save(),charge.Remaining);}
        Near(charge.Remaining+spent,.04,"Many small doses and reloads preserve the original charge");
        Reject(()=>NutrientCharge.Read(charge.Save(),.04),"Restoring native mass cannot refill a spent charge");
        Reject(()=>charge.Spend(-1),"Negative dose cannot refill nutrients");
        Reject(()=>charge.Spend(charge.Remaining+1),"Dose cannot overdraw contents");
        Reject(()=>new NutrientCharge(.04,double.NaN),"Nonfinite charge rejected");
        var job=new WorkupJob{Mode="formulate",Input="exact-source",Supplement="exact-makeup",Energy=.001};
        var loaded=WorkupJob.Read(job.Save());check(loaded.Input==job.Input && loaded.Supplement==job.Supplement && loaded.Energy==job.Energy,"Workup retains exact input identities and paid energy");
        foreach(var work in new[]{new WorkupJob(),new WorkupJob{Mode="recover",Input="exact-residue",Energy=.007},job})
        {
            var maps=new Dictionary<string,Dictionary<string,string>>();
            var store=new ObjectStateStore(maps,"AgricultureWorkup","test",1);
            check(store.TryWrite(work.Save()),"Idle, recovery and formulation workup fit the real state envelope");
            check(store.Read(out var fields)==SavedStateStatus.Ready,"Workup envelope reads as valid");
            var roundTrip=WorkupJob.Read(fields);
            check(roundTrip.Mode==work.Mode&&roundTrip.Input==work.Input&&roundTrip.Supplement==work.Supplement&&roundTrip.Energy==work.Energy,
                "Workup envelope preserves exact bindings and paid energy including absent inputs");
            maps["PhobosState.AgricultureWorkup"]["schema"]="2";
            check(!store.TryWrite(new WorkupJob().Save())&&maps["PhobosState.AgricultureWorkup"]["schema"]=="2","Future workup state remains untouched");
        }
        foreach(var source in new[]{"","exact-dosing-charge"})
        {
            var maps=new Dictionary<string,Dictionary<string,string>>();
            var store=new ObjectStateStore(maps,"AgricultureDosing","test",1);
            check(store.TryWrite(DosingBinding.Save(source)),"Absent or exact dosing choice fits the real state envelope");
            check(store.Read(out var fields)==SavedStateStatus.Ready&&DosingBinding.Read(fields)==source,"Dosing choice survives envelope roundtrip");
            maps["PhobosState.AgricultureDosing"]["owner"]="another-provider";
            check(!store.TryWrite(DosingBinding.Save(""))&&maps["PhobosState.AgricultureDosing"]["data.source"]==fields["source"],"Foreign dosing state is preserved");
        }
        Reject(()=>DosingBinding.Save("none"),"Reserved sentinel cannot bind a physical dosing item");
        Reject(()=>DosingBinding.Save(new string('x',201)),"Oversized dosing identity remains invalid");
        Reject(()=>DosingBinding.Read(new Dictionary<string,string>{{"source",""}}),"Malformed empty stored binding is not silently recovered");
        Reject(()=>new WorkupJob{Mode="recover",Input="none"}.Save(),"Reserved workup input is not a physical item");
        Reject(()=>new WorkupJob{Mode="recover",Input="unsafe=source"}.Save(),"Unsafe workup identity rejected before envelope mutation");
        Reject(()=>new WorkupJob{Energy=.001}.Save(),"Idle workup cannot retain unexplained paid energy");
        Reject(()=>new WorkupJob{Mode="future",Input="source"}.Save(),"Unknown job cannot run");
        Near(RecyclerRejectBudget.Rejected(10,6,4),4,"Recycler captures wet remainder, not dry nutrient");
        Near(RecyclerRejectBudget.Seconds(100,1,.6,4),10,"Full reservation bounds provider processing time");
        Near(RecyclerRejectBudget.Seconds(100,1,.6,0),0,"No reject capacity prevents source consumption");
        Near(RecyclerRejectBudget.Rejected(10,10,0),0,"100 percent provider recovery cannot create rejects");
        Reject(()=>RecyclerRejectBudget.Rejected(10,6,3),"Excess native rejection cannot exceed reserved storage");
        Reject(()=>RecyclerRejectBudget.Rejected(6,10,4),"Impossible provider receipt is protected");
        Reject(()=>RecyclerRejectBudget.Seconds(1,1,double.NaN,1),"Invalid provider configuration is protected");
    }
}
