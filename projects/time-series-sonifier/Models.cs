using System.Globalization;
using System.IO;

namespace TimeSeriesSonifier;

public sealed record RawDataRow(int OriginalRowIndex, IReadOnlyList<string> Values);

public sealed class RawImportedData
{
    public required IReadOnlyList<string> Headers { get; init; }
    public required IReadOnlyList<RawDataRow> Rows { get; init; }
    public string SourceName { get; init; } = "";
}

public readonly record struct DataPoint(double Time, double Value, int OriginalRowIndex, string OriginalTimeText);

public readonly record struct MappedDataPoint(double Time, double OriginalValue, double MappedValue, int OriginalRowIndex, string OriginalTimeText);

public sealed class DataSeries
{
    public required string Name { get; init; }
    public required IReadOnlyList<DataPoint> Points { get; init; }
    public double MinimumTime => Points[0].Time;
    public double MaximumTime => Points[^1].Time;
    public double MinimumValue => Points.Min(p => p.Value);
    public double MaximumValue => Points.Max(p => p.Value);
}

public sealed class MappedDataSeries
{
    public required string Name { get; init; }
    public required MappingMode Mode { get; init; }
    public required IReadOnlyList<MappedDataPoint> Points { get; init; }
    public FinancialDatasetProfile? FinancialProfile { get; init; }
    public string? ValueColumnName { get; init; }
    public double MinimumTime => Points[0].Time;
    public double MaximumTime => Points[^1].Time;
    public double MinimumValue => Points.Min(p => p.MappedValue);
    public double MaximumValue => Points.Max(p => p.MappedValue);
}

public enum DatasetKind { Generic, FinancialTimeSeries }

public sealed class FinancialDatasetProfile
{
    static readonly string[] PriceKeys = ["Open", "High", "Low", "Close", "Adj Close"];
    public required int TimeColumnIndex { get; init; }
    public required IReadOnlyDictionary<string, int> PriceColumnIndexes { get; init; }
    public int? VolumeColumnIndex { get; init; }
    public DatasetKind Kind => DatasetKind.FinancialTimeSeries;
    public IReadOnlyList<string> PriceOptions => PriceKeys.Where(PriceColumnIndexes.ContainsKey).ToArray();
    public string DefaultPrice => PriceColumnIndexes.ContainsKey("Close") ? "Close" : PriceColumnIndexes.ContainsKey("Adj Close") ? "Adj Close" : PriceColumnIndexes.ContainsKey("Open") ? "Open" : PriceOptions.FirstOrDefault() ?? "";
    public static FinancialDatasetProfile? TryClassify(RawImportedData raw)
    {
        var matches = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < raw.Headers.Count; i++)
        {
            var key = Normalize(raw.Headers[i]);
            var friendly = key switch { "open" => "Open", "high" => "High", "low" => "Low", "close" => "Close", "adjclose" or "adjustedclose" => "Adj Close", _ => null };
            if (friendly is not null && !matches.ContainsKey(friendly)) matches[friendly] = i;
        }
        var time = raw.Headers.Select((header, index) => (header, index)).FirstOrDefault(item => Normalize(item.header) is "date" or "datetime" or "timestamp");
        var volume = raw.Headers.Select((header, index) => (header, index)).FirstOrDefault(item => Normalize(item.header) == "volume");
        if (time.header is null || matches.Count == 0 || (matches.Count < 2 && volume.header is null)) return null;
        return new FinancialDatasetProfile { TimeColumnIndex = time.index, PriceColumnIndexes = matches, VolumeColumnIndex = volume.header is null ? null : volume.index };
    }
    public static string DisplayName(string column) => column == "Adj Close" ? "Adjusted Close" : column;
    public bool TryGetColumnIndex(string displayName, out int index) => PriceColumnIndexes.TryGetValue(displayName == "Adjusted Close" ? "Adj Close" : displayName, out index);
    static string Normalize(string value) => new string(value.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
}

public sealed record SeriesBuildResult(DataSeries? Series, int ValidRows, int SkippedRows, string? Error)
{
    public bool Success => Series is not null;
}

public sealed record CurrentDataState(
    double NormalizedPosition,
    double CurrentTime,
    double CurrentValue,
    int LeftPointIndex,
    int RightPointIndex,
    double InterpolationFactor)
{
    public double CurrentOriginalValue { get; init; } = CurrentValue;
    public double CurrentMappedValue { get; init; } = CurrentValue;
    public double CurrentNormalizedValue { get; init; } = 0.5;
    public double TimelinePosition01 => NormalizedPosition;
    public static CurrentDataState Empty => new(0, 0, 0, -1, -1, 0);
}

public static class ColumnLabel
{
    public static string Format(string? name, string fallback) => string.IsNullOrWhiteSpace(name) ? fallback : name.Trim().ToUpperInvariant();
}

public static class DatasetNameFormatter
{
    public static string Format(string? sourceName)
    {
        if (string.IsNullOrWhiteSpace(sourceName)) return "DATA VISUALIZATION";
        var name = Path.GetFileNameWithoutExtension(sourceName.Trim());
        if (string.IsNullOrWhiteSpace(name)) return "DATA VISUALIZATION";
        name = name.Replace('_', ' ').Replace('-', ' ');
        return string.Join(" ", name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();
    }
}

public enum TimelineState { Stopped, Playing, Paused }

public static class TimeValueParser
{
    public static bool TryParse(string text, out double time)
    {
        text = text.Trim();
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out time) && double.IsFinite(time)) return true;
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out time) && double.IsFinite(time)) return true;
        if (DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out var date)) { time = date.ToUniversalTime().Ticks / (double)TimeSpan.TicksPerSecond; return true; }
        time = 0;
        return false;
    }
}
