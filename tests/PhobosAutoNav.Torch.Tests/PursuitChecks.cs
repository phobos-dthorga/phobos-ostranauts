using System;
using System.Diagnostics;
using PhobosAutoNav;
using PhobosAutoNav.Core;

internal static class PursuitChecks
{
    private readonly record struct Result(double Time, double Clearance, double Overshoot, int Reversals, double Fuel, double Error, double Cost);
    internal static void Run(Action<bool,string> check)
    {
        var track = new MotionTrack();
        track.Observe(new(30000, -20000), 10); track.Observe(new(30001, -20000), 11);
        check(Math.Abs(track.Acceleration.X - 1) < 1e-9, "Observed acceleration excludes shared velocity");
        track.Observe(new(30001, -20000), 12);
        check(track.Horizon < 20 && track.Acceleration.X < .2, "Changed burn reduces trust and decays the acceleration estimate");
        track.Observe(new(100000, 0), 13); check(track.Samples == 1, "Velocity discontinuity reseeds prediction");
        check(PredictiveGuidance.TryPlan(new(0,5000), default, new(0,5), default,.5,100,0,1000,10,.25,10,true,out var limited)
            && limited.Limited, "Unattainable acceleration reports limited authority while returning bounded control");
        Console.WriteLine("Pursuit benchmark: policy,case,torch,settled_s,min_hull_clearance_m,overshoot_m,reversals,translation_dv,late_error_m,us_per_step");
        double legacyOvershoot = 0, predictiveOvershoot = 0; int legacyReversals = 0, predictiveReversals = 0;
        foreach (string scenario in new[] { "accelerating", "reversing", "crossing", "burst" })
        {
            var legacy = Simulate(0, scenario, false, false, false, 0, check);
            var feedback = Simulate(1, scenario, false, false, false, 0, check);
            var current = Simulate(2, scenario, false, false, false, 0, check);
            legacyOvershoot += legacy.Overshoot; predictiveOvershoot += current.Overshoot;
            legacyReversals += legacy.Reversals; predictiveReversals += current.Reversals;
            check(current.Time > 0 && current.Clearance > 850, "Predictive rendezvous completes feasible " + scenario);
            var hybrid = Simulate(2, scenario, true, false, false, 0, check);
            check(hybrid.Time > 0 && hybrid.Clearance > 850, "Hybrid rendezvous completes feasible " + scenario);
        }
        check(predictiveReversals < legacyReversals, "Fewer aggregate thrust reversals across feasible RCS target manoeuvres");
        check(predictiveOvershoot <= legacyOvershoot + 1, "Predictive guidance does not increase aggregate transient overshoot");
        foreach (bool torch in new[] { false, true })
        {
            var normal = Simulate(2, "burst", torch, true, false, 0, check);
            var reversed = Simulate(2, "burst", torch, true, true, 25000, check);
            check(normal.Clearance > 850 && normal.Error < 60, "Follow retains a useful distance band through changing burns");
            check(Math.Abs(normal.Error - reversed.Error) < .1 && Math.Abs(normal.Fuel - reversed.Fuel) < .1,
                "Both update orders, shared velocity and common gravity produce equivalent guidance");
        }
        AutoNavCore.ResetStatics();
    }
    private static Result Simulate(int controller, string scenario, bool torch, bool follow, bool reverseOrder, double background, Action<bool,string> check)
    {
        AutoNavCore.ResetStatics(); LegacyAutoNavCore.ResetStatics();
        Plugin.Service = new NavigationService(); CrewSim.system = new(); CrewSim.objInstance = new();
        NativeContactReader.State = ContactState.Ready; StarSystem.fEpoch = 100;
        var ship = new Ship(); CrewSim.coPlayer = ship.Reactor;
        var target = new TargetRef(); target.TargetSitu.vPosy = 5000 * AutoNavCore.M_TO_AU;
        ship.objSS.vVelX = target.TargetSitu.vVelX = background * AutoNavCore.M_TO_AU;
        ship.objSS.vVelY = target.TargetSitu.vVelY = -background * AutoNavCore.M_TO_AU;
        if (background != 0)
            ship.objSS.vAccEx = target.TargetSitu.vAccEx = new(.02 * AutoNavCore.M_TO_AU, -.01 * AutoNavCore.M_TO_AU);
        if (scenario == "crossing") target.TargetSitu.vVelX += 12 * AutoNavCore.M_TO_AU;
        AutoNavCore.CruiseAU = LegacyAutoNavCore.CruiseAU = 100 * AutoNavCore.M_TO_AU;
        AutoNavCore.ArrSpdAU = LegacyAutoNavCore.ArrSpdAU = 0;
        AutoNavCore.ArriveAU = LegacyAutoNavCore.ArriveAU = 1000 * AutoNavCore.M_TO_AU;
        if (controller == 0) LegacyAutoNavCore.BeginFlight(ship, target, new(3,10,.75,2),false);
        else AutoNavCore.BeginFlight(ship, target, new(3,10,.75,2),torch);
        AutoNavCore.Following = follow;
        double dt = .25, min = double.MaxValue, overshoot = 0, fuel = 0, error = 0, elapsed = -1, cost = 0;
        int reversals = 0, late = 0, steps = 0; NavVector previous = default;
        var estimator = new MotionTrack();
        for (double t = 0; t < 2400; t += dt)
        {
            var a = t > 1000 && !follow ? default : scenario switch
            {
                "accelerating" => new NavVector(0, .035),
                "reversing" => new NavVector(0, ((int)(t / 80) % 2 == 0 ? 1 : -1) * .07),
                "crossing" => new NavVector(Math.Sin(t / 30) * .06, 0),
                _ => new NavVector(t % 80 < 12 ? .12 : t % 80 < 24 ? -.12 : 0, t % 120 < 15 ? .04 : 0)
            };
            target.TargetSitu.vAccIn = new(a.X * AutoNavCore.M_TO_AU, a.Y * AutoNavCore.M_TO_AU);
            long start = Stopwatch.GetTimestamp();
            if (controller == 0) LegacyAutoNavCore.SteerFlight(ship, target, dt);
            else if (controller == 2) AutoNavCore.SteerFlight(ship, target, dt);
            else
            {
                var r = Offset(ship,target); var v = Velocity(ship,target);
                estimator.Observe(new(target.TargetSitu.vVelX / AutoNavCore.M_TO_AU,target.TargetSitu.vVelY / AutoNavCore.M_TO_AU), StarSystem.fEpoch);
                double gap = Math.Max(0,r.Length - 1000);
                double speed = Math.Min(100, Math.Min(gap / 4, Math.Sqrt(.3 * gap)));
                var u = (estimator.Acceleration + (r.Unit * speed - v) / 4).Limit(.265);
                RcsBudget.TryLimit(u.X / .5,u.Y / .5,0,1,.25,out var command);
                ship.Maneuver((float)command.X,(float)command.Y,0,0,(float)dt);
                if (r.Length <= 1005 && v.Length < .5) AutoNavCore.EndFlight(ship,"ARRIVED");
            }
            cost += (Stopwatch.GetTimestamp() - start) * 1e6 / Stopwatch.Frequency; steps++;
            check(Math.Abs(ship.LastX) + Math.Abs(ship.LastY) + Math.Abs(ship.LastTurn) <= 1.00001,"Pursuit actuator budget");
            var accel = new NavVector((ship.objSS.vAccIn.x + ship.objSS.vAccRCS.x) / AutoNavCore.M_TO_AU,
                (ship.objSS.vAccIn.y + ship.objSS.vAccRCS.y) / AutoNavCore.M_TO_AU);
            fuel += accel.Length * dt;
            if (accel.Length > .015)
            { if (previous.Length > .015 && accel.Unit.Dot(previous.Unit) < -.5) reversals++; previous = accel; }
            if (reverseOrder) { target.TargetSitu.Integrate(dt); ship.objSS.Integrate(dt); }
            else { ship.objSS.Integrate(dt); target.TargetSitu.Integrate(dt); }
            StarSystem.fEpoch += dt;
            double range = Offset(ship,target).Length; min = Math.Min(min,range - 10);
            overshoot = Math.Max(overshoot,1000 - range);
            if (t > 1800) { error += Math.Abs(range - 1000); late++; }
            if (controller == 0 ? !LegacyAutoNavCore.Engaged : !AutoNavCore.Engaged) { elapsed = t; break; }
        }
        if (follow) check(AutoNavCore.Engaged,"Follow continues through its distance band");
        var result = new Result(elapsed,min,overshoot,reversals,fuel,late > 0 ? error / late : 0,cost / steps);
        Console.WriteLine($"{controller},{scenario}{(follow ? "-follow" : "")}{(reverseOrder ? "-reordered-background-gravity" : "")},{torch},{result.Time:0.00},{min:0.00},{overshoot:0.00},{reversals},{fuel:0.00},{result.Error:0.00},{result.Cost:0.00}");
        return result;
    }
    private static NavVector Offset(Ship s, TargetRef t) => new((t.TargetSitu.vPosx-s.objSS.vPosx)/AutoNavCore.M_TO_AU,(t.TargetSitu.vPosy-s.objSS.vPosy)/AutoNavCore.M_TO_AU);
    private static NavVector Velocity(Ship s, TargetRef t) => new((s.objSS.vVelX-t.TargetSitu.vVelX)/AutoNavCore.M_TO_AU,(s.objSS.vVelY-t.TargetSitu.vVelY)/AutoNavCore.M_TO_AU);
}
