using System.Numerics;
using CreatureConstructionLab.Model;

namespace CreatureConstructionLab.Rendering;

public readonly record struct FinGeometry(Vector2 Attachment, Vector2 Tip, Vector2[] Outline)
{
    public static FinGeometry Build(CreatureFeature feature, Vector2 parentPosition, Vector2 parentHeading, float parentRadius, float absoluteAngle)
    {
        var heading = parentHeading.LengthSquared() > 0.0001f ? Vector2.Normalize(parentHeading) : Vector2.UnitX;
        var sideNormal = feature.FinSide == FinSide.Left ? new Vector2(heading.Y, -heading.X) : new Vector2(-heading.Y, heading.X);
        var attachment = parentPosition + sideNormal * Math.Max(0, parentRadius) + heading * feature.LocalPosition.X + sideNormal * feature.LocalPosition.Y;
        var direction = new Vector2(MathF.Cos(absoluteAngle), MathF.Sin(absoluteAngle));
        var perpendicular = new Vector2(-direction.Y, direction.X);
        var length = Math.Clamp(feature.FinLength, 2, 200) * Math.Max(0.05f, feature.Scale);
        var width = Math.Clamp(feature.FinWidth, 2, 100) * Math.Max(0.05f, feature.Scale);
        var tip = attachment + direction * length;
        var shoulder = attachment + direction * (length * 0.42f);
        var outline = new[]
        {
            attachment,
            shoulder + perpendicular * (width * 0.5f),
            tip,
            shoulder - perpendicular * (width * 0.5f)
        };
        return new FinGeometry(attachment, tip, outline);
    }

    public static float RestAngle(CreatureFeature feature, Vector2 parentHeading)
    {
        var heading = parentHeading.LengthSquared() > 0.0001f ? Vector2.Normalize(parentHeading) : Vector2.UnitX;
        var tangentAngle = MathF.Atan2(heading.Y, heading.X);
        var sideAngle = feature.FinSide == FinSide.Left ? -MathF.PI / 2 : MathF.PI / 2;
        return tangentAngle + sideAngle + Math.Clamp(feature.FinBaseAngle + feature.LocalRotation, -120, 120) * MathF.PI / 180;
    }
}
