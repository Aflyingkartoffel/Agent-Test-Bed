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
    public float EyeWidth { get; set; } = 16;
    public float EyeHeight { get; set; } = 9;
    public float EyeTrackingStrength { get; set; } = 0.5f;
    public float TongueLength { get; set; } = 28;
    public float TongueForkLength { get; set; } = 12;
    public float TongueForkAngle { get; set; } = 28;
    public FinSide FinSide { get; set; } = FinSide.Right;
    public float FinLength { get; set; } = 34;
    public float FinWidth { get; set; } = 16;
    public float FinBaseAngle { get; set; }
    public float FinAngularStiffness { get; set; } = 12;
    public float FinAngularDamping { get; set; } = 4;
    public bool SupportsMirroring => Type == CreatureFeatureType.Eye;

    public CreatureFeature Clone(bool newId = false) => new()
    {
        Id = newId ? Guid.NewGuid() : Id,
        Type = Type,
        ParentNodeId = ParentNodeId,
        LocalPosition = LocalPosition,
        LocalRotation = LocalRotation,
        Scale = Scale,
        Mirrored = Mirrored,
        Visible = Visible,
        EyeSize = EyeSize,
        EyeWidth = EyeWidth,
        EyeHeight = EyeHeight,
        EyeTrackingStrength = EyeTrackingStrength,
        TongueLength = TongueLength,
        TongueForkLength = TongueForkLength,
        TongueForkAngle = TongueForkAngle,
        FinSide = FinSide,
        FinLength = FinLength,
        FinWidth = FinWidth,
        FinBaseAngle = FinBaseAngle,
        FinAngularStiffness = FinAngularStiffness,
        FinAngularDamping = FinAngularDamping
    };
}
