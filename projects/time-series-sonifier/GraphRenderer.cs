using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TimeSeriesSonifier;

public sealed class GraphSurface : FrameworkElement
{
    public DataSeries? Series { get; set; }
    public CurrentDataState State { get; set; } = CurrentDataState.Empty;
    protected override void OnRender(DrawingContext dc) { base.OnRender(dc); GraphRenderer.Draw(dc, Series, State, new Rect(0, 0, ActualWidth, ActualHeight)); }
}

public static class GraphRenderer
{
    public static void Draw(DrawingContext dc, DataSeries? series, CurrentDataState state, Rect bounds)
    {
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(11, 16, 21)), null, bounds); if (series is null || series.Points.Count < 2) { DrawText(dc, "OPEN A CSV DATASET TO BEGIN", bounds.Left + 24, bounds.Top + 24, Brushes.LightSlateGray); return; }
        var plot = new Rect(bounds.Left + 56, bounds.Top + 20, Math.Max(1, bounds.Width - 76), Math.Max(1, bounds.Height - 58)); var xSpan = Math.Max(1e-12, series.MaximumTime - series.MinimumTime); var ySpan = Math.Max(1e-12, series.MaximumValue - series.MinimumValue); var map = new Func<DataPoint, Point>(p => new(plot.Left + (p.Time - series.MinimumTime) / xSpan * plot.Width, plot.Bottom - (p.Value - series.MinimumValue) / ySpan * plot.Height));
        var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(45, 130, 170, 190)), 1); for (var i = 0; i <= 5; i++) { var x = plot.Left + plot.Width * i / 5; var y = plot.Top + plot.Height * i / 5; dc.DrawLine(gridPen, new Point(x, plot.Top), new Point(x, plot.Bottom)); dc.DrawLine(gridPen, new Point(plot.Left, y), new Point(plot.Right, y)); }
        var geometry = new StreamGeometry(); using (var context = geometry.Open()) { context.BeginFigure(map(series.Points[0]), false, false); foreach (var point in series.Points.Skip(1)) context.LineTo(map(point), true, false); } dc.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromRgb(105, 218, 255)), 2), geometry);
        var playX = plot.Left + state.NormalizedPosition * plot.Width; dc.DrawLine(new Pen(Brushes.Gold, 1.5), new Point(playX, plot.Top), new Point(playX, plot.Bottom)); var marker = map(new DataPoint(state.CurrentTime, state.CurrentValue, 0, "")); dc.DrawEllipse(Brushes.Gold, null, marker, 5, 5);
        DrawText(dc, series.MinimumTime.ToString("G4"), plot.Left, plot.Bottom + 10, Brushes.LightSlateGray); DrawText(dc, series.MaximumTime.ToString("G4"), plot.Right - 40, plot.Bottom + 10, Brushes.LightSlateGray); DrawText(dc, series.MinimumValue.ToString("G4"), 5, plot.Bottom - 8, Brushes.LightSlateGray); DrawText(dc, series.MaximumValue.ToString("G4"), 5, plot.Top, Brushes.LightSlateGray);
    }
    static void DrawText(DrawingContext dc, string text, double x, double y, Brush brush) => dc.DrawText(new FormattedText(text, System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 12, brush, 1), new Point(x, y));
}
