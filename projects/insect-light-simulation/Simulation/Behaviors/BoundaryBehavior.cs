using System.Numerics;

namespace InsectLightSimulation.Simulation.Behaviors;

public sealed class BoundaryBehavior : IBehavior
{
    public Vector2 CalculateForce(Agent agent, SimulationEngine simulation, float deltaTime)
    {
        if (simulation.Settings.BoundaryMode == BoundaryMode.Wrap)
            return Vector2.Zero;

        const float margin = 55f;
        Vector2 force = Vector2.Zero;
        if (agent.Position.X < margin) force.X += (margin - agent.Position.X) / margin;
        if (agent.Position.X > simulation.Width - margin) force.X -= (agent.Position.X - (simulation.Width - margin)) / margin;
        if (agent.Position.Y < margin) force.Y += (margin - agent.Position.Y) / margin;
        if (agent.Position.Y > simulation.Height - margin) force.Y -= (agent.Position.Y - (simulation.Height - margin)) / margin;
        return force * 2.5f;
    }
}
