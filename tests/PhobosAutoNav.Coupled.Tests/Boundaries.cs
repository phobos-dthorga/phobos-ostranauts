using System;
using PhobosAutoNav;
internal sealed partial class ShipSitu
{
    internal bool bOrbitLocked=false,bBOLocked=false,bIsBO=false;
    internal UnityEngine.Vector2 vAccIn=default;
    internal void UnlockFromBO() { bBOLocked=false; }
}
internal sealed partial class GUIOrbitDraw
{
    internal static GUIOrbitDraw? Instance=null;
    internal bool PlayerThrusting=false;
}
internal static partial class CollisionManager
{ internal static double GetCollisionDistanceAU(ShipSitu a,ShipSitu b)=>200*AutoNavCore.M_TO_AU; }
namespace PhobosAutoNav
{
    internal static partial class Plugin
    {
        internal static Setting<bool> AbortOnManualThrust=new(true),UseThrusterRotation=new(true);
        internal static Setting<float> ArrivalSpeedTolerance=new(.5f),RotAccelMax=new(.5f),RotSpeedMax=new(.6f),TorchMinimumCorrectionMS=new(5);
        internal static void Verbose(string message) { }
    }
    internal sealed partial class TargetRef
    {
        internal ShipSitu TargetSitu=>CrewSim.system.GetShipByRegID(ShipId)!.objSS;
        internal static TargetRef? FromCrossHair()=>GUIOrbitDraw.CrossHairTarget?.Ship is Ship s ? FromShipId(s.strRegID):null;
        internal bool Resolve(out double x,out double y,out double vx,out double vy)
        {var s=TargetSitu;x=s.vPosx;y=s.vPosy;vx=s.vVelX;vy=s.vVelY;return true;}
    }
    internal sealed partial class TorchDouble
    {
        internal bool Available(Ship ship,bool prefer,double dt,out double acceleration){acceleration=0;return false;}
        internal bool Burn(Ship ship,double acceleration,double dt)=>false;
        internal void Align(){}
    }
}
