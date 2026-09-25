using System;
using System.Linq;
using HarmonyLib;
using Ostranauts.Utils;
using PhobosShipbreaker.Core;
using GridPoint = Ostranauts.Pathing.Vector2Int;

namespace PhobosShipbreaker;

internal sealed class CapturePlan
{
    internal JsonItem OwnPort = null!, TargetPort = null!;
    internal string Wall = "", Support = "", Floor = "";
    internal int Outward;
}

internal static class CaptureGeometry
{
    internal static string? TargetProblem(Ship own, Ship? target)
    {
        if (target == null || target == own || target.bDestroyed || target.objSS == null || target.HideFromSystem ||
            target.IsStation() || target.IsSubStation() || target.IsGroundStation() || target.IsStationHidden() ||
            CrewSim.system.IsInAtmo(target) || AIShipManager.GetAIShipByRegID(target.strRegID) != null)
            return Text.Get("Capture.target");
        // First capture slice requires ownership; it never fabricates salvage rights or takes occupied ships.
        if (CrewSim.system.GetShipOwner(target.strRegID) != CrewSim.coPlayer?.strID || target.People.Any())
            return Text.Get("Capture.ownership");
        return null;
    }

    internal static bool TryPlan(CondOwner grabber, Ship target, out CapturePlan? plan, string? selectedWall = null, int? selectedOutward = null, bool working = false)
    {
        plan = null;
        if (!DataHandler.dictCOs.TryGetValue("MooringPort", out var nativePort) || nativePort.strItemDef != "Blank" ||
            nativePort.mapPoints?.Any(p => p.StartsWith("DockA,", StringComparison.Ordinal) || p.StartsWith("DockB,", StringComparison.Ordinal)) == true)
            return false; // A changed native anchor contract must not move either ship speculatively.
        var own = grabber.ship;
        if (TargetProblem(own, target) != null || own.IsDocked() || own.IsMoored() || target.IsDocked() || target.IsMoored()) return false;
        double angle = grabber.Item.TF.eulerAngles.z;
        if (!CaptureRules.Cardinal(angle)) return false;
        var a = GridUtils.CreateFullGrid(own); var b = GridUtils.CreateFullGrid(target);
        var ao = GridUtils.FindCurrentOffset(own, a); var bo = GridUtils.FindCurrentOffset(target, b);
        var gp = grabber.GetPos();
        int attempts = 0;
        // Existing native fit testing includes the two full ship grids. No collision exemptions.
        foreach (var wall in b.Where(c => c.Value != null && c.Value.DataCO.Name == "ItmWall1x1" &&
            !string.IsNullOrEmpty(c.Value.ID) && (selectedWall == null || c.Value.ID == selectedWall)).OrderBy(c => c.Value.ID, StringComparer.Ordinal))
        foreach (int outward in new[] { 0, 90, 180, 270 })
        {
            if (selectedOutward.HasValue && selectedOutward.Value != outward) continue;
            var direction = IntakeRules.Rotate(0, 1, outward);
            int nx = wall.Key.x + (int)Math.Round(direction.X), ny = wall.Key.y + (int)Math.Round(direction.Y);
            if (nx >= 0 && ny >= 0 && nx < b.Width && ny < b.Height && b[nx, ny] != null && b[nx, ny].DataCO.Name != "Blank") continue;
            int rotation = GridUtils.GetIncomingDockRotation((float)angle, outward, out _);
            for (int column = 0; column < IntakeRules.Width; column++)
            {
                var offset = IntakeRules.Rotate(column - 1.5, CaptureRules.WallCentreY, angle);
                double x = gp.x + offset.X, y = gp.y + offset.Y;
                int ax = (int)Math.Round(x - ao.x), ay = (int)Math.Round(y - ao.y);
                if (!IntakeRules.Near(x, y, ax + ao.x, ay + ao.y)) continue;
                if (++attempts > CaptureRules.MaximumFitAttempts) return false;
                if (!GridUtils.CanOverlay(a, b, rotation, new GridPoint(ax, ay), wall.Key, new GridPoint(0, 0))) continue;
                plan = new CapturePlan { Wall = wall.Value.ID, Outward = outward,
                    OwnPort = new JsonItem { strName = "MooringPort", fX = (float)x, fY = (float)y, fRotation = (float)angle },
                    TargetPort = new JsonItem { strName = "MooringPort", fX = wall.Key.x + bo.x, fY = wall.Key.y + bo.y, fRotation = outward } };
                if (working)
                {
                    var selected = plan;
                    var parts = Parts(target);
                    var support = parts.Where(c => c.strID != selected.Wall && DataHandler.GetDataCO(c.strName)?.HasCond("IsWall")==true &&
                        DataHandler.GetDataCO(c.strName)?.HasCond("IsDamaged")!=true && Math.Abs(c.fX-selected.TargetPort.fX)+Math.Abs(c.fY-selected.TargetPort.fY)>=2)
                        .OrderBy(c=>Math.Abs(c.fX-selected.TargetPort.fX)+Math.Abs(c.fY-selected.TargetPort.fY)).ThenBy(c=>c.strID,StringComparer.Ordinal)
                        .FirstOrDefault(c=>parts.Any(f=>DataHandler.GetDataCO(f.strName)?.HasCond("IsFloor")==true &&
                            DataHandler.GetDataCO(f.strName)?.HasCond("IsDamaged")!=true && IntakeRules.Near(c.fX,c.fY,f.fX,f.fY)));
                    if(support==null) { plan=null;continue; }
                    var floor=parts.First(f=>DataHandler.GetDataCO(f.strName)?.HasCond("IsFloor")==true&&IntakeRules.Near(support.fX,support.fY,f.fX,f.fY));
                    var delta=IntakeRules.Rotate(support.fX-plan.TargetPort.fX,support.fY-plan.TargetPort.fY,rotation);
                    plan.OwnPort.fX+=(float)delta.X;plan.OwnPort.fY+=(float)delta.Y;
                    plan.TargetPort.fX=support.fX;plan.TargetPort.fY=support.fY;
                    plan.Support=support.strID;plan.Floor=floor.strID;
                }
                return true;
            }
        }
        return false;
    }
    internal static JsonItem[] Parts(Ship target) => target.LoadState>=Ship.Loaded.Edit ? target.GetCOs(null,false,false,true)
        .Where(c=>!c.bDestroyed&&c.objCOParent==null&&c.HasCond("IsInstalled")&&!c.HasCond("IsDamaged"))
        .Select(c=>new JsonItem { strID=c.strID,strName=c.strCODef,fX=c.GetPos().x,fY=c.GetPos().y,fRotation=c.Item.TF.eulerAngles.z }).ToArray() : target.json.aItems;
    internal static System.Collections.Generic.IEnumerable<(string Id,int Outward)> Windows(Ship target)
    {
        var grid=GridUtils.CreateFullGrid(target,target.GetDockedShipsAndPortIDs().Values.SingleOrDefault()?.strRegID);
        foreach(var cell in grid.Where(c=>c.Value!=null&&c.Value.DataCO.Name=="ItmWall1x1"&&!string.IsNullOrEmpty(c.Value.ID)).OrderBy(c=>c.Value.ID,StringComparer.Ordinal))
        foreach(int angle in new[]{0,90,180,270})
        {
            var delta=IntakeRules.Rotate(0,1,angle);int x=cell.Key.x+(int)Math.Round(delta.X),y=cell.Key.y+(int)Math.Round(delta.Y);
            if(x<0||y<0||x>=grid.Width||y>=grid.Height||grid[x,y]==null||grid[x,y].DataCO.Name=="Blank") yield return (cell.Value.ID,angle);
        }
    }
    internal static bool Contact(CondOwner grabber, Ship target, string wallId)
    {
        var wall = target.GetCOs(null, false, false, true).FirstOrDefault(c => c.strID == wallId);
        if (wall == null || wall.ship != target || wall.bDestroyed || wall.objCOParent != null ||
            wall.strCODef != "ItmWall1x1" || !wall.HasCond("IsInstalled") || wall.HasCond("IsDamaged")) return false;
        var g = grabber.GetPos(); var w = wall.GetPos();
        return CaptureRules.InContact(g.x, g.y, grabber.Item.TF.eulerAngles.z, w.x, w.y);
    }
    internal static bool ExactAttachment(Ship own, Ship target, CaptureRecord record) =>
        own.GetPortIdForDockedShip(target.strRegID) == record["ownPort"] &&
        target.GetPortIdForDockedShip(own.strRegID) == record["targetPort"] && own.IsDockedWith(target);

    internal static void AddPort(Ship ship, JsonItem port)
    {
        if (ship.LoadState >= Ship.Loaded.Edit)
        {
            var co = ship.CreatePart(port, port.strID, bLoot: false).GetComponent<CondOwner>();
            ship.AddCO(co, bTiles: true);
        }
        else ship.json.aItems = ship.json.aItems.Concat(new[] { port }).ToArray();
    }

    // MoorShip also clears unrelated crime flags. This strictly scoped synchronous guard preserves
    // them during our already-owned, unoccupied-ship capture; it never grants clearance or ownership.
    [ThreadStatic] internal static bool PreservingCrimeFlags;
}

[HarmonyPatch(typeof(CrimeManager), "ClearCrimeFlags", new[] { typeof(string), typeof(System.Collections.Generic.List<CondOwner>) })]
internal static class CaptureCrimePreservation
{
    private static bool Prefix() => !CaptureGeometry.PreservingCrimeFlags;
}
