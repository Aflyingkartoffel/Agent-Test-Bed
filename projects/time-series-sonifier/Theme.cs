using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace TimeSeriesSonifier;

public enum AppearanceMode { Light, Dark }
public enum GraphRevealMode { Progressive, FullGraph }

public static class GraphReveal
{
    public const double InitialFraction = .01;
    public static double Progress(double normalizedPosition, GraphRevealMode mode) => mode == GraphRevealMode.FullGraph ? 1 : Math.Clamp(double.IsFinite(normalizedPosition) ? Math.Max(InitialFraction, normalizedPosition) : InitialFraction, InitialFraction, 1);
}

public sealed record ThemePalette(Color AppBackground, Color PanelBackground, Color GraphBackground, Color PrimaryText, Color SecondaryText, Color Border, Color Blue, Color Green, Color Grid, Color SpectrumFill)
{
    public static ThemePalette For(AppearanceMode mode) => mode == AppearanceMode.Dark
        ? new(Color.FromRgb(16, 20, 25), Color.FromRgb(23, 28, 34), Color.FromRgb(17, 22, 28), Color.FromRgb(241, 245, 249), Color.FromRgb(170, 180, 191), Color.FromRgb(58, 67, 77), Color.FromRgb(85, 168, 255), Color.FromRgb(69, 209, 141), Color.FromRgb(57, 67, 77), Color.FromArgb(65, 69, 209, 141))
        : new(Color.FromRgb(247, 249, 251), Colors.White, Colors.White, Color.FromRgb(31, 41, 51), Color.FromRgb(101, 114, 126), Color.FromRgb(217, 225, 232), Color.FromRgb(25, 118, 210), Color.FromRgb(32, 164, 100), Color.FromRgb(225, 231, 236), Color.FromArgb(65, 32, 164, 100));
    public Brush Brush(Color color) { var brush = new SolidColorBrush(color); brush.Freeze(); return brush; }
}

public sealed class ThemeManager : INotifyPropertyChanged
{
    public static AppearanceMode ActiveMode { get; private set; }
    public AppearanceMode Mode { get; private set; } = AppearanceMode.Light;
    public ThemePalette Palette => ThemePalette.For(Mode);
    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? ThemeChanged;
    public void SetMode(AppearanceMode mode)
    {
        if (Mode == mode) return;
        Mode = mode;
        ActiveMode = mode;
        ApplyResources(Application.Current?.Resources);
        PropertyChanged?.Invoke(this, new(nameof(Mode)));
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }
    public void ApplyResources(ResourceDictionary? resources)
    {
        if (resources is null) return;
        var palette = Palette;
        Set(resources, "AppBackgroundBrush", palette.AppBackground); Set(resources, "PanelBackgroundBrush", palette.PanelBackground); Set(resources, "PrimaryBlueBrush", palette.Blue); Set(resources, "SecondaryGreenBrush", palette.Green); Set(resources, "PrimaryTextBrush", palette.PrimaryText); Set(resources, "SecondaryTextBrush", palette.SecondaryText); Set(resources, "BorderBrush", palette.Border); Set(resources, "MutedBackgroundBrush", palette.GraphBackground); Set(resources, "GraphBackgroundBrush", palette.GraphBackground); Set(resources, "GridBrush", palette.Grid); Set(resources, "GraphLineBrush", palette.Blue); Set(resources, "SpectrumFillBrush", palette.SpectrumFill);
    }
    static void Set(ResourceDictionary resources, string key, Color color) { if (resources[key] is SolidColorBrush brush) brush.Color = color; else resources[key] = new SolidColorBrush(color); }
}
