using System.Numerics;

namespace CreatureConstructionLab.Model;

public sealed class CreatureNode
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Vector2 Position { get; set; }
    public float Radius { get; set; } = 24;
    public float Rotation { get; set; }
    public float NormalizedPosition { get; internal set; }
    public float RampValue { get; internal set; }
}
