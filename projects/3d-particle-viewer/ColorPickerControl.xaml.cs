using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ParticleModelViewer;

public partial class ColorPickerControl : UserControl
{
    private const int WheelSize = 180;
    private bool suppressChanges;
    private bool isWheelDragging;
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

    private void ColorWheelImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        isWheelDragging = true;
        ColorWheelImage.CaptureMouse();
        UpdateWheel(e.GetPosition(ColorWheelImage));
        e.Handled = true;
    }

    private void ColorWheelImage_MouseMove(object sender, MouseEventArgs e)
    {
        if (isWheelDragging && e.LeftButton == MouseButtonState.Pressed) UpdateWheel(e.GetPosition(ColorWheelImage));
    }

    private void ColorWheelImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        EndWheelDrag();
        e.Handled = true;
    }

    private void ColorWheelImage_LostMouseCapture(object sender, MouseEventArgs e) => EndWheelDrag();

    private void EndWheelDrag()
    {
        isWheelDragging = false;
        if (Mouse.Captured == ColorWheelImage) Mouse.Capture(null);
    }

    private void UpdateWheel(Point point)
    {
        var width = Math.Max(1, ColorWheelImage.ActualWidth);
        var height = Math.Max(1, ColorWheelImage.ActualHeight);
        var dx = point.X - width / 2;
        var dy = point.Y - height / 2;
        var radius = Math.Sqrt(dx * dx + dy * dy);
        var maxRadius = Math.Min(width, height) / 2;
        saturation = Math.Clamp(radius / maxRadius, 0, 1);
        hue = (Math.Atan2(-dy, dx) * 180 / Math.PI + 360) % 360;
        RenderPicker(true);
    }

    private void ValueSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (suppressChanges) return;
        value = Math.Clamp(e.NewValue, 0, 1);
        RenderPicker(true);
    }

    private void RenderPicker(bool notify)
    {
        suppressChanges = true;
        ValueSlider.Value = value;
        suppressChanges = false;
        PreviewBorder.Background = new SolidColorBrush(SelectedColor);
        HexValueText.Text = $"HEX: {HexValue}";
        ColorWheelImage.Source = CreateColorWheelBitmap();
        if (notify) ColorChanged?.Invoke(this, EventArgs.Empty);
    }

    private static BitmapSource CreateColorWheelBitmap()
    {
        var pixels = new byte[WheelSize * WheelSize * 4];
        var center = (WheelSize - 1) / 2d;
        var maxRadius = WheelSize / 2d;
        for (var y = 0; y < WheelSize; y++)
        for (var x = 0; x < WheelSize; x++)
        {
            var dx = x - center;
            var dy = y - center;
            var radius = Math.Sqrt(dx * dx + dy * dy);
            var index = (y * WheelSize + x) * 4;
            if (radius > maxRadius) { pixels[index + 3] = 0; continue; }
            var selectedHue = (Math.Atan2(-dy, dx) * 180 / Math.PI + 360) % 360;
            var color = HsvToColor(selectedHue, radius / maxRadius, 1);
            pixels[index] = color.B; pixels[index + 1] = color.G; pixels[index + 2] = color.R; pixels[index + 3] = 255;
        }
        return BitmapSource.Create(WheelSize, WheelSize, 96, 96, PixelFormats.Bgra32, null, pixels, WheelSize * 4);
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
