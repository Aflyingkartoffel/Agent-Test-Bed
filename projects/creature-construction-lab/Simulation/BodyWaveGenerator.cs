using System.Numerics;

namespace CreatureConstructionLab.Simulation;

public static class BodyWaveGenerator
{
    public static Vector2 CalculateOffset(float time, float normalizedPosition, Vector2 normal, WaveMotionSettings settings, float spacing)
    {
        if (!settings.Enabled || normalizedPosition <= 0 || settings.Influence <= 0 || normal.LengthSquared() < 0.0001f) return Vector2.Zero;
        var safeNormal = Vector2.Normalize(normal);
        var amplitude = Math.Clamp(settings.Amplitude, 0, Math.Max(0, spacing * 0.45f));
        var phase = Math.Clamp(settings.Phase, 0, 20);
        var frequency = Math.Clamp(settings.Frequency, 0, 10);
        var influence = Math.Clamp(settings.Influence, 0, 1) * normalizedPosition;
        var wave = MathF.Sin(time * frequency * MathF.Tau - normalizedPosition * phase * MathF.Tau);
        return safeNormal * (wave * amplitude * influence);
    }
}
