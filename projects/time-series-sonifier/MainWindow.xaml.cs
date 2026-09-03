using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;

namespace TimeSeriesSonifier;

public partial class MainWindow : Window
{
    readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    readonly TimelineEngine timeline = new(); readonly AudioEngine audio = new(); readonly IconSettings iconSettings = new(); readonly IconRenderer iconRenderer = new();
    RawImportedData? raw; DataSeries? series; MappedDataSeries? mapped; MappedSeriesInterpolator? interpolator; BitmapImage? iconSource; bool sliderUpdate;
    public MainWindow() { InitializeComponent(); timer.Tick += (_, _) => { timeline.Advance(1.0 / 60); UpdateView(); }; Loaded += (_, _) => UpdateView(); Graph.SizeChanged += (_, _) => UpdateView(); Closing += (_, _) => { timer.Stop(); audio.Dispose(); }; }
    void OpenData_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Data files (*.csv;*.json)|*.csv;*.json|CSV files (*.csv)|*.csv|JSON files (*.json)|*.json|All files (*.*)|*.*" }; if (dialog.ShowDialog() != true) return;
        try { raw = Path.GetExtension(dialog.FileName).Equals(".json", StringComparison.OrdinalIgnoreCase) ? JsonImporter.Read(dialog.FileName) : CsvImporter.Read(dialog.FileName); TimeColumnBox.ItemsSource = raw.Headers; ValueColumnBox.ItemsSource = raw.Headers; TimeColumnBox.SelectedIndex = raw.Headers.Count > 1 ? 0 : -1; ValueColumnBox.SelectedIndex = raw.Headers.Count > 1 ? 1 : -1; SourceText.Text = $"{raw.SourceName}\nLoaded {raw.Rows.Count} rows"; FormatText.Text = $"Format: {Path.GetExtension(dialog.FileName).TrimStart('.').ToUpperInvariant()}"; StatusText.Text = "Select a time and value column"; } catch (Exception ex) { StatusText.Text = ex.Message; raw = null; }
    }
    void Column_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => RebuildSeries();
    void MappingMode_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { if (MappingModeBox.SelectedItem is System.Windows.Controls.ComboBoxItem item && Enum.TryParse<MappingMode>(item.Tag?.ToString(), out var mode)) { mappingMode = mode; RebuildSeries(); } }
    MappingMode mappingMode;
    void RebuildSeries()
    {
        if (raw is null || TimeColumnBox.SelectedIndex < 0 || ValueColumnBox.SelectedIndex < 0) return;
        var result = DataSeriesBuilder.Build(raw, TimeColumnBox.SelectedIndex, ValueColumnBox.SelectedIndex); series = result.Series; mapped = series is null ? null : MappingEngine.Map(series, mappingMode); interpolator = mapped is null ? null : new MappedSeriesInterpolator(mapped); if (mapped is null) { StatusText.Text = result.Error ?? "The selected columns are invalid."; RowsText.Text = $"{result.ValidRows} valid points, {result.SkippedRows} rows skipped"; return; }
        timeline.SetRange(mapped.MinimumTime, mapped.MaximumTime); RowsText.Text = $"{result.ValidRows} valid points\n{result.SkippedRows} rows skipped\nShowing: {mappingMode}"; StatusText.Text = "Mapped data series ready"; UpdateView();
    }
    void Play_Click(object sender, RoutedEventArgs e) { timeline.Play(); timer.Start(); }
    void Pause_Click(object sender, RoutedEventArgs e) => timeline.Pause();
    void Reset_Click(object sender, RoutedEventArgs e) { timeline.Reset(); UpdateView(); }
    void Speed_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { if (SpeedBox.SelectedItem is System.Windows.Controls.ComboBoxItem item && double.TryParse(item.Content?.ToString()?.TrimEnd('x'), out var speed)) timeline.PlaybackSpeed = speed; }
    void TimelineSlider_Changed(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e) { if (!sliderUpdate && mapped is not null) { timeline.SeekNormalized(TimelineSlider.Value); UpdateView(); } }
    void AudioEnable_Click(object sender, RoutedEventArgs e) { if (AudioEnableCheck.IsChecked == true) StartSound_Click(sender, e); else StopSound_Click(sender, e); }
    void StartSound_Click(object sender, RoutedEventArgs e) { if (mapped is null) { AudioStatusText.Text = "STATUS: Load a valid dataset first"; return; } UpdateAudioTarget(); if (!audio.Start()) StatusText.Text = audio.Status; UpdateAudioView(); }
    void StopSound_Click(object sender, RoutedEventArgs e) { audio.Stop(); AudioEnableCheck.IsChecked = false; UpdateAudioView(); }
    void Waveform_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { if (WaveformBox.SelectedIndex >= 0) audio.Waveform = (WaveformType)WaveformBox.SelectedIndex; }
    void PitchSettings_Changed(object sender, RoutedEventArgs e) => UpdateAudioTarget();
    void Volume_Changed(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e) { audio.Volume = VolumeSlider.Value; }
    double MinPitch() => double.TryParse(MinPitchBox.Text, out var value) && double.IsFinite(value) ? value : PitchMapper.DefaultMinimumFrequency;
    double MaxPitch() => double.TryParse(MaxPitchBox.Text, out var value) && double.IsFinite(value) ? value : PitchMapper.DefaultMaximumFrequency;
    void UpdateAudioTarget() { if (mapped is not null && interpolator is not null) audio.SetTargetFrequencyFromNormalized(interpolator.Evaluate(timeline.CurrentTime).CurrentNormalizedValue, MinPitch(), MaxPitch()); UpdateAudioView(); }
    void UpdateAudioView() { TargetFrequencyText.Text = $"TARGET: {audio.TargetFrequency:0.0} Hz"; CurrentFrequencyText.Text = $"CURRENT: {audio.CurrentFrequency:0.0} Hz"; AudioStatusText.Text = $"STATUS: {audio.Status}"; }
    void LoadImage_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Images (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files (*.*)|*.*" }; if (dialog.ShowDialog() != true) return;
        try { var candidate = IconImageLoader.Load(dialog.FileName); iconSource = candidate; iconSettings.ImagePath = dialog.FileName; iconSettings.Enabled = true; IconEnabledCheck.IsChecked = true; IconFileText.Text = Path.GetFileName(dialog.FileName); IconStatusText.Text = "Image loaded"; UpdateView(); } catch (Exception ex) { IconStatusText.Text = $"Image error: {ex.Message}"; }
    }
    void IconSetting_Changed(object sender, RoutedEventArgs e) { iconSettings.Enabled = IconEnabledCheck.IsChecked == true; iconSettings.ScalingEnabled = ScaleWithDataCheck.IsChecked == true; UpdateView(); }
    void IconScale_Changed(object sender, RoutedEventArgs e) { if (double.TryParse(MinScaleBox.Text, out var min)) iconSettings.MinimumScale = min; if (double.TryParse(MaxScaleBox.Text, out var max)) iconSettings.MaximumScale = max; if (!iconSettings.Validate(out var error)) IconStatusText.Text = error; else IconStatusText.Text = iconSource is null ? "No image loaded" : "Image loaded"; UpdateView(); }
    void UpdateView()
    {
        timeline.LoopEnabled = LoopCheck.IsChecked == true; var state = interpolator?.Evaluate(timeline.CurrentTime) ?? CurrentDataState.Empty; if (audio.State == AudioEngineState.Running && mapped is not null) audio.SetTargetFrequencyFromNormalized(state.CurrentNormalizedValue, MinPitch(), MaxPitch()); Graph.MappedSeries = mapped; Graph.State = state; Graph.InvalidateVisual(); CurrentTimeText.Text = mapped is null ? "—" : state.CurrentTime.ToString("G8"); CurrentOriginalText.Text = mapped is null ? "—" : state.CurrentOriginalValue.ToString("G8"); CurrentMappedText.Text = mapped is null ? "—" : state.CurrentMappedValue.ToString("G8"); CurrentNormalizedText.Text = mapped is null ? "—" : state.CurrentNormalizedValue.ToString("0.000"); iconRenderer.Update(IconImage, iconSettings, iconSource, state, mapped, new Size(Graph.ActualWidth, Graph.ActualHeight)); if (mapped is not null) { sliderUpdate = true; TimelineSlider.Value = timeline.NormalizedPosition; sliderUpdate = false; } UpdateAudioView();
    }
}
