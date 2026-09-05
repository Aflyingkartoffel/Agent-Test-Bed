using System.Numerics;

namespace InsectLightSimulation.Simulation.Behaviors;

public interface IBehavior
{
    Vector2 CalculateForce(Agent agent, SimulationEngine simulation, float deltaTime);
}
