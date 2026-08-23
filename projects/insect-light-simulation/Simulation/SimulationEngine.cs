using System.Numerics;
using InsectLightSimulation.Simulation.Behaviors;

namespace InsectLightSimulation.Simulation;

public sealed class SimulationEngine
{
    private readonly List<IBehavior> behaviors = new();
    private readonly List<Agent> agents = new();
    private int seed;

    public SimulationSettings Settings { get; }
    public LightSource Light { get; } = new();
    public IReadOnlyList<Agent> Agents => agents;
    public Random Random { get; private set; } = new(42);
    public float Width { get; private set; } = 900;
    public float Height { get; private set; } = 600;
    public double SimulationTime { get; private set; }

    public SimulationEngine(SimulationSettings settings)
    {
        Settings = settings;
        seed = settings.Seed;
        behaviors.Add(new LightAttractionBehavior());
        behaviors.Add(new WanderBehavior());
        behaviors.Add(new BoundaryBehavior());
        Resize(Width, Height);
        Reset();
    }

    public void Resize(float width, float height)
    {
        Width = Math.Max(1, width);
        Height = Math.Max(1, height);
        Light.Position = new Vector2(Width * 0.5f, Height * 0.48f);
    }

    public void SetSeed(int value) => seed = value;

    public void Reset()
    {
        Random = new Random(seed);
        agents.Clear();
        SimulationTime = 0;
        for (int i = 0; i < Settings.InsectCount; i++)
        {
            float angle = Random.NextSingle() * MathF.Tau;
            Vector2 velocity = new(MathF.Cos(angle), MathF.Sin(angle));
            velocity *= Settings.BaseSpeed * (0.55f + Random.NextSingle() * 0.65f);
            Vector2 position = new(Random.NextSingle() * Width, Random.NextSingle() * Height);
            agents.Add(new Agent(position, velocity, 0.75f + Random.NextSingle() * 0.5f,
                0.75f + Random.NextSingle() * 0.5f, 0.7f + Random.NextSingle() * 0.6f));
        }
    }

    public void SetAgentCount(int count)
    {
        Settings.InsectCount = Math.Clamp(count, 10, 2000);
        Reset();
    }

    public void Update(float deltaTime)
    {
        deltaTime = Math.Clamp(deltaTime, 0.001f, 0.05f);
        foreach (Agent agent in agents)
        {
            Vector2 totalForce = Vector2.Zero;
            foreach (IBehavior behavior in behaviors)
                totalForce += behavior.CalculateForce(agent, this, deltaTime);

            agent.Acceleration = totalForce;
            Vector2 desiredVelocity = agent.Velocity + agent.Acceleration * deltaTime * 60f;
            float desiredSpeed = Math.Clamp(Settings.BaseSpeed * agent.PreferredSpeed, 18f, 240f);
            if (desiredVelocity.LengthSquared() > 0.001f)
            {
                float currentAngle = MathF.Atan2(agent.Velocity.Y, agent.Velocity.X);
                float targetAngle = MathF.Atan2(desiredVelocity.Y, desiredVelocity.X);
                float turn = WrapAngle(targetAngle - currentAngle);
                float maxTurn = Settings.TurnRate * agent.TurnResponsiveness * deltaTime;
                float newAngle = currentAngle + Math.Clamp(turn, -maxTurn, maxTurn);
                agent.Heading = newAngle;
                agent.Velocity = new Vector2(MathF.Cos(newAngle), MathF.Sin(newAngle)) * desiredSpeed;
            }

            agent.Position += agent.Velocity * deltaTime;
            ApplyBoundary(agent);
        }
        SimulationTime += deltaTime;
    }

    private void ApplyBoundary(Agent agent)
    {
        if (Settings.BoundaryMode == BoundaryMode.Wrap)
        {
            if (agent.Position.X < 0) agent.Position.X += Width;
            if (agent.Position.X >= Width) agent.Position.X -= Width;
            if (agent.Position.Y < 0) agent.Position.Y += Height;
            if (agent.Position.Y >= Height) agent.Position.Y -= Height;
        }
        else
        {
            if (agent.Position.X < 0 || agent.Position.X > Width) agent.Velocity.X *= -1;
            if (agent.Position.Y < 0 || agent.Position.Y > Height) agent.Velocity.Y *= -1;
            agent.Position = new Vector2(Math.Clamp(agent.Position.X, 0, Width), Math.Clamp(agent.Position.Y, 0, Height));
        }
    }

    public float AverageSpeed => agents.Count == 0 ? 0 : agents.Average(agent => agent.Velocity.Length());

    private static float WrapAngle(float angle)
    {
        while (angle > MathF.PI) angle -= MathF.Tau;
        while (angle < -MathF.PI) angle += MathF.Tau;
        return angle;
    }
}
