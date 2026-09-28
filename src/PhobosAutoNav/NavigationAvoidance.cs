using System;
using System.Collections.Generic;
using System.Linq;
using PhobosAutoNav.Core;

namespace PhobosAutoNav;

internal sealed partial class NavigationService
{
    private readonly ObstacleRoute obstacleRoute = new();
    // Lateral reach for asteroid pre-filtering beyond braking travel, matching the body margin.
    private const double AsteroidReachM = 1000;
    private readonly HashSet<string> previousThreats = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (NavVector Position, double Epoch, NavVector Velocity)> bodyMotion = new();
    private NavVector cachedHop; // Relative to the tracked target, like obstacle velocities.
    private double nextRoute;
    private string routeTarget = "";
    private bool cachedRouted;
    private readonly List<ObstacleDisc> stepObstacles=new();
    private NavVector stepVelocity;
    private double stepBraking,stepEpoch=double.NegativeInfinity;
    internal bool ObstacleBurnAllowed(Ship own,double acceleration,double dt)
    {
        if(bodyMotion.Values.Any(b=>!b.Velocity.Finite)||avoidanceActive||industrial!=null||own!=console?.ship||stepEpoch!=StarSystem.fEpoch||previousThreats.Any(id=>!TowFlight.Contains(own,CrewSim.system.GetShipByRegID(id)) && !HazardRules.Tracked(NativeContactReader.Read(own,id).State))) return false;
        var heading=new NavVector(-Math.Sin(own.objSS.fRot),Math.Cos(own.objSS.fRot));
        return ObstacleRoute.BurnAndBrakeSafe(stepVelocity,heading*acceleration,dt+TorchRules.ZoneRefreshSeconds,stepBraking,stepObstacles);
    }
    internal void FilterAvoidanceCommand(Ship own,ref float x,ref float y,ref float turn,double dt)
    {
        if(!issuing||stepEpoch!=StarSystem.fEpoch||dt<=0||own!= (industrial?.Carrier??AutoNavCore.EngagedPlayer)) return;
        double full=own.RCSAccelMax/AutoNavCore.M_TO_AU,cos=Math.Cos(own.objSS.fRot),sin=Math.Sin(own.objSS.fRot);
        var acceleration=new NavVector((x*cos-y*sin)*full,(x*sin+y*cos)*full);
        if(ObstacleRoute.SweepSafe(stepVelocity,acceleration,dt,stepObstacles)) return;
        var brake=(-stepVelocity/Math.Max(dt,1)).Limit(stepBraking);
        if(RcsBudget.TryLimit((brake.X*cos+brake.Y*sin)/full,(-brake.X*sin+brake.Y*cos)/full,0,
            ReadThrottle(industrial?.Console??console!),RcsBudget.CombinedRotationShare,out var limited))
        { x=(float)limited.X;y=(float)limited.Y;turn=0; }
        else x=y=turn=0;
        avoidanceActive=true; HoldFireForGuidance();Torch.Cut();status=Text.Get("Avoidance.emergency");
    }
    private void SuspendAvoidance()
    {
        StopExtended(Text.Get("Avoidance.contact"));EndIndustrial(Text.Get("Avoidance.contact"));
        if(AutoNavCore.Engaged) SuspendForContact(new ContactReading(ContactState.Unavailable));
        else { Torch.Release(); status=Text.Get("Avoidance.contact"); }
        previousThreats.Clear();routeTarget="";
    }
    partial void ReadIndustrialRouteCost(CondOwner co,string module,string targetId,double bearing,double gap,ref bool valid,ref double cost)
    {
        if(co?.ship?.objSS==null||!HasIndustrialModule(co,module)||!ArrivalBrake.Finite(bearing)||!ArrivalBrake.Finite(gap)||gap<1000||
            !NativeContactReader.Read(co.ship,targetId).Usable) return;
        var own=co.ship;
        double reserve=own.DeltaVRemainingRCS*(own.IsDocked()?own.RCSAccelMaxUndocked/own.RCSAccelMax:1)/AutoNavCore.M_TO_AU;
        if(own.RCSCount<=0||!ArrivalBrake.Finite(reserve)||reserve<DepartureRules.MinimumReserveMS) return;
        var target=CrewSim.system.GetShipByRegID(targetId);var a=own.objSS;if(target==null)return;var b=target.objSS;
        var offset=new NavVector((b.vPosx-a.vPosx)/AutoNavCore.M_TO_AU,(b.vPosy-a.vPosy)/AutoNavCore.M_TO_AU);
        double radius=TowFlight.CollisionAU(own,target.objSS)/AutoNavCore.M_TO_AU;
        var goal=offset+new NavVector(Math.Cos(bearing*Math.PI/180),Math.Sin(bearing*Math.PI/180))*(radius+gap);
        // For a captured ship, route costing starts at its radial departure staging point.
        var start=own.IsDockedWith(target)?offset-offset.Unit*(radius+gap):default;
        var obstacles=new List<ObstacleDisc>();
        foreach(var other in CrewSim.system.dictShips.Values.ToArray())
        {
            if(other==own||other==null) continue;
            var reading=NativeContactReader.Read(own,other.strRegID);
            var p=new NavVector((other.objSS.vPosx-a.vPosx)/AutoNavCore.M_TO_AU,(other.objSS.vPosy-a.vPosy)/AutoNavCore.M_TO_AU);
            double? uncertainty=other==target?(reading.Usable?0:(double?)null):HazardRules.UncertaintyM(reading.State,p.Length/1000);
            if(uncertainty==null) continue;
            var v=new NavVector((other.objSS.vVelX-b.vVelX)/AutoNavCore.M_TO_AU,(other.objSS.vVelY-b.vVelY)/AutoNavCore.M_TO_AU);
            double r=TowFlight.CollisionAU(own,other.objSS)/AutoNavCore.M_TO_AU+Math.Max(25,v.Length*10)+uncertainty.Value;
            if(ObstacleRoute.Distance(start,goal,p)>r+gap) continue;
            obstacles.Add(new ObstacleDisc(other.strRegID,p-start,v,r));
        }
        foreach(var rock in NativeHazards.Asteroids(own,start,goal,gap+AsteroidReachM,targetId))
        {
            var p=new NavVector((rock.Situ.vPosx-a.vPosx)/AutoNavCore.M_TO_AU,(rock.Situ.vPosy-a.vPosy)/AutoNavCore.M_TO_AU);
            double? uncertainty=HazardRules.UncertaintyM(rock.Reading.State,p.Length/1000);
            if(uncertainty==null) continue;
            var v=new NavVector((rock.Situ.vVelX-b.vVelX)/AutoNavCore.M_TO_AU,(rock.Situ.vVelY-b.vVelY)/AutoNavCore.M_TO_AU);
            double r=TowFlight.CollisionAU(own,rock.Situ)/AutoNavCore.M_TO_AU+Math.Max(25,v.Length*10)+uncertainty.Value;
            if(ObstacleRoute.Distance(start,goal,p)<=r+gap) obstacles.Add(new ObstacleDisc(rock.Id,p-start,v,r));
        }
        foreach(var body in CrewSim.system.aBOs.Values)
        {
            if(body==null||body.IsAsteroidField||body.nDrawFlagsBody==ContactRules.PlaceholderBodyDrawFlag) continue;
            var p=new NavVector((body.dXReal-a.vPosx)/AutoNavCore.M_TO_AU,(body.dYReal-a.vPosy)/AutoNavCore.M_TO_AU);
            double r=(body.fRadius+a.GetRadiusAU())/AutoNavCore.M_TO_AU+1000;
            if(ObstacleRoute.Distance(start,goal,p)<=r+gap) obstacles.Add(new ObstacleDisc(body.strName,p-start,default,r));
        }
        var planner=new ObstacleRoute();valid=planner.Plan(goal-start,obstacles,out _);cost=planner.LastCost+start.Length;
    }

    // Runs before every flight controller, using one common pre-physics observation boundary.
    internal bool GuardNavigation(double dt)
    {
        avoidanceActive = avoidanceBlocked = false; stepEpoch=double.NegativeInfinity;
        if(CrewSim.system!=null&&!CrewSim.Paused&&dt>0)
            foreach(var body in CrewSim.system.aBOs.Values)
            {
                if(body==null||body.IsAsteroidField||body.nDrawFlagsBody==ContactRules.PlaceholderBodyDrawFlag)continue;
                var position=new NavVector(body.dXReal/AutoNavCore.M_TO_AU,body.dYReal/AutoNavCore.M_TO_AU);
                if(bodyMotion.TryGetValue(body.strName,out var previous)&&StarSystem.fEpoch>previous.Epoch)
                    bodyMotion[body.strName]=(position,StarSystem.fEpoch,(position-previous.Position)/(StarSystem.fEpoch-previous.Epoch));
                else if(!bodyMotion.ContainsKey(body.strName))bodyMotion[body.strName]=(position,StarSystem.fEpoch,new NavVector(double.NaN,double.NaN));
            }
        var co = industrial?.Console ?? (AutoNavCore.Engaged ? console : null);
        string? targetId = industrial?.Target ?? AutoNavCore.EngagedTarget?.ShipId;
        if (CrewSim.system==null || co == null || targetId == null || CrewSim.Paused || dt == 0) return false;
        try
        {
            var own = co.ship;
            var targetSitu = industrial != null ? CrewSim.system.GetShipByRegID(targetId)?.objSS : AutoNavCore.EngagedTarget?.TargetSitu;
            string? problem = !Plugin.Enabled.Value ? Text.Get("NavigationService.mod_disabled") : HardwareProblem(co);
            if(problem==null&&industrial==null&&(!FlightBindingValid()||Torch.ControlsChanged||AutoNavCore.AutoDockBusy())) problem=Text.Get("Persistence.binding_changed");
            if(problem==null&&(own.GetRCSRemain()<=0||industrial!=null&&industrial.Elapsed>=DockingRules.MaximumSeconds||AutoNavCore.Engaged&&AutoNavCore.ElapsedSeconds>= (DockingActive?DockingRules.MaximumSeconds:Plugin.MaxFlightSimHours.Value*3600))) problem=Text.Get("Docking.timeout");
            if (problem == null && industrial != null) problem = IndustrialProblem(industrial);
            if (problem == null && (!ArrivalBrake.Finite(dt) || dt <= 0 || dt > Plugin.MaximumStepSeconds.Value ||
                (industrial != null || DockingActive) && dt > DockingRules.MaximumStep)) problem = Text.Get("Docking.step");
            if(problem!=null) { Disengage(problem);return true; }
            if(targetSitu==null) { SuspendAvoidance();return true; }
            var targetSensing = SenseTarget(co,targetId);
            if(!targetSensing.Usable) { if(!SensorsSettling(own,targetSensing)) SuspendAvoidance(); return true; }
            var a = own.objSS; var b = targetSitu;
            var offset = new NavVector((b.vPosx-a.vPosx)/AutoNavCore.M_TO_AU,(b.vPosy-a.vPosy)/AutoNavCore.M_TO_AU);
            var velocity = new NavVector((a.vVelX-b.vVelX)/AutoNavCore.M_TO_AU,(a.vVelY-b.vVelY)/AutoNavCore.M_TO_AU);
            double hull = TowFlight.CollisionAU(own,targetSitu)/AutoNavCore.M_TO_AU;
            double stop = industrial != null || DockingActive ? hull * DockingRules.StandOffRadius : AutoNavCore.EffectiveArriveAU(own, AutoNavCore.EngagedTarget)/AutoNavCore.M_TO_AU;
            var goal = offset.Unit * Math.Max(0, offset.Length-stop);
            if (industrial != null && industrial.Move != IndustrialMove.CaptureApproach)
                goal = offset + new NavVector(Math.Cos(industrial.Bearing),Math.Sin(industrial.Bearing))*(hull+industrial.Gap);
            double acceleration = own.RCSAccelMax / AutoNavCore.M_TO_AU * ReadThrottle(co);
            if (!velocity.Finite || !goal.Finite || !ArrivalBrake.Finite(acceleration) || acceleration <= 0)
            { Disengage(Text.Get("Docking.unsafe")); return true; }
            double horizon = Math.Min(ObstacleRoute.MaximumHorizonSeconds, Math.Max(2*dt, velocity.Length/(acceleration*.45)+2*dt));
            var actual = new List<ObstacleDisc>(); var planning = new List<ObstacleDisc>();
            var threats = new HashSet<string>(StringComparer.Ordinal);
            // Weak contacts near the planned route: candidates for selective sensor engagement.
            var weakHazards = new List<string>();
            // A threat that fades to a weak contact stays a hazard with wider clearance; losing it entirely suspends.
            if(previousThreats.Any(id=>!TowFlight.Contains(own,CrewSim.system.GetShipByRegID(id)) && !HazardRules.Tracked(NativeContactReader.Read(own,id).State))) { SuspendAvoidance();return true; }
            foreach (var other in CrewSim.system.dictShips.Values.ToArray())
            {
                if (other == null || other == own || TowFlight.Contains(own, other)) continue;
                var reading = NativeContactReader.Read(own,other.strRegID);
                bool isTarget = other.strRegID == targetId;
                if (!HazardRules.Tracked(reading.State))
                {
                    if (previousThreats.Contains(other.strRegID)) { SuspendAvoidance(); return true; }
                    continue;
                }
                // Geometry/motion is read only after native sensing has admitted this contact.
                var c = other.objSS;
                var p = new NavVector((c.vPosx-a.vPosx)/AutoNavCore.M_TO_AU,(c.vPosy-a.vPosy)/AutoNavCore.M_TO_AU);
                // A weak contact is avoided with the native partial-contact position error added;
                // one too distant to place usefully is left out rather than guessed.
                double? uncertainty = isTarget ? 0 : HazardRules.UncertaintyM(reading.State, p.Length/1000);
                if (uncertainty == null) continue;
                var v = new NavVector((c.vVelX-b.vVelX)/AutoNavCore.M_TO_AU,(c.vVelY-b.vVelY)/AutoNavCore.M_TO_AU);
                double radius = TowFlight.CollisionAU(own,other.objSS)/AutoNavCore.M_TO_AU;
                double margin = (isTarget ? (industrial?.Move==IndustrialMove.Egress ? Math.Min(1,radius*.002) : radius*.002) : Math.Max(ObstacleRoute.MinimumMarginM, radius*.05)) + uncertainty.Value;
                if (!p.Finite || !v.Finite || !ArrivalBrake.Finite(radius) || radius <= 0) { Disengage(Text.Get("Docking.unsafe")); return true; }
                actual.Add(new ObstacleDisc(other.strRegID,p,v,radius+margin));
                threats.Add(other.strRegID);
                if (ObstacleRoute.Distance(default,goal,p) > radius+margin+(velocity-v).Length*horizon+1000) continue;
                double travel = isTarget ? 0 : v.Length*Math.Min(horizon,10);
                planning.Add(new ObstacleDisc(other.strRegID,p+v*(Math.Min(horizon,10)/2),v,radius+margin+travel/2));
                threats.Add(other.strRegID);
                if (!isTarget && reading.State == ContactState.Weak) weakHazards.Add(other.strRegID);
            }
            foreach (var rock in NativeHazards.Asteroids(own, default, goal, velocity.Length*horizon+AsteroidReachM, targetId))
            {
                if (!HazardRules.Tracked(rock.Reading.State))
                {
                    if (previousThreats.Contains(rock.Id)) { SuspendAvoidance(); return true; }
                    continue;
                }
                var c = rock.Situ;
                var p = new NavVector((c.vPosx-a.vPosx)/AutoNavCore.M_TO_AU,(c.vPosy-a.vPosy)/AutoNavCore.M_TO_AU);
                double? uncertainty = HazardRules.UncertaintyM(rock.Reading.State, p.Length/1000);
                if (uncertainty == null) continue;
                var v = new NavVector((c.vVelX-b.vVelX)/AutoNavCore.M_TO_AU,(c.vVelY-b.vVelY)/AutoNavCore.M_TO_AU);
                double radius = TowFlight.CollisionAU(own,c)/AutoNavCore.M_TO_AU;
                double margin = Math.Max(ObstacleRoute.MinimumMarginM, radius*.05) + uncertainty.Value;
                if (!p.Finite || !v.Finite || !ArrivalBrake.Finite(radius) || radius <= 0) { Disengage(Text.Get("Docking.unsafe")); return true; }
                actual.Add(new ObstacleDisc(rock.Id,p,v,radius+margin)); planning.Add(new ObstacleDisc(rock.Id,p,v,radius+margin));
                threats.Add(rock.Id);
                if (rock.Reading.State == ContactState.Weak) weakHazards.Add(rock.Id);
            }
            foreach (var body in CrewSim.system.aBOs.Values)
            {
                if (body == null || body.IsAsteroidField || body.nDrawFlagsBody == ContactRules.PlaceholderBodyDrawFlag) continue;
                var world = new NavVector(body.dXReal/AutoNavCore.M_TO_AU,body.dYReal/AutoNavCore.M_TO_AU);
                var p = world-new NavVector(a.vPosx/AutoNavCore.M_TO_AU,a.vPosy/AutoNavCore.M_TO_AU);
                double radius = body.fRadius/AutoNavCore.M_TO_AU + TowFlight.RadiusAU(own)/AutoNavCore.M_TO_AU + 1000;
                bool sampled = bodyMotion.TryGetValue(body.strName,out var old) && StarSystem.fEpoch > old.Epoch;
                var v = sampled ? (world-old.Position)/(StarSystem.fEpoch-old.Epoch) : old.Velocity;
                if (StarSystem.fEpoch != old.Epoch) bodyMotion[body.strName]=(world,StarSystem.fEpoch,v);
                if (!v.Finite) { if(ObstacleRoute.Distance(default,goal,p)<=radius+velocity.Length*horizon+1000) {SuspendAvoidance();return true;} continue; }
                v -= new NavVector(b.vVelX/AutoNavCore.M_TO_AU,b.vVelY/AutoNavCore.M_TO_AU);
                actual.Add(new ObstacleDisc(body.strName,p,v,radius));
                if(ObstacleRoute.Distance(default,goal,p)<=radius+(velocity-v).Length*horizon+1000) planning.Add(new ObstacleDisc(body.strName,p,v,radius));
            }
            // Weak hazards keep their wider clearance this step; switched-on sensors sharpen later ones.
            if (weakHazards.Count > 0) RestoreHazardSensors(co, weakHazards);
            previousThreats.Clear(); foreach (var id in threats) previousThreats.Add(id);
            stepObstacles.Clear();stepObstacles.AddRange(actual);stepVelocity=velocity;stepBraking=acceleration*.45;stepEpoch=StarSystem.fEpoch;
            bool direct = ObstacleRoute.Clear(default,goal,planning);
            // Check the whole immediate braking horizon even on otherwise direct legs.
            bool imminent = !ObstacleRoute.SweepSafe(velocity,default,horizon,actual);
            if (direct && !imminent && planning.Count <= ObstacleRoute.MaximumObstacles)
            { routeTarget=""; obstacleRoute.Reset(); return false; }
            avoidanceActive = true; if (industrial != null) industrial.Ready=false;
            HoldFireForGuidance(); Torch.Cut();
            var worldOwn = new NavVector(a.vPosx/AutoNavCore.M_TO_AU,a.vPosy/AutoNavCore.M_TO_AU);
            var worldTarget = new NavVector(b.vPosx/AutoNavCore.M_TO_AU,b.vPosy/AutoNavCore.M_TO_AU);
            NavVector hop = cachedHop + worldTarget - worldOwn;
            bool routed = cachedRouted && planning.Count <= ObstacleRoute.MaximumObstacles;
            if (routeTarget != targetId || StarSystem.fEpoch >= nextRoute || hop.Length < 25 || !ObstacleRoute.Clear(default,hop,planning))
            {
                routed = planning.Count <= ObstacleRoute.MaximumObstacles && obstacleRoute.Plan(goal,planning,out hop); cachedRouted=routed;
                cachedHop = worldOwn+hop-worldTarget; nextRoute=StarSystem.fEpoch+1; routeTarget=targetId;
            }
            avoidanceBlocked=!routed;
            double speed = routed ? Math.Min(DockingRules.CruiseMS,Math.Sqrt(Math.Max(0,hop.Length*acceleration*.25))) : 0;
            var desired = routed ? hop.Unit*speed : default;
            var demand = ((desired-velocity)/Math.Max(2,dt)).Limit(acceleration*.45);
            if (!ObstacleRoute.SweepSafe(velocity,demand,dt,actual))
                demand = (-velocity/Math.Max(dt,1)).Limit(acceleration*.45);
            // Never discard a braking command because admission for a fresh leg would fail.
            double cos=Math.Cos(a.fRot), sin=Math.Sin(a.fRot), full=own.RCSAccelMax/AutoNavCore.M_TO_AU;
            if (!RcsBudget.TryLimit((demand.X*cos+demand.Y*sin)/full,(-demand.X*sin+demand.Y*cos)/full,
                Math.Max(-.15,Math.Min(.15,-a.fW/2)),ReadThrottle(co),RcsBudget.CombinedRotationShare,out var command))
            { Disengage(Text.Get("Docking.unsafe")); return true; }
            issuing=true; try { own.Maneuver((float)command.X,(float)command.Y,(float)command.Turn,0,(float)dt); } finally { issuing=false; }
            avoidanceNotice=status=Text.Get(routed ? "Avoidance.detour" : "Avoidance.blocked",actual.OrderBy(o=>o.Position.Length-o.Radius).FirstOrDefault().Id??"?");
            if (industrial != null) { industrial.Elapsed+=dt; industrialNotice=avoidanceNotice; }
            if(AutoNavCore.Engaged) { AutoNavCore.AvoidanceStep(dt); PersistProgress(); dockStableSeconds=0; }
            return true;
        }
        catch(Exception ex) { log(ex.ToString()); Disengage(Text.Get("Avoidance.fault")); return true; }
    }
}
