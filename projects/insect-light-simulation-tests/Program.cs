using System.Numerics;
using InsectLightSimulation.Simulation;
using InsectLightSimulation.Simulation.Behaviors;

static class Tests
{
    private static int passed;

    public static int Main()
    {
        Run("attraction direction", AttractionDirection);
        Run("attraction falloff", AttractionFalloff);
        Run("speed limit", SpeedLimit);
        Run("turn rate limit", TurnRateLimit);
        Run("wrap boundary", WrapBoundary);
        Run("reset", Reset);
        Run("deterministic seed", DeterministicSeed);
        Console.WriteLine($"{passed} simulation tests passed.");
        return 0;
    }

    private static void AttractionDirection()
    {
        var settings = new SimulationSettings { InfluenceRadius = 100 };
        var simulation = new SimulationEngine(settings);
        simulation.Light.Position = new Vector2(10, 0);
        var agent = new Agent(Vector2.Zero, Vector2.UnitY, 1, 1, 1);
        Vector2 force = new LightAttractionBehavior().CalculateForce(agent, simulation, 1 / 60f);
        Check(force.X > 0 && Math.Abs(force.Y) < 0.001f, "force should point right");
    }

    private static void AttractionFalloff()
    {
        var settings = new SimulationSettings { InfluenceRadius = 100 };
        var simulation = new SimulationEngine(settings);
        simulation.Light.Position = new Vector2(50, 0);
        var near = new Agent(Vector2.Zero, Vector2.UnitY, 1, 1, 1);
        var far = new Agent(new Vector2(-50, 0), Vector2.UnitY, 1, 1, 1);
        float nearForce = new LightAttractionBehavior().CalculateForce(near, simulation, 1 / 60f).Length();
        float farForce = new LightAttractionBehavior().CalculateForce(far, simulation, 1 / 60f).Length();
        Check(nearForce > farForce && farForce == 0, "force should fall off to zero outside radius");
    }

    private static void SpeedLimit()
    {
        var settings = new SimulationSettings { InsectCount = 10, BaseSpeed = 40 };
        var simulation = new SimulationEngine(settings);
        for (int i = 0; i < 100; i++) simulation.Update(1 / 60f);
        Check(simulation.Agents.All(a => a.Velocity.Length() <= 60.001f), "velocity should stay bounded");
    }

    private static void TurnRateLimit()
    {
        var settings = new SimulationSettings { InsectCount = 1, TurnRate = 1, BaseSpeed = 50 };
        var simulation = new SimulationEngine(settings);
        simulation.Light.Position = simulation.Agents[0].Position - Vector2.Normalize(simulation.Agents[0].Velocity) * 100;
        float before = simulation.Agents[0].Heading;
        simulation.Update(1 / 60f);
        float after = simulation.Agents[0].Heading;
        Check(Math.Abs(after - before) <= 1.001f / 60f, "heading should turn gradually");
    }

    private static void WrapBoundary()
    {
        var simulation = new SimulationEngine(new SimulationSettings { InsectCount = 1 });
        Agent agent = simulation.Agents[0];
        agent.Position = new Vector2(-2, 20);
        simulation.Update(1 / 60f);
        Check(agent.Position.X >= 0 && agent.Position.X <= simulation.Width, "wrapped agent should remain in world");
    }

    private static void Reset()
    {
        var simulation = new SimulationEngine(new SimulationSettings { InsectCount = 10 });
        Vector2 initial = simulation.Agents[0].Position;
        simulation.Update(1);
        simulation.Reset();
        Check(simulation.SimulationTime == 0 && simulation.Agents[0].Position == initial, "reset should restore state");
    }

    private static void DeterministicSeed()
    {
        var a = new SimulationEngine(new SimulationSettings { Seed = 7, InsectCount = 20 });
        var b = new SimulationEngine(new SimulationSettings { Seed = 7, InsectCount = 20 });
        Check(a.Agents[5].Position == b.Agents[5].Position && a.Agents[5].Velocity == b.Agents[5].Velocity, "same seed should reproduce agents");
    }

    private static void Run(string name, Action test)
    {
        test();
        passed++;
        Console.WriteLine($"PASS {name}");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
