using System;
using System.Collections.Generic;
using PhobosAgriculture.Core;
using Phobos.Ostranauts.Framework.Liquids;

internal static class NutrientRecoveryChecks
{
    internal static void Run(Action<bool,string> check)
    {
        void Near(double a,double b,string message)=>check(Math.Abs(a-b)<1e-8,message);
        void Reject(Action action,string message){bool rejected=false;try{action();}catch(ArgumentException){rejected=true;}check(rejected,message);}
        foreach(var crop in new[]{Crop.Potato,Crop.Lettuce,Crop.LettuceSeed})
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
        var reservoir=new CropState{Water=20,Running=true};var solution=new NutrientSolution{Profile=NutrientSolution.Potato};
        Near(NutrientCharge.DoseAllowance(reservoir,solution,1),0,"Full liquid capacity does not consume a physical charge");
        reservoir.Water=10;check(NutrientCharge.DoseAllowance(reservoir,solution,1)>0,"Available water and measured blending work admit dosing");
        Near(NutrientCharge.DoseAllowance(reservoir,solution,0),0,"No electricity budget consumes nothing");
        reservoir.Running=false;Near(NutrientCharge.DoseAllowance(reservoir,solution,1),0,"Paused W2 consumes nothing");
        reservoir.Running=true;reservoir.Water=0;Near(NutrientCharge.DoseAllowance(reservoir,solution,1),0,"Dry W2 consumes nothing");
        var charge=new NutrientCharge(.04,.04);double spent=0;
        for(int n=0;n<100;n++){charge=charge.Spend(.0001);spent+=.0001;charge=NutrientCharge.Read(charge.Save(),charge.Remaining);}
        Near(charge.Remaining+spent,.04,"Many small doses and reloads preserve the original charge");
        Reject(()=>NutrientCharge.Read(charge.Save(),.04),"Restoring native mass cannot refill a spent charge");
        Reject(()=>charge.Spend(-1),"Negative dose cannot refill nutrients");
        Reject(()=>charge.Spend(charge.Remaining+1),"Dose cannot overdraw contents");
        Reject(()=>new NutrientCharge(.04,double.NaN),"Nonfinite charge rejected");
        var job=new WorkupJob{Mode="formulate",Input="exact-source",Supplement="exact-makeup",Energy=.001};
        var loaded=WorkupJob.Read(job.Save());check(loaded.Input==job.Input && loaded.Supplement==job.Supplement && loaded.Energy==job.Energy,"Workup retains exact input identities and paid energy");
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
