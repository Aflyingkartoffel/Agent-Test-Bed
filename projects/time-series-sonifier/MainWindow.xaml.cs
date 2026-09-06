using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;

namespace TimeSeriesSonifier;

public partial class MainWindow : Window
{
    readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(16) }; readonly FpsTracker fpsTracker = new(); readonly ThemeManager themeManager = new();
    readonly TimelineEngine timeline = new(); readonly AudioEngine audio = new(); readonly PlaybackCoordinator playback; readonly IconSettings iconSettings = new(); readonly IconRenderer iconRenderer = new(); readonly SpectrumAnalyzer spectrumAnalyzer = new(true); readonly float[] spectrumSamples = new float[4096];
    RawImportedData? raw; DataSeries? series; MappedDataSeries? mapped; FinancialDatasetProfile? financialProfile; string selectedPrice = ""; ImageSource? iconSource; OutputProfile outputProfile = OutputProfile.Vertical; bool sliderUpdate; bool uiReady; int readoutTick; long nextSpectrumTick; long nextWaveformTick; System.Windows.Controls.TextBlock? currentTimeLabel;
    public MainWindow() { playback = new PlaybackCoordinator(timeline, audio); InitializeComponent(); playback.SetAudioEnabled(true); AudioEnableCheck.IsChecked = true; SpectrumEnableCheck.IsChecked = true; themeManager.ApplyResources(Application.Current?.Resources); Graph.ThemeMode = AppearanceMode.Light; Graph.RevealMode = GraphRevealMode.Progressive; Spectrum.ThemeMode = AppearanceMode.Light; ConfigureFinalOutputLayout(); iconSource = IconImageLoader.CreateDefaultCube(); OutputProfileBox.ItemsSource = OutputProfile.All; OutputProfileBox.SelectedIndex = 0; uiReady = true; timer.Tick += (_, _) => { playback.Advance(1.0 / 60); UpdateView(); }; CompositionTarget.Rendering += OnRendering; Loaded += (_, _) => UpdateView(); Graph.SizeChanged += (_, _) => UpdateView(); Closing += (_, _) => { CompositionTarget.Rendering -= OnRendering; timer.Stop(); spectrumAnalyzer.Dispose(); audio.Dispose(); }; }
    void ConfigureFinalOutputLayout()
    {
        if (VisualTreeHelper.GetParent(OutputSurface) is not System.Windows.Controls.Grid surfaceHost || VisualTreeHelper.GetParent(surfaceHost) is not System.Windows.Controls.Grid root) return;
        surfaceHost.Children.Remove(OutputSurface); OutputSurface.Width = OutputProfile.Vertical.Width; OutputSurface.Height = OutputProfile.Vertical.Height;
        surfaceHost.Children.Add(new System.Windows.Controls.Viewbox { Stretch = System.Windows.Media.Stretch.Uniform, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Child = OutputSurface });
        if (root.RowDefinitions.Count > 1) root.RowDefinitions[1].Height = new GridLength(74);
        if (root.Children.OfType<System.Windows.Controls.StackPanel>().FirstOrDefault() is { } controls) { controls.Width = double.NaN; controls.Orientation = System.Windows.Controls.Orientation.Horizontal; controls.HorizontalAlignment = HorizontalAlignment.Center; controls.VerticalAlignment = VerticalAlignment.Center; }
    }
    void OnRendering(object? sender, EventArgs e) { if (e is RenderingEventArgs args && fpsTracker.TryUpdate(args.RenderingTime, out var fps)) FpsText.Text = "FPS: " + fps.ToString("0.0"); }
    void Theme_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { if (!uiReady || ThemeModeBox.SelectedItem is not System.Windows.Controls.ComboBoxItem item || !Enum.TryParse<AppearanceMode>(item.Tag?.ToString(), out var mode)) return; themeManager.SetMode(mode); Graph.ThemeMode = mode; Spectrum.ThemeMode = mode; UpdateView(); }
    void Reveal_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { if (!uiReady || RevealModeBox.SelectedItem is not System.Windows.Controls.ComboBoxItem item || !Enum.TryParse<GraphRevealMode>(item.Tag?.ToString(), out var mode)) return; TimelineSlider.Visibility = mode == GraphRevealMode.FullGraph ? Visibility.Visible : Visibility.Collapsed; Graph.RevealMode = mode; UpdateView(); }
    void OpenData_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Data files (*.csv;*.json)|*.csv;*.json|CSV files (*.csv)|*.csv|JSON files (*.json)|*.json|All files (*.*)|*.*" }; if (dialog.ShowDialog() != true) return;
        try { raw = Path.GetExtension(dialog.FileName).Equals(".json", StringComparison.OrdinalIgnoreCase) ? JsonImporter.Read(dialog.FileName) : CsvImporter.Read(dialog.FileName); financialProfile = FinancialDatasetProfile.TryClassify(raw); ConfigureDataControls(); SourceText.Text = $"{raw.SourceName}\nLoaded {raw.Rows.Count} rows"; FormatText.Text = $"Format: {Path.GetExtension(dialog.FileName).TrimStart('.').ToUpperInvariant()}"; StatusText.Text = financialProfile is null ? "Select a time and value column" : "Financial dataset detected"; RebuildSeries(); } catch (Exception ex) { StatusText.Text = ex.Message; raw = null; financialProfile = null; }
    }
    void Column_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { UpdateColumnLabels(); RebuildSeries(); }
    void FinancialPriceType_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { if (FinancialPriceTypeBox.SelectedItem is string name) { selectedPrice = name; RebuildSeries(); } }
    void AdvancedColumns_Click(object sender, RoutedEventArgs e) { var visible = financialProfile is null || AdvancedColumnsCheck.IsChecked == true; ValueColumnLabel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed; ValueColumnBox.Visibility = visible ? Visibility.Visible : Visibility.Collapsed; RebuildSeries(); }
    void MappingMode_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { if (MappingModeBox.SelectedItem is System.Windows.Controls.ComboBoxItem item && Enum.TryParse<MappingMode>(item.Tag?.ToString(), out var mode)) { mappingMode = mode; RebuildSeries(); } }
    void ConfigureDataControls()
    {
        if (raw is null) return;
        TimeColumnBox.ItemsSource = raw.Headers; ValueColumnBox.ItemsSource = raw.Headers;
        if (financialProfile is null) { FinancialControls.Visibility = Visibility.Collapsed; ValueColumnLabel.Visibility = Visibility.Visible; ValueColumnBox.Visibility = Visibility.Visible; AdvancedColumnsCheck.IsChecked = false; TimeColumnBox.SelectedIndex = raw.Headers.Count > 1 ? 0 : -1; ValueColumnBox.SelectedIndex = raw.Headers.Count > 1 ? 1 : -1; return; }
        FinancialControls.Visibility = Visibility.Visible; AdvancedColumnsCheck.IsChecked = false; ValueColumnLabel.Visibility = Visibility.Collapsed; ValueColumnBox.Visibility = Visibility.Collapsed; TimeColumnBox.SelectedIndex = financialProfile.TimeColumnIndex; FinancialPriceTypeBox.ItemsSource = financialProfile.PriceOptions.Select(FinancialDatasetProfile.DisplayName).ToArray(); selectedPrice = FinancialDatasetProfile.DisplayName(financialProfile.DefaultPrice); FinancialPriceTypeBox.SelectedItem = selectedPrice;
    }
    string SemanticValueLabel() => financialProfile is null || AdvancedColumnsCheck.IsChecked == true ? ColumnLabel.Format(SelectedColumn(ValueColumnBox), "VALUE") + " — " + mappingMode : mappingMode switch { MappingMode.ChangeFromPrevious => "PRICE CHANGE", MappingMode.PercentChange => "PERCENT CHANGE", _ => $"{selectedPrice.ToUpperInvariant()} PRICE" };
    MappingMode mappingMode;
    void RebuildSeries()
    {
        if (raw is null || TimeColumnBox.SelectedIndex < 0) return;
        var valueColumn = ValueColumnBox.SelectedIndex; var displayName = (string?)null;
        if (financialProfile is not null && AdvancedColumnsCheck.IsChecked != true) { if (!financialProfile.TryGetColumnIndex(selectedPrice, out valueColumn)) return; displayName = selectedPrice; }
        if (valueColumn < 0) return;
        var result = DataSeriesBuilder.Build(raw, TimeColumnBox.SelectedIndex, valueColumn); series = result.Series; mapped = series is null ? null : MappingEngine.Map(series, mappingMode, financialProfile is not null && AdvancedColumnsCheck.IsChecked != true ? financialProfile : null, displayName); if (mapped is null) { playback.SetSeries(null); StatusText.Text = result.Error ?? "The selected columns are invalid."; RowsText.Text = $"{result.ValidRows} valid points, {result.SkippedRows} rows skipped"; return; }
        playback.SetSeries(mapped); RowsText.Text = $"{result.ValidRows} valid points\n{result.SkippedRows} rows skipped\nShowing: {mappingMode}"; StatusText.Text = "Mapped data series ready"; UpdateView();
    }
    void Play_Click(object sender, RoutedEventArgs e) { playback.SetLoop(LoopCheck.IsChecked == true); playback.Play(); timer.Start(); UpdateView(); }
    void Pause_Click(object sender, RoutedEventArgs e) { playback.Pause(); UpdateView(); }
    void Reset_Click(object sender, RoutedEventArgs e) { playback.Reset(); UpdateView(); }
    void Speed_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { if (SpeedBox.SelectedItem is System.Windows.Controls.ComboBoxItem item && double.TryParse(item.Content?.ToString()?.TrimEnd('x'), out var speed)) playback.SetPlaybackSpeed(speed); }
    void TimelineSlider_Changed(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e) { if (!sliderUpdate && mapped is not null) { playback.SeekNormalized(TimelineSlider.Value); UpdateView(); } }
    void AudioEnable_Click(object sender, RoutedEventArgs e) { playback.SetAudioEnabled(AudioEnableCheck.IsChecked == true); if (playback.AudioEnabled && timeline.State == TimelineState.Playing) timer.Start(); UpdateView(); }
    void LaptopSpeakerMode_Click(object sender, RoutedEventArgs e) { playback.SetLaptopSpeakerMode(LaptopSpeakerCheck.IsChecked == true); UpdateView(); }
    void Waveform_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { if (WaveformBox.SelectedIndex >= 0) audio.Waveform = (WaveformType)WaveformBox.SelectedIndex; }
    void PitchSettings_Changed(object sender, RoutedEventArgs e) { playback.SetPitchRange(MinPitch(), MaxPitch()); UpdateView(); }
    void Volume_Changed(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e) { audio.Volume = VolumeSlider.Value; }
    void ImageOpacity_Changed(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e) { if (!uiReady) return; ImageOpacityText.Text = $"OPACITY: {Math.Round(IconOpacity.Clamp(ImageOpacitySlider.Value) * 100):0}%"; UpdateView(); }
    void SpectrumEnable_Click(object sender, RoutedEventArgs e) { if (SpectrumEnableCheck.IsChecked == true) spectrumAnalyzer.Enable(); else { spectrumAnalyzer.Disable(); Spectrum.Frame = null; } UpdateSpectrumView(); }
    void FftSize_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { if (!uiReady) return; if (FftSizeBox.SelectedItem is System.Windows.Controls.ComboBoxItem item && int.TryParse(item.Content?.ToString(), out var size)) spectrumAnalyzer.SetFftSize(size); UpdateSpectrumView(); }
    void OutputProfile_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { if (!uiReady || OutputProfileBox.SelectedItem is not OutputProfile profile) return; outputProfile = profile; UpdatePresentationViews(); UpdateExportStatus(); }
    void WorkflowTabs_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { if (uiReady && e.Source == WorkflowTabs) UpdatePresentationViews(); }
    double MinPitch() => double.TryParse(MinPitchBox.Text, out var value) && double.IsFinite(value) ? value : PitchMapper.DefaultMinimumFrequency;
    double MaxPitch() => double.TryParse(MaxPitchBox.Text, out var value) && double.IsFinite(value) ? value : PitchMapper.DefaultMaximumFrequency;
    void UpdateAudioTarget() { playback.SetPitchRange(MinPitch(), MaxPitch()); UpdateAudioView(); }
    void UpdateAudioView() { TargetFrequencyText.Text = $"TARGET: {audio.TargetFrequency:0.0} Hz"; CurrentFrequencyText.Text = $"CURRENT: {audio.CurrentFrequency:0.0} Hz"; AudioStatusText.Text = $"STATUS: {audio.Status}"; }
    void LoadImage_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Images (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files (*.*)|*.*" }; if (dialog.ShowDialog() != true) return;
        try { var candidate = IconImageLoader.Load(dialog.FileName); iconSource = candidate; iconSettings.ImagePath = dialog.FileName; iconSettings.Enabled = true; IconEnabledCheck.IsChecked = true; IconFileText.Text = Path.GetFileName(dialog.FileName); IconStatusText.Text = "Image loaded"; UpdateView(); } catch (Exception ex) { IconStatusText.Text = $"Image error: {ex.Message}"; }
    }
    void UseDefaultImage_Click(object sender, RoutedEventArgs e) { iconSource = IconImageLoader.CreateDefaultCube(); iconSettings.ImagePath = null; IconFileText.Text = "Default gray cube"; IconStatusText.Text = "Default image ready"; UpdateView(); }
    void IconSetting_Changed(object sender, RoutedEventArgs e) { iconSettings.Enabled = IconEnabledCheck.IsChecked == true; iconSettings.ScalingEnabled = ScaleWithDataCheck.IsChecked == true; UpdateView(); }
    void IconScale_Changed(object sender, RoutedEventArgs e) { if (double.TryParse(MinScaleBox.Text, out var min)) iconSettings.MinimumScale = min; if (double.TryParse(MaxScaleBox.Text, out var max)) iconSettings.MaximumScale = max; if (!iconSettings.Validate(out var error)) IconStatusText.Text = error; else IconStatusText.Text = iconSource is null ? "No image loaded" : "Image loaded"; UpdateView(); }
    string SelectedColumn(System.Windows.Controls.ComboBox box) => box.SelectedItem?.ToString() ?? "";
    void UpdateColumnLabels()
    {
        currentTimeLabel ??= FindTextBlock("CURRENT TIME");
        if (currentTimeLabel is not null) currentTimeLabel.Text = ColumnLabel.Format(SelectedColumn(TimeColumnBox), "CURRENT TIME");
        CurrentValueLabel.Text = financialProfile is not null && AdvancedColumnsCheck.IsChecked != true ? SemanticValueLabel() : ColumnLabel.Format(SelectedColumn(ValueColumnBox), "SOURCE VALUE");
    }
    System.Windows.Controls.TextBlock? FindTextBlock(string text)
    {
        System.Windows.Controls.TextBlock? found = null;
        void Visit(DependencyObject node) { for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) { var child = VisualTreeHelper.GetChild(node, i); if (child is System.Windows.Controls.TextBlock block && block.Text == text) { found = block; return; } Visit(child); if (found is not null) return; } }
        Visit(this); return found;
    }
    PresentationScene CreateScene(CurrentDataState state, SpectrumFrame? spectrum) => new(mapped, state, iconSettings.Enabled ? iconSource : null, IconOpacity.Clamp(ImageOpacitySlider.Value), iconSettings.MinimumScale, iconSettings.MaximumScale, spectrum, SelectedColumn(TimeColumnBox), SemanticValueLabel());
    void UpdateView()
    {
        playback.SetLoop(LoopCheck.IsChecked == true); UpdateColumnLabels(); var state = playback.CurrentDataState; Graph.MappedSeries = mapped; Graph.TimeLabel = SelectedColumn(TimeColumnBox); Graph.ValueLabel = SemanticValueLabel(); Graph.State = state; Graph.Refresh(); if (timeline.State != TimelineState.Playing || ++readoutTick >= 3) { readoutTick = 0; CurrentTimeText.Text = mapped is null ? "—" : state.CurrentTime.ToString("G8"); var displayValue = mapped is not null && mapped.FinancialProfile is not null && mapped.Mode != MappingMode.AbsoluteValue ? state.CurrentMappedValue : state.CurrentOriginalValue; CurrentOriginalText.Text = mapped is null ? "—" : AxisLabelFormatter.Value(displayValue, mapped?.Mode ?? MappingMode.AbsoluteValue, mapped?.FinancialProfile); CurrentMappedText.Text = mapped is null ? "—" : AxisLabelFormatter.Value(state.CurrentMappedValue, mapped.Mode, mapped.FinancialProfile); CurrentNormalizedText.Text = mapped is null ? "—" : state.CurrentNormalizedValue.ToString("0.000"); } iconRenderer.Update(IconImage, iconSettings, iconSource, state, mapped, new Size(Graph.ActualWidth, Graph.ActualHeight), ImageOpacitySlider.Value); if (mapped is not null) { sliderUpdate = true; TimelineSlider.Value = timeline.NormalizedPosition; sliderUpdate = false; } UpdateAudioView(); UpdateSpectrumView(); if (WorkflowTabs.SelectedIndex != 0) UpdatePresentationViews();
    }
    PresentationScene LivePresentationScene()
    {
        var scene = CreateScene(playback.CurrentDataState, Spectrum.Frame); var now = System.Diagnostics.Stopwatch.GetTimestamp();
        if (now >= nextWaveformTick) { nextWaveformTick = now + System.Diagnostics.Stopwatch.Frequency / 30; return scene with { Waveform = audio.CreateWaveformSnapshot() }; }
        return OutputSurface.Scene?.Waveform is { } previous ? scene with { Waveform = previous } : scene;
    }
    PresentationScene ExportPresentationScene(CurrentDataState state) { var range = playback.EffectivePitchRange; OfflineAudioRenderer.RangeOverride = range; return CreateScene(state, null) with { Waveform = WaveformSnapshot.Create(audio.Waveform, PitchMapper.Map(state.CurrentNormalizedValue, range.Minimum, range.Maximum), audio.Volume, state.NormalizedPosition) }; }
    void UpdatePresentationViews() { if (WorkflowTabs.SelectedIndex != 1) return; var scene = LivePresentationScene(); OutputSurface.Scene = scene; OutputSurface.Profile = outputProfile; OutputSurface.InvalidateVisual(); }
    void UpdateExportStatus() { var fps = FrameRateBox.SelectedIndex == 1 ? 60 : 30; ExportStatusText.Text = $"{outputProfile.Width} × {outputProfile.Height} · {fps} FPS · {TimelineEngine.DefaultPresentationDuration:0.0} sec · {Math.Ceiling(TimelineEngine.DefaultPresentationDuration * fps):0} frames"; }
    async void Export_Click(object sender, RoutedEventArgs e)
    {
        if (mapped is null) { ExportStatusText.Text = "Load a valid dataset before exporting"; return; }
        var ffmpeg = VideoEncoderService.FindFfmpeg(); if (ffmpeg is null) { ExportStatusText.Text = "FFmpeg not found. Video export unavailable."; return; }
        var dialog = new SaveFileDialog { Filter = "MP4 video (*.mp4)|*.mp4", FileName = $"{Path.GetFileNameWithoutExtension(raw?.SourceName ?? "visualization")}_visualization.mp4" }; if (dialog.ShowDialog() != true) return;
        var fps = FrameRateBox.SelectedIndex == 1 ? 60 : 30; var frames = (int)Math.Ceiling(TimelineEngine.DefaultPresentationDuration * fps); var temp = Path.Combine(Path.GetTempPath(), "time-series-sonifier-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(temp); var wav = Path.Combine(temp, "audio.wav"); var pattern = Path.Combine(temp, "frame-%05d.png");
        try { var visual = new DrawingVisual(); var exportCache = new GraphRenderCache(); for (var i = 0; i < frames; i++) { var scene = ExportPresentationScene(playback.EvaluateAtNormalized(i / (double)Math.Max(1, frames - 1))); using (var context = visual.RenderOpen()) PresentationRenderer.Draw(context, scene, new Rect(0, 0, outputProfile.Width, outputProfile.Height), outputProfile, exportCache); var bitmap = new RenderTargetBitmap(outputProfile.Width, outputProfile.Height, 96, 96, PixelFormats.Pbgra32); bitmap.Render(visual); using var file = File.Create(Path.Combine(temp, $"frame-{i:00000}.png")); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); encoder.Save(file); if (i % Math.Max(1, fps / 2) == 0) { ExportStatusText.Text = $"Rendering Frame {i} / {frames}..."; await Task.Yield(); } } OfflineAudioRenderer.RenderWav(wav, mapped, audio.Waveform, audio.Volume, TimelineEngine.DefaultPresentationDuration, playback.AudioEnabled); ExportStatusText.Text = "Encoding MP4..."; var args = VideoEncoderService.BuildArguments(pattern, wav, dialog.FileName, outputProfile.Width, outputProfile.Height, fps, playback.AudioEnabled); var success = await VideoEncoderService.EncodeAsync(ffmpeg, args, CancellationToken.None); ExportStatusText.Text = success ? $"Export complete: {Path.GetFileName(dialog.FileName)}" : "FFmpeg encoding failed"; } catch (Exception ex) { ExportStatusText.Text = $"Export failed: {ex.Message}"; } finally { try { Directory.Delete(temp, true); } catch { } }
    }
    void UpdateSpectrumView()
    {
        if (!uiReady) return;
        if (!spectrumAnalyzer.Enabled || audio.State != AudioEngineState.Running) { Spectrum.Frame = null; SpectrumStatusText.Text = spectrumAnalyzer.Enabled ? "Waiting for running audio" : "Spectrum disabled"; Spectrum.InvalidateVisual(); return; }
        var now = System.Diagnostics.Stopwatch.GetTimestamp(); if (now < nextSpectrumTick) return; nextSpectrumTick = now + System.Diagnostics.Stopwatch.Frequency / 30;
        if (audio.SampleBuffer.TryCopyLatest(spectrumSamples.AsSpan(0, spectrumAnalyzer.FftSize))) { Spectrum.Frame = spectrumAnalyzer.Analyze(spectrumSamples.AsSpan(0, spectrumAnalyzer.FftSize)); SpectrumStatusText.Text = $"{spectrumAnalyzer.FftSize} point FFT · {audio.SampleBuffer.Count} samples · Nyquist {Spectrum.Frame?.Nyquist:0} Hz"; }
        Spectrum.InvalidateVisual();
    }
}
