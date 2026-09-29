using System;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Propulsion;

/// <summary>What each gas is worth as cold-gas RCS remass, and the buffered-draw bookkeeping, on numbers alone.
/// The live RCS loops are exercised by the native suite against the game's own definitions.</summary>
internal static class PropellantChecks
{
    internal static void Run(Action<bool, string> check)
    {
        void Reject(Action action, string label) { bool failed = false; try { action(); } catch { failed = true; } check(failed, label); }
        // The game's fixed exhaust speed, 5.26077e-9 AU/s, is 787 m/s: cold nitrogen to vacuum near 298 K.
        double nitrogen = Math.Sqrt(2 * 1.4 / 0.4 * 8.314462618 * 298 / 0.0280134);
        check(Math.Abs(nitrogen - 787) < 3 && Math.Abs(5.26077e-9 * 149597870700 - 787) < 1, "The game's RCS exhaust speed is cold nitrogen's");
        check(RcsPropellant.ExhaustRatio("N2") == 1 && RcsPropellant.ExhaustRatio(null) == 1 && RcsPropellant.ExhaustRatio("Smoke") == 1 && RcsPropellant.ExhaustRatio("Plasma") == 1,
            "Nitrogen, and anything without data, keeps the vanilla worth exactly");
        check(Math.Abs(RcsPropellant.ExhaustRatio("H2") - 3.69) < 0.03, "Hydrogen is worth about 3.7 times nitrogen per kilogram");
        check(Math.Abs(RcsPropellant.ExhaustRatio("CH4") - 1.45) < 0.02, "Methane is worth about 1.45 times nitrogen");
        check(Math.Abs(RcsPropellant.ExhaustRatio("O2") - 0.935) < 0.01 && Math.Abs(RcsPropellant.ExhaustRatio("CO2") - 0.90) < 0.02,
            "Oxygen and carbon dioxide are slightly weaker than nitrogen");
        check(Math.Abs(RcsPropellant.ExhaustRatio("CO") - 1) < 0.005, "Carbon monoxide, with nitrogen's mass and heat-capacity ratio, is worth the same");
        check(RcsPropellant.MixtureRatio(new Dictionary<string, double>()) == 1, "An empty mixture is worth 1");
        double mix = RcsPropellant.MixtureRatio(new Dictionary<string, double> { ["N2"] = 3, ["CH4"] = 1 });
        check(Math.Abs(mix - (3 + RcsPropellant.ExhaustRatio("CH4")) / 4) < 1e-12, "A mixture is worth the mass-weighted ratio of its parts");
        check(RcsPropellant.MixtureRatio(new Dictionary<string, double> { ["N2"] = double.NaN, ["O2"] = -1 }) == 1, "Invalid parts are ignored");
        check(Math.Abs(160 * RcsPropellant.ExhaustRatio("CH4") - 232) < 3 && Math.Abs(24 * RcsPropellant.ExhaustRatio("H2") - 88.5) < 1.5,
            "A full methane store is about 232 kg of nitrogen-equivalent, a full hydrogen store about 88 kg");

        // Buffered draws never take more than the vessel held, and settlement clears the debt.
        var ledger = new DrawLedger(10);
        check(ledger.Take(4) == 4 && ledger.Take(7) == 6 && ledger.AvailableKg == 0 && ledger.OwedKg == 10, "Draws stop at what the vessel held");
        check(ledger.Take(1) == 0, "An exhausted ledger gives nothing");
        ledger.Settled(3);
        check(ledger.OwedKg == 0 && ledger.AvailableKg == 3 && ledger.HeldKg == 3, "Settlement clears the debt and takes a fresh reading");
        Reject(() => new DrawLedger(-1), "A negative holding is refused");
        Reject(() => ledger.Take(double.NaN), "An invalid draw is refused");
        Reject(() => ledger.Take(-2), "A negative draw is refused");
    }
}
