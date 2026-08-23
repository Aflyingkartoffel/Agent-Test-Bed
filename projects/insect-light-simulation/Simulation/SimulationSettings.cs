namespace InsectLightSimulation.Simulation;

public enum BoundaryMode
{
    Wrap,
    SoftBounce
}

public sealed class SimulationSettings
{
    public int InsectCount { get; set; } = 500;
    public float BaseSpeed { get; set; } = 72f;
    public float TurnRate { get; set; } = 3.4f;
    public float WanderStrength { get; set; } = 0.75f;
    public BoundaryMode BoundaryMode { get; set; } = BoundaryMode.Wrap;
    public int Seed { get; set; } = 42;
}
