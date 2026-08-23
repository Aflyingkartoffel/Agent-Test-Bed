namespace CreatureConstructionLab.Model;

public sealed class RampPoint
{
    public float Position { get; set; }
    public float Value { get; set; }

    public RampPoint(float position, float value) { Position = position; Value = value; }
}
