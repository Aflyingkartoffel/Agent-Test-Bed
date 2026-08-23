namespace InsectLightSimulation.Rendering;

public sealed class FpsCounter
{
    public double Value { get; private set; }

    public void Update(double deltaTime)
    {
        if (deltaTime <= 0) return;
        double instantFps = 1d / Math.Min(deltaTime, 1d);
        Value = Value == 0 ? instantFps : Value * 0.92 + instantFps * 0.08;
    }
}
