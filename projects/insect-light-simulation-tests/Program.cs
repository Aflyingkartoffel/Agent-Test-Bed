using System.Numerics;
using InsectLightSimulation.Simulation;
using InsectLightSimulation.Simulation.Behaviors;
using InsectLightSimulation.Rendering;

static class Tests
{
    private static int passed;

    public static int Main()
    {
        Run("attraction direction", AttractionDirection);
        Run("attraction falloff", AttractionFalloff);
        Run("default light power", DefaultLightPower);
        Run("power scales light attributes", PowerScalesAttributes);
        Run("power is independent per light", PowerIsIndependent);
        Run("new light has default power", NewLightHasDefaultPower);
        Run("reset restores light power", ResetRestoresLightPower);
        Run("multiple light forces combine", MultipleLightForcesCombine);
        Run("independent light radius", IndependentLightRadius);
        Run("add and remove lights", AddRemoveLights);
        Run("nearest light selection", NearestLightSelection);
        Run("light position persists", LightPositionPersists);
        Run("FPS measurement", FpsMeasurement);
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
        var settings = new SimulationSettings();
        var simulation = new SimulationEngine(settings);
        simulation.Lights[0].InfluenceRadius = 100;
        simulation.Lights[0].Position = new Vector2(10, 0);
        var agent = new Agent(Vector2.Zero, Vector2.UnitY, 1, 1, 1);
        Vector2 force = new LightAttractionBehavior().CalculateForce(agent, simulation, 1 / 60f);
        Check(force.X > 0 && Math.Abs(force.Y) < 0.001f, "force should point right");
    }

    private static void AttractionFalloff()
    {
        var settings = new SimulationSettings();
        var simulation = new SimulationEngine(settings);
        simulation.Lights[0].InfluenceRadius = 100;
        simulation.Lights[0].Position = new Vector2(50, 0);
        var near = new Agent(Vector2.Zero, Vector2.UnitY, 1, 1, 1);
        var far = new Agent(new Vector2(-50, 0), Vector2.UnitY, 1, 1, 1);
        float nearForce = new LightAttractionBehavior().CalculateForce(near, simulation, 1 / 60f).Length();
        float farForce = new LightAttractionBehavior().CalculateForce(far, simulation, 1 / 60f).Length();
        Check(nearForce > farForce && farForce == 0, "force should fall off to zero outside radius");
    }

    private static void DefaultLightPower()
    {
        var simulation = new SimulationEngine(new SimulationSettings());
        LightSource light = simulation.Lights[0];
        Check(light.Power == 1f && light.AttractionStrength == LightSource.DefaultAttractionStrength
            && light.InfluenceRadius == LightSource.DefaultInfluenceRadius
            && light.VisualIntensity == LightSource.DefaultVisualIntensity, "default power should produce default values");
    }

    private static void PowerScalesAttributes()
    {
        var simulation = new SimulationEngine(new SimulationSettings());
        LightSource light = simulation.Lights[0];
        light.SetPower(1.5f);
        Check(light.AttractionStrength > LightSource.DefaultAttractionStrength
            && light.InfluenceRadius > LightSource.DefaultInfluenceRadius
            && light.VisualIntensity > LightSource.DefaultVisualIntensity, "higher power should increase all attributes");
        light.SetPower(0.5f);
        Check(light.AttractionStrength < LightSource.DefaultAttractionStrength
            && light.InfluenceRadius < LightSource.DefaultInfluenceRadius
            && light.VisualIntensity < LightSource.DefaultVisualIntensity, "lower power should decrease all attributes");
    }

    private static void PowerIsIndependent()
    {
        var simulation = new SimulationEngine(new SimulationSettings());
        LightSource first = simulation.Lights[0];
        LightSource second = simulation.AddLight();
        first.SetPower(0.5f);
        second.SetPower(1.5f);
        Check(first.Power == 0.5f && second.Power == 1.5f && first.VisualIntensity != second.VisualIntensity, "each light should retain its own power");
    }

    private static void NewLightHasDefaultPower()
    {
        var simulation = new SimulationEngine(new SimulationSettings());
        LightSource light = simulation.AddLight();
        Check(light.Power == 1f && light.AttractionStrength == LightSource.DefaultAttractionStrength, "new light should use default power");
    }

    private static void ResetRestoresLightPower()
    {
        var simulation = new SimulationEngine(new SimulationSettings());
        simulation.Lights[0].SetPower(1.75f);
        simulation.AddLight().SetPower(0.25f);
        simulation.Reset();
        Check(simulation.Lights.Count == 1 && simulation.Lights[0].Power == 1f, "reset should restore one default-power light");
    }

    private static void MultipleLightForcesCombine()
    {
        var simulation = new SimulationEngine(new SimulationSettings());
        simulation.Lights[0].Position = new Vector2(10, 0);
        simulation.Lights[0].InfluenceRadius = 100;
        LightSource second = simulation.AddLight(new Vector2(-10, 0));
        second.InfluenceRadius = 100;
        var agent = new Agent(Vector2.Zero, Vector2.UnitY, 1, 1, 1);
        Vector2 force = new LightAttractionBehavior().CalculateForce(agent, simulation, 1 / 60f);
        Check(force.Length() < 0.001f, "equal opposite light forces should cancel");
    }

    private static void IndependentLightRadius()
    {
        var simulation = new SimulationEngine(new SimulationSettings());
        simulation.Lights[0].Position = new Vector2(10, 0);
        simulation.Lights[0].InfluenceRadius = 20;
        LightSource second = simulation.AddLight(new Vector2(100, 0));
        second.InfluenceRadius = 20;
        var agent = new Agent(Vector2.Zero, Vector2.UnitY, 1, 1, 1);
        Vector2 force = new LightAttractionBehavior().CalculateForce(agent, simulation, 1 / 60f);
        Check(force.X > 0, "a light inside its radius should still influence the agent");
    }

    private static void AddRemoveLights()
    {
        var simulation = new SimulationEngine(new SimulationSettings());
        simulation.AddLight();
        Check(simulation.Lights.Count == 2, "adding a light should increase the count");
        simulation.RemoveLight(1);
        Check(simulation.Lights.Count == 1, "removing a light should decrease the count");
    }

    private static void NearestLightSelection()
    {
        var simulation = new SimulationEngine(new SimulationSettings());
        simulation.Lights[0].Position = new Vector2(100, 100);
        simulation.AddLight(new Vector2(120, 100));
        Check(simulation.FindClosestLight(new Vector2(118, 100), 16) == 1, "selection should choose the closest light");
    }

    private static void LightPositionPersists()
    {
        var simulation = new SimulationEngine(new SimulationSettings());
        simulation.Lights[0].Position = new Vector2(123, 234);
        simulation.Update(1 / 60f);
        Check(simulation.Lights[0].Position == new Vector2(123, 234), "dragged simulation-space position should persist");
    }

    private static void FpsMeasurement()
    {
        var counter = new FpsCounter();
        for (int i = 0; i < 30; i++) counter.Update(1 / 60d);
        Check(counter.Value > 0 && counter.Value < 1000, "FPS measurement should be positive and bounded");
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
        simulation.Lights[0].Position = simulation.Agents[0].Position - Vector2.Normalize(simulation.Agents[0].Velocity) * 100;
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
