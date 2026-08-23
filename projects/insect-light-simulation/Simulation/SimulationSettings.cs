namespace InsectLightSimulation.Simulation;

public enum BoundaryMode
{
    Wrap,
    SoftBounce
}

public sealed class SimulationSettings
{
    public int InsectCount { get; set; } = 500;
    public float AttractionStrength { get; set; } = 1.25f;
    public float InfluenceRadius { get; set; } = 360f;
    public float BaseSpeed { get; set; } = 72f;
    public float TurnRate { get; set; } = 3.4f;
    public float WanderStrength { get; set; } = 0.75f;
    public float LightIntensity { get; set; } = 1f;
    public BoundaryMode BoundaryMode { get; set; } = BoundaryMode.Wrap;
    public int Seed { get; set; } = 42;
}
