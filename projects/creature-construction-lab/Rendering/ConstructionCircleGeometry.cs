using System.Numerics;

namespace CreatureConstructionLab.Rendering;

public readonly record struct ConstructionCircle(Vector2 Center, float Radius);

public static class ConstructionCircleGeometry
{
    public static ConstructionCircle[] Build(IReadOnlyList<Vector2> positions, IReadOnlyList<float> radii)
    {
        var count = Math.Min(positions.Count, radii.Count);
        var circles = new ConstructionCircle[count];
        for (var i = 0; i < count; i++)
        {
            var center = positions[i];
            if (!float.IsFinite(center.X) || !float.IsFinite(center.Y)) center = Vector2.Zero;
            var radius = float.IsFinite(radii[i]) ? Math.Max(0, radii[i]) : 0;
            circles[i] = new ConstructionCircle(center, radius);
        }
        return circles;
    }
}
