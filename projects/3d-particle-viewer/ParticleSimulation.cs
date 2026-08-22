using System.Windows.Media.Media3D;

namespace ParticleModelViewer;

public sealed class ParticleSimulation
{
    private readonly List<Point3D> positions = [];
    private readonly List<Vector3D> velocities = [];

    public IReadOnlyList<Point3D> Positions => positions;

    public void Reset(IReadOnlyList<Point3D> restPositions)
    {
        positions.Clear();
        velocities.Clear();
        positions.AddRange(restPositions);
        velocities.AddRange(Enumerable.Repeat(new Vector3D(), restPositions.Count));
    }

    public void Step(double timeStep, double strength, double damping, bool groundEnabled, double groundHeight, double particleRadius)
    {
        if (positions.Count == 0) return;
        var gravity = new Vector3D(0, -4.2, 0);
        var velocityDamping = Math.Clamp(damping, 0.75, 0.999);
        for (var i = 0; i < positions.Count; i++)
        {
            var rest = positions[i];
            var current = positions[i];
            var restoringForce = rest - current;
            var acceleration = restoringForce * Math.Clamp(strength, 0, 20) + gravity;
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
