using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Ostranauts.Trading;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

internal static class ReclamationGeometry
{
    internal static CondOwner? Resolve(Ship target,string id) => target.GetCOs(null,false,false,true).FirstOrDefault(c=>c.strID==id && !c.bDestroyed);
    internal static bool Reach(CondOwner g,CondOwner wall) => wall.objCOParent==null &&
        CaptureRules.InContact(g.GetPos().x,g.GetPos().y,g.Item.TF.eulerAngles.z,wall.GetPos().x,wall.GetPos().y);
    internal static bool Floor(Ship target,CondOwner wall) => target.GetCOs(null,false,false,true).Any(c=>c.HasCond("IsFloor")&&
        c.HasCond("IsInstalled")&&!c.HasCond("IsDamaged")&&IntakeRules.Near(c.GetPos().x,c.GetPos().y,wall.GetPos().x,wall.GetPos().y));
    internal static IEnumerable<int> Exposed(Ship target,CondOwner wall)
    {
        foreach(int angle in new[]{0,90,180,270})
        {
            var d=IntakeRules.Rotate(0,1,angle); var p=wall.GetPos();
            var tile=target.GetTileAtWorldCoords1((float)(p.x+d.X),(float)(p.y+d.Y),false);
            if(tile?.coProps==null || tile.coProps.strCODef=="Blank") yield return angle;
        }
    }
    internal static string? TargetProblem(CondOwner g,Ship target)
    {
        string? problem=CaptureGeometry.TargetProblem(g.ship,target);
        if(problem!=null) return problem;
        if(target.LoadState<Ship.Loaded.Edit || target.aRooms==null) return Text.Get("Reclamation.geometry");
        if(target.bCheckRooms) return null; // Caller holds cutting/settlement until native rebuild completes.
        foreach(var room in target.aRooms.Where(r=>!r.Void))
        {
            double pressure=room.CO?.GetCondAmount("StatGasPressure")??double.NaN;
            if(!ReclamationRules.Finite(pressure)||pressure<0||pressure>ReclamationRules.MaximumTargetPressureKPa) return Text.Get("Reclamation.pressure");
        }
        return null;
    }
    internal static bool CanReserve(CondOwner g,CondOwner wall)
    {
        // Reserve the native loose output footprint, not the installed tile footprint.
        if(!DataHandler.dictCOs.TryGetValue(ProcessRules.Wall,out var definition)||
            !DataHandler.dictItemDefs.TryGetValue(definition.strItemDef,out var item)||g.objContainer?.gridLayout==null) return false;
        int width=definition.inventoryWidth>0?definition.inventoryWidth:item.nCols;
        int height=definition.inventoryHeight>0?definition.inventoryHeight:item.nCols>0?(item.aSocketAdds?.Length??0)/item.nCols:0;
        return width>0&&height>0&&g.objContainer.ctAllowed?.TriggeredDataCO(new DataCO(definition),false)==true&&
            g.objContainer.gridLayout.FindFirstUnoccupiedTile(width,height,wall.strID).IsValid();
    }
    internal static bool NativeWallContract() =>
        DataHandler.dictInteractions.TryGetValue("MSWall1x1Uninstall",out var action)&&action.objLootModeSwitch=="OutputWall1x1Uninstall"&&
        DataHandler.dictLoot.TryGetValue("MSWall1x1Uninstall",out var trigger)&&trigger.strType=="interaction"&&trigger.aCOs.SequenceEqual(new[]{"MSWall1x1Uninstall=1.0x1"})&&
        DataHandler.dictLoot.TryGetValue("OutputWall1x1Uninstall",out var output)&&output.strType=="item"&&output.aCOs.SequenceEqual(new[]{"ItmWall1x1Loose=1.0x1"});
    internal static bool Wall(CondOwner g,Ship target,CondOwner wall,CaptureRecord? capture=null,bool started=false)
    {
        if(!NativeWallContract() || wall.ship!=target || wall.bDestroyed || wall.strCODef!="ItmWall1x1" || wall.objCOParent!=null || wall.Item==null ||
            !ReclamationRules.CanCut(wall.GetTotalMass(),wall.HasCond("IsInstalled"),wall.HasCond("IsDamaged")||wall.GetCondAmount("StatDamage")>0,
                wall.GetCOsSafe(true).Count==0,wall.coStackHead!=null||wall.aStack.Count!=0,Exposed(target,wall).Any(),Floor(target,wall),
                capture!=null&&(wall.strID==capture["support"]||wall.strID==capture["floor"]||wall.strID==capture["targetPort"]))) return false;
        if(!started && wall.GetCondAmount("StatUninstallProgress")!=0) return false;
        var onTile=new List<CondOwner>(); target.GetCOsAtWorldCoords1(wall.GetPos(),null,false,true,onTile);
        if(onTile.Any(c=>c!=wall && !c.bDestroyed && !c.HasCond("IsFloor"))) return false;
        // Native uninstallation owns mode replacement. A changed definition must be reviewed.
        return wall.GetCondAmount("StatUninstallProgressMax")>0 &&
            wall.GetComponent<Destructable>()?.GetDmgLoot("StatUninstallProgress")=="MSWall1x1Uninstall";
    }
    internal static bool Support(Ship target,CaptureRecord r)
    {
        var wall=Resolve(target,r["support"]); var floor=Resolve(target,r["floor"]); var port=Resolve(target,r["targetPort"]);
        return wall!=null&&floor!=null&&port!=null&&wall.HasCond("IsWall")&&floor.HasCond("IsFloor")&&
            wall.HasCond("IsInstalled")&&floor.HasCond("IsInstalled")&&!wall.HasCond("IsDamaged")&&!floor.HasCond("IsDamaged")&&
            IntakeRules.Near(wall.GetPos().x,wall.GetPos().y,floor.GetPos().x,floor.GetPos().y)&&
            IntakeRules.Near(port.GetPos().x,port.GetPos().y,floor.GetPos().x,floor.GetPos().y);
    }
}
