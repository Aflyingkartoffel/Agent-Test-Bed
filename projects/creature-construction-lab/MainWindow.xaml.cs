using System.Globalization;
using System.Numerics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CreatureConstructionLab.Editor;
using CreatureConstructionLab.Rendering;

namespace CreatureConstructionLab;

public partial class MainWindow : Window
{
    private readonly EditorState state = new();
    private readonly CreatureCanvas canvas;
    private readonly DispatcherTimer simulationTimer = new() { Interval = TimeSpan.FromMilliseconds(16) };

    public MainWindow()
    {
        InitializeComponent();
        canvas = new CreatureCanvas(state);
        CanvasHost.Content = canvas;
        RampHost.Content = new BodySizeRampCanvas(state);
        state.Changed += Refresh;
        simulationTimer.Tick += (_, _) => state.UpdateSimulation(1f / 60f);
        simulationTimer.Start();
        Refresh();
    }

    private void Refresh()
    {
        SelectionText.Text = state.SelectedNode is null ? "NONE" : state.SelectedNode.Id.ToString()[..8].ToUpperInvariant();
        var node = state.SelectedNode;
        XBox.Text = node is null ? "" : node.Position.X.ToString("0.##", CultureInfo.InvariantCulture);
        YBox.Text = node is null ? "" : node.Position.Y.ToString("0.##", CultureInfo.InvariantCulture);
        NormalizedText.Text = node is null ? "" : node.NormalizedPosition.ToString("0.##", CultureInfo.InvariantCulture);
        RampValueText.Text = node is null ? "" : node.RampValue.ToString("0.##", CultureInfo.InvariantCulture);
        RadiusText.Text = node is null ? "" : node.Radius.ToString("0.##", CultureInfo.InvariantCulture);
        RotationBox.Text = node is null ? "" : node.Rotation.ToString("0.##", CultureInfo.InvariantCulture);
        BaseRadiusBox.Text = state.Creature.BaseRadius.ToString("0.##", CultureInfo.InvariantCulture);
        SpacingBox.Text = state.Creature.ChainSettings.Spacing.ToString("0.##", CultureInfo.InvariantCulture);
        StiffnessBox.Text = state.Creature.ChainSettings.Stiffness.ToString("0.##", CultureInfo.InvariantCulture);
        DampingBox.Text = state.Creature.ChainSettings.Damping.ToString("0.##", CultureInfo.InvariantCulture);
        HelpText.Text = state.StatusMessage ?? "Drag the root to move the whole chain. Drag the bright direction handle to rotate. Press DELETE to remove the selected node and its descendants.";
        PlayPanel.Visibility = state.Mode == EditorMode.Play ? Visibility.Visible : Visibility.Collapsed;
        var editing = state.Mode == EditorMode.Create;
        AddNextButton.IsEnabled = editing;
        ResetCurveButton.IsEnabled = editing;
        RampHost.IsEnabled = editing;
        XBox.IsEnabled = editing;
        YBox.IsEnabled = editing;
        RotationBox.IsEnabled = editing;
        SpacingBox.IsEnabled = editing;
        StiffnessBox.IsEnabled = editing;
        DampingBox.IsEnabled = editing;
        BaseRadiusBox.IsEnabled = editing;
        PlayPauseButton.Content = state.Simulator.State.Paused ? "RESUME" : "PAUSE";
        MaxSpeedBox.Text = state.Simulator.State.MaxSpeed.ToString("0.##", CultureInfo.InvariantCulture);
        AccelerationBox.Text = state.Simulator.State.AccelerationStrength.ToString("0.##", CultureInfo.InvariantCulture);
        PlayDampingBox.Text = state.Simulator.State.Damping.ToString("0.##", CultureInfo.InvariantCulture);
        SimulationSpeedBox.SelectedIndex = state.Simulator.State.SimulationSpeed switch { <= 0.3f => 0, <= 0.75f => 1, <= 1.5f => 2, _ => 3 };
        StatusText.Text = $"{state.Mode.ToString().ToUpperInvariant()} MODE  /  {state.Creature.Nodes.Count} NODE{(state.Creature.Nodes.Count == 1 ? "" : "S")}";
        CreateModeButton.Background = state.Mode == EditorMode.Create ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(18, 70, 43)) : null;
        PlayModeButton.Background = state.Mode == EditorMode.Play ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(18, 70, 43)) : null;
    }

    private void CreateMode_Click(object sender, RoutedEventArgs e) => state.SetMode(EditorMode.Create);
    private void PlayMode_Click(object sender, RoutedEventArgs e) => state.SetMode(EditorMode.Play);
    private void NodeTool_Click(object sender, RoutedEventArgs e) => state.Tool = EditorTool.Node;
    private void SelectTool_Click(object sender, RoutedEventArgs e) => state.Tool = EditorTool.Select;
    private void Reset_Click(object sender, RoutedEventArgs e) => state.Reset();
    private void PlayPause_Click(object sender, RoutedEventArgs e) => state.SetPaused(!state.Simulator.State.Paused);
    private void ResetSimulation_Click(object sender, RoutedEventArgs e) => state.ResetSimulation();
    private void SimulationSpeed_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (!IsLoaded || SimulationSpeedBox.SelectedIndex < 0) return;
        var speed = SimulationSpeedBox.SelectedIndex switch { 0 => 0.25f, 1 => 0.5f, 2 => 1f, _ => 2f };
        state.SetPlaySettings(speed, state.Simulator.State.MaxSpeed, state.Simulator.State.AccelerationStrength, state.Simulator.State.Damping);
    }
    private void PlaySetting_LostFocus(object sender, RoutedEventArgs e)
    {
        if (float.TryParse(MaxSpeedBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var maxSpeed) && float.TryParse(AccelerationBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var acceleration) && float.TryParse(PlayDampingBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var damping))
            state.SetPlaySettings(state.Simulator.State.SimulationSpeed, maxSpeed, acceleration, damping);
    }
    private void ResetCurve_Click(object sender, RoutedEventArgs e) { if (state.Mode == EditorMode.Create) state.ResetBodySizeRamp(); }
    private void BaseRadius_LostFocus(object sender, RoutedEventArgs e)
    {
        if (state.Mode == EditorMode.Create && float.TryParse(BaseRadiusBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var radius)) state.SetBaseRadius(radius);
    }
    private void AddNextNode_Click(object sender, RoutedEventArgs e)
    {
        if (state.Mode != EditorMode.Create) return;
        if (state.SelectedNode is null) { state.ClearStatus(); return; }
        if (state.AddNextNode() is null) { state.SetStatus("Only the current chain end can be extended."); }
    }

    private void ChainProperty_LostFocus(object sender, RoutedEventArgs e)
    {
        if (state.Mode != EditorMode.Create) return;
        if (float.TryParse(SpacingBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var spacing)) state.SetSpacing(spacing);
        if (float.TryParse(StiffnessBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var stiffness) && float.TryParse(DampingBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var damping)) state.SetChainSettings(stiffness, damping);
    }

    private void Property_LostFocus(object sender, RoutedEventArgs e)
    {
        if (state.Mode != EditorMode.Create) return;
        var node = state.SelectedNode;
        if (node is null) return;
        if (float.TryParse(XBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var x) && float.TryParse(YBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var y)) node.Position = new Vector2(x, y);
        if (float.TryParse(RotationBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var rotation)) node.Rotation = rotation;
        state.Select(node);
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Delete or Key.Back) { state.DeleteSelected(); e.Handled = true; }
    }
}
