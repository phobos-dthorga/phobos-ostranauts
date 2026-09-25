using System;
using System.Collections.Generic;
using System.Linq;
using PhobosShipbreaker.Core;
using Phobos.Ostranauts.Framework.Persistence;
using Phobos.Ostranauts.Framework.Registration;
using Phobos.Ostranauts.Framework.Trading;
using Phobos.Ostranauts.Framework.Inventory;
using Ostranauts.Inventory;

namespace PhobosShipbreaker;
internal static partial class FurnaceService
{
    internal const string CoolantStock="PhobosRivetlineCoolantCharge", CoolantWaste="PhobosRivetlineRetainedCoolant";
    private static ObjectStateStore ChargeStore(CondOwner co)=>new(co.mapGUIPropMaps,"FurnaceCoolantCharge",Text.Owner,1);
    private static ObjectStateStore ChargeJournal(CondOwner co)=>new(co.mapGUIPropMaps,"FurnaceCoolantService",Text.Owner,1);
    private static void ReadCharge(Session s)
    {
        if(!FurnaceRules.Machine(s.Object.strCODef))return;
        try {
            var status=ChargeStore(s.Object).Read(out var d);
            if(status==SavedStateStatus.Ready)s.Coolant=CoolantCharge.Read(d);
            else if(status!=SavedStateStatus.Missing)s.Protected=true;
            s.AccountedCoolantKg=s.Coolant.TotalKg;
            status=ChargeJournal(s.Object).Read(out d);
            if(status!=SavedStateStatus.Missing&&(status!=SavedStateStatus.Ready||d.Count!=1||!d.TryGetValue("state",out var value)||value!="clear"))s.Protected=true;
        }catch{s.Protected=true;}
    }
    private static void SaveCharge(Session s)
    {
        if(!FurnaceRules.Machine(s.Object.strCODef))return;
        if(!ChargeStore(s.Object).TryWrite(s.Coolant.Save())) {s.Protected=true;throw new InvalidOperationException("Protected coolant charge.");}
        s.Object.AddMass(s.Coolant.TotalKg-s.AccountedCoolantKg,true);s.AccountedCoolantKg=s.Coolant.TotalKg;
    }
    private static int ChargeCells(Session s)
    {var peer=SelectedCooling(s.Object);return peer!=null&&Routed(s.Object)&&CoolantRoute(s.Object,peer,out var count)?count:0;}
    private static bool ChargeReady(Session s)=>!s.Coolant.Enabled||ChargeCells(s) is int n&&n>0&&s.Coolant.Flow(n)>0;
    private static string ChargeStatus(Session s)
    {int cells=ChargeCells(s);return Text.Get("Furnace.charge_status",s.Coolant.Enabled?Text.Get("Furnace.charge_managed"):Text.Get("Furnace.charge_legacy"),s.Coolant.CleanKg,s.Coolant.CapturedKg,cells>0?s.Coolant.PressureKPa(cells):0,s.Coolant.PrimeSeconds);}
    private static bool ChargeCommand(Session s,string action,bool local,out string message)
    {
        message=Text.Get("Furnace.charge_service");var co=s.Object;
        if(!local||!Mounted(co)||co.HasCond("IsLocked")||action!="coolant-drain"&&!Intact(co)||!FurnaceRules.Machine(co.strCODef)||!Routed(co)||!s.State.Batch.SafeOpen||s.State.Batch.Phase!=FurnacePhase.Idle||s.State.Batch.Armed||co.objContainer==null||UnsafeRepair(co))return false;
        var peer=SelectedCooling(co);if(peer!=null&&UnsafeRepair(peer))return false;
        if(action=="coolant-managed"||action=="coolant-sealed")
        {if(s.Coolant.TotalKg>1e-9)return false;s.Coolant.Enabled=action=="coolant-managed";s.Coolant.PrimeSeconds=0;Save(s);return true;}
        if(!s.Coolant.Enabled)return false;
        if(action=="coolant-fill")
        {
            var item=co.objContainer.ContainedCOs.FirstOrDefault(c=>c.strCODef==CoolantStock&&!c.bDestroyed&&c.coStackHead==null&&c.aStack.Count==0&&c.GetCOsSafe(true).Count==0&&c.GetLotCOs(true).Count==0&&Math.Abs(c.GetTotalMass()-1)<1e-7);
            if(item==null||s.Coolant.TotalKg+1>CoolantCharge.CapacityKg+1e-9)return false;
            if(!ChargeJournal(co).TryWrite(new Dictionary<string,string>{["state"]="pending",["input"]=item.strID}))return false;
            s.State.NativeMutation=true;
            item.RemoveFromCurrentHome(true);if(item.objCOParent!=null||item.ship!=null)throw new InvalidOperationException("Coolant charge did not detach.");
            s.Coolant.CleanKg+=1;Save(s);item.Destroy();
        }
        else if(action=="coolant-drain")
        {
            if(s.Coolant.TotalKg<=0)return false;
            var product=DataHandler.GetCondOwner(CoolantWaste);bool pending=false;
            try {
                product.SetCondAmount("StatMass",s.Coolant.TotalKg);
                var grid=co.objContainer.gridLayout;var occupied=new bool[grid.gridMaxX,grid.gridMaxY];
                for(int x=0;x<grid.gridMaxX;x++)for(int y=0;y<grid.gridMaxY;y++)occupied[x,y]=grid.gridID[x,y]!=null||grid.gridInventoryItem[x,y]!=null;
                var size=GUIInventoryItem.GetWidthHeightForCO(product);var plan=BatchPlacement.Plan(occupied,new[]{new ItemSize(size.x,size.y)});
                if(plan==null||!co.objContainer.AllowedCO(product))return false;
                if(!ChargeJournal(co).TryWrite(new Dictionary<string,string>{["state"]="pending",["output"]=product.strID}))return false;
                s.State.NativeMutation=true;
                pending=true;co.objContainer.AddCOSimple(product,new PairXY(plan[0].X,plan[0].Y));
                if(product.objCOParent!=co)throw new InvalidOperationException("Coolant drain placement failed.");
                s.Coolant.CleanKg=s.Coolant.CapturedKg=s.Coolant.PrimeSeconds=0;Save(s);
            }finally{if(!pending&&!product.bDestroyed)product.Destroy();}
        }
        else return false;
        s.State.NativeMutation=false;Save(s);
        if(!ChargeJournal(co).TryWrite(new Dictionary<string,string>{["state"]="clear"}))throw new InvalidOperationException("Coolant service completion failed.");
        co.objContainer.Redraw();message=ChargeStatus(s);return true;
    }
    internal static void AddCoolantStock(NativeDefinitions d)
    {
        foreach(string id in new[]{CoolantStock,CoolantWaste})
        {
            var co=NativeDefinitions.Clone(DataHandler.dictCOs["ItmScrapTrash"]);co.strName=id;co.strNameFriendly=co.strNameShort=Text.Get(id==CoolantStock?"Furnace.charge_item":"Furnace.charge_waste");co.strDesc=Text.Get("Furnace.charge_desc");
            co.nStackLimit=1;co.inventoryWidth=co.inventoryHeight=1;co.aTickers=co.aUpdateCommands=Array.Empty<string>();co.aStartingConds=new[]{"IsSolid=1x1","IsPocketable=1x1"};Content.SetStat(co,"StatMass",1);Content.SetStat(co,"StatBasePrice",id==CoolantStock?20:.01);d.Objects[id]=co;
        }
        foreach(string merchant in new[]{"ItmOKLGSupplyKioskInv","ItmOKLGFixer","ItmTraderSanDiegoHalvorsonInv"})MarketStock.Add(d,merchant,"PhobosCoolantStock_"+merchant,CoolantStock,1,StockCondition.Pristine);
    }
}

// Both furnace damage forms retain the same dry mass and physical inventories.
// Native mode replacement rebuilds definition mass; keep the filled service assembly.
[HarmonyLib.HarmonyPatch(typeof(CondOwner), nameof(CondOwner.ModeSwitch))]
internal static class CoolantModeMass
{
    private static void Prefix(CondOwner __instance,CondOwner coNew,out double? __state)
    { __state=FurnaceRules.Machine(__instance.strCODef)&&FurnaceRules.Machine(coNew.strCODef)&&FurnaceService.Get(__instance).Coolant.TotalKg>0 ? __instance.GetCondAmount("StatMass") : null; }
    private static void Postfix(CondOwner coNew,double? __state)
    { if(__state.HasValue) coNew.AddMass(__state.Value-coNew.GetCondAmount("StatMass"),true); }
}
