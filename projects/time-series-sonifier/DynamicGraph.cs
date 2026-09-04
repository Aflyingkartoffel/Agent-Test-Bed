using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace TimeSeriesSonifier;

public readonly record struct GraphDisplayDomain(double VisibleStartTime, double VisibleEndTime, double VisibleMinY, double VisibleMaxY)
{
    public double TimeSpan => Math.Max(1e-12, VisibleEndTime - VisibleStartTime);
    public double YSpan => Math.Max(1e-12, VisibleMaxY - VisibleMinY);
}

public static class DynamicGraphDomain
{
    public const double InitialDomainFraction = .03;
    static readonly ConditionalWeakTable<MappedDataSeries, PrefixExtrema> extrema = new();
    public static GraphDisplayDomain Calculate(MappedDataSeries series, CurrentDataState state, double progress)
    {
        var p = Math.Clamp(double.IsFinite(progress) ? progress : 0, 0, 1); var total = Math.Max(1e-12, series.MaximumTime - series.MinimumTime); var end = series.MinimumTime + total * (InitialDomainFraction + (1 - InitialDomainFraction) * p); if (double.IsFinite(state.CurrentTime)) end = Math.Max(end, state.CurrentTime); end = Math.Clamp(end, series.MinimumTime + Math.Min(total, 1e-12), series.MaximumTime); var last = LastIndexAtOrBefore(series.Points, end); var prefix = extrema.GetValue(series, static key => new PrefixExtrema(key)); var min = prefix.Minimum[last]; var max = prefix.Maximum[last]; if (last < series.Points.Count - 1 && state.CurrentTime <= end) { min = Math.Min(min, state.CurrentMappedValue); max = Math.Max(max, state.CurrentMappedValue); } if (series.Mode == MappingMode.AbsoluteValue && min >= 0) min = 0; var range = Math.Max(1e-12, max - min); var padding = range * .08; min -= padding; max += padding; if (series.Mode == MappingMode.AbsoluteValue && min < 0 && prefix.NonNegative[last]) min = 0; return new(series.MinimumTime, end, min, max);
    }
    sealed class PrefixExtrema
    {
        public readonly double[] Minimum; public readonly double[] Maximum; public readonly bool[] NonNegative;
        public PrefixExtrema(MappedDataSeries series) { Minimum = new double[series.Points.Count]; Maximum = new double[series.Points.Count]; NonNegative = new bool[series.Points.Count]; var min = double.PositiveInfinity; var max = double.NegativeInfinity; var nonNegative = true; for (var i = 0; i < series.Points.Count; i++) { var value = series.Points[i].MappedValue; min = Math.Min(min, value); max = Math.Max(max, value); nonNegative &= value >= 0; Minimum[i] = min; Maximum[i] = max; NonNegative[i] = nonNegative; } }
    }
    public static int LastIndexAtOrBefore(IReadOnlyList<MappedDataPoint> points, double time) { var low = 0; var high = points.Count - 1; while (low < high) { var mid = (low + high + 1) / 2; if (points[mid].Time <= time) low = mid; else high = mid - 1; } return low; }
}

public static class DynamicGraphRenderer
{
    public static void Draw(DrawingContext dc, MappedDataSeries series, CurrentDataState state, Rect bounds, ThemePalette palette, double progress, double textScale = 1)
    {
        var domain = DynamicGraphDomain.Calculate(series, state, progress); var plot = GraphRenderer.PlotBounds(bounds); var xTicks = AxisTickGenerator.NiceTicks(domain.VisibleStartTime, domain.VisibleEndTime, bounds.Width < 500 ? 4 : 6); var yTicks = AxisTickGenerator.NiceTicks(domain.VisibleMinY, domain.VisibleMaxY, 6); var grid = new Pen(palette.Brush(palette.Grid), 1); var labels = palette.Brush(palette.SecondaryText);
        foreach (var x in xTicks) { var px = X(x); dc.DrawLine(grid, new Point(px, plot.Top), new Point(px, plot.Bottom)); Text(dc, AxisLabelFormatter.Time(x, series.Points), px - 18, plot.Bottom + 8, labels); } foreach (var y in yTicks) { var py = Y(y); dc.DrawLine(grid, new Point(plot.Left, py), new Point(plot.Right, py)); Text(dc, AxisLabelFormatter.Number(y), bounds.Left + 2, py - 8, labels); } Text(dc, "VALUE", plot.Left, bounds.Bottom - 18, labels); Text(dc, "TIME", plot.Right - 50, bounds.Bottom - 18, labels);
        var last = DynamicGraphDomain.LastIndexAtOrBefore(series.Points, domain.VisibleEndTime); var display = SampleVisible(series.Points, last, Math.Max(32, (int)plot.Width * 2)); if (state.CurrentTime > series.Points[last].Time && state.CurrentTime <= domain.VisibleEndTime) display.Add(new GraphDisplayPoint(state.CurrentTime, state.CurrentMappedValue)); if (display.Count > 1) { var geometry = new StreamGeometry(); using var c = geometry.Open(); c.BeginFigure(Map(display[0]), false, false); for (var i = 1; i < display.Count; i++) c.LineTo(Map(display[i]), true, false); dc.DrawGeometry(null, new Pen(palette.Brush(palette.Blue), 2), geometry); }
        var current = new Point(plot.Right, Y(state.CurrentMappedValue)); dc.DrawRectangle(null, new Pen(palette.Brush(palette.SecondaryText), 1), plot); var accent = palette.Brush(palette.Green); dc.DrawEllipse(accent, null, current, 5, 5);
        Point Map(GraphDisplayPoint p) => new(X(p.Time), Y(p.Value)); double X(double time) => plot.Left + Math.Clamp((time - domain.VisibleStartTime) / domain.TimeSpan, 0, 1) * plot.Width; double Y(double value) => plot.Bottom - Math.Clamp((value - domain.VisibleMinY) / domain.YSpan, 0, 1) * plot.Height; void Text(DrawingContext dc, string text, double x, double y, Brush brush) => dc.DrawText(new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 11 * Math.Clamp(textScale, .25, 4), brush, 1), new Point(x, y));
    }
    static List<GraphDisplayPoint> SampleVisible(IReadOnlyList<MappedDataPoint> points, int last, int budget)
    { if (last < 1) return new(); if (last + 1 <= budget) return points.Take(last + 1).Select(p => new GraphDisplayPoint(p.Time, p.MappedValue)).ToList(); var buckets = Math.Max(1, budget / 4); var selected = new SortedSet<int> { 0, last }; for (var bucket = 0; bucket < buckets; bucket++) { var start = bucket * (last + 1) / buckets; var end = Math.Max(start, (bucket + 1) * (last + 1) / buckets - 1); var min = start; var max = start; for (var i = start + 1; i <= end; i++) { if (points[i].MappedValue < points[min].MappedValue) min = i; if (points[i].MappedValue > points[max].MappedValue) max = i; } selected.Add(start); selected.Add(min); selected.Add(max); selected.Add(end); } return selected.Select(i => new GraphDisplayPoint(points[i].Time, points[i].MappedValue)).ToList(); }
}
