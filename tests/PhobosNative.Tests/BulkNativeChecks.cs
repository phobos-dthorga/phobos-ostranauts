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
        var charge=d.Objects[BulkDefinitions.Nutrients];
        check(Stat(charge,"StatMass")==.5&&Stat(charge,"StatBasePrice")==750&&charge.nStackLimit==1,"Finite bulk charge has an individual physical identity and authored price");
        check(charge.inventoryWidth==1&&charge.inventoryHeight==1&&charge.aUpdateCommands.Length==0,"Bulk nutrient charge fits one slot and cannot regenerate through a native damage mode");
        check(typeof(GUIStationRefuel).GetMethod("SetupFields",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!=null,"Audited station entry hook exists");
        check(typeof(CrewSim).GetMethod(nameof(CrewSim.ScheduleCODestruction))?.GetParameters()[0].Name=="coToDestroy","Destruction guard binds the inspected argument");
        check(typeof(CondOwner).GetMethod(nameof(CondOwner.ModeSwitch))?.GetParameters()[0].Name=="coNew","Containment mode-switch guard binds the inspected argument");
    }
}
