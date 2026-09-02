using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;

namespace TimeSeriesSonifier;

public partial class MainWindow : Window
{
    readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    readonly TimelineEngine timeline = new();
    RawImportedData? raw; DataSeries? series; SeriesInterpolator? interpolator; bool sliderUpdate;

    public MainWindow() { InitializeComponent(); timer.Tick += (_, _) => { timeline.Advance(1.0 / 60); UpdateView(); }; Loaded += (_, _) => UpdateView(); Closing += (_, _) => timer.Stop(); }
    void OpenData_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*" }; if (dialog.ShowDialog() != true) return;
        try { raw = CsvImporter.Read(dialog.FileName); TimeColumnBox.ItemsSource = raw.Headers; ValueColumnBox.ItemsSource = raw.Headers; TimeColumnBox.SelectedIndex = raw.Headers.Count > 1 ? 0 : -1; ValueColumnBox.SelectedIndex = raw.Headers.Count > 1 ? 1 : -1; SourceText.Text = $"{raw.SourceName}\nLoaded {raw.Rows.Count} rows"; StatusText.Text = "Select a time and value column"; } catch (Exception ex) { StatusText.Text = ex.Message; raw = null; }
    }
    void Column_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (raw is null || TimeColumnBox.SelectedIndex < 0 || ValueColumnBox.SelectedIndex < 0) return;
        var result = DataSeriesBuilder.Build(raw, TimeColumnBox.SelectedIndex, ValueColumnBox.SelectedIndex); series = result.Series; interpolator = series is null ? null : new SeriesInterpolator(series); if (series is null) { StatusText.Text = result.Error ?? "The selected columns are invalid."; RowsText.Text = $"{result.ValidRows} valid points, {result.SkippedRows} rows skipped"; return; }
        timeline.SetRange(series.MinimumTime, series.MaximumTime); RowsText.Text = $"{result.ValidRows} valid points\n{result.SkippedRows} rows skipped"; StatusText.Text = "Data series ready"; UpdateView();
    }
    void Play_Click(object sender, RoutedEventArgs e) { timeline.Play(); timer.Start(); }
    void Pause_Click(object sender, RoutedEventArgs e) { timeline.Pause(); }
    void Reset_Click(object sender, RoutedEventArgs e) { timeline.Reset(); UpdateView(); }
    void Speed_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { if (SpeedBox.SelectedItem is System.Windows.Controls.ComboBoxItem item && double.TryParse(item.Content?.ToString()?.TrimEnd('x'), out var speed)) timeline.PlaybackSpeed = speed; }
    void TimelineSlider_Changed(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e) { if (!sliderUpdate && series is not null) { timeline.SeekNormalized(TimelineSlider.Value); UpdateView(); } }
    void UpdateView()
    {
        var state = interpolator?.Evaluate(timeline.CurrentTime) ?? CurrentDataState.Empty; Graph.Series = series; Graph.State = state; Graph.InvalidateVisual(); CurrentTimeText.Text = series is null ? "—" : state.CurrentTime.ToString("G8"); CurrentValueText.Text = series is null ? "—" : state.CurrentValue.ToString("G8"); if (series is not null) { sliderUpdate = true; TimelineSlider.Value = timeline.NormalizedPosition; sliderUpdate = false; }
        if (LoopCheck.IsChecked == true) timeline.LoopEnabled = true;
    }
}
