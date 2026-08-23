using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Input;
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
    private readonly Rendering.FpsCounter fpsCounter = new();
    private double speedMultiplier = 1;
    private bool paused;
    private int selectedLightIndex;
    private bool updatingLightControls;
    private bool draggingLight;
    private double statsAccumulator;

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
        SyncLightControls();
        UpdateControlValueLabels();
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
        fpsCounter.Update(deltaTime);
        if (!paused) simulation.Update(deltaTime * (float)speedMultiplier);
        statsAccumulator += deltaTime;
        Render();
    }

    private void Render()
    {
        renderer.Render(simulation, selectedLightIndex);
        Viewport.Source = renderer.Bitmap as BitmapSource;
        if (statsAccumulator >= 0.2)
        {
            statsAccumulator = 0;
            UpdateStatistics();
        }
    }

    private void UpdateStatistics()
    {
        StatsText.Text = $"FPS: {fpsCounter.Value,3:0}   SIM TIME {simulation.SimulationTime,5:0.0}s   INSECTS {simulation.Agents.Count,4}   AVG SPEED {simulation.AverageSpeed,5:0.0}   LIGHTS {simulation.Lights.Count}";
        StatsPanelText.Text = $"FPS: {fpsCounter.Value:0}\nTIME: {simulation.SimulationTime:0.0}s\nINSECTS: {simulation.Agents.Count}\nAVG SPEED: {simulation.AverageSpeed:0.0}\nLIGHTS: {simulation.Lights.Count}";
    }

    private void PauseButton_Click(object sender, RoutedEventArgs e)
    {
        paused = !paused;
        PauseButton.Content = paused ? "RESUME" : "PAUSE";
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        simulation.Reset();
        selectedLightIndex = 0;
        SyncLightControls();
    }

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
        settings.BaseSpeed = (float)SpeedSlider.Value;
        settings.TurnRate = (float)TurnSlider.Value;
        settings.WanderStrength = (float)WanderSlider.Value;
        UpdateControlValueLabels();
        if (!updatingLightControls && selectedLightIndex < simulation.Lights.Count)
        {
            LightSource light = simulation.Lights[selectedLightIndex];
            light.SetPower((float)PowerSlider.Value);
            UpdatePowerLabels(light);
        }
    }

    private void BoundaryBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (IsInitialized) settings.BoundaryMode = BoundaryBox.SelectedIndex == 1 ? BoundaryMode.SoftBounce : BoundaryMode.Wrap;
    }

    private void AddLightButton_Click(object sender, RoutedEventArgs e)
    {
        if (simulation.Lights.Count >= SimulationEngine.MaxLights) return;
        simulation.AddLight();
        selectedLightIndex = simulation.Lights.Count - 1;
        SyncLightControls();
    }

    private void RemoveLightButton_Click(object sender, RoutedEventArgs e)
    {
        selectedLightIndex = simulation.RemoveLight(selectedLightIndex);
        SyncLightControls();
    }

    private void Viewport_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        Point mousePoint = e.GetPosition(Viewport);
        int hit = simulation.FindClosestLight(ViewportToSimulation(mousePoint), 16f);
        if (hit < 0) return;
        selectedLightIndex = hit;
        draggingLight = true;
        Viewport.CaptureMouse();
        SyncLightControls();
        e.Handled = true;
    }

    private void Viewport_MouseMove(object sender, MouseEventArgs e)
    {
        if (!draggingLight || selectedLightIndex >= simulation.Lights.Count) return;
        simulation.Lights[selectedLightIndex].Position = ViewportToSimulation(e.GetPosition(Viewport));
        UpdateLightLabels();
    }

    private void Viewport_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        draggingLight = false;
        Viewport.ReleaseMouseCapture();
    }

    private System.Numerics.Vector2 ViewportToSimulation(Point viewportPoint)
    {
        double xScale = simulation.Width / Math.Max(1, Viewport.ActualWidth);
        double yScale = simulation.Height / Math.Max(1, Viewport.ActualHeight);
        return new System.Numerics.Vector2((float)Math.Clamp(viewportPoint.X * xScale, 0, simulation.Width), (float)Math.Clamp(viewportPoint.Y * yScale, 0, simulation.Height));
    }

    private void SyncLightControls()
    {
        if (selectedLightIndex >= simulation.Lights.Count) selectedLightIndex = simulation.Lights.Count - 1;
        if (selectedLightIndex < 0) return;
        LightSource light = simulation.Lights[selectedLightIndex];
        updatingLightControls = true;
        PowerSlider.Value = light.Power;
        updatingLightControls = false;
        UpdatePowerLabels(light);
        UpdateLightLabels();
    }

    private void UpdateLightLabels()
    {
        if (selectedLightIndex < 0 || selectedLightIndex >= simulation.Lights.Count) return;
        LightSource light = simulation.Lights[selectedLightIndex];
        SelectedLightText.Text = $"SELECTED LIGHT: {light.Id} / {simulation.Lights.Count}";
        LightPositionText.Text = $"POSITION: ({light.Position.X:0}, {light.Position.Y:0})";
    }

    private void UpdatePowerLabels(LightSource light)
    {
        PowerValueText.Text = light.Power.ToString("0.00");
        LightValuesText.Text = $"ATTR {light.AttractionStrength:0.00}   RADIUS {light.InfluenceRadius:0}   INTENSITY {light.VisualIntensity:0.00}";
    }

    private void UpdateControlValueLabels()
    {
        if (!IsInitialized) return;
        BaseSpeedValueText.Text = SpeedSlider.Value.ToString("0");
        TurnValueText.Text = TurnSlider.Value.ToString("0.00");
        WanderValueText.Text = WanderSlider.Value.ToString("0.00");
    }
}
