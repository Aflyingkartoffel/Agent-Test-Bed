using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace TimeSeriesSonifier;

public readonly record struct GraphDisplayPoint(double Time, double Value);
public sealed record GraphStaticLayer(StreamGeometry Geometry, DrawingGroup Axes);

public static class GraphSamplingService
{
    public static IReadOnlyList<GraphDisplayPoint> Sample(MappedDataSeries series, int viewportWidth, int pointsPerPixel = 2)
    {
        var source = series.Points; if (source.Count < 2) return Array.Empty<GraphDisplayPoint>();
        var budget = Math.Max(32, Math.Max(1, viewportWidth) * Math.Clamp(pointsPerPixel, 1, 4)); if (source.Count <= budget) return source.Select(p => new GraphDisplayPoint(p.Time, p.MappedValue)).ToArray();
        var bucketCount = Math.Max(1, Math.Min(viewportWidth, budget / 3)); var first = new int[bucketCount]; var min = new int[bucketCount]; var max = new int[bucketCount]; var last = new int[bucketCount]; Array.Fill(first, -1); Array.Fill(min, -1); Array.Fill(max, -1); Array.Fill(last, -1); var span = Math.Max(1e-12, series.MaximumTime - series.MinimumTime);
        for (var i = 0; i < source.Count; i++) { var bucket = Math.Clamp((int)((source[i].Time - series.MinimumTime) / span * bucketCount), 0, bucketCount - 1); if (first[bucket] < 0) first[bucket] = i; last[bucket] = i; if (min[bucket] < 0 || source[i].MappedValue < source[min[bucket]].MappedValue) min[bucket] = i; if (max[bucket] < 0 || source[i].MappedValue > source[max[bucket]].MappedValue) max[bucket] = i; }
        var selected = new bool[source.Count]; for (var b = 0; b < bucketCount; b++) if (first[b] >= 0) selected[first[b]] = selected[min[b]] = selected[max[b]] = selected[last[b]] = true;
        var result = new List<GraphDisplayPoint>(Math.Min(source.Count, budget)); for (var i = 0; i < source.Count; i++) if (selected[i]) result.Add(new GraphDisplayPoint(source[i].Time, source[i].MappedValue)); return result;
    }
}

public sealed class GraphRenderCache
{
    MappedDataSeries? source; Rect viewport; string timeLabel = ""; string valueLabel = ""; GraphStaticLayer? layer;
    public int RebuildCount { get; private set; }
    public GraphStaticLayer Get(MappedDataSeries series, Rect requestedViewport, string timeAxisLabel = "TIME", string valueAxisLabel = "VALUE")
    { var requested = new Rect(requestedViewport.Left, requestedViewport.Top, Math.Max(1, requestedViewport.Width), Math.Max(1, requestedViewport.Height)); if (!ReferenceEquals(source, series) || viewport != requested || timeLabel != timeAxisLabel || valueLabel != valueAxisLabel) { source = series; viewport = requested; timeLabel = timeAxisLabel; valueLabel = valueAxisLabel; layer = GraphRenderer.BuildStaticLayer(series, requested, timeAxisLabel, valueAxisLabel); RebuildCount++; } return layer!; }
    public GraphStaticLayer Get(MappedDataSeries series, Size requestedViewport, string timeAxisLabel = "TIME", string valueAxisLabel = "VALUE") => Get(series, new Rect(0, 0, requestedViewport.Width, requestedViewport.Height), timeAxisLabel, valueAxisLabel);
    public GraphStaticLayer Get(MappedDataSeries series, int viewportWidth) => Get(series, new Size(viewportWidth, 300));
    public void Invalidate() { source = null; layer = null; }
}

public static class AxisTickGenerator
{
    public static IReadOnlyList<double> NiceTicks(double minimum, double maximum, int desired = 6)
    { if (!double.IsFinite(minimum) || !double.IsFinite(maximum)) return Array.Empty<double>(); if (minimum > maximum) (minimum, maximum) = (maximum, minimum); if (minimum == maximum) return new[] { minimum }; var step = NiceStep((maximum - minimum) / Math.Max(2, desired)); var start = Math.Ceiling(minimum / step) * step; var end = Math.Floor(maximum / step) * step; var ticks = new List<double>(); for (var value = start; value <= end + step * .001 && ticks.Count < 20; value += step) ticks.Add(Math.Abs(value) < step * 1e-9 ? 0 : value); return ticks; }
    public static double NiceStep(double raw) { if (!double.IsFinite(raw) || raw <= 0) return 1; var power = Math.Pow(10, Math.Floor(Math.Log10(raw))); var normalized = raw / power; var factor = normalized <= 1 ? 1 : normalized <= 2 ? 2 : normalized <= 5 ? 5 : 10; return factor * power; }
}

public static class AxisLabelFormatter
{
    public static string Number(double value) { if (!double.IsFinite(value)) return "—"; var abs = Math.Abs(value); if (abs >= 1_000_000) return $"{value / 1_000_000:0.##}M"; if (abs >= 1_000) return value.ToString("#,##0.##", CultureInfo.InvariantCulture); if (abs >= .01) return value.ToString("0.###", CultureInfo.InvariantCulture); return value.ToString("0.#######", CultureInfo.InvariantCulture); }
    public static string Time(double value, IReadOnlyList<MappedDataPoint> points) { if (points.Count > 0 && DateTime.TryParse(points[0].OriginalTimeText, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out _) && !double.TryParse(points[0].OriginalTimeText, NumberStyles.Float, CultureInfo.InvariantCulture, out _)) { var date = new DateTime((long)Math.Clamp(value * TimeSpan.TicksPerSecond, DateTime.MinValue.Ticks, DateTime.MaxValue.Ticks), DateTimeKind.Utc).ToLocalTime(); return date.ToString("MMM d", CultureInfo.InvariantCulture); } if (points.Count > 1 && Math.Abs(points[0].Time - Math.Round(points[0].Time)) < 1e-9 && Math.Abs(points[^1].Time - Math.Round(points[^1].Time)) < 1e-9 && points[^1].Time - points[0].Time <= 100) return value.ToString("0", CultureInfo.InvariantCulture); return Number(value); }
}

public sealed class GraphSurface : FrameworkElement
{
    readonly GraphRenderCache cache = new();
    readonly DrawingVisual staticVisual = new(); readonly DrawingVisual dynamicVisual = new(); readonly VisualCollection visuals; bool staticVisible;
    public GraphSurface() { visuals = new VisualCollection(this) { staticVisual, dynamicVisual }; }
    public DataSeries? Series { get; set; } public MappedDataSeries? MappedSeries { get; set; } public CurrentDataState State { get; set; } = CurrentDataState.Empty; public string TimeLabel { get; set; } = "TIME"; public string ValueLabel { get; set; } = "VALUE";
    public void Refresh()
    {
        if (MappedSeries is null || MappedSeries.Points.Count < 2) { using var dc = staticVisual.RenderOpen(); dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, ActualWidth, ActualHeight)); GraphRenderer.DrawEmpty(dc, new Rect(0, 0, ActualWidth, ActualHeight)); staticVisible = true; }
        else { var previousRebuilds = cache.RebuildCount; var layer = cache.Get(MappedSeries, new Rect(0, 0, ActualWidth, ActualHeight), TimeLabel, ValueLabel); if (!staticVisible || cache.RebuildCount != previousRebuilds) { using var dc = staticVisual.RenderOpen(); dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, ActualWidth, ActualHeight)); dc.DrawDrawing(layer.Axes); dc.DrawGeometry(null, GraphRenderer.LinePen, layer.Geometry); staticVisible = true; } }
        using (var dc = dynamicVisual.RenderOpen()) { if (MappedSeries is not null && MappedSeries.Points.Count >= 2) GraphRenderer.DrawDynamic(dc, MappedSeries, State, new Rect(0, 0, ActualWidth, ActualHeight)); }
    }
    protected override int VisualChildrenCount => visuals.Count; protected override Visual GetVisualChild(int index) => visuals[index]; protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo) { base.OnRenderSizeChanged(sizeInfo); Refresh(); }
}

public static class GraphRenderer
{
    static readonly Brush GridBrush = Freeze(new SolidColorBrush(Color.FromRgb(225, 231, 236))); static readonly Pen GridPen = Freeze(new Pen(GridBrush, 1)); static readonly Brush LabelBrush = Freeze(new SolidColorBrush(Color.FromRgb(80, 90, 100)));
    public static bool TryMapPoint(MappedDataSeries series, double time, double value, Rect bounds, out Point point) { var plot = Plot(bounds); var xSpan = series.MaximumTime - series.MinimumTime; var ySpan = Math.Max(1e-12, series.MaximumValue - series.MinimumValue); if (!double.IsFinite(time) || !double.IsFinite(value) || xSpan <= 0 || plot.Width <= 0 || plot.Height <= 0) { point = default; return false; } point = new(plot.Left + (time - series.MinimumTime) / xSpan * plot.Width, plot.Bottom - (value - series.MinimumValue) / ySpan * plot.Height); return true; }
    public static void Draw(DrawingContext dc, MappedDataSeries? mapped, CurrentDataState state, Rect bounds, string timeLabel = "TIME", string valueLabel = "VALUE", GraphRenderCache? cache = null)
    { dc.DrawRectangle(Brushes.White, null, bounds); if (mapped is null || mapped.Points.Count < 2) { DrawText(dc, "OPEN A CSV DATASET TO BEGIN", bounds.Left + 24, bounds.Top + 24, Brushes.LightSlateGray); return; } var layer = cache?.Get(mapped, bounds, timeLabel, valueLabel) ?? BuildStaticLayer(mapped, bounds, timeLabel, valueLabel); dc.DrawDrawing(layer.Axes); dc.DrawGeometry(null, LinePen, layer.Geometry); DrawDynamic(dc, mapped, state, bounds); }
    internal static Pen LinePen { get; } = Freeze(new Pen(Freeze(new SolidColorBrush(Color.FromRgb(25, 118, 210))), 2));
    internal static void DrawEmpty(DrawingContext dc, Rect bounds) => DrawText(dc, "OPEN A CSV DATASET TO BEGIN", bounds.Left + 24, bounds.Top + 24, Brushes.LightSlateGray);
    internal static void DrawDynamic(DrawingContext dc, MappedDataSeries mapped, CurrentDataState state, Rect bounds) { var plot = Plot(bounds); var playX = plot.Left + state.NormalizedPosition * plot.Width; dc.DrawLine(new Pen(Brushes.Gold, 1.5), new Point(playX, plot.Top), new Point(playX, plot.Bottom)); if (TryMapPoint(mapped, state.CurrentTime, state.CurrentMappedValue, bounds, out var marker)) dc.DrawEllipse(Brushes.Gold, null, marker, 5, 5); }
    internal static GraphStaticLayer BuildStaticLayer(MappedDataSeries mapped, Rect bounds, string timeLabel, string valueLabel)
    { var plot = Plot(bounds); var xTicks = AxisTickGenerator.NiceTicks(mapped.MinimumTime, mapped.MaximumTime, bounds.Width < 500 ? 4 : 6); var yTicks = AxisTickGenerator.NiceTicks(mapped.MinimumValue, mapped.MaximumValue, 6); var axes = new DrawingGroup(); using (var dc = axes.Open()) { foreach (var x in xTicks) { var px = plot.Left + (x - mapped.MinimumTime) / Math.Max(1e-12, mapped.MaximumTime - mapped.MinimumTime) * plot.Width; dc.DrawLine(GridPen, new Point(px, plot.Top), new Point(px, plot.Bottom)); DrawText(dc, AxisLabelFormatter.Time(x, mapped.Points), px - 18, plot.Bottom + 8, LabelBrush); } foreach (var y in yTicks) { var py = plot.Bottom - (y - mapped.MinimumValue) / Math.Max(1e-12, mapped.MaximumValue - mapped.MinimumValue) * plot.Height; dc.DrawLine(GridPen, new Point(plot.Left, py), new Point(plot.Right, py)); DrawText(dc, AxisLabelFormatter.Number(y), bounds.Left + 2, py - 8, LabelBrush); } DrawText(dc, ColumnLabel.Format(valueLabel, "VALUE"), plot.Left, bounds.Bottom - 18, LabelBrush); DrawText(dc, ColumnLabel.Format(timeLabel, "TIME"), plot.Right - 50, bounds.Bottom - 18, LabelBrush); } axes.Freeze(); var display = GraphSamplingService.Sample(mapped, Math.Max(1, (int)plot.Width)); var geometry = new StreamGeometry(); using (var context = geometry.Open()) { context.BeginFigure(Map(display[0]), false, false); for (var i = 1; i < display.Count; i++) context.LineTo(Map(display[i]), true, false); } geometry.Freeze(); return new GraphStaticLayer(geometry, axes); Point Map(GraphDisplayPoint p) => new(plot.Left + (p.Time - mapped.MinimumTime) / Math.Max(1e-12, mapped.MaximumTime - mapped.MinimumTime) * plot.Width, plot.Bottom - (p.Value - mapped.MinimumValue) / Math.Max(1e-12, mapped.MaximumValue - mapped.MinimumValue) * plot.Height); }
    static Rect Plot(Rect bounds) => new(bounds.Left + 72, bounds.Top + 20, Math.Max(1, bounds.Width - 88), Math.Max(1, bounds.Height - 62)); static void DrawText(DrawingContext dc, string text, double x, double y, Brush brush) => dc.DrawText(new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 11, brush, 1), new Point(x, y)); static T Freeze<T>(T freezable) where T : Freezable { freezable.Freeze(); return freezable; }
}
