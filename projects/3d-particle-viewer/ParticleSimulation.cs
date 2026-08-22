using System.Windows.Media.Media3D;

namespace ParticleModelViewer;

public sealed class ParticleSimulation
{
    private readonly List<Point3D> restPositions = [];
    private readonly List<Point3D> positions = [];
    private readonly List<Vector3D> velocities = [];

    public IReadOnlyList<Point3D> Positions => positions;

    public void Reset(IReadOnlyList<Point3D> newRestPositions)
    {
        restPositions.Clear();
        positions.Clear();
        velocities.Clear();
        restPositions.AddRange(newRestPositions);
        positions.AddRange(newRestPositions);
        velocities.AddRange(Enumerable.Repeat(new Vector3D(), newRestPositions.Count));
    }

    public void Step(double timeStep, double strength, double deformationResistance, double damping, bool groundEnabled, double groundHeight, double particleRadius)
    {
        if (positions.Count == 0) return;
        var gravity = new Vector3D(0, -4.2, 0);
        var velocityDamping = Math.Clamp(damping, 0.75, 0.999);
        for (var i = 0; i < positions.Count; i++)
        {
            var rest = restPositions[i];
            var current = positions[i];
            var restoringForce = rest - current;
            var restoringStrength = Math.Clamp(strength, 0, 20) * Math.Clamp(deformationResistance, 0, 1);
            var acceleration = restoringForce * restoringStrength + gravity;
            var velocity = (velocities[i] + acceleration * timeStep) * velocityDamping;
            var next = current + velocity * timeStep;

            if (groundEnabled && next.Y - particleRadius < groundHeight)
            {
                next.Y = groundHeight + particleRadius;
                if (velocity.Y < 0) velocity.Y *= -0.2;
                velocity.X *= 0.92;
                velocity.Z *= 0.92;
            }

            positions[i] = next;
            velocities[i] = velocity;
        }
    }
}
