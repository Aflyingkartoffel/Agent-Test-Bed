using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using InsectLightSimulation.Rendering;
using InsectLightSimulation.Simulation;

namespace InsectLightSimulation;

public partial class MainWindow : Window
{
    private readonly SimulationSettings settings = new();
    private readonly SimulationEngine simulation;
    private readonly PixelRenderer renderer = new();
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private long lastTicks;
    private double fps;
    private double speedMultiplier = 1;
    private bool paused;

    public MainWindow()
    {
        InitializeComponent();
        simulation = new SimulationEngine(settings);
        timer.Tick += Timer_Tick;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        lastTicks = clock.ElapsedTicks;
        ResizeSimulation();
        timer.Start();
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e) => ResizeSimulation();

    private void ResizeSimulation()
    {
        if (Viewport.ActualWidth < 2 || Viewport.ActualHeight < 2) return;
        simulation.Resize((float)Viewport.ActualWidth, (float)Viewport.ActualHeight);
        Render();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        long now = clock.ElapsedTicks;
        float deltaTime = (float)(now - lastTicks) / Stopwatch.Frequency;
        lastTicks = now;
        fps = fps * 0.92 + (1 / Math.Max(0.001, deltaTime)) * 0.08;
        if (!paused) simulation.Update(deltaTime * (float)speedMultiplier);
        Render();
    }

    private void Render()
    {
        renderer.Render(simulation, settings.LightIntensity);
        Viewport.Source = renderer.Bitmap as BitmapSource;
        StatsText.Text = $"SIM TIME {simulation.SimulationTime,5:0.0}s   FPS {fps,4:0}   INSECTS {simulation.Agents.Count,4}   AVG SPEED {simulation.AverageSpeed,5:0.0}";
    }

    private void PauseButton_Click(object sender, RoutedEventArgs e)
    {
        paused = !paused;
        PauseButton.Content = paused ? "RESUME" : "PAUSE";
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e) => simulation.Reset();

    private void SpeedBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (SpeedBox.SelectedIndex >= 0) speedMultiplier = new[] { 0.25, 0.5, 1.0, 2.0, 4.0 }[SpeedBox.SelectedIndex];
    }

    private void CountSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsInitialized) return;
        int count = (int)e.NewValue;
        CountValue.Text = count.ToString();
        if (simulation.Settings.InsectCount != count) simulation.SetAgentCount(count);
    }

    private void SettingSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsInitialized) return;
        settings.AttractionStrength = (float)AttractionSlider.Value;
        settings.InfluenceRadius = (float)RadiusSlider.Value;
        settings.LightIntensity = (float)IntensitySlider.Value;
        settings.BaseSpeed = (float)SpeedSlider.Value;
        settings.TurnRate = (float)TurnSlider.Value;
        settings.WanderStrength = (float)WanderSlider.Value;
    }

    private void BoundaryBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (IsInitialized) settings.BoundaryMode = BoundaryBox.SelectedIndex == 1 ? BoundaryMode.SoftBounce : BoundaryMode.Wrap;
    }
}
