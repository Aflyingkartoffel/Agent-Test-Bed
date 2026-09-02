namespace TimeSeriesSonifier;

public sealed class SeriesInterpolator
{
    readonly DataSeries series;
    public SeriesInterpolator(DataSeries series) => this.series = series;
    public CurrentDataState Evaluate(double time)
    {
        var points = series.Points; time = Math.Clamp(time, series.MinimumTime, series.MaximumTime);
        if (time <= points[0].Time) return new(0, points[0].Time, points[0].Value, 0, 0, 0);
        if (time >= points[^1].Time) return new(1, points[^1].Time, points[^1].Value, points.Count - 1, points.Count - 1, 0);
        var right = 1; while (right < points.Count && points[right].Time <= time) right++;
        var left = right - 1; var span = points[right].Time - points[left].Time; var factor = span <= 0 ? 0 : (time - points[left].Time) / span;
        return new((time - series.MinimumTime) / (series.MaximumTime - series.MinimumTime), time, points[left].Value + (points[right].Value - points[left].Value) * factor, left, right, factor);
    }
}

public sealed class TimelineEngine
{
    public const double DefaultPresentationDuration = 10;
    public TimelineState State { get; private set; } = TimelineState.Stopped;
    public double CurrentTime { get; private set; }
    public double PlaybackSpeed { get; set; } = 1;
    public bool LoopEnabled { get; set; }
    public double StartTime { get; private set; }
    public double EndTime { get; private set; } = 1;
    public void SetRange(double start, double end) { StartTime = start; EndTime = Math.Max(start, end); Reset(); }
    public void Play() { if (EndTime > StartTime) State = TimelineState.Playing; }
    public void Pause() { if (State == TimelineState.Playing) State = TimelineState.Paused; }
    public void Reset() { CurrentTime = StartTime; State = TimelineState.Stopped; }
    public void SeekNormalized(double position) { CurrentTime = StartTime + Math.Clamp(position, 0, 1) * (EndTime - StartTime); if (State == TimelineState.Stopped) State = TimelineState.Paused; }
    public void Advance(double elapsedSeconds)
    {
        if (State != TimelineState.Playing || EndTime <= StartTime) return;
        elapsedSeconds = Math.Clamp(elapsedSeconds, 0, 1); CurrentTime += elapsedSeconds * (EndTime - StartTime) / DefaultPresentationDuration * Math.Max(0, PlaybackSpeed);
        if (CurrentTime < EndTime) return;
        if (LoopEnabled) CurrentTime = StartTime + (CurrentTime - StartTime) % (EndTime - StartTime); else { CurrentTime = EndTime; State = TimelineState.Stopped; }
    }
    public double NormalizedPosition => EndTime <= StartTime ? 0 : Math.Clamp((CurrentTime - StartTime) / (EndTime - StartTime), 0, 1);
}
