using System;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Processing;

// The reactor rules and guarded controls, exercised over a dictionary panel and a thermal model that
// follows the inspected FusionIC.Run constants. Not a claim about the live reactor.
internal static class ReactorChecks
{
    private sealed class FakeReactor : IReactorState
    {
        internal readonly Dictionary<string, string> Props = new() { ["slidCycle"] = "0", ["slidFlow"] = "0.2", ["knobRatio"] = "0", ["bNWZ"] = "false" };
        internal readonly HashSet<string> Conditions = new() { "IsInstalled", "IsReadyFusion" };
        internal int Writes;
        public bool Destroyed { get; set; }
        public double CoreTemperatureMeV { get; set; } = ReactorRules.IdealCoreMeV;
        public bool Has(string condition) => Conditions.Contains(condition);
        public string Read(string key) => Props.TryGetValue(key, out var value) ? value : "";
        public void Write(string key, string value) { Props[key] = value; Writes++; }
    }

    internal static void Run(Action<bool, string> check)
    {
        check(Math.Abs(ReactorRules.CoreRatio(ReactorRules.IdealCoreMeV) - 1) < 1e-12 && double.IsNaN(ReactorRules.CoreRatio(double.NaN)) &&
            double.IsNaN(ReactorRules.CoreRatio(0)), "Core ratio is the fraction of the ideal, unknown when the reading is unusable");
        check(ReactorRules.WithinBand(1.19, ReactorRules.CoreAbortBand) && !ReactorRules.WithinBand(1.21, ReactorRules.CoreAbortBand) &&
            !ReactorRules.WithinBand(double.NaN, .05), "Bands are symmetric about the ideal and reject unknown readings");
        check(Math.Abs(ReactorRules.InitialFlow(.5, 8, 4) - .9) < 1e-12 && ReactorRules.InitialFlow(.5, 8, 0) == 0 &&
            ReactorRules.InitialFlow(0, 8, 4) == 0 && ReactorRules.InitialFlow(.9, 8, 4) == 1, "Flow for cycle follows the native rule and stays on the slider");
        check(Math.Abs(ReactorRules.AdjustFlow(1.2, .5, 1, 1) - .5 / 1.2) < 1e-12 && Math.Abs(ReactorRules.AdjustFlow(.9, .5, 1, 1) - .5 / .9) < 1e-12,
            "A core outside the correction band rescales flow toward the ideal");
        check(ReactorRules.AdjustFlow(1.04, .5, 1, 1) < .5, "The hot side corrects before the game's wall-damage temperature, tighter than the course plot");
        check(Math.Abs(ReactorRules.AdjustFlow(1.02, .5, .9, 1) - .501) < 1e-12 && Math.Abs(ReactorRules.AdjustFlow(1.02, .5, 1.1, 1) - .499) < 1e-12 &&
            ReactorRules.AdjustFlow(1.02, .5, 1.0005, 1) == .5, "Inside the band flow nudges toward the thrust target and rests when it agrees");
        check(ReactorRules.AdjustFlow(1, 1, 0, 1) == 1 && ReactorRules.AdjustFlow(1, 0, 1, 0) == 0 && ReactorRules.AdjustFlow(1.5, 0, 0, 1) == ReactorRules.FlowStep &&
            ReactorRules.AdjustFlow(1, double.NaN, 1, 1) == 0, "Flow stays on the slider; zero flow is nudged, never rescaled");

        var reactor = new FakeReactor();
        check(ReactorRules.Ready(reactor), "An installed, lit reactor at the ideal core is ready");
        reactor.CoreTemperatureMeV = ReactorRules.IdealCoreMeV * 1.25;
        check(!ReactorRules.Ready(reactor) && ReactorRules.Intact(reactor), "A core outside the abort band is not ready but the reactor is intact");
        reactor.CoreTemperatureMeV = ReactorRules.IdealCoreMeV; reactor.Conditions.Add("IsOverrideOff");
        check(!ReactorRules.Ready(reactor) && !ReactorRules.Intact(reactor), "A shut-down reactor is neither ready nor intact");
        reactor.Conditions.Remove("IsOverrideOff"); reactor.Destroyed = true;
        check(!ReactorRules.Ready(reactor), "A destroyed reactor is not ready");
        reactor.Destroyed = false;
        check(!ReactorRules.IsNoWake(reactor), "Explicit false no-wake state permits");
        reactor.Props["bNWZ"] = "true"; check(ReactorRules.IsNoWake(reactor), "Explicit true no-wake state restricts");
        reactor.Props.Remove("bNWZ"); check(ReactorRules.IsNoWake(reactor), "Missing no-wake state fails closed");
        reactor.Props["bNWZ"] = "false";
        check(!ReactorRules.PilotTouchedFlow(reactor, 100), "No pilot flow stamp means automatic flow may adjust");
        reactor.Props["fFlowEpochResume"] = "105";
        check(ReactorRules.PilotTouchedFlow(reactor, 104) && !ReactorRules.PilotTouchedFlow(reactor, 105), "A pilot flow move pauses adjustment until the stamped epoch");
        check(ReactorRules.ValidControls(reactor), "Idle controls are valid");
        reactor.Props["knobRatio"] = "2"; check(!ReactorRules.ValidControls(reactor), "An unknown mode value is invalid");
        reactor.Props["knobRatio"] = "0";

        var controls = new ReactorControls(reactor);
        controls.Adopt(); reactor.Props["slidFlow"] = ".3"; controls.Adopt();
        check(controls.Idle["slidFlow"] == "0.2" && controls.Idle["knobRatio"] == "0" && controls.Idle["slidCycle"] == "0", "Adopt records the pre-ownership idle once");
        int before = reactor.Writes;
        controls.Write("knobRatio", "1"); controls.Write("slidFlow", .45); controls.Write("slidCycle", .6);
        check(reactor.Writes == before + 3 && reactor.Props["slidFlow"] == "0.45" && !controls.Differs, "Commanded values are written and remembered");
        check(!controls.WriteIfChanged("slidCycle", "0.6") && reactor.Writes == before + 3, "An unchanged value is not rewritten");
        check(controls.ChangedByPilot("slidFlow", "0.9") && !controls.ChangedByPilot("slidFlow", "0.45") && !controls.ChangedByPilot("bNWZ", "true"),
            "Only a differing external write to a flight control counts as the pilot's");
        reactor.Props["slidFlow"] = "0.9";
        check(controls.Differs, "A panel value that no longer matches the command is detected");
        reactor.Props["slidFlow"] = "0.45";
        controls.RestoreIdle(restoreFlowAndMode: true, noWake: false);
        check(reactor.Props["slidCycle"] == "0" && reactor.Props["slidFlow"] == "0.2" && reactor.Props["knobRatio"] == "0", "Release hands the idle flow and mode back");
        controls.Write("knobRatio", "1"); controls.RestoreIdle(true, noWake: true);
        check(reactor.Props["knobRatio"] == "0", "A no-wake zone keeps the game's mode interlock on release");
        controls.Write("knobRatio", "1"); controls.Write("slidFlow", .7); controls.RestoreIdle(false, false);
        check(reactor.Props["slidCycle"] == "0" && reactor.Props["knobRatio"] == "1" && reactor.Props["slidFlow"] == "0.7", "A reactor that cannot take idle settings only loses its cycle");
        controls.Forget(); check(controls.Idle.Count == 0 && !controls.Differs, "Forget drops ownership without writes");

        // Closed loop against the inspected FusionIC.Run core model (no cryos): starts from the native
        // flow-for-cycle value, settles inside the hot band and never nears the abort band.
        const double pelletMax = 4, pelletMaxTheory = 8, cycle = .6, dt = ReactorRules.FusionPeriodSeconds;
        double temperature = ReactorRules.IdealCoreMeV, flow = ReactorRules.InitialFlow(cycle, pelletMaxTheory, pelletMax), peak = 0, floor = double.MaxValue;
        int settled = 0;
        for (int tick = 0; tick < 4000; tick++)
        {
            double ratio = ReactorRules.CoreRatio(temperature);
            double target = cycle, actual = cycle * ratio; // thrust scales with the core ratio in FusionIC.Fusion
            flow = ReactorRules.AdjustFlow(ratio, flow, actual, target);
            temperature -= cycle * .2 * dt;
            double idleRate = pelletMax + (.001 - pelletMax) * Math.Min(ratio, 1);
            double pelletRate = idleRate + (pelletMax - idleRate) * flow;
            temperature += pelletRate * .04 * dt;
            peak = Math.Max(peak, temperature); floor = Math.Min(floor, temperature);
            if (tick >= 3000) settled += Math.Abs(ReactorRules.CoreRatio(temperature) - 1) <= .05 ? 1 : 0;
        }
        check(settled == 1000, $"Flow regulation settles the core inside the correction band (final {temperature / ReactorRules.IdealCoreMeV:0.000})");
        check(peak / ReactorRules.IdealCoreMeV < 1 + ReactorRules.CoreAbortBand / 2 && floor / ReactorRules.IdealCoreMeV > 1 - ReactorRules.CoreAbortBand / 2,
            $"The regulated core stays well inside the abort band (peak {peak / ReactorRules.IdealCoreMeV:0.000}, floor {floor / ReactorRules.IdealCoreMeV:0.000})");
    }
}
