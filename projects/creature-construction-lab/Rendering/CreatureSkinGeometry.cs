using System.Numerics;

namespace CreatureConstructionLab.Rendering;

public sealed class CreatureSkinGeometry
{
    private const int SamplesPerSegment = 8;
    public Vector2[] Left { get; }
    public Vector2[] Right { get; }
    public float[] Radii { get; }

    private CreatureSkinGeometry(Vector2[] left, Vector2[] right, float[] radii) { Left = left; Right = right; Radii = radii; }

    public static CreatureSkinGeometry Build(IReadOnlyList<Vector2> positions, IReadOnlyList<float> radii)
    {
        var count = Math.Min(positions.Count, radii.Count);
        if (count == 0) return new CreatureSkinGeometry([], [], []);
        var left = new Vector2[count];
        var right = new Vector2[count];
        var safeRadii = new float[count];
        for (var i = 0; i < count; i++)
        {
            var direction = GetDirection(positions, i);
            var normal = new Vector2(-direction.Y, direction.X);
            safeRadii[i] = Math.Max(0, float.IsFinite(radii[i]) ? radii[i] : 0);
            left[i] = positions[i] + normal * safeRadii[i];
            right[i] = positions[i] - normal * safeRadii[i];
        }
        if (count < 3) return new CreatureSkinGeometry(left, right, safeRadii);
        return new CreatureSkinGeometry(SampleSide(left), SampleSide(right), SampleRadii(safeRadii));
    }

    private static Vector2[] SampleSide(Vector2[] points)
    {
        var samples = new List<Vector2>((points.Length - 1) * SamplesPerSegment + 1);
        for (var segment = 0; segment < points.Length - 1; segment++)
        {
            var p0 = points[Math.Max(0, segment - 1)];
            var p1 = points[segment];
            var p2 = points[segment + 1];
            var p3 = points[Math.Min(points.Length - 1, segment + 2)];
            for (var step = 0; step < SamplesPerSegment; step++) samples.Add(CatmullRom(p0, p1, p2, p3, step / (float)SamplesPerSegment));
        }
        samples.Add(points[^1]);
        return samples.ToArray();
    }

    private static float[] SampleRadii(float[] radii)
    {
        var samples = new List<float>((radii.Length - 1) * SamplesPerSegment + 1);
        for (var segment = 0; segment < radii.Length - 1; segment++)
            for (var step = 0; step < SamplesPerSegment; step++) samples.Add(Math.Max(0, float.Lerp(radii[segment], radii[segment + 1], step / (float)SamplesPerSegment)));
        samples.Add(radii[^1]);
        return samples.ToArray();
    }

    private static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        var t2 = t * t;
        var t3 = t2 * t;
        return 0.5f * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t3);
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
