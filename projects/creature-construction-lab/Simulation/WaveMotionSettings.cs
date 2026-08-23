namespace CreatureConstructionLab.Simulation;

public sealed class WaveMotionSettings
{
    public bool Enabled { get; set; } = true;
    public float Amplitude { get; set; } = 4;
    public float Frequency { get; set; } = 1.2f;
    public float Phase { get; set; } = 2.8f;
    public float Influence { get; set; } = 0.75f;
}
