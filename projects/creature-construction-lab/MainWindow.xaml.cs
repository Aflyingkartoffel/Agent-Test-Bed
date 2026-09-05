using System.Globalization;
using System.Numerics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CreatureConstructionLab.Editor;
using CreatureConstructionLab.IO;
using CreatureConstructionLab.Model;
using CreatureConstructionLab.Rendering;

namespace CreatureConstructionLab;

public partial class MainWindow : Window
{
    private readonly EditorState state = new();
    private readonly CreatureCanvas canvas;
    private readonly DispatcherTimer simulationTimer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private bool refreshing;

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
        if (refreshing) return;
        refreshing = true;
        SelectionText.Text = state.SelectedNode is not null ? state.SelectedNode.Id.ToString()[..8].ToUpperInvariant() : state.SelectedFeature is null ? "NONE" : $"FEATURE {state.SelectedFeature.Id.ToString()[..8].ToUpperInvariant()}";
        var node = state.SelectedNode;
        XBox.Text = node is null ? "" : node.Position.X.ToString("0.##", CultureInfo.InvariantCulture);
        YBox.Text = node is null ? "" : node.Position.Y.ToString("0.##", CultureInfo.InvariantCulture);
        NormalizedText.Text = node is null ? "" : node.NormalizedPosition.ToString("0.##", CultureInfo.InvariantCulture);
        RampValueText.Text = node is null ? "" : node.RampValue.ToString("0.##", CultureInfo.InvariantCulture);
        RadiusText.Text = node is null ? "" : node.Radius.ToString("0.##", CultureInfo.InvariantCulture);
        RotationBox.Text = node is null ? "" : node.Rotation.ToString("0.##", CultureInfo.InvariantCulture);
        BaseRadiusBox.Text = state.Creature.BaseRadius.ToString("0.##", CultureInfo.InvariantCulture);
        InterpolationBox.SelectedIndex = (int)state.Creature.BodySizeRamp.Interpolation;
        SpacingBox.Text = state.Creature.ChainSettings.Spacing.ToString("0.##", CultureInfo.InvariantCulture);
        StiffnessBox.Text = state.Creature.ChainSettings.Stiffness.ToString("0.##", CultureInfo.InvariantCulture);
        DampingBox.Text = state.Creature.ChainSettings.Damping.ToString("0.##", CultureInfo.InvariantCulture);
        CreatePanel.Visibility = state.Mode == EditorMode.Create ? Visibility.Visible : Visibility.Collapsed;
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
        InterpolationBox.IsEnabled = editing;
        CreateShowNodesBox.IsChecked = state.Display.CreateShowNodes;
        CreateShowSkinBox.IsChecked = state.Display.CreateShowSkin;
        CreateShowMusclesBox.IsChecked = state.Display.CreateShowMuscles;
        SkinColorBox.Text = $"#{state.Creature.SkinColorArgb:X8}";
        FinColorBox.Text = $"#{state.Creature.FinColorArgb:X8}";
        PlaySolidBodyBox.IsChecked = state.Display.PlaySolidBody;
        PlayShowSkinBox.IsChecked = state.Display.PlayShowSkin;
        PlayShowSkeletonBox.IsChecked = state.Display.PlayShowSkeleton;
        PlayShowMusclesBox.IsChecked = state.Display.PlayShowMuscles;
        PlayShowEyesBox.IsChecked = state.Display.PlayShowFeatures;
        FeatureListBox.Items.Clear();
        foreach (var feature in state.Creature.Features) FeatureListBox.Items.Add($"{feature.Type} {feature.Id.ToString()[..6].ToUpperInvariant()}");
        FeatureListBox.SelectedIndex = state.SelectedFeature is null ? -1 : state.Creature.Features.IndexOf(state.SelectedFeature);
        var selectedFeature = state.SelectedFeature;
        FeatureTypeBox.SelectedIndex = selectedFeature is null ? -1 : (int)selectedFeature.Type;
        FeatureParentBox.Items.Clear();
        foreach (var featureNode in state.Creature.Nodes) FeatureParentBox.Items.Add($"Node {state.Creature.Nodes.IndexOf(featureNode)}");
        FeatureParentBox.SelectedIndex = selectedFeature is null ? -1 : state.Creature.Nodes.FindIndex(n => n.Id == selectedFeature.ParentNodeId);
        FeatureXBox.Text = selectedFeature is null ? "" : selectedFeature.LocalPosition.X.ToString("0.##", CultureInfo.InvariantCulture);
        FeatureYBox.Text = selectedFeature is null ? "" : selectedFeature.LocalPosition.Y.ToString("0.##", CultureInfo.InvariantCulture);
        FeatureRotationBox.Text = selectedFeature is null ? "" : selectedFeature.LocalRotation.ToString("0.##", CultureInfo.InvariantCulture);
        FeatureScaleBox.Text = selectedFeature is null ? "" : selectedFeature.Scale.ToString("0.##", CultureInfo.InvariantCulture);
        FeatureEyeSizeBox.Text = selectedFeature is null ? "" : selectedFeature.EyeSize.ToString("0.##", CultureInfo.InvariantCulture);
        FeatureTrackingBox.Text = selectedFeature is null ? "" : selectedFeature.EyeTrackingStrength.ToString("0.##", CultureInfo.InvariantCulture);
        TongueLengthBox.Text = selectedFeature is null ? "" : selectedFeature.TongueLength.ToString("0.##", CultureInfo.InvariantCulture);
        TongueForkLengthBox.Text = selectedFeature is null ? "" : selectedFeature.TongueForkLength.ToString("0.##", CultureInfo.InvariantCulture);
        TongueForkAngleBox.Text = selectedFeature is null ? "" : selectedFeature.TongueForkAngle.ToString("0.##", CultureInfo.InvariantCulture);
        FinSideBox.SelectedIndex = selectedFeature is null ? -1 : (int)selectedFeature.FinSide;
        FinLengthBox.Text = selectedFeature is null ? "" : selectedFeature.FinLength.ToString("0.##", CultureInfo.InvariantCulture);
        FinWidthBox.Text = selectedFeature is null ? "" : selectedFeature.FinWidth.ToString("0.##", CultureInfo.InvariantCulture);
        FinBaseAngleBox.Text = selectedFeature is null ? "" : selectedFeature.FinBaseAngle.ToString("0.##", CultureInfo.InvariantCulture);
        FinStiffnessBox.Text = selectedFeature is null ? "" : selectedFeature.FinAngularStiffness.ToString("0.##", CultureInfo.InvariantCulture);
        FinDampingBox.Text = selectedFeature is null ? "" : selectedFeature.FinAngularDamping.ToString("0.##", CultureInfo.InvariantCulture);
        var isEye = selectedFeature?.Type == CreatureFeatureType.Eye;
        var isTongue = selectedFeature?.Type == CreatureFeatureType.ForkedTongue;
        var isFin = selectedFeature?.Type == CreatureFeatureType.Fin;
        EyeSettingsPanel.Visibility = isEye ? Visibility.Visible : Visibility.Collapsed;
        TongueSettingsPanel.Visibility = isTongue ? Visibility.Visible : Visibility.Collapsed;
        FinSettingsPanel.Visibility = isFin ? Visibility.Visible : Visibility.Collapsed;
        FeatureMirrorBox.Content = isFin ? "MIRROR PAIR" : "MIRROR";
        FeatureMirrorBox.IsChecked = (isEye || isFin) && selectedFeature?.Mirrored == true;
        FeatureVisibleBox.IsChecked = selectedFeature?.Visible == true;
        var featureEditing = editing && selectedFeature is not null;
        FeatureTypeBox.IsEnabled = featureEditing;
        FeatureParentBox.IsEnabled = featureEditing;
        FeatureXBox.IsEnabled = featureEditing && !isFin;
        FeatureYBox.IsEnabled = featureEditing && !isFin;
        FeatureXBox.IsReadOnly = isFin;
        FeatureYBox.IsReadOnly = isFin;
        FeatureRotationBox.IsEnabled = featureEditing && !isFin;
        FeatureScaleBox.IsEnabled = featureEditing;
        FeatureEyeSizeBox.IsEnabled = featureEditing && isEye;
        FeatureTrackingBox.IsEnabled = featureEditing;
        TongueLengthBox.IsEnabled = featureEditing;
        TongueForkLengthBox.IsEnabled = featureEditing;
        TongueForkAngleBox.IsEnabled = featureEditing;
        FinSideBox.IsEnabled = featureEditing && isFin;
        FinLengthBox.IsEnabled = featureEditing && isFin;
        FinWidthBox.IsEnabled = featureEditing && isFin;
        FinBaseAngleBox.IsEnabled = featureEditing && isFin;
        FinStiffnessBox.IsEnabled = featureEditing && isFin;
        FinDampingBox.IsEnabled = featureEditing && isFin;
        FeatureMirrorBox.IsEnabled = featureEditing && (isEye || isFin);
        FeatureVisibleBox.IsEnabled = featureEditing;
        PlayPauseButton.Content = state.Simulator.State.Paused ? "RESUME" : "PAUSE";
        MaxSpeedBox.Text = state.Simulator.State.MaxSpeed.ToString("0.##", CultureInfo.InvariantCulture);
        AccelerationBox.Text = state.Simulator.State.AccelerationStrength.ToString("0.##", CultureInfo.InvariantCulture);
        PlayDampingBox.Text = state.Simulator.State.Damping.ToString("0.##", CultureInfo.InvariantCulture);
        SimulationSpeedBox.SelectedIndex = state.Simulator.State.SimulationSpeed switch { <= 0.3f => 0, <= 0.75f => 1, <= 1.5f => 2, _ => 3 };
        WaveEnabledBox.IsChecked = state.Simulator.State.Wave.Enabled;
        WaveAmplitudeBox.Text = state.Simulator.State.Wave.Amplitude.ToString("0.##", CultureInfo.InvariantCulture);
        WaveFrequencyBox.Text = state.Simulator.State.Wave.Frequency.ToString("0.##", CultureInfo.InvariantCulture);
        WavePhaseBox.Text = state.Simulator.State.Wave.Phase.ToString("0.##", CultureInfo.InvariantCulture);
        WaveInfluenceBox.Text = state.Simulator.State.Wave.Influence.ToString("0.##", CultureInfo.InvariantCulture);
        StatusText.Text = state.StatusMessage ?? $"{state.Mode.ToString().ToUpperInvariant()} MODE  /  {state.Creature.Nodes.Count} NODE{(state.Creature.Nodes.Count == 1 ? "" : "S")}";
        CreateModeButton.Background = state.Mode == EditorMode.Create ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(18, 70, 43)) : null;
        PlayModeButton.Background = state.Mode == EditorMode.Play ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(18, 70, 43)) : null;
        refreshing = false;
    }

    private void CreateMode_Click(object sender, RoutedEventArgs e) => state.SetMode(EditorMode.Create);
    private void PlayMode_Click(object sender, RoutedEventArgs e) => state.SetMode(EditorMode.Play);
    private void NodeTool_Click(object sender, RoutedEventArgs e) => state.Tool = EditorTool.Node;
    private void SelectTool_Click(object sender, RoutedEventArgs e) => state.Tool = EditorTool.Select;
    private void Delete_Click(object sender, RoutedEventArgs e) => state.DeleteSelected();
    private void Reset_Click(object sender, RoutedEventArgs e) => state.Reset();
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "Creature definition (*.creature.json)|*.creature.json|JSON files (*.json)|*.json", DefaultExt = ".creature.json", AddExtension = true, FileName = "creature" };
        if (dialog.ShowDialog() != true) return;
        try { CreatureFileService.Save(dialog.FileName, state.Creature); state.SetStatus($"Saved authored creature to {System.IO.Path.GetFileName(dialog.FileName)}."); }
        catch (Exception) { state.SetStatus("Could not save the creature definition."); }
    }
    private void Load_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Creature definition (*.creature.json;*.json)|*.creature.json;*.json|All files (*.*)|*.*", Multiselect = false };
        if (dialog.ShowDialog() != true) return;
        if (CreatureFileService.TryLoad(dialog.FileName, out var loaded, out var error) && loaded is not null) state.LoadDefinition(loaded);
        else state.SetStatus($"Load failed: {error}");
    }
    private void PlayPause_Click(object sender, RoutedEventArgs e) => state.SetPaused(!state.Simulator.State.Paused);
    private void ResetSimulation_Click(object sender, RoutedEventArgs e) => state.ResetSimulation();
    private void SimulationSpeed_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (!IsLoaded || refreshing || SimulationSpeedBox.SelectedIndex < 0) return;
        var speed = SimulationSpeedBox.SelectedIndex switch { 0 => 0.25f, 1 => 0.5f, 2 => 1f, _ => 2f };
        state.SetPlaySettings(speed, state.Simulator.State.MaxSpeed, state.Simulator.State.AccelerationStrength, state.Simulator.State.Damping);
    }
    private void PlaySetting_LostFocus(object sender, RoutedEventArgs e)
    {
        if (float.TryParse(MaxSpeedBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var maxSpeed) && float.TryParse(AccelerationBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var acceleration) && float.TryParse(PlayDampingBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var damping))
            state.SetPlaySettings(state.Simulator.State.SimulationSpeed, maxSpeed, acceleration, damping);
    }
    private void WaveSetting_Changed(object sender, RoutedEventArgs e) => ApplyWaveSettings();
    private void WaveSetting_LostFocus(object sender, RoutedEventArgs e) => ApplyWaveSettings();
    private void ApplyWaveSettings()
    {
        if (!IsLoaded || refreshing || !float.TryParse(WaveAmplitudeBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var amplitude) || !float.TryParse(WaveFrequencyBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var frequency) || !float.TryParse(WavePhaseBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var phase) || !float.TryParse(WaveInfluenceBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var influence)) return;
        state.SetWaveSettings(WaveEnabledBox.IsChecked == true, amplitude, frequency, phase, influence);
    }
    private void ResetCurve_Click(object sender, RoutedEventArgs e) { if (state.Mode == EditorMode.Create) state.ResetBodySizeRamp(); }
    private void Interpolation_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (IsLoaded && !refreshing && state.Mode == EditorMode.Create && InterpolationBox.SelectedIndex >= 0) state.SetRampInterpolation((RampInterpolationMode)InterpolationBox.SelectedIndex);
    }
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

    private void DisplayToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded || refreshing) return;
        if (state.Mode == EditorMode.Create) state.SetDisplay(CreateShowNodesBox.IsChecked == true, CreateShowSkinBox.IsChecked == true, CreateShowMusclesBox.IsChecked == true, state.Display.CreateShowFeatures);
        else state.SetPlayDisplay(PlayShowSkinBox.IsChecked == true, PlaySolidBodyBox.IsChecked == true, PlayShowSkeletonBox.IsChecked == true, PlayShowMusclesBox.IsChecked == true, PlayShowEyesBox.IsChecked == true);
    }

    private void SkinColor_LostFocus(object sender, RoutedEventArgs e) => ApplySkinColor();
    private void PickSkinColor_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.ColorDialog { FullOpen = true, Color = ToFormsColor(state.Creature.SkinColorArgb) };
        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
        state.SetSkinColor((uint)(dialog.Color.ToArgb()));
    }

    private void ApplySkinColor()
    {
        var text = SkinColorBox.Text.Trim().TrimStart('#');
        if (text.Length == 6) text = "FF" + text;
        if (text.Length == 8 && uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var color)) state.SetSkinColor(color);
    }

    private void FinColor_LostFocus(object sender, RoutedEventArgs e) => ApplyFinColor();
    private void PickFinColor_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.ColorDialog { FullOpen = true, Color = ToFormsColor(state.Creature.FinColorArgb) };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) state.SetFinColor((uint)dialog.Color.ToArgb());
    }

    private void ApplyFinColor()
    {
        var text = FinColorBox.Text.Trim().TrimStart('#');
        if (text.Length == 6) text = "FF" + text;
        if (text.Length == 8 && uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var color)) state.SetFinColor(color);
    }

    private static System.Drawing.Color ToFormsColor(uint argb) => System.Drawing.Color.FromArgb((int)argb);

    private void AddFeature_Click(object sender, RoutedEventArgs e)
    {
        if (state.Mode == EditorMode.Create && state.Creature.Nodes.Count > 0) state.AddFeature((CreatureFeatureType)AddFeatureTypeBox.SelectedIndex);
        else state.SetStatus("Create a head node before adding a feature.");
    }

    private void DeleteFeature_Click(object sender, RoutedEventArgs e)
    {
        if (state.Mode == EditorMode.Create) state.DeleteSelectedFeature();
    }

    private void FeatureSelection_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (!IsLoaded || refreshing || FeatureListBox.SelectedIndex < 0 || FeatureListBox.SelectedIndex >= state.Creature.Features.Count) return;
        state.SelectFeature(state.Creature.Features[FeatureListBox.SelectedIndex]);
    }

    private void FeatureProperty_Changed(object sender, RoutedEventArgs e) => ApplyFeatureProperties();
    private void FeatureProperty_LostFocus(object sender, RoutedEventArgs e) => ApplyFeatureProperties();
    private void ApplyFeatureProperties()
    {
        if (!IsLoaded || refreshing || state.SelectedFeature is null || !float.TryParse(FeatureXBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var x) || !float.TryParse(FeatureYBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var y) || !float.TryParse(FeatureRotationBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var rotation) || !float.TryParse(FeatureScaleBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var scale) || !float.TryParse(FeatureEyeSizeBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var eyeSize) || !float.TryParse(FeatureTrackingBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var tracking) || !float.TryParse(TongueLengthBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var tongueLength) || !float.TryParse(TongueForkLengthBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var tongueForkLength) || !float.TryParse(TongueForkAngleBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var tongueForkAngle) || !float.TryParse(FinLengthBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var finLength) || !float.TryParse(FinWidthBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var finWidth) || !float.TryParse(FinBaseAngleBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var finBaseAngle) || !float.TryParse(FinStiffnessBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var finStiffness) || !float.TryParse(FinDampingBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var finDamping)) return;
        var parentIndex = FeatureParentBox.SelectedIndex;
        var parentId = parentIndex >= 0 && parentIndex < state.Creature.Nodes.Count ? state.Creature.Nodes[parentIndex].Id : Guid.Empty;
        var type = (CreatureFeatureType)Math.Clamp(FeatureTypeBox.SelectedIndex, 0, (int)CreatureFeatureType.Fin);
        state.SetSelectedFeature(type, parentId, new Vector2(x, y), rotation, scale, FeatureMirrorBox.IsChecked == true, FeatureVisibleBox.IsChecked == true, eyeSize, 16, 9, tracking, tongueLength, tongueForkLength, tongueForkAngle, (FinSide)Math.Clamp(FinSideBox.SelectedIndex, 0, 1), finLength, finWidth, finBaseAngle, finStiffness, finDamping);
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
        var position = node.Position;
        if (float.TryParse(XBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var x) && float.TryParse(YBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var y)) position = new Vector2(x, y);
        var rotation = node.Rotation;
        if (float.TryParse(RotationBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedRotation)) rotation = parsedRotation;
        state.SetSelectedNodeProperties(position, rotation);
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (state.Mode == EditorMode.Create && Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Z) { state.Undo(); e.Handled = true; return; }
        if (state.Mode == EditorMode.Create && Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.C) { state.CopySelectedFeature(); e.Handled = true; return; }
        if (state.Mode == EditorMode.Create && Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.V) { state.PasteFeature(); e.Handled = true; return; }
        if (e.Key is Key.Delete or Key.Back) { state.DeleteSelected(); e.Handled = true; }
    }
}
