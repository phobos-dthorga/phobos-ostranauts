using System;
using System.Linq;
using System.Collections.Generic;
using PhobosAutoNav.Core;
internal static class ObstacleChecks
{
    internal static void Run(Action<bool,string> check)
    {
        var goal=new NavVector(1000,0);var planner=new ObstacleRoute();
        var obstacle=new[]{new ObstacleDisc("station",new NavVector(500,0),default,100)};
        check(planner.Plan(goal,obstacle,out var hop)&&hop.Y!=0&&ObstacleRoute.Clear(default,hop,obstacle)&&planner.LastCost>goal.Length,"Static obstruction receives a clear finite detour");
        double side=Math.Sign(hop.Y);
        for(int i=0;i<8;i++)
        {
            var jitter=new[]{new ObstacleDisc("station",new NavVector(500,(i%2==0?1:-1)*.01),default,100)};
            check(planner.Plan(goal,jitter,out hop)&&Math.Sign(hop.Y)==side,"Small observation changes retain passing side");
        }
        check(!planner.Plan(goal,new[]{new ObstacleDisc("blocked destination",goal,default,50)},out _),"Blocked arrival holds rather than deleting its hazard");
        check(!planner.Plan(goal,Enumerable.Range(0,33).Select(i=>new ObstacleDisc(i.ToString(),new NavVector(i+200,0),default,20)).ToArray(),out _),"Planning budget rejects excess hazards without truncation");
        check(!planner.Plan(goal,new[]{new ObstacleDisc("unknown",new NavVector(double.NaN,0),default,50)},out _),"Invalid observation cannot become a route");
        var crossing=new[]{new ObstacleDisc("traffic",new NavVector(50,50),new NavVector(0,-10),5)};
        check(!ObstacleRoute.SweepSafe(new NavVector(10,0),default,10,crossing),"Crossing traffic checked in relative motion");
        check(ObstacleRoute.SweepSafe(default,default,1,crossing),"A clear stationary hold is admissible");
        var torchHazard=new[]{new ObstacleDisc("braking corridor",new NavVector(180,0),default,10)};
        check(ObstacleRoute.SweepSafe(default,new NavVector(10,0),2,torchHazard)&&!ObstacleRoute.BurnAndBrakeSafe(default,new NavVector(10,0),2,1,torchHazard),"Torch denied when its burn fits but subsequent RCS braking does not");
        check(ObstacleRoute.BurnAndBrakeSafe(default,new NavVector(1,0),1,1,Array.Empty<ObstacleDisc>()),"Unobstructed burn retains torch preference");
        // Traverse two windows on opposite sides of a target without crossing its hull.
        var position=new NavVector(-1300,0);var destination=new NavVector(1300,0);var velocity=default(NavVector);
        bool arrived=false;planner.Reset();
        for(int i=0;i<20000;i++)
        {
            var discs=new[]{new ObstacleDisc("wreck",-position,default,500)};
            check(planner.Plan(destination-position,discs,out hop),"A route around a retained target remains available");
            var desired=hop.Unit*Math.Min(20,Math.Sqrt(hop.Length*.25));
            if((destination-position).Length<5) desired=default;
            var demand=((desired-velocity)/2).Limit(.45);
            if(!ObstacleRoute.SweepSafe(velocity,demand,.5,discs)) demand=(-velocity/2).Limit(.45);
            position+=velocity*.5+demand*.125;velocity+=demand*.5;
            check(position.Length>500,"Every simulated traversal step stays outside native hull boundary");
            if((destination-position).Length<10&&velocity.Length<.2) { arrived=true;break; }
        }
        check(arrived,"Multiple-window traversal converges at the new staging point");
        foreach(var phase in DepartureRules.Phases)
        {
            var actual=DepartureRules.Reconcile(phase,true,true,false,false);
            check(actual!=DepartureRecovery.Detach||phase=="Prepared","Pending or completed detachment is never replayed");
            check(DepartureRules.Reconcile(phase,false,false,true,false)==DepartureRecovery.Retain,"Extra attachment protects every recovery phase");
            check(DepartureRules.Reconcile(phase,false,false,false,true)==DepartureRecovery.Retain,"Orphan anchors retain uncertain evidence");
        }
        check(DepartureRules.Reconcile("DetachPending",false,false,false,false)==DepartureRecovery.Egress,"Completed native detachment reconciles into newly planned egress");
        check(DepartureRules.Reconcile("Continue",false,false,false,false)==DepartureRecovery.Continue,"Only the saved continuation intent is resumed");
        check(DepartureRules.Reconcile("unknown",false,false,false,false)==DepartureRecovery.Retain,"Unknown departure records cannot issue thrust");
    }
}
