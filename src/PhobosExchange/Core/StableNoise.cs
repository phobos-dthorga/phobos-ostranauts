using System;
using System.Text;

namespace PhobosExchange.Core;

/// <summary>The market's random numbers, never rolled (owner rule, 7 October 2026): each one is a hash of the save's
/// seed, a key (a company, a sector or the market), the step on the time grid and a stream number, so a reload never
/// rerolls a price and the same save always produces the same market. Allocation-free.
/// <para>Part of the save contract: changing this file changes every save's future prices. It is deliberately kept in
/// Phobos Exchange rather than shared, so nothing else can change it by accident.</para>
/// <para>Mixing is Sebastiano Vigna's SplitMix64 finaliser (public domain); the normal draws use Peter J. Acklam's
/// rational approximation of the inverse normal distribution (relative error below 1.15e-9).</para></summary>
public static class StableNoise
{
    /// <summary>SplitMix64's finaliser: every input bit affects every output bit.</summary>
    public static ulong Mix(ulong z)
    {
        unchecked
        {
            z += 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }

    /// <summary>The 64-bit hash for one draw.</summary>
    public static ulong Hash(ulong seed, ulong key, long step, int stream)
    {
        unchecked
        {
            ulong h = Mix(seed ^ 0x6A09E667F3BCC909UL);
            h = Mix(h ^ key);
            h = Mix(h ^ (ulong)step);
            return Mix(h ^ ((ulong)(uint)stream * 0x9E3779B97F4A7C15UL));
        }
    }

    /// <summary>A uniform number strictly between 0 and 1 (53 bits).</summary>
    public static double Uniform(ulong seed, ulong key, long step, int stream) =>
        ((Hash(seed, key, step, stream) >> 11) + 0.5) * (1.0 / 9007199254740992.0);

    /// <summary>A standard normal number.</summary>
    public static double Normal(ulong seed, ulong key, long step, int stream) => InverseNormal(Uniform(seed, key, step, stream));

    /// <summary>The 64-bit FNV-1a hash of a text's UTF-8 bytes: the keys for ids and the seed for a player.</summary>
    public static ulong Fnv64(string text)
    {
        unchecked
        {
            ulong h = 14695981039346656037UL;
            foreach (byte b in Encoding.UTF8.GetBytes(text ?? ""))
            {
                h ^= b;
                h *= 1099511628211UL;
            }
            return h;
        }
    }

    private static readonly double[] A = { -3.969683028665376e+01, 2.209460984245205e+02, -2.759285104469687e+02, 1.383577518672690e+02, -3.066479806614716e+01, 2.506628277459239e+00 };
    private static readonly double[] B = { -5.447609879822406e+01, 1.615858368580409e+02, -1.556989798598866e+02, 6.680131188771972e+01, -1.328068155288572e+01 };
    private static readonly double[] C = { -7.784894002430293e-03, -3.223964580411365e-01, -2.400758277161838e+00, -2.549732539343734e+00, 4.374664141464968e+00, 2.938163982698783e+00 };
    private static readonly double[] D = { 7.784695709041462e-03, 3.224671290700398e-01, 2.445134137142996e+00, 3.754408661907416e+00 };
    private const double Low = 0.02425, High = 1 - Low;

    /// <summary>The standard normal number with probability <paramref name="p"/> below it (0 &lt; p &lt; 1).</summary>
    public static double InverseNormal(double p)
    {
        if (!(p > 0 && p < 1)) throw new ArgumentOutOfRangeException(nameof(p));
        if (p < Low)
        {
            double q = Math.Sqrt(-2 * Math.Log(p));
            return (((((C[0] * q + C[1]) * q + C[2]) * q + C[3]) * q + C[4]) * q + C[5]) / ((((D[0] * q + D[1]) * q + D[2]) * q + D[3]) * q + 1);
        }
        if (p <= High)
        {
            double q = p - 0.5, r = q * q;
            return (((((A[0] * r + A[1]) * r + A[2]) * r + A[3]) * r + A[4]) * r + A[5]) * q / (((((B[0] * r + B[1]) * r + B[2]) * r + B[3]) * r + B[4]) * r + 1);
        }
        double t = Math.Sqrt(-2 * Math.Log(1 - p));
        return -(((((C[0] * t + C[1]) * t + C[2]) * t + C[3]) * t + C[4]) * t + C[5]) / ((((D[0] * t + D[1]) * t + D[2]) * t + D[3]) * t + 1);
    }
}
