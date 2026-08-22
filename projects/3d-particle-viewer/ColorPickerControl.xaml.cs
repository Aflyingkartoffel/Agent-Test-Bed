using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ParticleModelViewer;

public partial class ColorPickerControl : UserControl
{
    private const int HueWidth = 360;
    private const int HueHeight = 18;
    private const int SaturationValueWidth = 220;
    private const int SaturationValueHeight = 128;
    private bool suppressChanges;
    private double hue = 252;
    private double saturation = 0.42;
    private double value = 0.97;

    public ColorPickerControl()
    {
        InitializeComponent();
        ValueSlider.ValueChanged += ValueSlider_Changed;
        RenderPicker(false);
    }

    public event EventHandler? ColorChanged;

    public Color SelectedColor => HsvToColor(hue, saturation, value);

    public string HexValue => $"#{SelectedColor.R:X2}{SelectedColor.G:X2}{SelectedColor.B:X2}";

    public void SetColor(Color color, bool notify = false)
    {
        RgbToHsv(color, out hue, out saturation, out value);
        RenderPicker(notify);
    }

    private void HueImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        HueImage.CaptureMouse();
        UpdateHue(e.GetPosition(HueImage).X);
    }

    private void HueImage_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed) UpdateHue(e.GetPosition(HueImage).X);
    }

    private void SaturationValueImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        SaturationValueImage.CaptureMouse();
        UpdateSaturationAndValue(e.GetPosition(SaturationValueImage));
    }

    private void SaturationValueImage_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed) UpdateSaturationAndValue(e.GetPosition(SaturationValueImage));
    }

    private void UpdateHue(double x)
    {
        hue = Math.Clamp(x / Math.Max(1, HueImage.ActualWidth) * 360, 0, 360);
        RenderPicker(true);
    }

    private void UpdateSaturationAndValue(Point point)
    {
        saturation = Math.Clamp(point.X / Math.Max(1, SaturationValueImage.ActualWidth), 0, 1);
        value = Math.Clamp(1 - point.Y / Math.Max(1, SaturationValueImage.ActualHeight), 0, 1);
        RenderPicker(true);
    }

    private void ValueSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (suppressChanges) return;
        value = e.NewValue;
        RenderPicker(true);
    }

    private void RenderPicker(bool notify)
    {
        suppressChanges = true;
        ValueSlider.Value = value;
        suppressChanges = false;
        PreviewBorder.Background = new SolidColorBrush(SelectedColor);
        HexValueText.Text = $"HEX: {HexValue}";
        HueImage.Source = CreateHueBitmap();
        SaturationValueImage.Source = CreateSaturationValueBitmap();
        if (notify) ColorChanged?.Invoke(this, EventArgs.Empty);
    }

    private BitmapSource CreateHueBitmap()
    {
        var pixels = new byte[HueWidth * HueHeight * 4];
        for (var x = 0; x < HueWidth; x++)
        {
            var color = HsvToColor(x, 1, 1);
            for (var y = 0; y < HueHeight; y++) SetPixel(pixels, x, y, HueWidth, color);
        }
        return BitmapSource.Create(HueWidth, HueHeight, 96, 96, PixelFormats.Bgra32, null, pixels, HueWidth * 4);
    }

    private BitmapSource CreateSaturationValueBitmap()
    {
        var pixels = new byte[SaturationValueWidth * SaturationValueHeight * 4];
        for (var y = 0; y < SaturationValueHeight; y++)
        for (var x = 0; x < SaturationValueWidth; x++)
            SetPixel(pixels, x, y, SaturationValueWidth, HsvToColor(hue, (double)x / (SaturationValueWidth - 1), 1 - (double)y / (SaturationValueHeight - 1)));
        return BitmapSource.Create(SaturationValueWidth, SaturationValueHeight, 96, 96, PixelFormats.Bgra32, null, pixels, SaturationValueWidth * 4);
    }

    private static void SetPixel(byte[] pixels, int x, int y, int width, Color color)
    {
        var index = (y * width + x) * 4;
        pixels[index] = color.B;
        pixels[index + 1] = color.G;
        pixels[index + 2] = color.R;
        pixels[index + 3] = 255;
    }

    private static Color HsvToColor(double hue, double saturation, double value)
    {
        var chroma = value * saturation;
        var sector = hue / 60;
        var x = chroma * (1 - Math.Abs(sector % 2 - 1));
        var (red, green, blue) = sector switch
        {
            < 1 => (chroma, x, 0d), < 2 => (x, chroma, 0d), < 3 => (0d, chroma, x),
            < 4 => (0d, x, chroma), < 5 => (x, 0d, chroma), _ => (chroma, 0d, x)
        };
        var match = value - chroma;
        return Color.FromRgb((byte)Math.Round((red + match) * 255), (byte)Math.Round((green + match) * 255), (byte)Math.Round((blue + match) * 255));
    }

    private static void RgbToHsv(Color color, out double hue, out double saturation, out double value)
    {
        var red = color.R / 255d; var green = color.G / 255d; var blue = color.B / 255d;
        var max = Math.Max(red, Math.Max(green, blue)); var min = Math.Min(red, Math.Min(green, blue));
        var delta = max - min;
        hue = delta == 0 ? 0 : max == red ? 60 * ((green - blue) / delta % 6) : max == green ? 60 * ((blue - red) / delta + 2) : 60 * ((red - green) / delta + 4);
        if (hue < 0) hue += 360;
        saturation = max == 0 ? 0 : delta / max;
        value = max;
    }
}
