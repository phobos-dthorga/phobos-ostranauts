using System;

namespace PhobosExchange.Core;

/// <summary>Exact steps for the market's random processes. Every process is an Ornstein–Uhlenbeck (OU) process, so a step
/// of any length, a minute or fifty years, is one exact draw from its known transition: the price at a moment does not
/// depend on how time reached it (owner requirement, 7 October 2026). Short steps need care, because the variances are
/// differences of nearly equal numbers; the series below keep them accurate.</summary>
public static class ExactMath
{
    /// <summary>(1 − e^−x)/x − 1, accurate for small x (series) and exact in the limit of large x.</summary>
    public static double PhiMinusOne(double x)
    {
        if (x < 0.1)
        {
            // Σ_{n≥1} (−x)^n / (n+1)!
            double term = 1, sum = 0;
            for (int n = 1; n <= 12; n++)
            {
                term *= -x / (n + 1);
                sum += term;
            }
            return sum;
        }
        return -Expm1(-x) / x - 1;
    }

    /// <summary>e^x − 1, without losing small x to rounding.</summary>
    public static double Expm1(double x) => Math.Abs(x) < 1e-5 ? x + 0.5 * x * x + x * x * x / 6 : Math.Exp(x) - 1;

    /// <summary>(1 − e^−x)/x.</summary>
    public static double Phi(double x) => 1 + PhiMinusOne(x);

    /// <summary>φ(2a) + φ(2b) − 2φ(a+b), the variance of the difference of two OU processes driven by the same noise over a
    /// step, divided by the step. Its first two series terms cancel exactly, so small steps use the series.</summary>
    public static double PhiCombo(double a, double b)
    {
        if (Math.Max(a, b) < 0.05)
        {
            // Σ_{n≥2} (−1)^n [(2a)^n + (2b)^n − 2(a+b)^n] / (n+1)!
            double sum = 0, pa = 1, pb = 1, ps = 1, fact = 1;
            for (int n = 1; n <= 14; n++)
            {
                pa *= 2 * a; pb *= 2 * b; ps *= a + b; fact *= n + 1;
                if (n < 2) continue;
                double bracket = n == 2 ? 2 * (a - b) * (a - b) : pa + pb - 2 * ps;
                sum += (n % 2 == 0 ? 1 : -1) * bracket / fact;
            }
            return sum;
        }
        return Phi(2 * a) + Phi(2 * b) - 2 * Phi(a + b);
    }
}

/// <summary>A plain OU process with unit noise: X ← aX + c·z over a step of <c>dt</c> seconds, with a = e^−k·dt and
/// c² = dt·φ(2k·dt) (the exact variance). The caller scales the noise.</summary>
public sealed class OuKernel
{
    public readonly double K;
    private double cachedDt = double.NaN, cachedA, cachedC;

    public OuKernel(double halfLifeSeconds)
    {
        if (!(halfLifeSeconds > 0)) throw new ArgumentOutOfRangeException(nameof(halfLifeSeconds));
        K = Math.Log(2) / halfLifeSeconds;
    }

    /// <summary>The decay and noise factors for a step; the last step's are kept, so fine steps cost no exponent.</summary>
    public void Factors(double dt, out double a, out double c)
    {
        if (dt != cachedDt) { cachedDt = dt; cachedA = Math.Exp(-K * dt); cachedC = Math.Sqrt(dt * ExactMath.Phi(2 * K * dt)); }
        a = cachedA; c = cachedC;
    }

    /// <summary>The stationary standard deviation for unit noise.</summary>
    public double StationarySd => Math.Sqrt(1 / (2 * K));

    /// <summary>The variance of a change over <paramref name="seconds"/> from a stationary start, for unit noise:
    /// (1 − e^−kT)/k.</summary>
    public double VarianceOfChange(double seconds) => seconds * ExactMath.Phi(K * seconds);
}

/// <summary>A trend phase (Phobos Exchange 0.1.0): the difference of a slow and a fast OU process driven by the same noise,
/// scaled to a set standard deviation. The noise cancels over short steps, so the level moves smoothly, with momentum:
/// a rise carries on for weeks, then turns. Its variance stays bounded however long the save runs, which keeps the
/// expected return within the guard. State: the slow process <c>slow</c> and the gap <c>gap</c> = slow − fast; the level is
/// A·gap.</summary>
public sealed class TrendKernel
{
    public readonly double Ks, Kf, A, Sd;
    private readonly double vs, vf, c;
    private double cachedDt = double.NaN, aS, aF, l11, l21, l22;

    public TrendKernel(double slowSeconds, double fastSeconds, double sd)
    {
        if (!(slowSeconds > fastSeconds) || !(fastSeconds > 0) || !(sd >= 0)) throw new ArgumentOutOfRangeException(nameof(slowSeconds));
        Ks = Math.Log(2) / slowSeconds; Kf = Math.Log(2) / fastSeconds; Sd = sd;
        vs = 1 / (2 * Ks); vf = 1 / (2 * Kf); c = 1 / (Ks + Kf);
        double gapVariance = vs + vf - 2 * c;
        A = sd <= 0 ? 0 : sd / Math.Sqrt(gapVariance);
    }

    public static TrendKernel FromWeeks(double slowWeeks, double fastWeeks, double sd) =>
        new(slowWeeks * ExchangeRules.WeekSeconds, fastWeeks * ExchangeRules.WeekSeconds, sd);

    /// <summary>The level the phase adds to the log price.</summary>
    public double Level(double gap) => A * gap;

    /// <summary>How fast the level is moving, in log units per second (the direction of the phase).</summary>
    public double Speed(double slow, double gap) => A * ((Kf - Ks) * slow - Kf * gap);

    /// <summary>One exact step of <paramref name="dt"/> seconds with two standard normal numbers.</summary>
    public void Step(ref double slow, ref double gap, double dt, double z1, double z2)
    {
        if (dt != cachedDt)
        {
            cachedDt = dt;
            double a = Ks * dt, b = Kf * dt;
            aS = Math.Exp(-a); aF = Math.Exp(-b);
            double varS = dt * ExactMath.Phi(2 * a);
            // Cov(n_slow, n_gap) = Var(n_slow) − Cov(n_slow, n_fast) = dt·[φ(2a) − φ(a+b)].
            double covSG = dt * (ExactMath.PhiMinusOne(2 * a) - ExactMath.PhiMinusOne(a + b));
            double varG = dt * ExactMath.PhiCombo(a, b);
            l11 = Math.Sqrt(varS); l21 = l11 > 0 ? covSG / l11 : 0; l22 = Math.Sqrt(Math.Max(0, varG - l21 * l21));
        }
        double s = slow, g = gap;
        // gap' = slow' − fast' = aS·slow − aF·(slow − gap) + noise
        slow = aS * s + l11 * z1;
        gap = (aS - aF) * s + aF * g + l21 * z1 + l22 * z2;
    }

    /// <summary>A draw from the stationary distribution, for a new market or a newly listed company.</summary>
    public void Stationary(out double slow, out double gap, double z1, double z2)
    {
        double varG = vs + vf - 2 * c, covSG = vs - c;
        double m11 = Math.Sqrt(vs), m21 = covSG / m11, m22 = Math.Sqrt(Math.Max(0, varG - m21 * m21));
        slow = m11 * z1; gap = m21 * z1 + m22 * z2;
    }

    /// <summary>The variance of the level's change over <paramref name="seconds"/> from a stationary start.</summary>
    public double VarianceOfChange(double seconds)
    {
        double cov = A * A * (Math.Exp(-Ks * seconds) * (vs - c) + Math.Exp(-Kf * seconds) * (vf - c));
        return Math.Max(0, 2 * (Sd * Sd - cov));
    }
}
