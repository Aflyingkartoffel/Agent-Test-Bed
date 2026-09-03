using System.Windows;
using System.Windows.Media;

namespace TimeSeriesSonifier;

public sealed class AudioSampleRingBuffer
{
    readonly float[] storage; long written;
    public AudioSampleRingBuffer(int capacity = 16384) { if (capacity < 2) throw new ArgumentOutOfRangeException(nameof(capacity)); storage = new float[capacity]; }
    public int Capacity => storage.Length;
    public long Count => Math.Min(Volatile.Read(ref written), storage.Length);
    public void Write(ReadOnlySpan<float> samples) { foreach (var sample in samples) { var index = Interlocked.Increment(ref written) - 1; storage[index % storage.Length] = float.IsFinite(sample) ? Math.Clamp(sample, -1, 1) : 0; } }
    public bool TryCopyLatest(Span<float> destination)
    {
        if (destination.Length == 0) return true;
        var count = Volatile.Read(ref written); if (count < destination.Length) return false;
        var start = count - destination.Length;
        for (var attempt = 0; attempt < 2; attempt++) { for (var i = 0; i < destination.Length; i++) destination[i] = storage[(start + i) % storage.Length]; if (Volatile.Read(ref written) - count < storage.Length) return true; count = Volatile.Read(ref written); start = count - destination.Length; }
        return true;
    }
    public void Clear() => Interlocked.Exchange(ref written, 0);
}

public sealed class SpectrumFrame
{
    public int SampleRate { get; }
    public int FftSize { get; }
    public double[] Frequencies { get; }
    public double[] Magnitudes { get; }
    public double[] Decibels { get; }
    public SpectrumFrame(int sampleRate, int fftSize, double[] frequencies, double[] magnitudes, double[] decibels) { SampleRate = sampleRate; FftSize = fftSize; Frequencies = frequencies; Magnitudes = magnitudes; Decibels = decibels; }
    public double Nyquist => SampleRate / 2.0;
    public double PeakFrequency => Frequencies.Length == 0 ? 0 : Frequencies[Array.IndexOf(Decibels, Decibels.Max())];
}

public sealed class SpectrumAnalyzer : IDisposable
{
    public const double FloorDb = -100;
    readonly object gate = new(); double[] window = Array.Empty<double>(); double[] real = Array.Empty<double>(); double[] imaginary = Array.Empty<double>(); double[] previousDb = Array.Empty<double>(); bool disposed;
    public bool Enabled { get; private set; }
    public int FftSize { get; private set; } = 2048;
    public double Smoothing { get; set; } = .35;
    public SpectrumAnalyzer(bool enabled = false) { SetFftSize(FftSize); Enabled = enabled; }
    public void Enable() { lock (gate) if (!disposed) Enabled = true; }
    public void Disable() { lock (gate) Enabled = false; }
    public void SetFftSize(int size) { if (size is not (1024 or 2048 or 4096)) throw new ArgumentOutOfRangeException(nameof(size)); lock (gate) { if (disposed) return; FftSize = size; window = Enumerable.Range(0, size).Select(i => .5 * (1 - Math.Cos(2 * Math.PI * i / (size - 1)))).ToArray(); real = new double[size]; imaginary = new double[size]; previousDb = new double[size / 2 + 1]; Array.Fill(previousDb, FloorDb); } }
    public SpectrumFrame? Analyze(ReadOnlySpan<float> samples, int sampleRate = Oscillator.SampleRate)
    {
        lock (gate) { if (disposed || !Enabled || samples.Length < FftSize) return null; for (var i = 0; i < FftSize; i++) { real[i] = samples[samples.Length - FftSize + i] * window[i]; imaginary[i] = 0; } Fft(real, imaginary); var bins = FftSize / 2 + 1; var frequencies = new double[bins]; var magnitudes = new double[bins]; var db = new double[bins]; var scale = 2.0 / window.Sum(); var smoothing = Math.Clamp(double.IsFinite(Smoothing) ? Smoothing : 1, 0, 1); for (var i = 0; i < bins; i++) { frequencies[i] = BinFrequency(i, sampleRate, FftSize); var magnitude = Math.Sqrt(real[i] * real[i] + imaginary[i] * imaginary[i]) * scale; magnitudes[i] = double.IsFinite(magnitude) ? magnitude : 0; var currentDb = ToDb(magnitudes[i]); db[i] = previousDb[i] + (currentDb - previousDb[i]) * smoothing; previousDb[i] = db[i]; } return new SpectrumFrame(sampleRate, FftSize, frequencies, magnitudes, db); }
    }
    public static double BinFrequency(int bin, int sampleRate, int fftSize) => bin * (double)sampleRate / fftSize;
    public static double ToDb(double magnitude) { if (!double.IsFinite(magnitude) || magnitude <= 0) return FloorDb; return Math.Clamp(20 * Math.Log10(Math.Max(magnitude, 1e-5)), FloorDb, 0); }
    public static double[] HannWindow(int size) => size < 2 ? Array.Empty<double>() : Enumerable.Range(0, size).Select(i => .5 * (1 - Math.Cos(2 * Math.PI * i / (size - 1)))).ToArray();
    static void Fft(double[] r, double[] im) { var n = r.Length; for (int i = 1, j = 0; i < n; i++) { var bit = n >> 1; for (; (j & bit) != 0; bit >>= 1) j ^= bit; j ^= bit; if (i < j) (r[i], r[j], im[i], im[j]) = (r[j], r[i], im[j], im[i]); } for (var len = 2; len <= n; len <<= 1) { var angle = -2 * Math.PI / len; for (var i = 0; i < n; i += len) for (var j = 0; j < len / 2; j++) { var c = Math.Cos(angle * j); var s = Math.Sin(angle * j); var tr = r[i + j + len / 2] * c - im[i + j + len / 2] * s; var ti = r[i + j + len / 2] * s + im[i + j + len / 2] * c; r[i + j + len / 2] = r[i + j] - tr; im[i + j + len / 2] = im[i + j] - ti; r[i + j] += tr; im[i + j] += ti; } } }
    public void Dispose() { lock (gate) { disposed = true; Enabled = false; } }
}

public sealed class SpectrumSurface : FrameworkElement
{
    public SpectrumFrame? Frame { get; set; }
    protected override void OnRender(DrawingContext dc) { dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(11, 16, 21)), null, new Rect(0, 0, ActualWidth, ActualHeight)); SpectrumRenderer.Draw(dc, Frame, new Rect(0, 0, ActualWidth, ActualHeight)); }
}

public static class SpectrumRenderer
{
    public static void Draw(DrawingContext dc, SpectrumFrame? frame, Rect bounds)
    {
        var plot = new Rect(bounds.Left + 42, bounds.Top + 10, Math.Max(1, bounds.Width - 54), Math.Max(1, bounds.Height - 32)); var grid = new Pen(new SolidColorBrush(Color.FromArgb(45, 130, 170, 190)), 1); for (var i = 0; i <= 5; i++) { var y = plot.Top + plot.Height * i / 5; dc.DrawLine(grid, new Point(plot.Left, y), new Point(plot.Right, y)); DrawText(dc, (-i * 20).ToString(), 4, y - 8, Brushes.LightSlateGray); }
        if (frame is null || frame.Decibels.Length == 0) { DrawText(dc, "ENABLE FFT WHILE SOUND IS RUNNING", plot.Left, plot.Top + 20, Brushes.LightSlateGray); return; }
        var geometry = new StreamGeometry(); using (var c = geometry.Open()) { c.BeginFigure(new Point(plot.Left, plot.Bottom), true, false); for (var i = 1; i < frame.Decibels.Length; i++) { var f = Math.Max(20, frame.Frequencies[i]); var x = plot.Left + Math.Clamp(Math.Log10(f / 20) / Math.Log10(Math.Max(20, frame.Nyquist) / 20), 0, 1) * plot.Width; var y = plot.Bottom - (Math.Clamp(frame.Decibels[i], SpectrumAnalyzer.FloorDb, 0) - SpectrumAnalyzer.FloorDb) / -SpectrumAnalyzer.FloorDb * plot.Height; c.LineTo(new Point(x, y), true, false); } c.LineTo(new Point(plot.Right, plot.Bottom), true, false); } dc.DrawGeometry(new SolidColorBrush(Color.FromArgb(80, 67, 194, 255)), new Pen(new SolidColorBrush(Color.FromRgb(105, 218, 255)), 1.5), geometry); DrawText(dc, "20 Hz", plot.Left, plot.Bottom + 5, Brushes.LightSlateGray); DrawText(dc, $"{frame.Nyquist:0} Hz", plot.Right - 52, plot.Bottom + 5, Brushes.LightSlateGray);
    }
    static void DrawText(DrawingContext dc, string text, double x, double y, Brush brush) => dc.DrawText(new FormattedText(text, System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 10, brush, 1), new Point(x, y));
}
