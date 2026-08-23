using System.Numerics;

namespace CreatureConstructionLab.Rendering;

public sealed class CreatureSkinGeometry
{
    public Vector2[] Left { get; }
    public Vector2[] Right { get; }
    private CreatureSkinGeometry(Vector2[] left, Vector2[] right) { Left = left; Right = right; }

    public static CreatureSkinGeometry Build(IReadOnlyList<Vector2> positions, IReadOnlyList<float> radii)
    {
        var count = Math.Min(positions.Count, radii.Count);
        var left = new Vector2[count];
        var right = new Vector2[count];
        for (var i = 0; i < count; i++)
        {
            var direction = GetDirection(positions, i);
            var normal = new Vector2(-direction.Y, direction.X);
            var radius = Math.Max(0, float.IsFinite(radii[i]) ? radii[i] : 0);
            left[i] = positions[i] + normal * radius;
            right[i] = positions[i] - normal * radius;
        }
        return new CreatureSkinGeometry(left, right);
    }

    private static Vector2 GetDirection(IReadOnlyList<Vector2> positions, int index)
    {
        Vector2 direction;
        if (positions.Count <= 1) direction = Vector2.UnitX;
        else if (index == 0) direction = positions[1] - positions[0];
        else if (index == positions.Count - 1) direction = positions[index] - positions[index - 1];
        else direction = positions[index + 1] - positions[index - 1];
        return direction.LengthSquared() < 0.0001f ? Vector2.UnitX : Vector2.Normalize(direction);
    }
}
