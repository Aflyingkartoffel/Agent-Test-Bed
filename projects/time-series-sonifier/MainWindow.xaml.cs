using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;

namespace TimeSeriesSonifier;

public partial class MainWindow : Window
{
    readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    readonly TimelineEngine timeline = new(); RawImportedData? raw; DataSeries? series; MappedDataSeries? mapped; MappedSeriesInterpolator? interpolator; bool sliderUpdate; MappingMode mappingMode;
    public MainWindow() { InitializeComponent(); timer.Tick += (_, _) => { timeline.Advance(1.0 / 60); UpdateView(); }; Loaded += (_, _) => UpdateView(); Closing += (_, _) => timer.Stop(); }
    void OpenData_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Data files (*.csv;*.json)|*.csv;*.json|CSV files (*.csv)|*.csv|JSON files (*.json)|*.json|All files (*.*)|*.*" }; if (dialog.ShowDialog() != true) return;
        try { raw = Path.GetExtension(dialog.FileName).Equals(".json", StringComparison.OrdinalIgnoreCase) ? JsonImporter.Read(dialog.FileName) : CsvImporter.Read(dialog.FileName); TimeColumnBox.ItemsSource = raw.Headers; ValueColumnBox.ItemsSource = raw.Headers; TimeColumnBox.SelectedIndex = raw.Headers.Count > 1 ? 0 : -1; ValueColumnBox.SelectedIndex = raw.Headers.Count > 1 ? 1 : -1; SourceText.Text = $"{raw.SourceName}\nLoaded {raw.Rows.Count} rows"; FormatText.Text = $"Format: {Path.GetExtension(dialog.FileName).TrimStart('.').ToUpperInvariant()}"; StatusText.Text = "Select a time and value column"; } catch (Exception ex) { StatusText.Text = ex.Message; raw = null; }
    }
    void Column_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { RebuildSeries(); }
    void MappingMode_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { if (MappingModeBox.SelectedItem is System.Windows.Controls.ComboBoxItem item && Enum.TryParse<MappingMode>(item.Tag?.ToString(), out var mode)) { mappingMode = mode; RebuildSeries(); } }
    void RebuildSeries()
    {
        if (raw is null || TimeColumnBox.SelectedIndex < 0 || ValueColumnBox.SelectedIndex < 0) return;
        var result = DataSeriesBuilder.Build(raw, TimeColumnBox.SelectedIndex, ValueColumnBox.SelectedIndex); series = result.Series; mapped = series is null ? null : MappingEngine.Map(series, mappingMode); interpolator = mapped is null ? null : new MappedSeriesInterpolator(mapped); if (mapped is null) { StatusText.Text = result.Error ?? "The selected columns are invalid."; RowsText.Text = $"{result.ValidRows} valid points, {result.SkippedRows} rows skipped"; return; }
        timeline.SetRange(mapped.MinimumTime, mapped.MaximumTime); RowsText.Text = $"{result.ValidRows} valid points\n{result.SkippedRows} rows skipped\nShowing: {mappingMode}"; StatusText.Text = "Mapped data series ready"; UpdateView();
    }
    void Play_Click(object sender, RoutedEventArgs e) { timeline.Play(); timer.Start(); }
    void Pause_Click(object sender, RoutedEventArgs e) { timeline.Pause(); }
    void Reset_Click(object sender, RoutedEventArgs e) { timeline.Reset(); UpdateView(); }
    void Speed_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { if (SpeedBox.SelectedItem is System.Windows.Controls.ComboBoxItem item && double.TryParse(item.Content?.ToString()?.TrimEnd('x'), out var speed)) timeline.PlaybackSpeed = speed; }
    void TimelineSlider_Changed(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e) { if (!sliderUpdate && mapped is not null) { timeline.SeekNormalized(TimelineSlider.Value); UpdateView(); } }
    void UpdateView()
    {
        timeline.LoopEnabled = LoopCheck.IsChecked == true; var state = interpolator?.Evaluate(timeline.CurrentTime) ?? CurrentDataState.Empty; Graph.MappedSeries = mapped; Graph.State = state; Graph.InvalidateVisual(); CurrentTimeText.Text = mapped is null ? "—" : state.CurrentTime.ToString("G8"); CurrentOriginalText.Text = mapped is null ? "—" : state.CurrentOriginalValue.ToString("G8"); CurrentMappedText.Text = mapped is null ? "—" : state.CurrentMappedValue.ToString("G8"); CurrentNormalizedText.Text = mapped is null ? "—" : state.CurrentNormalizedValue.ToString("0.000"); if (mapped is not null) { sliderUpdate = true; TimelineSlider.Value = timeline.NormalizedPosition; sliderUpdate = false; }
    }
}
