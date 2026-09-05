using System.Numerics;

namespace InsectLightSimulation.Simulation.Behaviors;

public sealed class WanderBehavior : IBehavior
{
    public Vector2 CalculateForce(Agent agent, SimulationEngine simulation, float deltaTime)
    {
        agent.WanderAngle += (simulation.Random.NextSingle() - 0.5f) * 2.2f * deltaTime;
        float angle = agent.Heading + agent.WanderAngle;
        Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
        return direction * simulation.Settings.WanderStrength * agent.WanderStrength;
    }
}
