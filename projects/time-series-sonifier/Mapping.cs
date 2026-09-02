namespace TimeSeriesSonifier;

public enum MappingMode { AbsoluteValue, ChangeFromPrevious, PercentChange }

public static class MappingEngine
{
    public static MappedDataSeries Map(DataSeries source, MappingMode mode)
    {
        var points = new List<MappedDataPoint>(source.Points.Count); var previous = 0d;
        for (var i = 0; i < source.Points.Count; i++)
        {
            var original = source.Points[i].Value; var mapped = mode == MappingMode.AbsoluteValue ? original : i == 0 ? 0 : mode == MappingMode.ChangeFromPrevious ? original - previous : previous == 0 ? 0 : (original - previous) / previous * 100;
            points.Add(new MappedDataPoint(source.Points[i].Time, original, double.IsFinite(mapped) ? mapped : 0, source.Points[i].OriginalRowIndex, source.Points[i].OriginalTimeText)); previous = original;
        }
        return new MappedDataSeries { Name = source.Name, Mode = mode, Points = points };
    }
}

public static class ValueNormalizer
{
    public static double Normalize(double value, double minimum, double maximum)
    {
        if (!double.IsFinite(value) || !double.IsFinite(minimum) || !double.IsFinite(maximum)) return 0.5;
        if (minimum == maximum) return 0.5;
        return Math.Clamp((value - minimum) / (maximum - minimum), 0, 1);
    }
    public static double MapRange(double normalized, double outputMinimum, double outputMaximum) => outputMinimum + Math.Clamp(double.IsFinite(normalized) ? normalized : 0.5, 0, 1) * (outputMaximum - outputMinimum);
}

public sealed class MappedSeriesInterpolator
{
    readonly MappedDataSeries series;
    public MappedSeriesInterpolator(MappedDataSeries series) => this.series = series;
    public CurrentDataState Evaluate(double time)
    {
        var points = series.Points; time = Math.Clamp(time, series.MinimumTime, series.MaximumTime); var right = 0;
        if (time > points[0].Time && time < points[^1].Time) { right = 1; while (right < points.Count && points[right].Time <= time) right++; }
        else right = time >= points[^1].Time ? points.Count - 1 : 0;
        var left = right; var factor = 0d; var original = points[left].OriginalValue; var mapped = points[left].MappedValue;
        if (right > 0 && right < points.Count && points[right].Time > points[right - 1].Time) { left = right - 1; var span = points[right].Time - points[left].Time; factor = (time - points[left].Time) / span; original = points[left].OriginalValue + (points[right].OriginalValue - points[left].OriginalValue) * factor; mapped = points[left].MappedValue + (points[right].MappedValue - points[left].MappedValue) * factor; }
        var position = series.MaximumTime == series.MinimumTime ? 0 : (time - series.MinimumTime) / (series.MaximumTime - series.MinimumTime); return new CurrentDataState(position, time, mapped, left, right, factor) { CurrentOriginalValue = original, CurrentMappedValue = mapped, CurrentNormalizedValue = ValueNormalizer.Normalize(mapped, series.MinimumValue, series.MaximumValue) };
    }
}
