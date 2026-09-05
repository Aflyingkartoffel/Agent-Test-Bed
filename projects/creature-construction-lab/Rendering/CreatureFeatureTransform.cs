using System.Numerics;
using CreatureConstructionLab.Model;

namespace CreatureConstructionLab.Rendering;

public readonly record struct FeatureWorldTransform(Vector2 Position, float Rotation, float Scale);

public static class CreatureFeatureTransform
{
    public static FeatureWorldTransform ToWorld(CreatureFeature feature, Vector2 parentPosition, Vector2 parentHeading, bool mirrored)
    {
        var heading = parentHeading.LengthSquared() > 0.0001f ? Vector2.Normalize(parentHeading) : Vector2.UnitX;
        var right = new Vector2(-heading.Y, heading.X);
        var local = feature.LocalPosition;
        if (mirrored) local.Y = -local.Y;
        var parentAngle = MathF.Atan2(heading.Y, heading.X) * 180 / MathF.PI;
        return new FeatureWorldTransform(parentPosition + heading * local.X + right * local.Y, parentAngle + (mirrored ? -feature.LocalRotation : feature.LocalRotation), Math.Max(0.05f, feature.Scale));
    }
}
