namespace TimeSeriesSonifier;

/// Connects the authoritative timeline state to one shared interpolated data state and optional audio.
public sealed class PlaybackCoordinator
{
    readonly TimelineEngine timeline;
    readonly AudioEngine audio;
    MappedSeriesInterpolator? interpolator;
    double minimumPitch = PitchMapper.DefaultMinimumFrequency;
    double maximumPitch = PitchMapper.DefaultMaximumFrequency;
    bool wasPlaying;

    public PlaybackCoordinator(TimelineEngine timeline, AudioEngine audio) { this.timeline = timeline; this.audio = audio; CurrentDataState = CurrentDataState.Empty; }
    public TimelineEngine Timeline => timeline;
    public AudioEngine Audio => audio;
    public CurrentDataState CurrentDataState { get; private set; }
    public bool AudioEnabled { get; private set; }

    public void SetSeries(MappedDataSeries? series)
    {
        interpolator = series is null ? null : new MappedSeriesInterpolator(series);
        if (series is null) { timeline.SetRange(0, 1); CurrentDataState = CurrentDataState.Empty; return; }
        timeline.SetRange(series.MinimumTime, series.MaximumTime); PublishState();
    }
    public void SetPitchRange(double minimum, double maximum) { minimumPitch = minimum; maximumPitch = maximum; PublishState(); }
    public void SetLoop(bool enabled) => timeline.LoopEnabled = enabled;
    public void SetPlaybackSpeed(double speed) => timeline.PlaybackSpeed = speed;
    public void Play()
    {
        if (interpolator is null || timeline.State == TimelineState.Playing) return;
        PublishState();
        if (AudioEnabled && audio.State != AudioEngineState.Running) audio.Start();
        timeline.Play();
        wasPlaying = true;
    }
    public void Pause() { timeline.Pause(); }
    public void Reset() { timeline.Reset(); PublishState(); audio.Stop(); wasPlaying = false; }
    public void SeekNormalized(double position) { timeline.SeekNormalized(position); PublishState(); }
    public void SetAudioEnabled(bool enabled)
    {
        AudioEnabled = enabled;
        if (!enabled) { audio.Stop(); return; }
        PublishState();
        if (timeline.State == TimelineState.Playing && audio.State != AudioEngineState.Running) audio.Start();
    }
    public void Advance(double elapsedSeconds)
    {
        var playing = timeline.State == TimelineState.Playing;
        timeline.Advance(elapsedSeconds); PublishState();
        if (playing && timeline.State == TimelineState.Stopped && !timeline.LoopEnabled) audio.Stop();
        wasPlaying = timeline.State == TimelineState.Playing;
    }
    void PublishState()
    {
        CurrentDataState = interpolator?.Evaluate(timeline.CurrentTime) ?? CurrentDataState.Empty;
        if (AudioEnabled && interpolator is not null) audio.SetTargetFrequencyFromNormalized(CurrentDataState.CurrentNormalizedValue, minimumPitch, maximumPitch);
    }
}
