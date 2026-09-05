using System.Numerics;

namespace CreatureConstructionLab.Model;

public sealed class RampPoint
{
    public float Position { get; set; }
    public float Value { get; set; }
    public Vector2? InHandle { get; set; }
    public Vector2? OutHandle { get; set; }

    public RampPoint(float position, float value) { Position = position; Value = value; }
}
