using System.Numerics;

namespace InsectLightSimulation.Simulation;

public sealed class LightSource
{
    public const float DefaultAttractionStrength = 1.25f;
    public const float DefaultInfluenceRadius = 360f;
    public const float DefaultVisualIntensity = 1f;
    public int Id { get; }
    public Vector2 Position { get; set; }
    public float AttractionStrength { get; set; }
    public float InfluenceRadius { get; set; }
    public float VisualIntensity { get; set; }
    public float Power { get; private set; }

    public LightSource(int id, Vector2 position, float attractionStrength, float influenceRadius, float visualIntensity)
    {
        Id = id;
        Position = position;
        AttractionStrength = attractionStrength;
        InfluenceRadius = influenceRadius;
        VisualIntensity = visualIntensity;
        Power = 1f;
    }

    public void SetPower(float power)
    {
        Power = Math.Clamp(power, 0f, 2f);
        AttractionStrength = DefaultAttractionStrength * Power;
        InfluenceRadius = DefaultInfluenceRadius * Power;
        VisualIntensity = DefaultVisualIntensity * Power;
    }
}
