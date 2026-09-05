namespace TimeSeriesSonifier;

public sealed class FpsTracker
{
    long frames; double previousSeconds; double smoothed; bool started;
    public bool TryUpdate(TimeSpan renderingTime, out double fps)
    {
        var seconds = renderingTime.TotalSeconds; if (!started) { started = true; previousSeconds = seconds; frames = 1; fps = 0; return false; }
        frames++; var elapsed = seconds - previousSeconds; if (elapsed < .5) { fps = smoothed; return false; }
        var measured = frames / elapsed; smoothed = smoothed <= 0 ? measured : smoothed * .7 + measured * .3; frames = 0; previousSeconds = seconds; fps = double.IsFinite(smoothed) ? smoothed : 0; return true;
    }
    public void Reset() { frames = 0; previousSeconds = 0; smoothed = 0; started = false; }
}
