using System;
using System.Collections.Generic;
using System.Linq;
using PhobosShipbreaker.Core;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Persistence;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Registration;
using Phobos.Ostranauts.Framework.Trading;
using Phobos.Ostranauts.Framework.Inventory;
using Ostranauts.Inventory;

namespace PhobosShipbreaker;
internal static partial class FurnaceService
{
    internal const string CoolantStock="PhobosRivetlineCoolantCharge", CoolantWaste="PhobosRivetlineRetainedCoolant";
    internal const int CoolantChargeStackLimit = 3;
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
        if(!ChargeStore(s.Object).TryWriteIfChanged(s.Coolant.Save())) {s.Protected=true;throw new InvalidOperationException("Protected coolant charge.");}
        s.Object.AddMass(s.Coolant.TotalKg-s.AccountedCoolantKg,true);s.AccountedCoolantKg=s.Coolant.TotalKg;
    }
    private static int ChargeCells(Session s)
    {var peer=SelectedCooling(s.Object);return peer!=null&&Routed(s.Object)&&CoolantRoute(s.Object,peer,out var count)?count:0;}
    private static bool ChargeReady(Session s)=>!s.Coolant.Enabled||ChargeCells(s) is int n&&n>0&&s.Coolant.Flow(n,CircuitFull(s))>0;
    private static string ChargeStatus(Session s)
    {
        int cells=ChargeCells(s);bool full=CircuitFull(s);var circuit=CoolantCircuit(s);
        double held=CoolantHolding==null?0:LineContents.Holding(circuit,CoolantCharge.Commodity),room=CoolantHolding==null?0:LineContents.Room(circuit,CoolantHolding,CoolantCharge.Commodity);
        return Text.Get("Furnace.charge_status",s.Coolant.Enabled?Text.Get("Furnace.charge_managed"):Text.Get("Furnace.charge_legacy"),s.Coolant.CleanKg,s.Coolant.CapturedKg,cells>0?s.Coolant.PressureKPa(full):0,s.Coolant.PrimeSeconds)+
            (s.Coolant.Enabled&&cells>0?"\n"+Text.Get(full?"Furnace.circuit_full":"Furnace.circuit_filling",held,held+room,Math.Max(0,room-s.Coolant.SurplusKg)):"");
    }
    /// <summary>The service guards every coolant action shares, and a canister pour too: mounted, unlocked, intact (drain
    /// excepted), piped, idle, open, unarmed, with a container, and no unsafe repair on the furnace or its assembly.</summary>
    /// <summary>Why the coolant cannot be serviced now, by name (Shipbreaker 0.85.0), or null. Armed heating is not a
    /// reason: the second press withdraws it.</summary>
    private static string? ChargeReason(Session s,bool drain)
    {
        var co=s.Object;var b=s.State.Batch;
        if(!FurnaceRules.Machine(co.strCODef))return Text.Get("Industry.unsupported_action");
        if(s.State.NativeMutation)return Text.Get("Furnace.protected");
        if(co.HasCond("IsLocked"))return Text.Get("Furnace.locked");
        if(!Mounted(co)||!drain&&!Intact(co)||co.objContainer==null)return Text.Get("Furnace.charge_install");
        if(!Routed(co))return Text.Get("Furnace.charge_route");
        if(b.Phase!=FurnacePhase.Idle)return Text.Get("Furnace.charge_batch");
        if(!b.SafeOpen||UnsafeRepair(co))return Text.Get("Furnace.charge_hot");
        var peer=SelectedCooling(co);
        return peer!=null&&UnsafeRepair(peer)?Text.Get("Furnace.charge_hot"):null;
    }
    private static bool ChargeServiceable(Session s,bool drain)
    {
        var co=s.Object;
        if(!Mounted(co)||co.HasCond("IsLocked")||!drain&&!Intact(co)||!FurnaceRules.Machine(co.strCODef)||!Routed(co)||!s.State.Batch.SafeOpen||s.State.Batch.Phase!=FurnacePhase.Idle||s.State.Batch.Armed||co.objContainer==null||UnsafeRepair(co))return false;
        var peer=SelectedCooling(co);return peer==null||!UnsafeRepair(peer);
    }
    /// <summary>A drain canister of coolant put in the furnace's Products pours into the reservoir (Shipbreaker 0.58.0),
    /// up to its capacity, through the same service journal and guards as loading a charge.</summary>
    internal static double AcceptCoolant(CondOwner co,double kg)
    {
        var s=Get(co);
        if(s.Protected||!s.Coolant.Enabled||!ChargeServiceable(s,false))return 0;
        double moved=Math.Min(kg,CoolantCharge.CapacityKg-s.Coolant.TotalKg);
        if(moved<=1e-9||!ChargeJournal(co).TryWrite(new Dictionary<string,string>{["state"]="pending",["input"]="drain canister"}))return 0;
        s.Coolant.CleanKg+=moved;Save(s);
        if(!ChargeJournal(co).TryWrite(new Dictionary<string,string>{["state"]="clear"}))throw new InvalidOperationException("Coolant service completion failed.");
        return moved;
    }
    private static bool ChargeCommand(Session s,string action,bool local,out string message)
    {
        var co=s.Object;
        if(!local){message=Text.Get("Industry.local_only");return false;}
        // The one condition that blocks, by name (Shipbreaker 0.85.0); heating permission alone is withdrawn on the second
        // press, and the furnace's own resume stays explicit.
        if(ChargeReason(s,action=="coolant-drain") is string blocked){message=blocked;return false;}
        if(s.State.Batch.Armed)
        {
            if(!Confirmations.Ask(Text.Get("Overrides.stop_furnace",ObjectPresentation.Name(co)),Overrides.Confirmed,out message))return false;
            s.State.Batch.Armed=false;s.State.Batch.Hold=0;Save(s);
        }
        message=Text.Get("Furnace.protected");
        if(action=="coolant-managed"||action=="coolant-sealed")
        {if(s.Coolant.TotalKg>1e-9){message=Text.Get("Furnace.coolant_drain_first");return false;}s.Coolant.Enabled=action=="coolant-managed";s.Coolant.PrimeSeconds=0;Save(s);message=ChargeStatus(s);return true;}
        if(!s.Coolant.Enabled){message=Text.Get("Furnace.charge_enable");return false;}
        if(action=="coolant-fill")
        {
            // The game stacks matching coolant dropped into the bin; a stack member is one charge like any other.
            var item=Phobos.Ostranauts.Framework.Inventory.StackUnits.All(co).FirstOrDefault(c=>c.strCODef==CoolantStock&&!c.bDestroyed&&Phobos.Ostranauts.Framework.Inventory.StackUnits.Empty(c)&&Math.Abs(Phobos.Ostranauts.Framework.Inventory.StackUnits.UnitMass(c)-1)<1e-7);
            if(item==null||s.Coolant.TotalKg+1>CoolantCharge.CapacityKg+1e-9){message=Text.Get("Furnace.charge_fill");return false;}
            if(!ChargeJournal(co).TryWrite(new Dictionary<string,string>{["state"]="pending",["input"]=item.strID}))return false;
            s.State.NativeMutation=true;
            item.RemoveFromCurrentHome(true);if(item.objCOParent!=null||item.ship!=null)throw new InvalidOperationException("Coolant charge did not detach.");
            s.Coolant.CleanKg+=1;Save(s);item.Destroy();
        }
        else if(action=="coolant-drain")
        {
            if(s.Coolant.TotalKg<=0){message=Text.Get("Furnace.charge_drain_empty");return false;}
            var product=DataHandler.GetCondOwner(CoolantWaste);bool pending=false;
            try {
                product.SetCondAmount("StatMass",s.Coolant.TotalKg);
                var grid=co.objContainer.gridLayout;var occupied=new bool[grid.gridMaxX,grid.gridMaxY];
                for(int x=0;x<grid.gridMaxX;x++)for(int y=0;y<grid.gridMaxY;y++)occupied[x,y]=grid.gridID[x,y]!=null||grid.gridInventoryItem[x,y]!=null;
                var size=GUIInventoryItem.GetWidthHeightForCO(product);var plan=BatchPlacement.Plan(occupied,new[]{new ItemSize(size.x,size.y)});
                if(plan==null||!co.objContainer.AllowedCO(product)){message=Text.Get("Furnace.charge_drain_space");return false;}
                if(!ChargeJournal(co).TryWrite(new Dictionary<string,string>{["state"]="pending",["output"]=product.strID}))return false;
                s.State.NativeMutation=true;
                pending=true;co.objContainer.AddCOSimple(product,new PairXY(plan[0].X,plan[0].Y));
                if(product.objCOParent!=co)throw new InvalidOperationException("Coolant drain placement failed.");
                s.Coolant.CleanKg=s.Coolant.CapturedKg=s.Coolant.PrimeSeconds=0;Save(s);
            }finally{if(!pending&&!product.bDestroyed)product.Destroy();}
        }
        else{message=Text.Get("Industry.unsupported_action");return false;}
        s.State.NativeMutation=false;Save(s);
        if(!ChargeJournal(co).TryWrite(new Dictionary<string,string>{["state"]="clear"}))throw new InvalidOperationException("Coolant service completion failed.");
        co.objContainer.Redraw();message=ChargeStatus(s);return true;
    }
    internal static void AddCoolantStock(NativeDefinitions d)
    {
        foreach(string id in new[]{CoolantStock,CoolantWaste})
        {
            var co=NativeDefinitions.Clone(DataHandler.dictCOs["ItmScrapTrash"]);co.strName=id;co.strNameFriendly=co.strNameShort=Text.Get(id==CoolantStock?"Furnace.charge_item":"Furnace.charge_waste");co.strDesc=Text.Get("Furnace.charge_desc");
            co.nStackLimit=id==CoolantStock?CoolantChargeStackLimit:1;co.inventoryWidth=co.inventoryHeight=1;co.aTickers=co.aUpdateCommands=Array.Empty<string>();co.aStartingConds=new[]{"IsSolid=1x1","IsPocketable=1x1"};Content.SetStat(co,"StatMass",1);Content.SetStat(co,"StatBasePrice",id==CoolantStock?20:.01);
            var item=NativeDefinitions.Clone(DataHandler.dictItemDefs[co.strItemDef]);
            co.strItemDef=item.strName=id;
            Content.ApplyStockArtwork(co,item,id==CoolantStock?"StockCoolantCharge":"StockRetainedCoolant");
            d.Items[id]=item;d.Objects[id]=co;
        }
    }
}

/// <summary>The F6 as a drain-canister receiver: a canister of coolant in its Products pours into the reservoir.</summary>
internal sealed class FurnaceCoolantReceiver : Phobos.Ostranauts.Framework.Liquids.ICanisterReceiver
{
    public string Id => "PhobosShipbreaker.FurnaceCoolant";
    public bool Handles(CondOwner machine) => machine != null && FurnaceRules.Machine(machine.strCODef);
    public double Accept(CondOwner machine, string commodity, double kg) => commodity == CoolantCharge.Commodity ? FurnaceService.AcceptCoolant(machine, kg) : 0;
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
