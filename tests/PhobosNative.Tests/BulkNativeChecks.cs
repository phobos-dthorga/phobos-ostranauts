using System;
using System.IO;
using System.Linq;
using PhobosAgriculture;
using Phobos.Ostranauts.Framework.Registration;

internal static class BulkNativeChecks
{
    internal static void Run(NativeDefinitions d,string repo,Action<bool,string> check)
    {
        double Stat(JsonCondOwner co,string key)=>double.Parse(co.aStartingConds.Single(s=>s.StartsWith(key+"=")).Split('x').Last(),System.Globalization.CultureInfo.InvariantCulture);
        foreach(string form in new[]{"Installed","Loose","InstalledDmg","LooseDmg"})
        {
            var co=d.Objects[BulkDefinitions.Tank+form];var item=d.Items[co.strItemDef];
            check(item.nCols==3&&co.inventoryWidth==3&&co.inventoryHeight==3,"R3 placement and loose bounds agree at three tiles");
            check(co.jsonPI==null&&co.aTickers.Length==0,"Passive R3 never gets a duplicate pump/electricity ticker");
            check(Stat(co,"StatMass")==25,"R3 begins empty at its physical dry mass");
            check(Stat(co,"StatBasePrice")== (form.EndsWith("Dmg")?90:450),"R3 authored condition prices");
            check(d.Installables.ContainsKey(co.strName+(form.StartsWith("Loose")?"Install":"Uninstall")),"R3 retains native installation workflow");
            check(Math.Abs(d.Installables[co.strName+"Dismantle"].aLootCOs.Sum(id=>Stat(d.Objects.TryGetValue(id,out var product)?product:DataHandler.dictCOs[id],"StatMass"))-25)<1e-8,"R3 dismantling preserves the complete housing mass");
            foreach(var key in new[]{item.strImg,item.strImgNorm})
            {
                var png=File.ReadAllBytes(Path.Combine(repo,"mods/PhobosAgriculture/images",key+".png"));
                int Size(int offset)=>(png[offset]<<24)|(png[offset+1]<<16)|(png[offset+2]<<8)|png[offset+3];
                check(Size(16)==48&&Size(20)==48,"R3 colour and normal use native 48-pixel bounds");
            }
        }
        // The R4 and R5: the R3's pattern at 4 x 4 and 5 x 5, each its own registered water vessel.
        foreach(var size in BulkDefinitions.Sizes.Skip(1))
        foreach(string form in new[]{"Installed","Loose","InstalledDmg","LooseDmg"})
        {
            var co=d.Objects[size.Prefix+form];var item=d.Items[co.strItemDef];
            check(item.nCols==size.Footprint&&co.inventoryWidth==size.Footprint&&co.jsonPI==null&&Stat(co,"StatMass")==size.DryKg,"Each larger reservoir is a passive vessel of its own footprint at its dry mass: "+size.Prefix+form);
            check(co.strNameFriendly.StartsWith("Phobos' Verdemorrow Groundwork R"+size.Footprint+" ",StringComparison.Ordinal),"Each larger reservoir carries its Groundwork model: "+size.Prefix+form);
            check(Math.Abs(d.Installables[co.strName+"Dismantle"].aLootCOs.Sum(id=>Stat(d.Objects.TryGetValue(id,out var product)?product:DataHandler.dictCOs[id],"StatMass"))-size.DryKg)<1e-8,
                "Each larger reservoir's dismantling preserves its housing mass: "+size.Prefix+form);
            check(Phobos.Ostranauts.Framework.Liquids.BulkVessels.SpecFor(co.strName)?.CapacityKg==size.CapacityKg&&Phobos.Ostranauts.Framework.Liquids.BulkVessels.SpecFor(co.strName)?.Commodity=="water",
                "Each larger reservoir is its own registered water vessel: "+size.Prefix+form);
        }
        check(BulkDefinitions.Sizes[0].Spec.Record=="AgricultureBulk"&&BulkDefinitions.Sizes[1].CapacityKg==235&&BulkDefinitions.Sizes[2].CapacityKg==400,
            "The R3 keeps its saved record name; the R4 holds 235 kg and the R5 400 kg");
        check(BulkDefinitions.ReserveChoices(120).SequenceEqual(new double[]{0,5,10,20,40,80,120}),"The R3's reserve steps are unchanged");
        var charge=d.Objects[BulkDefinitions.Nutrients];
        check(Stat(charge,"StatMass")==.5&&Stat(charge,"StatBasePrice")==750&&charge.nStackLimit==3,"Finite bulk charge has an individual physical identity and authored price");
        check(charge.inventoryWidth==1&&charge.inventoryHeight==1&&charge.aUpdateCommands.Length==0,"Bulk nutrient charge fits one slot and cannot regenerate through a native damage mode");
        check(typeof(GUIStationRefuel).GetMethod("SetupFields",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!=null,"Audited station entry hook exists");
        check(typeof(CrewSim).GetMethod(nameof(CrewSim.ScheduleCODestruction))?.GetParameters()[0].Name=="coToDestroy","Destruction guard binds the inspected argument");
        check(typeof(CondOwner).GetMethod(nameof(CondOwner.ModeSwitch))?.GetParameters()[0].Name=="coNew","Containment mode-switch guard binds the inspected argument");
    }
}
