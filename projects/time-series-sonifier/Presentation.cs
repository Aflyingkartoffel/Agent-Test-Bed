using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TimeSeriesSonifier;

public enum OutputAspectRatio { Vertical, Square, Horizontal }

public sealed record OutputProfile(OutputAspectRatio Aspect, int Width, int Height, string Label)
{
    public static OutputProfile Vertical { get; } = new(OutputAspectRatio.Vertical, 1080, 1920, "VERTICAL 1080 × 1920 (9:16)");
    public static OutputProfile Square { get; } = new(OutputAspectRatio.Square, 1080, 1080, "SQUARE 1080 × 1080 (1:1)");
    public static OutputProfile Horizontal { get; } = new(OutputAspectRatio.Horizontal, 1920, 1080, "HORIZONTAL 1920 × 1080 (16:9)");
    public static IReadOnlyList<OutputProfile> All { get; } = new[] { Vertical, Square, Horizontal };
}

public static class OutputFrameTiming
{
    public static double FrameTime(int frameIndex, int framesPerSecond) => frameIndex < 0 || framesPerSecond <= 0 ? 0 : frameIndex / (double)framesPerSecond;
    public static int FrameCount(double durationSeconds, int framesPerSecond) => !double.IsFinite(durationSeconds) || durationSeconds <= 0 || framesPerSecond <= 0 ? 0 : (int)Math.Ceiling(durationSeconds * framesPerSecond);
}

public sealed record PresentationScene(MappedDataSeries? Series, CurrentDataState State, ImageSource? Image, double ImageOpacity, double MinimumScale, double MaximumScale, SpectrumFrame? Spectrum, string TimeColumnName = "TIME", string ValueColumnName = "VALUE");

public sealed class PresentationSurface : FrameworkElement
{
    public PresentationScene? Scene { get; set; }
    public OutputProfile Profile { get; set; } = OutputProfile.Vertical;
    protected override void OnRender(DrawingContext dc) { base.OnRender(dc); PresentationRenderer.Draw(dc, Scene, new Rect(0, 0, ActualWidth, ActualHeight), Profile); }
}

public static class PresentationRenderer
{
    public static void Draw(DrawingContext dc, PresentationScene? scene, Rect bounds, OutputProfile profile)
    {
        dc.DrawRectangle(Brushes.White, null, bounds); var vertical = profile.Aspect == OutputAspectRatio.Vertical; var title = vertical ? new Rect(bounds.Left + bounds.Width * .08, bounds.Top + bounds.Height * .04, bounds.Width * .84, bounds.Height * .08) : new Rect(bounds.Left + bounds.Width * .06, bounds.Top + bounds.Height * .05, bounds.Width * .88, bounds.Height * .12); var chart = vertical ? new Rect(bounds.Left + bounds.Width * .07, bounds.Top + bounds.Height * .19, bounds.Width * .86, bounds.Height * .48) : new Rect(bounds.Left + bounds.Width * .05, bounds.Top + bounds.Height * .18, bounds.Width * .9, bounds.Height * .58); var spectrum = vertical ? new Rect(bounds.Left + bounds.Width * .08, bounds.Top + bounds.Height * .73, bounds.Width * .84, bounds.Height * .16) : new Rect(bounds.Left + bounds.Width * .06, bounds.Top + bounds.Height * .8, bounds.Width * .88, bounds.Height * .14);
        DrawText(dc, "TIME-SERIES SONIFIER", title.Left, title.Top, 18, Brushes.Black); if (scene is null || scene.Series is null || scene.Series.Points.Count < 2) { DrawText(dc, "IMPORT DATA TO PREVIEW", chart.Left, chart.Top + chart.Height * .4, 14, Brushes.Gray); return; }
        DrawImage(dc, scene, chart); DrawGraph(dc, scene, chart); if (scene.Spectrum is not null) DrawSpectrum(dc, scene.Spectrum, spectrum); DrawText(dc, $"{ColumnLabel.Format(scene.TimeColumnName, "TIME")}  {scene.State.CurrentTime:G5}     {ColumnLabel.Format(scene.ValueColumnName, "VALUE")}  {scene.State.CurrentMappedValue:G5}", chart.Left, chart.Bottom + bounds.Height * .025, 12, new SolidColorBrush(Color.FromRgb(31, 41, 51)));
    }
    static void DrawImage(DrawingContext dc, PresentationScene scene, Rect chart)
    {
        if (scene.Image is null || scene.State.LeftPointIndex < 0) return; var scale = IconScaleMapper.Map(scene.State.CurrentNormalizedValue, scene.MinimumScale, scene.MaximumScale); var size = Math.Min(chart.Width, chart.Height) * .42 * scale; var rect = new Rect(chart.Left + (chart.Width - size) / 2, chart.Top + (chart.Height - size) / 2, size, size); var brush = new ImageBrush(scene.Image) { Stretch = Stretch.Uniform, Opacity = IconOpacity.Clamp(scene.ImageOpacity) }; dc.DrawRectangle(brush, null, rect);
    }
    static void DrawGraph(DrawingContext dc, PresentationScene scene, Rect chart)
    {
        var plot = new Rect(chart.Left + chart.Width * .11, chart.Top + chart.Height * .08, chart.Width * .82, chart.Height * .8); var grid = new Pen(new SolidColorBrush(Color.FromRgb(225, 231, 236)), 1); for (var i = 0; i <= 5; i++) { var x = plot.Left + plot.Width * i / 5; var y = plot.Top + plot.Height * i / 5; dc.DrawLine(grid, new Point(x, plot.Top), new Point(x, plot.Bottom)); dc.DrawLine(grid, new Point(plot.Left, y), new Point(plot.Right, y)); }
        var xSpan = Math.Max(1e-12, scene.Series!.MaximumTime - scene.Series.MinimumTime); var ySpan = Math.Max(1e-12, scene.Series.MaximumValue - scene.Series.MinimumValue); Point Map(MappedDataPoint p) => new(plot.Left + (p.Time - scene.Series!.MinimumTime) / xSpan * plot.Width, plot.Bottom - (p.MappedValue - scene.Series!.MinimumValue) / ySpan * plot.Height); var geometry = new StreamGeometry(); using (var c = geometry.Open()) { c.BeginFigure(Map(scene.Series.Points[0]), false, false); foreach (var p in scene.Series.Points.Skip(1)) c.LineTo(Map(p), true, false); } dc.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromRgb(25, 118, 210)), 2), geometry); var playX = plot.Left + scene.State.NormalizedPosition * plot.Width; dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(32, 164, 100)), 2), new Point(playX, plot.Top), new Point(playX, plot.Bottom));
    }
    static void DrawSpectrum(DrawingContext dc, SpectrumFrame frame, Rect bounds) { dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(247, 249, 251)), null, bounds); if (frame.Decibels.Length < 2) return; var g = new StreamGeometry(); using (var c = g.Open()) { c.BeginFigure(new Point(bounds.Left, bounds.Bottom), true, false); for (var i = 1; i < frame.Decibels.Length; i++) c.LineTo(new Point(bounds.Left + i * bounds.Width / (frame.Decibels.Length - 1), bounds.Bottom - Math.Clamp((frame.Decibels[i] + 100) / 100, 0, 1) * bounds.Height), true, false); c.LineTo(new Point(bounds.Right, bounds.Bottom), true, false); } dc.DrawGeometry(new SolidColorBrush(Color.FromArgb(70, 25, 118, 210)), new Pen(new SolidColorBrush(Color.FromRgb(25, 118, 210)), 1), g); }
    static void DrawText(DrawingContext dc, string text, double x, double y, double size, Brush brush) => dc.DrawText(new FormattedText(text, System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), size, brush, 1), new Point(x, y));
}

public static class OfflineAudioRenderer
{
    public static int RenderWav(string path, MappedDataSeries series, WaveformType waveform, double volume, double durationSeconds, bool enabled, int sampleRate = Oscillator.SampleRate)
    {
        var count = Math.Max(0, (int)Math.Round(durationSeconds * sampleRate)); var oscillator = new Oscillator { Waveform = waveform }; var bytes = new byte[count * 2]; var interpolator = new MappedSeriesInterpolator(series); using var stream = File.Create(path); using var writer = new BinaryWriter(stream); writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + bytes.Length); writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(sampleRate); writer.Write(sampleRate * 2); writer.Write((short)2); writer.Write((short)16); writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(bytes.Length);
        for (var i = 0; i < count; i++) { var time = series.MinimumTime + (series.MaximumTime - series.MinimumTime) * (i / (double)Math.Max(1, count - 1)); var state = interpolator.Evaluate(time); var sample = enabled ? oscillator.NextSample(PitchMapper.Map(state.CurrentNormalizedValue), sampleRate) * (float)Math.Clamp(double.IsFinite(volume) ? volume : 0, 0, 1) : 0; var pcm = (short)Math.Round(Math.Clamp(sample, -1, 1) * short.MaxValue); writer.Write(pcm); } return count;
    }
}

public static class VideoEncoderService
{
    public static string? FindFfmpeg() { var path = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator).FirstOrDefault(p => File.Exists(Path.Combine(p, "ffmpeg.exe"))); return path is null ? null : Path.Combine(path, "ffmpeg.exe"); }
    public static string BuildArguments(string framesPattern, string wavPath, string outputPath, int width, int height, int fps, bool includeAudio) { var audio = includeAudio ? $"-i {Quote(wavPath)} -c:a aac -shortest" : "-an"; return $"-y -framerate {fps} -i {Quote(framesPattern)} {audio} -c:v libx264 -pix_fmt yuv420p -vf \"scale={width}:{height}:flags=lanczos\" {Quote(outputPath)}"; }
    public static async Task<bool> EncodeAsync(string ffmpeg, string arguments, CancellationToken token) { using var process = Process.Start(new ProcessStartInfo(ffmpeg, arguments) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true }); if (process is null) return false; await process.WaitForExitAsync(token); return process.ExitCode == 0; }
    public static string Quote(string path) => $"\"{path.Replace("\"", "\\\"")}\"";
}
