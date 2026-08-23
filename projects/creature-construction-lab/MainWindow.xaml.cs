using System.Globalization;
using System.Numerics;
using System.Windows;
using System.Windows.Input;
using CreatureConstructionLab.Editor;
using CreatureConstructionLab.Rendering;

namespace CreatureConstructionLab;

public partial class MainWindow : Window
{
    private readonly EditorState state = new();
    private readonly CreatureCanvas canvas;

    public MainWindow()
    {
        InitializeComponent();
        canvas = new CreatureCanvas(state);
        CanvasHost.Content = canvas;
        state.Changed += Refresh;
        Refresh();
    }

    private void Refresh()
    {
        SelectionText.Text = state.SelectedNode is null ? "NONE" : state.SelectedNode.Id.ToString()[..8].ToUpperInvariant();
        var node = state.SelectedNode;
        XBox.Text = node is null ? "" : node.Position.X.ToString("0.##", CultureInfo.InvariantCulture);
        YBox.Text = node is null ? "" : node.Position.Y.ToString("0.##", CultureInfo.InvariantCulture);
        RadiusBox.Text = node is null ? "" : node.Radius.ToString("0.##", CultureInfo.InvariantCulture);
        RotationBox.Text = node is null ? "" : node.Rotation.ToString("0.##", CultureInfo.InvariantCulture);
        SpacingBox.Text = state.Creature.ChainSettings.Spacing.ToString("0.##", CultureInfo.InvariantCulture);
        StiffnessBox.Text = state.Creature.ChainSettings.Stiffness.ToString("0.##", CultureInfo.InvariantCulture);
        DampingBox.Text = state.Creature.ChainSettings.Damping.ToString("0.##", CultureInfo.InvariantCulture);
        HelpText.Text = state.StatusMessage ?? "Drag the root to move the whole chain. Drag the bright direction handle to rotate. Press DELETE to remove the selected node and its descendants.";
        StatusText.Text = $"{state.Mode.ToString().ToUpperInvariant()} MODE  /  {state.Creature.Nodes.Count} NODE{(state.Creature.Nodes.Count == 1 ? "" : "S")}";
        CreateModeButton.Background = state.Mode == EditorMode.Create ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(18, 70, 43)) : null;
        PlayModeButton.Background = state.Mode == EditorMode.Play ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(18, 70, 43)) : null;
    }

    private void CreateMode_Click(object sender, RoutedEventArgs e) => state.SetMode(EditorMode.Create);
    private void PlayMode_Click(object sender, RoutedEventArgs e) => state.SetMode(EditorMode.Play);
    private void NodeTool_Click(object sender, RoutedEventArgs e) => state.Tool = EditorTool.Node;
    private void SelectTool_Click(object sender, RoutedEventArgs e) => state.Tool = EditorTool.Select;
    private void Reset_Click(object sender, RoutedEventArgs e) => state.Reset();
    private void AddNextNode_Click(object sender, RoutedEventArgs e)
    {
        if (state.SelectedNode is null) { state.ClearStatus(); return; }
        if (state.AddNextNode() is null) { state.SetStatus("Only the current chain end can be extended."); }
    }

    private void ChainProperty_LostFocus(object sender, RoutedEventArgs e)
    {
        if (float.TryParse(SpacingBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var spacing)) state.SetSpacing(spacing);
        if (float.TryParse(StiffnessBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var stiffness) && float.TryParse(DampingBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var damping)) state.SetChainSettings(stiffness, damping);
    }

    private void Property_LostFocus(object sender, RoutedEventArgs e)
    {
        var node = state.SelectedNode;
        if (node is null) return;
        if (float.TryParse(XBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var x) && float.TryParse(YBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var y)) node.Position = new Vector2(x, y);
        if (float.TryParse(RadiusBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var radius)) node.Radius = Math.Max(4, radius);
        if (float.TryParse(RotationBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var rotation)) node.Rotation = rotation;
        state.Select(node);
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Delete or Key.Back) { state.DeleteSelected(); e.Handled = true; }
    }
}
