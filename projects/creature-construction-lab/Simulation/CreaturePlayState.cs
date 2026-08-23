using System.Numerics;
using CreatureConstructionLab.Editor;
using CreatureConstructionLab.Model;

namespace CreatureConstructionLab.Simulation;

public sealed class CreaturePlayState
{
    public const float FixedStep = 1f / 60f;
    public List<Vector2> Positions { get; } = [];
    public List<Vector2> Velocities { get; } = [];
    public List<Vector2> Accelerations { get; } = [];
    public Vector2 TargetPosition { get; private set; }
    public float MaxSpeed { get; set; } = 720;
    public float AccelerationStrength { get; set; } = 420;
    public float Damping { get; set; } = 3.5f;
    public float SimulationSpeed { get; set; } = 2;
    public float MaxBendDegrees { get; set; } = 75;
    public float BendStiffness { get; set; } = 0.25f;
    public WaveMotionSettings Wave { get; } = new();
    public bool Paused { get; private set; }
    public float SimulationTime { get; private set; }
    private float accumulator;

    public void ResetSettings()
    {
        MaxSpeed = 720;
        AccelerationStrength = 420;
        Damping = 3.5f;
        SimulationSpeed = 2;
        MaxBendDegrees = 75;
        BendStiffness = 0.25f;
        Wave.Enabled = false;
        Wave.Amplitude = 4;
        Wave.Frequency = 1.2f;
        Wave.Phase = 2.8f;
        Wave.Influence = 0.75f;
    }

    public void ResetFromDefinition(CreatureDefinition definition)
    {
        Positions.Clear();
        Velocities.Clear();
        Accelerations.Clear();
        Positions.AddRange(definition.Nodes.Select(n => n.Position));
        Velocities.AddRange(Enumerable.Repeat(Vector2.Zero, Positions.Count));
        Accelerations.AddRange(Enumerable.Repeat(Vector2.Zero, Positions.Count));
        TargetPosition = Positions.Count > 0 ? Positions[0] : Vector2.Zero;
        SimulationTime = 0;
        accumulator = 0;
    }

    public void SetTarget(Vector2 target) { if (float.IsFinite(target.X) && float.IsFinite(target.Y)) TargetPosition = target; }
    public void SetPaused(bool paused) => Paused = paused;

    public void Advance(CreatureDefinition definition, float elapsedSeconds)
    {
        if (Paused || Positions.Count == 0 || !float.IsFinite(elapsedSeconds)) return;
        accumulator += Math.Clamp(elapsedSeconds, 0, 0.1f) * Math.Clamp(SimulationSpeed, 0.1f, 4);
        while (accumulator + 0.000001f >= FixedStep)
        {
            Step(definition, FixedStep);
            accumulator -= FixedStep;
        }
    }

    public void Step(CreatureDefinition definition, float dt)
    {
        if (Positions.Count == 0 || dt <= 0) return;
        var rootDelta = TargetPosition - Positions[0];
        var desiredVelocity = rootDelta.LengthSquared() < 16 ? Vector2.Zero : Vector2.Normalize(rootDelta) * MaxSpeed;
        var steering = desiredVelocity - Velocities[0];
        var maxChange = Math.Max(1, AccelerationStrength) * dt;
        if (steering.LengthSquared() > maxChange * maxChange) steering = Vector2.Normalize(steering) * maxChange;
        Accelerations[0] = steering / dt;
        Velocities[0] += steering;
        Velocities[0] *= MathF.Exp(-Math.Clamp(Damping, 0, 20) * dt);
        if (Velocities[0].LengthSquared() > MaxSpeed * MaxSpeed) Velocities[0] = Vector2.Normalize(Velocities[0]) * MaxSpeed;
        Positions[0] += Velocities[0] * dt;

        SolveDistanceConstraints(definition, dt);
        ApplyWave(definition);
        for (var iteration = 0; iteration < 6; iteration++)
        {
            SolveDistanceConstraints(definition, dt);
            ApplySoftBendConstraint(definition);
            SolveDistanceConstraints(definition, dt);
        }
        ReapplyRestLengths(definition);
        UpdateConstraintVelocities(definition, dt);
        SimulationTime += dt;
    }

    private void SolveDistanceConstraints(CreatureDefinition definition, float dt)
    {
        for (var i = 1; i < Positions.Count; i++)
        {
            var parent = Positions[i - 1];
            var offset = Positions[i] - parent;
            var fallback = ChainMath.GetDirectionFromRotation(definition.Nodes[Math.Min(i - 1, definition.Nodes.Count - 1)].Rotation);
            var direction = offset.LengthSquared() < 0.0001f ? fallback : Vector2.Normalize(offset);
            var connection = i - 1 < definition.Connections.Count ? definition.Connections[i - 1] : null;
            var restLength = connection?.RestLength ?? definition.ChainSettings.Spacing;
            var desired = parent + direction * restLength;
            var stiffness = Math.Clamp(connection?.Stiffness ?? 1, 0.1f, 1);
            Positions[i] = Vector2.Lerp(Positions[i], desired, 1 - MathF.Pow(1 - stiffness, 6));
            Velocities[i] = (Positions[i] - parent) / dt;
            Velocities[i] *= MathF.Exp(-Math.Clamp(connection?.Damping ?? 0.1f, 0, 20) * dt);
            Accelerations[i] = Vector2.Zero;
        }
    }

    private void ApplySoftBendConstraint(CreatureDefinition definition)
    {
        if (Positions.Count < 3) return;
        var limit = Math.Clamp(MaxBendDegrees, 5, 135) * MathF.PI / 180;
        var stiffness = Math.Clamp(BendStiffness, 0.05f, 0.5f);
        for (var i = 1; i < Positions.Count - 1; i++)
        {
            var incomingVector = Positions[i] - Positions[i - 1];
            var outgoingVector = Positions[i + 1] - Positions[i];
            if (incomingVector.LengthSquared() < 0.0001f || outgoingVector.LengthSquared() < 0.0001f) continue;
            var incoming = Vector2.Normalize(incomingVector);
            var outgoing = Vector2.Normalize(outgoingVector);
            var angle = SignedAngle(incoming, outgoing);
            var excess = Math.Abs(angle) - limit;
            if (excess <= 0) continue;

            // Keep the signed bend direction and apply only part of the excess this pass.
            var correction = MathF.CopySign(excess * stiffness, angle);
            var upstreamShare = i <= 1 ? 0.15f : 0.3f;
            var incomingCorrection = correction * upstreamShare;
            var outgoingCorrection = correction - incomingCorrection;
            var parent = Positions[i - 1];
            var joint = parent + Rotate(incoming, incomingCorrection) * incomingVector.Length();
            Positions[i] = joint;
            Positions[i + 1] = joint + Rotate(outgoing, -outgoingCorrection) * outgoingVector.Length();
        }
    }

    private void ReapplyRestLengths(CreatureDefinition definition)
    {
        for (var i = 1; i < Positions.Count; i++)
        {
            var delta = Positions[i] - Positions[i - 1];
            if (delta.LengthSquared() < 0.0001f) continue;
            var restLength = i - 1 < definition.Connections.Count ? definition.Connections[i - 1].RestLength : definition.ChainSettings.Spacing;
            Positions[i] = Positions[i - 1] + Vector2.Normalize(delta) * restLength;
        }
    }

    private void UpdateConstraintVelocities(CreatureDefinition definition, float dt)
    {
        for (var i = 1; i < Positions.Count; i++)
        {
            var parent = Positions[i - 1];
            var delta = Positions[i] - parent;
            if (delta.LengthSquared() < 0.0001f) continue;
            var constrainedVelocity = delta / dt;
            Velocities[i] = Vector2.Lerp(Velocities[i], constrainedVelocity, 0.35f);
            var connection = i - 1 < definition.Connections.Count ? definition.Connections[i - 1] : null;
            Velocities[i] *= MathF.Exp(-Math.Clamp(connection?.Damping ?? 0.1f, 0, 20) * dt);
        }
    }

    private static Vector2 Rotate(Vector2 vector, float radians) => new(vector.X * MathF.Cos(radians) - vector.Y * MathF.Sin(radians), vector.X * MathF.Sin(radians) + vector.Y * MathF.Cos(radians));

    private static float SignedAngle(Vector2 from, Vector2 to) => MathF.Atan2(from.X * to.Y - from.Y * to.X, Vector2.Dot(from, to));

    private void ApplyWave(CreatureDefinition definition)
    {
        if (!Wave.Enabled || Positions.Count < 2) return;
        for (var i = 1; i < Positions.Count; i++)
        {
            var parent = Positions[i - 1];
            var current = Positions[i];
            var forward = parent - current;
            if (forward.LengthSquared() < 0.0001f) forward = -ChainMath.GetDirectionFromRotation(definition.Nodes[i - 1].Rotation);
            forward = Vector2.Normalize(forward);
            var normal = new Vector2(-forward.Y, forward.X);
            var t = (float)i / (Positions.Count - 1);
            var waveOffset = BodyWaveGenerator.CalculateOffset(SimulationTime, t, normal, Wave, definition.ChainSettings.Spacing);
            var connection = i - 1 < definition.Connections.Count ? definition.Connections[i - 1] : null;
            var restLength = connection?.RestLength ?? definition.ChainSettings.Spacing;
            var candidate = current + waveOffset;
            var direction = candidate - parent;
            if (direction.LengthSquared() < 0.0001f) direction = -forward;
            Positions[i] = parent + Vector2.Normalize(direction) * restLength;
            Velocities[i] = (Positions[i] - parent) / FixedStep;
        }
    }
}
