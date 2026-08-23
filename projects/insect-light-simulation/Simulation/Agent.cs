using System.Numerics;

namespace InsectLightSimulation.Simulation;

public sealed class Agent
{
    public Vector2 Position;
    public Vector2 Velocity;
    public Vector2 Acceleration;
    public float Heading;
    public readonly float PreferredSpeed;
    public readonly float TurnResponsiveness;
    public readonly float WanderStrength;
    public float WanderAngle;
    public float AnimationPhase;
    public readonly float WingAnimationSpeed;

    public Agent(Vector2 position, Vector2 velocity, float preferredSpeed, float turnResponsiveness, float wanderStrength,
        float animationPhase = 0, float wingAnimationSpeed = 8)
    {
        Position = position;
        Velocity = velocity;
        Heading = MathF.Atan2(velocity.Y, velocity.X);
        PreferredSpeed = preferredSpeed;
        TurnResponsiveness = turnResponsiveness;
        WanderStrength = wanderStrength;
        AnimationPhase = animationPhase;
        WingAnimationSpeed = wingAnimationSpeed;
    }
}
