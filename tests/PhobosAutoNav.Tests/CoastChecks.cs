using System;
using PhobosAutoNav.Core;

internal static class CoastChecks
{
    internal static int Run()
    {
        int checks = 0;
        void Check(bool condition, string name) { if (!condition) throw new Exception(name); checks++; }
        var settings = new CoastSettings(3, 10, .75, 2);
        CoastDecision Decide(bool coasting, double error, double cross = 0, bool braking = false, double radius = 1000)
        {
            Check(CoastRules.TryDecide(coasting, 100, error, cross, radius, braking, settings, out var decision), "Valid cruise decision");
            return decision;
        }
        Check(Decide(false, 7.5).Coasting, "Stop cruise corrections at 7.5 m/s rather than inherited 0.6 m/s");
        Check(Decide(true, 10).Coasting, "Remain coasting at outer band boundary");
        Check(!Decide(true, 10.1).Coasting, "A large deviation resumes corrections");
        Check(!Decide(false, 9).Coasting && Decide(true, 9).Coasting, "Real hysteresis in the intermediate band");
        var pulse = Decide(false, 12);
        double after = 12 * (1 - pulse.CorrectionFraction);
        Check(Math.Abs(after - 7.5) < 1e-9 && Decide(false, after).Coasting, "One sufficient correction ends at the inner band, not zero");
        Check(Decide(false, 0).CorrectionFraction == 0, "Exact course requires no burn");
        Check(!Decide(true, 9, 9).Coasting, "30 second sideways drift exceeding 250 m breaks a coast");
        Check(!Decide(true, 1, 1, radius: 100).Coasting, "Closer arrivals tighten cross-track allowance");
        Check(Decide(true, 2, 0).Coasting, "Low longitudinal error is acceptable");
        Check(!Decide(true, 0, braking: true).Coasting && Decide(true, .1, braking: true).CorrectionFraction == 1,
            "Braking overrides both cruise bands, including small errors");
        Check(CoastRules.TryDecide(false, 10, 2, 0, 1000, false, settings, out var slow)
            && slow.ResumeToleranceMS == 3, "Existing absolute tolerance remains a floor");
        Check(CoastRules.TryDecide(false, 5000, 350, 0, 1000, false, settings, out var fast)
            && fast.Coasting && fast.CrossTrackToleranceMS < 9, "Relative speed band never widens the drift guard");
        Check(CoastRules.CoastRotation(0, 1, .5) == 0, "No target-heading chasing when there is no spin");
        foreach (double spin in new[] { -.5, -.1, .1, .5 })
        foreach (double dt in new[] { .1, 1, 10 })
        {
            double rotation = CoastRules.CoastRotation(spin, dt, .3);
            Check(rotation * spin < 0 && Math.Abs(rotation) <= .3, "Coast spin damping opposes spin within command cap");
        }
        Check(CoastRules.TryBrakingSpeedLimit(1000, 100, 0, 1, 1, out var limit), "Valid stopping envelope");
        Check(limit < Math.Sqrt(2 * 1 * .85 * 1000), "Reserve next step and aggregate two-axis braking cost");
        Check(CoastRules.TryBrakingSpeedLimit(100, 100, 0, 1, 10, out var imminent) && imminent == 0,
            "Do not coast through the stopping boundary in a large step");
        Check(CoastRules.TryBrakingSpeedLimit(0, 100, 20, 1, 1, out var flyby) && flyby == 20,
            "Nonzero requested arrival speed is retained");
        foreach (double bad in new[] { double.NaN, double.PositiveInfinity, -1d })
        {
            Check(!CoastRules.TryDecide(false, 100, bad, 0, 1000, false, settings, out _), "Invalid velocity error rejected");
            Check(!CoastRules.TryDecide(false, 100, 0, bad, 1000, false, settings, out _), "Invalid cross-track data rejected");
            Check(!CoastRules.TryBrakingSpeedLimit(bad, 0, 0, 1, 1, out _), "Invalid stopping distance rejected");
            Check(!new CoastSettings(3, bad, .75, 2).IsValid, "Invalid coasting settings rejected");
        }
        Check(!new CoastSettings(3, 10, 1, 2).IsValid, "Entry and exit bands must remain distinct");
        Check(!CoastRules.TryBrakingSpeedLimit(1000, 100, 0, 1, 0, out _), "Zero simulation step rejected");

        // Compare summed correction delta-v (not in-game fuel) on a slowly
        // varying desired cruise velocity. Both policies see the same stimulus.
        double SimulatedCorrections(bool economy)
        {
            double velocity = 100, spent = 0;
            bool coasting = false;
            for (int second = 0; second < 600; second++)
            {
                double desired = 100 + 4 * Math.Sin(second * Math.PI / 60);
                double error = Math.Abs(desired - velocity);
                double gain;
                if (economy)
                {
                    var decision = Decide(coasting, error);
                    coasting = decision.Coasting;
                    gain = decision.CorrectionFraction;
                }
                else
                {
                    if (coasting && error > 3) coasting = false;
                    else if (!coasting && error < .6) coasting = true;
                    gain = coasting ? 0 : 1;
                }
                double change = Math.Clamp((desired - velocity) * gain, -1, 1);
                spent += Math.Abs(change);
                velocity += change;
            }
            return spent;
        }
        double oldCorrections = SimulatedCorrections(false), newCorrections = SimulatedCorrections(true);
        Check(oldCorrections > 0 && newCorrections < oldCorrections, "Cruise perturbations produce fewer corrective burns");

        // Bounded straight-line integration through the existing arrival brake.
        // This is a numerical harness, not the game's orbit or fuel simulation.
        foreach (double start in new[] { 2500d, 10000d, 100000d })
        foreach (double acceleration in new[] { .1, 1, 5 })
        foreach (double dt in new[] { .25, 1, 5, 10 })
        {
            const double radius = 1000, cruise = 100, arrivalTolerance = .5;
            double range = start, velocity = 0;
            bool coasting = false, arrived = false;
            for (int step = 0; step < 100000; step++)
            {
                if (ArrivalBrake.NeedsBrake(range, radius, Math.Abs(velocity), 0, arrivalTolerance))
                {
                    Check(ArrivalBrake.TryCommand(velocity, 0, 0, acceleration, 1, 0, dt, out var brake), "Arrival brake available");
                    velocity += brake.X * acceleration * dt;
                }
                else if (range <= radius * ApproachRules.ArrivalBandMultiplier && Math.Abs(velocity) <= arrivalTolerance)
                { arrived = true; break; }
                else
                {
                    Check(CoastRules.TryBrakingSpeedLimit(Math.Max(0, range - radius * ApproachRules.ArrivalBandMultiplier),
                        velocity, 0, acceleration, dt, out var safeSpeed), "Approach stopping envelope available");
                    double desired = Math.Min(cruise, Math.Sqrt(2 * acceleration * .85 * Math.Max(0, range - radius)));
                    desired = Math.Min(desired, safeSpeed);
                    bool braking = desired < cruise || Math.Abs(velocity) > safeSpeed;
                    Check(CoastRules.TryDecide(coasting, cruise, Math.Abs(desired - velocity), 0, radius,
                        braking, settings, out var decision), "Approach cruise decision available");
                    coasting = decision.Coasting;
                    double delta = Math.Clamp((desired - velocity) * decision.CorrectionFraction, -acceleration * dt, acceleration * dt);
                    velocity += delta;
                }
                Check(velocity >= -1e-8 && range > 0, "Approach does not reverse or pass through target");
                range -= velocity * dt;
            }
            Check(arrived && Math.Abs(velocity) <= arrivalTolerance, "Economy cruise still reaches arrival-speed tolerance");
        }
        Console.WriteLine($"Coasting harness: corrective delta-v {oldCorrections:0.##} -> {newCorrections:0.##} m/s in a bounded cruise-perturbation scenario; not measured game fuel.");
        return checks;
    }
}
