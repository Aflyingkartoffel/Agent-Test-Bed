using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TimeSeriesSonifier;

public enum IconDisplayMode { FollowPlayhead, CenterFixed }

public sealed class IconSettings
{
    public bool Enabled { get; set; }
    public bool ScalingEnabled { get; set; } = true;
    public double MinimumScale { get; set; } = .5;
    public double MaximumScale { get; set; } = 1.5;
    public string? ImagePath { get; set; }
    public IconDisplayMode DisplayMode { get; set; } = IconDisplayMode.FollowPlayhead;
    public bool Validate(out string error)
    {
        if (!TryScale(MinimumScale, out _) || !TryScale(MaximumScale, out _)) { error = "Scale must be finite and between 0.05 and 10."; return false; }
        if (MinimumScale > MaximumScale) { error = "Minimum scale must not exceed maximum scale."; return false; }
        error = ""; return true;
    }
    public static bool TryScale(double value, out double safe) { safe = double.IsFinite(value) ? Math.Clamp(value, .05, 10) : 1; return double.IsFinite(value) && value > 0; }
}

public static class IconScaleMapper
{
    public static double Map(double normalized, double minimum, double maximum)
    {
        var min = IconSettings.TryScale(minimum, out var safeMin) ? safeMin : .5; var max = IconSettings.TryScale(maximum, out var safeMax) ? safeMax : 1.5; if (min > max) (min, max) = (max, min); return min + Math.Clamp(double.IsFinite(normalized) ? normalized : .5, 0, 1) * (max - min);
    }
}

public static class IconImageLoader
{
    public static BitmapImage Load(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Image file was not found.", path);
        using var stream = File.OpenRead(path); var image = new BitmapImage(); image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad; image.StreamSource = stream; image.EndInit(); image.Freeze(); if (image.PixelWidth == 0 || image.PixelHeight == 0) throw new InvalidDataException("Image has no visible pixels."); return image;
    }
}

public sealed class IconRenderer
{
    public const double BaseSize = 80;
    public void Update(Image image, IconSettings settings, BitmapImage? source, CurrentDataState state, MappedDataSeries? series, Size viewport)
    {
        var visible = settings.Enabled && source is not null && series is not null && state.LeftPointIndex >= 0 && settings.DisplayMode == IconDisplayMode.FollowPlayhead;
        image.Source = source; image.Visibility = visible ? Visibility.Visible : Visibility.Collapsed; if (!visible || source is null || series is null) return;
        image.Width = BaseSize; image.Height = BaseSize; image.RenderTransformOrigin = new Point(.5, .5); image.RenderTransform = new ScaleTransform(settings.ScalingEnabled ? IconScaleMapper.Map(state.CurrentNormalizedValue, settings.MinimumScale, settings.MaximumScale) : 1, settings.ScalingEnabled ? IconScaleMapper.Map(state.CurrentNormalizedValue, settings.MinimumScale, settings.MaximumScale) : 1);
        if (GraphRenderer.TryMapPoint(series, state.CurrentTime, state.CurrentMappedValue, new Rect(0, 0, viewport.Width, viewport.Height), out var point)) { Canvas.SetLeft(image, point.X - BaseSize / 2); Canvas.SetTop(image, point.Y - BaseSize / 2); }
    }
}
