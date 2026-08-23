using System.Numerics;

namespace CreatureConstructionLab.Rendering;

public sealed class CreatureSkinGeometry
{
    private const int SamplesPerSegment = 8;
    private const int CapSamples = 10;
    public Vector2[] Left { get; }
    public Vector2[] Right { get; }
    public float[] Radii { get; }
    public Vector2[] Outline { get; }

    private CreatureSkinGeometry(Vector2[] left, Vector2[] right, float[] radii, Vector2[] outline)
    {
        Left = left;
        Right = right;
        Radii = radii;
        Outline = outline;
    }

    public static CreatureSkinGeometry Build(IReadOnlyList<Vector2> positions, IReadOnlyList<float> radii)
    {
        var count = Math.Min(positions.Count, radii.Count);
        if (count == 0) return new CreatureSkinGeometry([], [], [], []);
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

        if (count == 1) return new CreatureSkinGeometry(left, right, safeRadii, BuildCircle(positions[0], safeRadii[0]));
        var sampledLeft = SampleSide(left);
        var sampledRight = SampleSide(right);
        var sampledRadii = SampleRadii(safeRadii);
        return new CreatureSkinGeometry(sampledLeft, sampledRight, sampledRadii, BuildOutline(positions, safeRadii, sampledLeft, sampledRight));
    }

    private static Vector2[] BuildOutline(IReadOnlyList<Vector2> positions, IReadOnlyList<float> radii, Vector2[] left, Vector2[] right)
    {
        var outline = new List<Vector2>(left.Length + right.Length + CapSamples * 2);
        var headTangent = GetDirection(positions, 0);
        var headNormal = new Vector2(-headTangent.Y, headTangent.X);
        AddCap(outline, positions[0], headTangent, headNormal, radii[0], MathF.PI / 2, -MathF.PI / 2);
        for (var i = 1; i < right.Length; i++) outline.Add(right[i]);
        var tailTangent = GetDirection(positions, positions.Count - 1);
        var tailNormal = new Vector2(-tailTangent.Y, tailTangent.X);
        AddCap(outline, positions[^1], tailTangent, tailNormal, radii[^1], -MathF.PI / 2, -3 * MathF.PI / 2);
        for (var i = left.Length - 2; i >= 0; i--) outline.Add(left[i]);
        return outline.ToArray();
    }

    private static void AddCap(List<Vector2> outline, Vector2 center, Vector2 tangent, Vector2 normal, float radius, float start, float end)
    {
        for (var i = 0; i <= CapSamples; i++)
        {
            var angle = start + (end - start) * i / CapSamples;
            outline.Add(center + tangent * (MathF.Cos(angle) * radius) + normal * (MathF.Sin(angle) * radius));
        }
    }

    private static Vector2[] BuildCircle(Vector2 center, float radius)
    {
        var points = new Vector2[CapSamples * 2];
        for (var i = 0; i < points.Length; i++)
        {
            var angle = i * MathF.Tau / points.Length;
            points[i] = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
        }
        return points;
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
            var tangent1 = segment == 0 ? p2 - p1 : (p2 - p0) * 0.5f;
            var tangent2 = segment == points.Length - 2 ? p2 - p1 : (p3 - p1) * 0.5f;
            for (var step = 0; step < SamplesPerSegment; step++) samples.Add(Hermite(p1, p2, tangent1, tangent2, step / (float)SamplesPerSegment));
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

    private static Vector2 Hermite(Vector2 p1, Vector2 p2, Vector2 tangent1, Vector2 tangent2, float t)
    {
        var t2 = t * t;
        var t3 = t2 * t;
        return (2 * t3 - 3 * t2 + 1) * p1 + (t3 - 2 * t2 + t) * tangent1 + (-2 * t3 + 3 * t2) * p2 + (t3 - t2) * tangent2;
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
