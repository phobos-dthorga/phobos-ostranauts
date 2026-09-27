using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Registration;
using Phobos.Ostranauts.Framework.Trading;

namespace PhobosAgriculture;
internal static class BulkDefinitions
{
    internal const string Tank="PhobosVerdemorrowGroundworkR3",Nutrients="PhobosVerdemorrowGroundworkBulkNutrients",Controls="PhobosAgricultureBulkControls";
    internal const double CapacityKg=120,DryKg=25,Price=450,NutrientKg=.5,NutrientPrice=750,WaterPricePerKg=10;
    internal const int TankStock=4,NutrientStock=8;
    internal static bool IsTank(CondOwner? co)=>co!=null && EquipmentIdentity.IsFamily(co.strCODef,Tank);
    internal static readonly string[] Work={"bulk-load","bulk-recover","bulk-drain"};
    internal static string WorkId(string action)=>"PhobosAgriculture_"+action;
    internal static void Add(NativeDefinitions d)
    {
        var controls=NativeDefinitions.Clone(d.Interactions[Definitions.Controls]);controls.strName=Controls;d.Interactions[Controls]=controls;
        foreach(var action in Work)
        {
            var work=NativeDefinitions.Clone(controls);work.strName=WorkId(action);work.strTitle=work.strTooltip=Text.Get(action);work.fDuration=(action=="bulk-load"?10:60)/3600d;work.strAnim="Tablet";work.strActionGroup="Work";d.Interactions[work.strName]=work;
            Phobos.Ostranauts.Framework.Crew.CrewSpecialities.RegisterPractical(work.strName,"Agriculture");
        }
        ApplianceDefinitions.Add(d,Tank,Text.Get("bulk_tank"),Text.Get("bulk_tank_desc"),3,DryKg,Price,"phobos/agriculture/Reservoir",Controls,0);
        d.Power.Remove(Tank+"Power");
        foreach(var form in new[]{"Installed","Loose","InstalledDmg","LooseDmg"})
        {
            var co=d.Objects[Tank+form];co.jsonPI=null;co.aTickers=Array.Empty<string>();
            co.mapPoints=new[]{"use,0,-32","PhobosBulkOut,24,0"};
            if(form == "Installed")co.aInteractions=co.aInteractions.Concat(Work.Select(WorkId)).ToArray();
            string image="phobos/agriculture/Reservoir"+(form.Contains("Loose")?"Loose":"")+(form.EndsWith("Dmg")?"Damaged":"");
            co.strPortraitImg=image;d.Items[co.strItemDef].strImg=image;d.Items[co.strItemDef].strImgNorm=image+"Normal";
            if(form.EndsWith("Dmg"))
            { d.Installables[co.strName+"Repair"].aInputs=new[]{"TIsPartsMechSmall=1x1","TIsPartsElecSmall=1x1","TIsScrapAluminum=1x1"};MaintenanceDefinitions.SetStat(co,"StatRepairProgressMax",1800); }
            string waste=Tank+(form.EndsWith("Dmg")?"Broken":"")+"HousingWaste";int scraps=form.EndsWith("Dmg")?1:4;
            if(!d.Objects.ContainsKey(waste))MaintenanceDefinitions.Remainder(d,waste,Text.Get("housing_waste"),DryKg-scraps);
            MaintenanceDefinitions.Dismantle(d,co.strName,600,Enumerable.Repeat("ItmScrapSteel",scraps).Concat(new[]{waste}).ToArray());
        }
        Definitions.Stock(d,Nutrients,NutrientKg,NutrientPrice,"bulk_nutrients",false);
        MaintenanceDefinitions.SetStat(d.Objects[Nutrients],"IsCategoryIndustrialProducts",1);
        foreach(string merchant in new[]{"ItmOKLGSupplyKioskInv","ItmOKLGFixer","ItmTraderSanDiegoHalvorsonInv"})
        {
            MarketStock.Add(d,merchant,Tank+merchant,Tank+"Loose",1,StockCondition.Pristine,TankStock);
            MarketStock.Add(d,merchant,Nutrients+merchant,Nutrients,1,StockCondition.Pristine,NutrientStock);
        }
    }
}
