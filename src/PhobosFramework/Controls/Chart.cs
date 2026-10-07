using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Phobos.Ostranauts.Framework.Controls;

/// <summary>One line on a <see cref="Chart"/>: points ordered by x. The consumer owns the arrays and may reuse them;
/// <see cref="Count"/> says how many points are in use.</summary>
public sealed class ChartSeries
{
    public double[] X = Array.Empty<double>(), Y = Array.Empty<double>();
    public int Count;
    public Tone Tone = Tone.Good;
    /// <summary>Shade the area under the line, faintly.</summary>
    public bool Fill;
}

/// <summary>A dashed level across a chart, such as an alert or what the player paid, with a short label.</summary>
public sealed class ChartLevel
{
    public double Y;
    public Tone Tone = Tone.Attention;
    public string Label = "";
}

/// <summary>What a chart shows. Bump <see cref="Version"/> whenever the data changes; the chart redraws only then (or when
/// its size changes), never per frame.</summary>
public sealed class ChartData
{
    public readonly List<ChartSeries> Series = new();
    public readonly List<ChartLevel> Levels = new();
    /// <summary>Axis and hover words, supplied by the consumer in the player's language.</summary>
    public Func<double, string> FormatX = v => v.ToString("0.##");
    public Func<double, string> FormatY = v => v.ToString("0.##");
    public int Version;
}

/// <summary>A line chart for panels (Framework 0.128.0): drawn as one mesh in the game's palette, faint slate grid lines on
/// steps of 1, 2 or 5, series thinned to the pixel columns, dashed levels, and a cursor readout while the pointer is over
/// it. Axis labels and the readout are ordinary panel text, so they follow the panel's font and the player's language.
/// Presentation only: it never reads or changes game state. Never on world sprites: live readings belong on panels.</summary>
public sealed class Chart : MaskableGraphic, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    private const float Left = 68, Bottom = 24, Top = 8, Right = 10, LineWidth = 2, GridWidth = 1, DashOn = 6, DashOff = 4;
    private const int TargetTicksY = 4, TargetTicksX = 5, MaxTicks = 12;
    private static readonly Color Grid = new(.32f, .49f, .59f, .28f);
    private ChartData? data;
    private int drawnVersion = -1;
    private Vector2 drawnSize, measuredSize;
    private bool measureDirty = true;
    private readonly double[] ticksY = new double[MaxTicks], ticksX = new double[MaxTicks];
    private int tickCountY, tickCountX;
    private double minX, maxX, minY, maxY;
    private readonly List<(double[] X, double[] Y, int Count)> thinned = new();
    private readonly List<(double[] X, double[] Y)> buffers = new();
    private readonly double[] range = new double[2];
    private readonly List<TMP_Text> labels = new();
    private TMP_Text? readout;
    private bool hovering, labelsDirty;
    private Vector2 pointer;

    /// <summary>Adds a chart of a fixed height to a panel's layout.</summary>
    public static Chart Create(Transform parent, float height)
    {
        var rect = PanelWidgets.Rect(parent, "Chart");
        var layout = rect.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = layout.preferredHeight = height; layout.flexibleWidth = 1;
        rect.gameObject.AddComponent<CanvasRenderer>();
        var chart = rect.gameObject.AddComponent<Chart>();
        chart.raycastTarget = true;
        return chart;
    }

    /// <summary>Shows data; redraws only when its version or the chart's size has changed.</summary>
    public void Show(ChartData chartData)
    {
        if (ReferenceEquals(chartData, data) && chartData.Version == drawnVersion && rectTransform.rect.size == drawnSize) return;
        data = chartData;
        Rebuild();
    }

    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        if (data != null && rectTransform.rect.size != drawnSize) { measureDirty = true; labelsDirty = true; SetVerticesDirty(); }
    }

    private void Rebuild()
    {
        drawnVersion = data?.Version ?? -1;
        measureDirty = true;
        labelsDirty = true;
        SetVerticesDirty();
    }

    /// <summary>Ranges, ticks and thinned points, worked out again only when the data or the size changed (never on a
    /// pointer move), into buffers that are kept.</summary>
    private void EnsureMeasured()
    {
        var size = rectTransform.rect.size;
        if (!measureDirty && size == measuredSize) return;
        measureDirty = false; measuredSize = size; labelsDirty = true;
        Measure(Plot());
    }

    private void Measure(Rect plot)
    {
        thinned.Clear();
        minX = double.PositiveInfinity; maxX = double.NegativeInfinity; minY = double.PositiveInfinity; maxY = double.NegativeInfinity;
        if (data == null) return;
        foreach (var s in data.Series)
        {
            int n = Math.Min(s.Count, Math.Min(s.X.Length, s.Y.Length));
            if (n <= 0) continue;
            minX = Math.Min(minX, s.X[0]); maxX = Math.Max(maxX, s.X[n - 1]);
            ChartRules.Range(s.Y, n, 0, out double lo, out double hi);
            minY = Math.Min(minY, lo); maxY = Math.Max(maxY, hi);
        }
        foreach (var level in data.Levels) { minY = Math.Min(minY, level.Y); maxY = Math.Max(maxY, level.Y); }
        if (double.IsInfinity(minX) || !(maxX > minX)) { minX = 0; maxX = Math.Max(1, double.IsInfinity(maxX) ? 1 : maxX); }
        if (double.IsInfinity(minY)) { minY = 0; maxY = 1; }
        range[0] = minY; range[1] = maxY;
        ChartRules.Range(range, 2, 0.08, out minY, out maxY);
        int columns = Math.Max(1, (int)plot.width);
        for (int k = 0; k < data.Series.Count; k++)
        {
            var s = data.Series[k];
            int n = Math.Min(s.Count, Math.Min(s.X.Length, s.Y.Length));
            int need = Math.Max(1, Math.Min(n, 2 * columns + 1));
            if (buffers.Count <= k) buffers.Add((new double[need], new double[need]));
            else if (buffers[k].X.Length < need) buffers[k] = (new double[need], new double[need]);
            var (outX, outY) = buffers[k];
            thinned.Add((outX, outY, ChartRules.Thin(s.X, s.Y, n, minX, maxX, columns, outX, outY)));
        }
        tickCountY = ChartRules.Ticks(minY, maxY, TargetTicksY, ticksY);
        tickCountX = ChartRules.Ticks(minX, maxX, TargetTicksX, ticksX);
    }

    private Rect Plot()
    {
        var r = rectTransform.rect;
        return new Rect(r.xMin + Left, r.yMin + Bottom, Math.Max(1, r.width - Left - Right), Math.Max(1, r.height - Bottom - Top));
    }

    private Vector2 At(Rect plot, double x, double y) =>
        new(plot.xMin + (float)ChartRules.Map(x, minX, maxX, plot.width), plot.yMin + (float)ChartRules.Map(y, minY, maxY, plot.height));

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        drawnSize = rectTransform.rect.size;
        if (data == null) return;
        var plot = Plot();
        EnsureMeasured();
        for (int t = 0; t < tickCountY; t++) { var a = At(plot, minX, ticksY[t]); Segment(vh, new Vector2(plot.xMin, a.y), new Vector2(plot.xMax, a.y), GridWidth, Grid); }
        for (int t = 0; t < tickCountX; t++) { var a = At(plot, ticksX[t], minY); Segment(vh, new Vector2(a.x, plot.yMin), new Vector2(a.x, plot.yMax), GridWidth, Grid); }
        for (int s = 0; s < data.Series.Count && s < thinned.Count; s++)
        {
            var series = data.Series[s]; var (xs, ys, n) = thinned[s];
            var ink = Tones.Of(series.Tone);
            if (series.Fill)
            {
                var shade = new Color(ink.r, ink.g, ink.b, .12f);
                for (int i = 1; i < n; i++)
                {
                    var a = At(plot, xs[i - 1], ys[i - 1]); var b = At(plot, xs[i], ys[i]);
                    Quad(vh, new Vector2(a.x, plot.yMin), a, b, new Vector2(b.x, plot.yMin), shade);
                }
            }
            for (int i = 1; i < n; i++) Segment(vh, At(plot, xs[i - 1], ys[i - 1]), At(plot, xs[i], ys[i]), LineWidth, ink);
            if (n > 0) { var last = At(plot, xs[n - 1], ys[n - 1]); Box(vh, last, 3, ink); }
        }
        foreach (var level in data.Levels)
        {
            var ink = Tones.Of(level.Tone);
            float y = At(plot, minX, level.Y).y;
            for (float x = plot.xMin; x < plot.xMax; x += DashOn + DashOff)
                Segment(vh, new Vector2(x, y), new Vector2(Math.Min(plot.xMax, x + DashOn), y), LineWidth, ink);
        }
        if (hovering && data.Series.Count > 0 && thinned.Count > 0 && thinned[0].Count > 0)
        {
            var (xs, ys, n) = thinned[0];
            int nearest = Nearest(plot, xs, n);
            var p = At(plot, xs[nearest], ys[nearest]);
            Segment(vh, new Vector2(p.x, plot.yMin), new Vector2(p.x, plot.yMax), GridWidth, new Color(1, 1, 1, .35f));
            Box(vh, p, 4, ConsoleWidgets.Amber);
        }
    }

    private int Nearest(Rect plot, double[] xs, int n)
    {
        double x = minX + (pointer.x - plot.xMin) / Math.Max(1, plot.width) * (maxX - minX);
        int best = 0; double gap = double.MaxValue;
        for (int i = 0; i < n; i++) { double d = Math.Abs(xs[i] - x); if (d < gap) { gap = d; best = i; } }
        return best;
    }

    private void LateUpdate()
    {
        if (data == null) return;
        EnsureMeasured();
        if (!labelsDirty) return;
        labelsDirty = false;
        var plot = Plot();
        int used = 0;
        for (int t = 0; t < tickCountY; t++)
        {
            var a = At(plot, minX, ticksY[t]);
            Place(used++, data.FormatY(ticksY[t]), new Vector2(rectTransform.rect.xMin + 2, a.y - 10), new Vector2(Left - 6, 20), TextAlignmentOptions.MidlineRight);
        }
        for (int t = 0; t < tickCountX; t++)
        {
            var a = At(plot, ticksX[t], minY);
            Place(used++, data.FormatX(ticksX[t]), new Vector2(a.x - 40, rectTransform.rect.yMin), new Vector2(80, Bottom - 2), TextAlignmentOptions.Midline);
        }
        foreach (var level in data.Levels)
        {
            if (string.IsNullOrEmpty(level.Label)) continue;
            var a = At(plot, minX, level.Y);
            Place(used++, level.Label, new Vector2(plot.xMin + 4, a.y + 1), new Vector2(Math.Max(40, plot.width - 8), 20), TextAlignmentOptions.BottomLeft);
        }
        for (int i = used; i < labels.Count; i++) labels[i].gameObject.SetActive(false);
        UpdateReadout(plot);
    }

    private void Place(int index, string text, Vector2 position, Vector2 size, TextAlignmentOptions align)
    {
        while (labels.Count <= index)
        {
            var label = PanelWidgets.Label(transform, "", false);
            label.fontSize = 14; label.textWrappingMode = TextWrappingModes.NoWrap; label.overflowMode = TextOverflowModes.Overflow;
            var r = (RectTransform)label.transform; r.anchorMin = r.anchorMax = new Vector2(0, 0); r.pivot = Vector2.zero;
            labels.Add(label);
        }
        var l = labels[index];
        l.gameObject.SetActive(true);
        if (l.text != text) l.text = text;
        l.alignment = align;
        var rect = (RectTransform)l.transform;
        var origin = rectTransform.rect.min;
        rect.anchoredPosition = position - origin; rect.sizeDelta = size;
    }

    private void UpdateReadout(Rect plot)
    {
        if (!hovering || data == null || thinned.Count == 0 || thinned[0].Count == 0) { if (readout != null) readout.gameObject.SetActive(false); return; }
        if (readout == null)
        {
            readout = PanelWidgets.Label(transform, "", false);
            readout.fontSize = 14; readout.color = ConsoleWidgets.Amber; readout.textWrappingMode = TextWrappingModes.NoWrap;
            var r = (RectTransform)readout.transform; r.anchorMin = r.anchorMax = new Vector2(0, 0); r.pivot = Vector2.zero; r.sizeDelta = new Vector2(220, 20);
        }
        var (xs, ys, n) = thinned[0];
        int i = Nearest(plot, xs, n);
        readout.gameObject.SetActive(true);
        readout.text = data.FormatX(xs[i]) + "  " + data.FormatY(ys[i]);
        float x = Math.Min(plot.xMax - 220, Math.Max(plot.xMin, At(plot, xs[i], ys[i]).x + 6));
        ((RectTransform)readout.transform).anchoredPosition = new Vector2(x, plot.yMax - 20) - rectTransform.rect.min;
    }

    public void OnPointerEnter(PointerEventData eventData) { hovering = true; Move(eventData); }
    public void OnPointerExit(PointerEventData eventData) { hovering = false; labelsDirty = true; SetVerticesDirty(); }
    public void OnPointerMove(PointerEventData eventData) => Move(eventData);

    private void Move(PointerEventData eventData)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera ?? eventData.enterEventCamera, out pointer)) return;
        labelsDirty = true;
        SetVerticesDirty();
    }

    private static void Quad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
    {
        int s = vh.currentVertCount;
        vh.AddVert(a, color, Vector2.zero); vh.AddVert(b, color, Vector2.zero); vh.AddVert(c, color, Vector2.zero); vh.AddVert(d, color, Vector2.zero);
        vh.AddTriangle(s, s + 1, s + 2); vh.AddTriangle(s, s + 2, s + 3);
    }

    private static void Box(VertexHelper vh, Vector2 at, float half, Color color) =>
        Quad(vh, at + new Vector2(-half, -half), at + new Vector2(-half, half), at + new Vector2(half, half), at + new Vector2(half, -half), color);

    private static void Segment(VertexHelper vh, Vector2 a, Vector2 b, float width, Color color)
    {
        var n = new Vector2(b.y - a.y, a.x - b.x);
        if (n.sqrMagnitude < 1e-6f) return;
        n = n.normalized * (width / 2);
        Quad(vh, a - n, a + n, b + n, b - n, color);
    }
}
