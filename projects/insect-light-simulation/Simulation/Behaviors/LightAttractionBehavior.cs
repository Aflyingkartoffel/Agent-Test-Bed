using System.Numerics;

namespace InsectLightSimulation.Simulation.Behaviors;

public sealed class LightAttractionBehavior : IBehavior
{
    public Vector2 CalculateForce(Agent agent, SimulationEngine simulation, float deltaTime)
    {
        Vector2 totalForce = Vector2.Zero;
        for (int i = 0; i < simulation.Lights.Count; i++)
        {
            LightSource light = simulation.Lights[i];
            Vector2 offset = light.Position - agent.Position;
            float distance = offset.Length();
            if (distance < 0.001f || distance > light.InfluenceRadius)
                continue;

            float falloff = 1f - distance / light.InfluenceRadius;
            // Each light contributes a force. Their vector sum creates the field.
            totalForce += Vector2.Normalize(offset) * light.AttractionStrength * falloff;
        }
        return totalForce;
    }
}
