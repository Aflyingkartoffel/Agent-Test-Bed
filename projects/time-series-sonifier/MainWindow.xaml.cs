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
    readonly TimelineEngine timeline = new(); readonly AudioEngine audio = new(); readonly PlaybackCoordinator playback; readonly IconSettings iconSettings = new(); readonly IconRenderer iconRenderer = new(); readonly SpectrumAnalyzer spectrumAnalyzer = new(); readonly float[] spectrumSamples = new float[4096];
    RawImportedData? raw; DataSeries? series; MappedDataSeries? mapped; ImageSource? iconSource; OutputProfile outputProfile = OutputProfile.Vertical; bool sliderUpdate; bool uiReady; int readoutTick; long nextSpectrumTick; System.Windows.Controls.TextBlock? currentTimeLabel; System.Windows.Controls.TextBlock? currentValueLabel;
    public MainWindow() { playback = new PlaybackCoordinator(timeline, audio); InitializeComponent(); iconSource = IconImageLoader.CreateDefaultCube(); OutputProfileBox.ItemsSource = OutputProfile.All; OutputProfileBox.SelectedIndex = 0; uiReady = true; timer.Tick += (_, _) => { playback.Advance(1.0 / 60); UpdateView(); }; CompositionTarget.Rendering += OnRendering; Loaded += (_, _) => UpdateView(); Graph.SizeChanged += (_, _) => UpdateView(); Closing += (_, _) => { CompositionTarget.Rendering -= OnRendering; timer.Stop(); spectrumAnalyzer.Dispose(); audio.Dispose(); }; }
    void OnRendering(object? sender, EventArgs e) { if (e is RenderingEventArgs args && fpsTracker.TryUpdate(args.RenderingTime, out var fps)) FpsText.Text = "FPS: " + fps.ToString("0.0"); }
    void Theme_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { if (!uiReady || ThemeModeBox.SelectedItem is not System.Windows.Controls.ComboBoxItem item || !Enum.TryParse<AppearanceMode>(item.Tag?.ToString(), out var mode)) return; themeManager.SetMode(mode); Graph.ThemeMode = mode; Spectrum.ThemeMode = mode; UpdateView(); }
    void Reveal_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { if (!uiReady || RevealModeBox.SelectedItem is not System.Windows.Controls.ComboBoxItem item || !Enum.TryParse<GraphRevealMode>(item.Tag?.ToString(), out var mode)) return; TimelineSlider.Visibility = mode == GraphRevealMode.FullGraph ? Visibility.Visible : Visibility.Collapsed; Graph.RevealMode = mode; UpdateView(); }
    void OpenData_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Data files (*.csv;*.json)|*.csv;*.json|CSV files (*.csv)|*.csv|JSON files (*.json)|*.json|All files (*.*)|*.*" }; if (dialog.ShowDialog() != true) return;
        try { raw = Path.GetExtension(dialog.FileName).Equals(".json", StringComparison.OrdinalIgnoreCase) ? JsonImporter.Read(dialog.FileName) : CsvImporter.Read(dialog.FileName); TimeColumnBox.ItemsSource = raw.Headers; ValueColumnBox.ItemsSource = raw.Headers; TimeColumnBox.SelectedIndex = raw.Headers.Count > 1 ? 0 : -1; ValueColumnBox.SelectedIndex = raw.Headers.Count > 1 ? 1 : -1; SourceText.Text = $"{raw.SourceName}\nLoaded {raw.Rows.Count} rows"; FormatText.Text = $"Format: {Path.GetExtension(dialog.FileName).TrimStart('.').ToUpperInvariant()}"; StatusText.Text = "Select a time and value column"; } catch (Exception ex) { StatusText.Text = ex.Message; raw = null; }
    }
    void Column_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { UpdateColumnLabels(); RebuildSeries(); }
    void MappingMode_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { if (MappingModeBox.SelectedItem is System.Windows.Controls.ComboBoxItem item && Enum.TryParse<MappingMode>(item.Tag?.ToString(), out var mode)) { mappingMode = mode; RebuildSeries(); } }
    MappingMode mappingMode;
    void RebuildSeries()
    {
        if (raw is null || TimeColumnBox.SelectedIndex < 0 || ValueColumnBox.SelectedIndex < 0) return;
        var result = DataSeriesBuilder.Build(raw, TimeColumnBox.SelectedIndex, ValueColumnBox.SelectedIndex); series = result.Series; mapped = series is null ? null : MappingEngine.Map(series, mappingMode); if (mapped is null) { playback.SetSeries(null); StatusText.Text = result.Error ?? "The selected columns are invalid."; RowsText.Text = $"{result.ValidRows} valid points, {result.SkippedRows} rows skipped"; return; }
        playback.SetSeries(mapped); RowsText.Text = $"{result.ValidRows} valid points\n{result.SkippedRows} rows skipped\nShowing: {mappingMode}"; StatusText.Text = "Mapped data series ready"; UpdateView();
    }
    void Play_Click(object sender, RoutedEventArgs e) { playback.SetLoop(LoopCheck.IsChecked == true); playback.Play(); timer.Start(); UpdateView(); }
    void Pause_Click(object sender, RoutedEventArgs e) { playback.Pause(); UpdateView(); }
    void Reset_Click(object sender, RoutedEventArgs e) { playback.Reset(); UpdateView(); }
    void Speed_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { if (SpeedBox.SelectedItem is System.Windows.Controls.ComboBoxItem item && double.TryParse(item.Content?.ToString()?.TrimEnd('x'), out var speed)) playback.SetPlaybackSpeed(speed); }
    void TimelineSlider_Changed(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e) { if (!sliderUpdate && mapped is not null) { playback.SeekNormalized(TimelineSlider.Value); UpdateView(); } }
    void AudioEnable_Click(object sender, RoutedEventArgs e) { playback.SetAudioEnabled(AudioEnableCheck.IsChecked == true); if (playback.AudioEnabled && timeline.State == TimelineState.Playing) timer.Start(); UpdateView(); }
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
        currentTimeLabel ??= FindTextBlock("CURRENT TIME"); currentValueLabel ??= FindTextBlock("SOURCE VALUE");
        if (currentTimeLabel is not null) currentTimeLabel.Text = ColumnLabel.Format(SelectedColumn(TimeColumnBox), "CURRENT TIME");
        if (currentValueLabel is not null) currentValueLabel.Text = ColumnLabel.Format(SelectedColumn(ValueColumnBox), "SOURCE VALUE");
    }
    System.Windows.Controls.TextBlock? FindTextBlock(string text)
    {
        System.Windows.Controls.TextBlock? found = null;
        void Visit(DependencyObject node) { for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) { var child = VisualTreeHelper.GetChild(node, i); if (child is System.Windows.Controls.TextBlock block && block.Text == text) { found = block; return; } Visit(child); if (found is not null) return; } }
        Visit(this); return found;
    }
    PresentationScene CreateScene(CurrentDataState state, SpectrumFrame? spectrum) => new(mapped, state, iconSettings.Enabled ? iconSource : null, IconOpacity.Clamp(ImageOpacitySlider.Value), iconSettings.MinimumScale, iconSettings.MaximumScale, spectrum, SelectedColumn(TimeColumnBox), SelectedColumn(ValueColumnBox) + " — " + mappingMode);
    void UpdateView()
    {
        playback.SetLoop(LoopCheck.IsChecked == true); UpdateColumnLabels(); var state = playback.CurrentDataState; Graph.MappedSeries = mapped; Graph.TimeLabel = SelectedColumn(TimeColumnBox); Graph.ValueLabel = SelectedColumn(ValueColumnBox) + " — " + mappingMode; Graph.State = state; Graph.Refresh(); if (timeline.State != TimelineState.Playing || ++readoutTick >= 3) { readoutTick = 0; CurrentTimeText.Text = mapped is null ? "—" : state.CurrentTime.ToString("G8"); CurrentOriginalText.Text = mapped is null ? "—" : state.CurrentOriginalValue.ToString("G8"); CurrentMappedText.Text = mapped is null ? "—" : state.CurrentMappedValue.ToString("G8"); CurrentNormalizedText.Text = mapped is null ? "—" : state.CurrentNormalizedValue.ToString("0.000"); } IconImage.Opacity = IconOpacity.Clamp(ImageOpacitySlider.Value); iconRenderer.Update(IconImage, iconSettings, iconSource, state, mapped, new Size(Graph.ActualWidth, Graph.ActualHeight)); if (mapped is not null) { sliderUpdate = true; TimelineSlider.Value = timeline.NormalizedPosition; sliderUpdate = false; } UpdateAudioView(); UpdateSpectrumView(); if (WorkflowTabs.SelectedIndex != 0) UpdatePresentationViews();
    }
    void UpdatePresentationViews() { var scene = CreateScene(playback.CurrentDataState, Spectrum.Frame); if (WorkflowTabs.SelectedIndex == 1) { PreviewSurface.Scene = scene; PreviewSurface.Profile = OutputProfile.Horizontal; PreviewSurface.InvalidateVisual(); } else if (WorkflowTabs.SelectedIndex == 2) { OutputSurface.Scene = scene; OutputSurface.Profile = outputProfile; OutputSurface.InvalidateVisual(); } }
    void UpdateExportStatus() { var fps = FrameRateBox.SelectedIndex == 1 ? 60 : 30; ExportStatusText.Text = $"{outputProfile.Width} × {outputProfile.Height} · {fps} FPS · {TimelineEngine.DefaultPresentationDuration:0.0} sec · {Math.Ceiling(TimelineEngine.DefaultPresentationDuration * fps):0} frames"; }
    async void Export_Click(object sender, RoutedEventArgs e)
    {
        if (mapped is null) { ExportStatusText.Text = "Load a valid dataset before exporting"; return; }
        var ffmpeg = VideoEncoderService.FindFfmpeg(); if (ffmpeg is null) { ExportStatusText.Text = "FFmpeg not found. Video export unavailable."; return; }
        var dialog = new SaveFileDialog { Filter = "MP4 video (*.mp4)|*.mp4", FileName = $"{Path.GetFileNameWithoutExtension(raw?.SourceName ?? "visualization")}_visualization.mp4" }; if (dialog.ShowDialog() != true) return;
        var fps = FrameRateBox.SelectedIndex == 1 ? 60 : 30; var frames = (int)Math.Ceiling(TimelineEngine.DefaultPresentationDuration * fps); var temp = Path.Combine(Path.GetTempPath(), "time-series-sonifier-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(temp); var wav = Path.Combine(temp, "audio.wav"); var pattern = Path.Combine(temp, "frame-%05d.png");
        try { var visual = new DrawingVisual(); var exportCache = new GraphRenderCache(); for (var i = 0; i < frames; i++) { var scene = CreateScene(playback.EvaluateAtNormalized(i / (double)Math.Max(1, frames - 1)), null); using (var context = visual.RenderOpen()) PresentationRenderer.Draw(context, scene, new Rect(0, 0, outputProfile.Width, outputProfile.Height), outputProfile, exportCache); var bitmap = new RenderTargetBitmap(outputProfile.Width, outputProfile.Height, 96, 96, PixelFormats.Pbgra32); bitmap.Render(visual); using var file = File.Create(Path.Combine(temp, $"frame-{i:00000}.png")); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); encoder.Save(file); if (i % Math.Max(1, fps / 2) == 0) { ExportStatusText.Text = $"Rendering Frame {i} / {frames}..."; await Task.Yield(); } } OfflineAudioRenderer.RenderWav(wav, mapped, audio.Waveform, audio.Volume, TimelineEngine.DefaultPresentationDuration, playback.AudioEnabled); ExportStatusText.Text = "Encoding MP4..."; var args = VideoEncoderService.BuildArguments(pattern, wav, dialog.FileName, outputProfile.Width, outputProfile.Height, fps, playback.AudioEnabled); var success = await VideoEncoderService.EncodeAsync(ffmpeg, args, CancellationToken.None); ExportStatusText.Text = success ? $"Export complete: {Path.GetFileName(dialog.FileName)}" : "FFmpeg encoding failed"; } catch (Exception ex) { ExportStatusText.Text = $"Export failed: {ex.Message}"; } finally { try { Directory.Delete(temp, true); } catch { } }
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
