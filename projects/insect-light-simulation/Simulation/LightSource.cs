using System.Numerics;

namespace InsectLightSimulation.Simulation;

public sealed class LightSource
{
    public int Id { get; }
    public Vector2 Position { get; set; }
    public float AttractionStrength { get; set; }
    public float InfluenceRadius { get; set; }
    public float VisualIntensity { get; set; }

    public LightSource(int id, Vector2 position, float attractionStrength, float influenceRadius, float visualIntensity)
    {
        Id = id;
        Position = position;
        AttractionStrength = attractionStrength;
        InfluenceRadius = influenceRadius;
        VisualIntensity = visualIntensity;
    }
}
