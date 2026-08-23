using System.Numerics;
using InsectLightSimulation.Animation;
using InsectLightSimulation.Simulation.Behaviors;

namespace InsectLightSimulation.Simulation;

public sealed class SimulationEngine
{
    public const float MaximumSpeed = 240f;
    private readonly List<IBehavior> behaviors = new();
    private readonly List<Agent> agents = new();
    private readonly List<LightSource> lights = new();
    private int seed;

    public SimulationSettings Settings { get; }
    public IReadOnlyList<LightSource> Lights => lights;
    public IReadOnlyList<Agent> Agents => agents;
    public Random Random { get; private set; } = new(42);
    public float Width { get; private set; } = 900;
    public float Height { get; private set; } = 600;
    public double SimulationTime { get; private set; }
    public const int MaxLights = 16;

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
        if (lights.Count == 0)
            AddLight(new Vector2(Width * 0.5f, Height * 0.48f));
        else
            foreach (LightSource light in lights)
                light.Position = new Vector2(Math.Clamp(light.Position.X, 0, Width), Math.Clamp(light.Position.Y, 0, Height));
    }

    public void SetSeed(int value) => seed = value;

    public void Reset()
    {
        Random = new Random(seed);
        agents.Clear();
        lights.Clear();
        AddLight(new Vector2(Width * 0.5f, Height * 0.48f));
        SimulationTime = 0;
        for (int i = 0; i < Settings.InsectCount; i++)
        {
            float angle = Random.NextSingle() * MathF.Tau;
            Vector2 velocity = new(MathF.Cos(angle), MathF.Sin(angle));
            velocity *= Settings.BaseSpeed * (0.55f + Random.NextSingle() * 0.65f);
            Vector2 position = new(Random.NextSingle() * Width, Random.NextSingle() * Height);
            agents.Add(new Agent(position, velocity, 0.75f + Random.NextSingle() * 0.5f,
                0.75f + Random.NextSingle() * 0.5f, 0.7f + Random.NextSingle() * 0.6f,
                Random.NextSingle() * WingAnimation.FrameCount, 7.2f + Random.NextSingle() * 2.4f));
        }
    }

    public LightSource AddLight(Vector2? position = null)
    {
        if (lights.Count >= MaxLights) return lights[^1];
        int id = 1;
        for (int i = 0; i < lights.Count; i++)
            id = Math.Max(id, lights[i].Id + 1);
        Vector2 newPosition = position ?? NewLightPosition(lights.Count);
        return AddLight(id, newPosition);
    }

    public int RemoveLight(int index)
    {
        if (lights.Count <= 1 || index < 0 || index >= lights.Count) return Math.Clamp(index, 0, lights.Count - 1);
        lights.RemoveAt(index);
        return Math.Min(index, lights.Count - 1);
    }

    public int FindClosestLight(Vector2 position, float selectionRadius)
    {
        int closestIndex = -1;
        float closestDistanceSquared = selectionRadius * selectionRadius;
        for (int i = 0; i < lights.Count; i++)
        {
            float distanceSquared = Vector2.DistanceSquared(position, lights[i].Position);
            if (distanceSquared <= closestDistanceSquared)
            {
                closestDistanceSquared = distanceSquared;
                closestIndex = i;
            }
        }
        return closestIndex;
    }

    private LightSource AddLight(int id, Vector2 position)
    {
        var light = new LightSource(id, position, LightSource.DefaultAttractionStrength,
            LightSource.DefaultInfluenceRadius, LightSource.DefaultVisualIntensity);
        light.SetPower(1f);
        lights.Add(light);
        return light;
    }

    private Vector2 NewLightPosition(int index)
    {
        float angle = index * 1.7f;
        float radius = Math.Min(Width, Height) * 0.22f;
        return new Vector2(Width * 0.5f + MathF.Cos(angle) * radius, Height * 0.48f + MathF.Sin(angle) * radius);
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
            float desiredSpeed = CalculateDesiredSpeed(agent);
            if (desiredVelocity.LengthSquared() > 0.001f)
            {
                float currentAngle = MathF.Atan2(agent.Velocity.Y, agent.Velocity.X);
                float targetAngle = MathF.Atan2(desiredVelocity.Y, desiredVelocity.X);
                float turn = WrapAngle(targetAngle - currentAngle);
                float maxTurn = Settings.TurnRate * agent.TurnResponsiveness * deltaTime;
                float newAngle = currentAngle + Math.Clamp(turn, -maxTurn, maxTurn);
                agent.Heading = newAngle;
                float currentSpeed = agent.Velocity.Length();
                float maximumSpeedChange = Math.Max(40f, Settings.BaseSpeed * 2.5f) * deltaTime;
                float newSpeed = currentSpeed + Math.Clamp(desiredSpeed - currentSpeed, -maximumSpeedChange, maximumSpeedChange);
                agent.Velocity = new Vector2(MathF.Cos(newAngle), MathF.Sin(newAngle)) * newSpeed;
            }

            agent.Position += agent.Velocity * deltaTime;
            ApplyBoundary(agent);
            agent.AnimationPhase = WingAnimation.AdvancePhase(agent.AnimationPhase, agent.WingAnimationSpeed, deltaTime);
        }
        SimulationTime += deltaTime;
    }

    public float CalculateDesiredSpeed(Agent agent)
    {
        float strongestApproach = 0;
        float currentSpeed = agent.Velocity.Length();
        Vector2 velocityDirection = currentSpeed > 0.001f ? agent.Velocity / currentSpeed : Vector2.Zero;
        for (int i = 0; i < lights.Count; i++)
        {
            LightSource light = lights[i];
            Vector2 offset = light.Position - agent.Position;
            float distance = offset.Length();
            if (distance < 0.001f || distance >= light.InfluenceRadius) continue;
            Vector2 toLight = offset / distance;
            float proximity = 1f - distance / light.InfluenceRadius;
            float smoothProximity = proximity * proximity * (3f - 2f * proximity);
            float alignment = Vector2.Dot(velocityDirection, toLight);
            float approaching = Math.Clamp(alignment, 0, 1);
            float powerInfluence = Math.Clamp(light.AttractionStrength / LightSource.DefaultAttractionStrength, 0, 2f);
            strongestApproach = Math.Max(strongestApproach, smoothProximity * approaching * powerInfluence);
        }

        float baseSpeed = Settings.BaseSpeed * agent.PreferredSpeed;
        float desiredSpeed = baseSpeed * (1f + 0.75f * strongestApproach);
        return Math.Clamp(desiredSpeed, 18f, MaximumSpeed);
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

    public float AverageSpeed
    {
        get
        {
            if (agents.Count == 0) return 0;
            float total = 0;
            for (int i = 0; i < agents.Count; i++)
                total += agents[i].Velocity.Length();
            return total / agents.Count;
        }
    }

    private static float WrapAngle(float angle)
    {
        while (angle > MathF.PI) angle -= MathF.Tau;
        while (angle < -MathF.PI) angle += MathF.Tau;
        return angle;
    }
}
