using System.Numerics;
using CreatureConstructionLab.Model;

namespace CreatureConstructionLab.Rendering;

public readonly record struct FinGeometry(Vector2 Attachment, Vector2 Tip, Vector2[] Outline)
{
    public static FinGeometry Build(CreatureFeature feature, Vector2 parentPosition, Vector2 parentHeading, float parentRadius, float absoluteAngle, bool mirrored = false)
    {
        var heading = parentHeading.LengthSquared() > 0.0001f ? Vector2.Normalize(parentHeading) : Vector2.UnitX;
        var side = mirrored ? (feature.FinSide == FinSide.Left ? FinSide.Right : FinSide.Left) : feature.FinSide;
        var sideNormal = side == FinSide.Left ? new Vector2(heading.Y, -heading.X) : new Vector2(-heading.Y, heading.X);
        var localY = mirrored ? -feature.LocalPosition.Y : feature.LocalPosition.Y;
        var attachment = parentPosition + sideNormal * (Math.Max(0, parentRadius) * 0.25f) + heading * feature.LocalPosition.X + sideNormal * localY;
        var direction = new Vector2(MathF.Cos(absoluteAngle), MathF.Sin(absoluteAngle));
        var perpendicular = new Vector2(-direction.Y, direction.X);
        var length = Math.Clamp(feature.FinLength, 2, 200) * Math.Max(0.05f, feature.Scale);
        var width = Math.Clamp(feature.FinWidth, 2, 100) * Math.Max(0.05f, feature.Scale);
        var tipRadius = Math.Clamp(width * 0.5f, 2, Math.Max(2, length * 0.35f));
        var tipCenter = attachment + direction * Math.Max(1, length - tipRadius);
        var upperTip = tipCenter + perpendicular * tipRadius;
        var lowerTip = tipCenter - perpendicular * tipRadius;
        var outline = new List<Vector2>(19) { attachment };
        AddCubic(outline, attachment + direction * (length * 0.2f) + perpendicular * (tipRadius * 0.25f), attachment + direction * (length * 0.62f) + perpendicular * tipRadius, upperTip, 6);
        for (var i = 1; i <= 6; i++)
        {
            var theta = MathF.PI / 2 - MathF.PI * i / 6;
            outline.Add(tipCenter + direction * (MathF.Cos(theta) * tipRadius) + perpendicular * (MathF.Sin(theta) * tipRadius));
        }
        AddCubic(outline, tipCenter - direction * (length * 0.62f) - perpendicular * tipRadius, tipCenter - direction * (length * 0.2f) - perpendicular * (tipRadius * 0.25f), attachment, 6);
        outline.Add(attachment);
        return new FinGeometry(attachment, tipCenter + direction * tipRadius, outline.ToArray());
    }

    public static float RestAngle(CreatureFeature feature, Vector2 parentHeading, bool mirrored = false)
    {
        var heading = parentHeading.LengthSquared() > 0.0001f ? Vector2.Normalize(parentHeading) : Vector2.UnitX;
        var tangentAngle = MathF.Atan2(heading.Y, heading.X);
        var side = mirrored ? (feature.FinSide == FinSide.Left ? FinSide.Right : FinSide.Left) : feature.FinSide;
        var sideAngle = side == FinSide.Left ? -MathF.PI / 2 : MathF.PI / 2;
        var authoredAngle = feature.FinBaseAngle + feature.LocalRotation;
        return tangentAngle + sideAngle + Math.Clamp(mirrored ? -authoredAngle : authoredAngle, -120, 120) * MathF.PI / 180;
    }

    private static void AddCubic(List<Vector2> points, Vector2 c1, Vector2 c2, Vector2 end, int samples)
    {
        var start = points[^1];
        for (var i = 1; i <= samples; i++)
        {
            var t = i / (float)samples;
            var inverse = 1 - t;
            points.Add(inverse * inverse * inverse * start + 3 * inverse * inverse * t * c1 + 3 * inverse * t * t * c2 + t * t * t * end);
        }
    }
}
