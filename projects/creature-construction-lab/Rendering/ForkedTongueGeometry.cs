using System.Numerics;
using CreatureConstructionLab.Model;

namespace CreatureConstructionLab.Rendering;

public readonly record struct ForkedTongueGeometry(Vector2 Start, Vector2 Junction, Vector2 UpperTip, Vector2 LowerTip)
{
    public static ForkedTongueGeometry Build(CreatureFeature feature, FeatureWorldTransform transform, float headRadius)
    {
        var radians = transform.Rotation * MathF.PI / 180;
        var forward = new Vector2(MathF.Cos(radians), MathF.Sin(radians));
        var stemLength = Math.Clamp(feature.TongueLength, 2, 200) * transform.Scale;
        var forkLength = Math.Clamp(feature.TongueForkLength, 2, 100) * transform.Scale;
        var forkAngle = Math.Clamp(feature.TongueForkAngle, 5, 75) * MathF.PI / 180;
        var start = transform.Position + forward * Math.Max(0, headRadius);
        var junction = start + forward * stemLength;
        var upperDirection = new Vector2(MathF.Cos(radians - forkAngle), MathF.Sin(radians - forkAngle));
        var lowerDirection = new Vector2(MathF.Cos(radians + forkAngle), MathF.Sin(radians + forkAngle));
        return new ForkedTongueGeometry(start, junction, junction + upperDirection * forkLength, junction + lowerDirection * forkLength);
    }
}
