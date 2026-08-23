using System.Numerics;

namespace InsectLightSimulation.Simulation.Behaviors;

public sealed class LightAttractionBehavior : IBehavior
{
    public Vector2 CalculateForce(Agent agent, SimulationEngine simulation, float deltaTime)
    {
        Vector2 offset = simulation.Light.Position - agent.Position;
        float distance = offset.Length();
        if (distance < 0.001f || distance > simulation.Settings.InfluenceRadius)
            return Vector2.Zero;

        float falloff = 1f - distance / simulation.Settings.InfluenceRadius;
        // A target direction is a force, not a teleport: inertia and turning still matter.
        return Vector2.Normalize(offset) * simulation.Settings.AttractionStrength * falloff;
    }
}
