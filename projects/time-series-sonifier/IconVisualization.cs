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
    public IconDisplayMode DisplayMode { get; set; } = IconDisplayMode.CenterFixed;
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

public static class IconOpacity
{
    public const double Default = .35;
    public static double Clamp(double value) => double.IsFinite(value) ? Math.Clamp(value, 0, 1) : Default;
}

public static class IconImageLoader
{
    public static ImageSource CreateDefaultCube()
    {
        var group = new DrawingGroup(); group.Children.Add(new GeometryDrawing(new SolidColorBrush(Color.FromRgb(170, 177, 184)), new Pen(Brushes.DimGray, 2), Geometry.Parse("M 80,10 L 145,45 L 80,80 L 15,45 Z"))); group.Children.Add(new GeometryDrawing(new SolidColorBrush(Color.FromRgb(135, 143, 151)), new Pen(Brushes.DimGray, 2), Geometry.Parse("M 15,45 L 80,80 L 80,155 L 15,120 Z"))); group.Children.Add(new GeometryDrawing(new SolidColorBrush(Color.FromRgb(105, 113, 121)), new Pen(Brushes.DimGray, 2), Geometry.Parse("M 80,80 L 145,45 L 145,120 L 80,155 Z"))); var image = new DrawingImage(group); image.Freeze(); return image;
    }
    public static BitmapImage Load(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Image file was not found.", path);
        using var stream = File.OpenRead(path); var image = new BitmapImage(); image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad; image.StreamSource = stream; image.EndInit(); image.Freeze(); if (image.PixelWidth == 0 || image.PixelHeight == 0) throw new InvalidDataException("Image has no visible pixels."); return image;
    }
}

public sealed class IconRenderer
{
    public const double BaseSize = 160;
    double displayedScale = 1;
    public void Update(Image image, IconSettings settings, ImageSource? source, CurrentDataState state, MappedDataSeries? series, Size viewport)
    {
        var visible = settings.Enabled && source is not null && state.LeftPointIndex >= 0;
        image.Source = source; image.Visibility = visible ? Visibility.Visible : Visibility.Collapsed; if (!visible || source is null) return;
        var targetScale = settings.ScalingEnabled ? IconScaleMapper.Map(state.CurrentNormalizedValue, settings.MinimumScale, settings.MaximumScale) : 1; displayedScale += (targetScale - displayedScale) * .35; if (!double.IsFinite(displayedScale) || displayedScale <= 0) displayedScale = 1;
        image.Width = BaseSize; image.Height = BaseSize; image.RenderTransformOrigin = new Point(.5, .5); image.RenderTransform = new ScaleTransform(displayedScale, displayedScale); Canvas.SetLeft(image, Math.Max(0, (viewport.Width - BaseSize) / 2)); Canvas.SetTop(image, Math.Max(0, (viewport.Height - BaseSize) / 2));
    }
}
