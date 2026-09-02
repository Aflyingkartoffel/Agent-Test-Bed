using System.Globalization;

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
    public double MinimumTime => Points[0].Time;
    public double MaximumTime => Points[^1].Time;
    public double MinimumValue => Points.Min(p => p.MappedValue);
    public double MaximumValue => Points.Max(p => p.MappedValue);
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
