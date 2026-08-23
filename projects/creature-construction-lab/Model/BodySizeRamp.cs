namespace CreatureConstructionLab.Model;

public sealed class BodySizeRamp
{
    public const float MinValue = 0.1f;
    public const float MaxValue = 2f;
    public List<RampPoint> Points { get; } = [];

    public BodySizeRamp() => Reset();

    public void Reset()
    {
        Points.Clear();
        Points.Add(new RampPoint(0, 1));
        Points.Add(new RampPoint(1, 0.4f));
    }

    public float Sample(float position)
    {
        if (Points.Count == 0) return 1;
        SortAndClamp();
        position = Math.Clamp(position, 0, 1);
        if (position <= Points[0].Position) return Points[0].Value;
        for (var i = 1; i < Points.Count; i++)
        {
            var right = Points[i];
            if (position <= right.Position)
            {
                var left = Points[i - 1];
                var amount = (position - left.Position) / Math.Max(0.0001f, right.Position - left.Position);
                return left.Value + (right.Value - left.Value) * amount;
            }
        }
        return Points[^1].Value;
    }

    public RampPoint AddPoint(float position, float value)
    {
        var point = new RampPoint(Math.Clamp(position, 0.001f, 0.999f), Math.Clamp(value, MinValue, MaxValue));
        Points.Add(point);
        SortAndClamp();
        return point;
    }

    public bool RemovePoint(RampPoint point)
    {
        if (Points.Count <= 2 || point == Points[0] || point == Points[^1]) return false;
        return Points.Remove(point);
    }

    public void SortAndClamp()
    {
        Points.Sort((a, b) => a.Position.CompareTo(b.Position));
        for (var i = 0; i < Points.Count; i++)
        {
            Points[i].Position = i == 0 ? 0 : i == Points.Count - 1 ? 1 : Math.Clamp(Points[i].Position, Points[i - 1].Position + 0.001f, 1 - (Points.Count - i - 1) * 0.001f);
            Points[i].Value = Math.Clamp(float.IsFinite(Points[i].Value) ? Points[i].Value : 1, MinValue, MaxValue);
        }
    }
}
