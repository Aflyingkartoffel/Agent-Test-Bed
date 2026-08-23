using System.Numerics;

namespace CreatureConstructionLab.Model;

public sealed class CreatureFeature
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public CreatureFeatureType Type { get; set; } = CreatureFeatureType.Eye;
    public Guid ParentNodeId { get; set; }
    public Vector2 LocalPosition { get; set; }
    public float LocalRotation { get; set; }
    public float Scale { get; set; } = 1;
    public bool Mirrored { get; set; } = true;
    public bool Visible { get; set; } = true;
    public float EyeSize { get; set; } = 5;
}
