using System;

namespace Phobos.Ostranauts.Framework.Controls;

/// <summary>The arithmetic behind <see cref="Chart"/> (Framework 0.128.0): value ranges, tick steps of 1, 2 or 5 times a
/// power of ten, and thinning a long series to at most a lowest and a highest point per pixel column, so a chart's mesh
/// stays small however much history it shows. No game or Unity types, so the offline checks run it.</summary>
public static class ChartRules
{
    /// <summary>A tick step of 1, 2 or 5 times a power of ten giving about <paramref name="target"/> ticks over a span.</summary>
    public static double NiceStep(double span, int target)
    {
        if (!(span > 0) || double.IsInfinity(span) || target < 1) return 1;
        double raw = span / target;
        double power = Math.Pow(10, Math.Floor(Math.Log10(raw)));
        double unit = raw / power;
        double nice = unit <= 1 ? 1 : unit <= 2 ? 2 : unit <= 5 ? 5 : 10;
        return nice * power;
    }

    /// <summary>Ticks on multiples of the nice step within [min, max], written to <paramref name="ticks"/>; returns how many.</summary>
    public static int Ticks(double min, double max, int target, double[] ticks)
    {
        if (ticks == null || ticks.Length == 0 || !(max > min)) return 0;
        double step = NiceStep(max - min, target);
        double first = Math.Ceiling(min / step - 1e-9) * step;
        int n = 0;
        for (double v = first; v <= max + step * 1e-9 && n < ticks.Length; v = first + n * step)
            ticks[n++] = Math.Abs(v) < step * 1e-9 ? 0 : v;
        return n;
    }

    /// <summary>The value range to draw: the data's lowest and highest, padded by a share of the span; a flat or single
    /// value gets a small span around it; no finite data gives 0 to 1.</summary>
    public static void Range(double[] values, int count, double padShare, out double min, out double max)
    {
        min = double.PositiveInfinity; max = double.NegativeInfinity;
        for (int i = 0; i < Math.Min(count, values?.Length ?? 0); i++)
        {
            double v = values![i];
            if (double.IsNaN(v) || double.IsInfinity(v)) continue;
            if (v < min) min = v;
            if (v > max) max = v;
        }
        if (double.IsInfinity(min)) { min = 0; max = 1; return; }
        double span = max - min;
        if (span <= Math.Abs(max) * 1e-9 || span <= 1e-12)
        {
            double half = Math.Max(Math.Abs(max) * 0.05, 1e-6);
            min -= half; max += half; return;
        }
        min -= span * padShare; max += span * padShare;
    }

    /// <summary>A value's position across <paramref name="pixels"/>, 0 at <paramref name="min"/>.</summary>
    public static double Map(double v, double min, double max, double pixels) => max > min ? (v - min) / (max - min) * pixels : pixels / 2;

    /// <summary>Thins a series ordered by x to at most two points per pixel column (the lowest and highest, in the order
    /// they occur), keeping the first and last point. Returns the number of points written.</summary>
    public static int Thin(double[] xs, double[] ys, int count, double x0, double x1, int columns, double[] outX, double[] outY)
    {
        count = Math.Min(count, Math.Min(xs?.Length ?? 0, ys?.Length ?? 0));
        int capacity = Math.Min(outX?.Length ?? 0, outY?.Length ?? 0);
        if (count <= 0 || capacity == 0) return 0;
        if (columns < 1) columns = 1;
        if (count <= capacity && count <= 2 * columns)
        {
            for (int i = 0; i < count; i++) { outX![i] = xs![i]; outY![i] = ys![i]; }
            return count;
        }
        int n = 0;
        int column = -1, lowAt = -1, highAt = -1;
        void Flush()
        {
            if (lowAt < 0) return;
            int a = Math.Min(lowAt, highAt), b = Math.Max(lowAt, highAt);
            if (n < capacity) { outX![n] = xs![a]; outY![n] = ys![a]; n++; }
            if (b != a && n < capacity) { outX![n] = xs![b]; outY![n] = ys![b]; n++; }
        }
        for (int i = 0; i < count; i++)
        {
            int c = (int)Math.Floor(Map(xs![i], x0, x1, columns));
            if (c < 0) c = 0;
            if (c >= columns) c = columns - 1;
            if (c != column)
            {
                Flush();
                column = c; lowAt = highAt = i;
                continue;
            }
            if (ys![i] < ys[lowAt]) lowAt = i;
            if (ys[i] > ys[highAt]) highAt = i;
        }
        Flush();
        // The newest point always ends the line, so the chart reaches "now".
        if (n > 0 && outX![n - 1] != xs![count - 1])
        {
            if (n < capacity) n++;
            outX[n - 1] = xs[count - 1]; outY![n - 1] = ys![count - 1];
        }
        return n;
    }
}
