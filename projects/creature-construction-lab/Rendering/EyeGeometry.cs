using System.Numerics;
using CreatureConstructionLab.Model;

namespace CreatureConstructionLab.Rendering;

public readonly record struct EyeGeometry(Vector2 Center, float Radius)
{
    public const uint OrbColorArgb = 0xFFFFFFFF;

    public static EyeGeometry Build(CreatureFeature feature, FeatureWorldTransform transform) => new(transform.Position, Math.Max(2, feature.EyeSize * transform.Scale));
}
