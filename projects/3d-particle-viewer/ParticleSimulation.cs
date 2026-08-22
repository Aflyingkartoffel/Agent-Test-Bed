using System.Windows.Media.Media3D;

namespace ParticleModelViewer;

public sealed class ParticleSimulation
{
    private readonly List<Point3D> restPositions = [];
    private readonly List<Point3D> positions = [];
    private readonly List<Vector3D> velocities = [];
    private Point3D restCenter;
    private double restHeight;

    public IReadOnlyList<Point3D> Positions => positions;

    public void Reset(IReadOnlyList<Point3D> newRestPositions)
    {
        restPositions.Clear();
        positions.Clear();
        velocities.Clear();
        restPositions.AddRange(newRestPositions);
        positions.AddRange(newRestPositions);
        velocities.AddRange(Enumerable.Repeat(new Vector3D(), newRestPositions.Count));
        if (newRestPositions.Count > 0)
        {
            restCenter = new Point3D(newRestPositions.Average(point => point.X), newRestPositions.Average(point => point.Y), newRestPositions.Average(point => point.Z));
            restHeight = newRestPositions.Max(point => point.Y) - newRestPositions.Min(point => point.Y);
        }
        else
        {
            restCenter = new Point3D();
            restHeight = 0;
        }
    }

    public void Step(double timeStep, double strength, double deformationResistance, double elasticity, double damping, bool groundEnabled, double groundHeight, double particleRadius)
    {
        if (positions.Count == 0) return;
        timeStep = Math.Clamp(timeStep, 0.005, 0.033);
        var gravity = new Vector3D(0, -4.2, 0);
        var velocityDamping = Math.Clamp(damping, 0.75, 0.999);
        var currentMinY = positions.Min(point => point.Y);
        var currentMaxY = positions.Max(point => point.Y);
        var currentHeight = currentMaxY - currentMinY;
        var contactCompression = groundEnabled && restHeight > 0.0001 && currentMinY <= groundHeight + particleRadius + 0.03
            ? Math.Clamp((restHeight - currentHeight) / restHeight, 0, 0.6)
            : 0;
        var boundedElasticity = Math.Clamp(elasticity, 0, 1);
        var lateralScale = 1 + contactCompression * boundedElasticity * 0.35;
        for (var i = 0; i < positions.Count; i++)
        {
            var rest = restPositions[i];
            var current = positions[i];
            var restoringForce = rest - current;
            var restoringStrength = Math.Clamp(strength, 0, 20) * Math.Clamp(deformationResistance, 0, 1);
            var lateralTarget = new Point3D(
                restCenter.X + (rest.X - restCenter.X) * lateralScale,
                rest.Y,
                restCenter.Z + (rest.Z - restCenter.Z) * lateralScale);
            var lateralForce = new Vector3D(lateralTarget.X - current.X, 0, lateralTarget.Z - current.Z) * Math.Clamp(contactCompression * boundedElasticity * 6, 0, 3);
            var elasticRecovery = restoringForce * (contactCompression * boundedElasticity * 0.75);
            var acceleration = restoringForce * restoringStrength + elasticRecovery + lateralForce + gravity;
            var velocity = (velocities[i] + acceleration * timeStep) * velocityDamping;
            var next = current + velocity * timeStep;

            if (groundEnabled && next.Y - particleRadius < groundHeight)
            {
                next.Y = groundHeight + particleRadius;
                if (velocity.Y < 0) velocity.Y *= 0.05 + boundedElasticity * 0.75;
                velocity.X *= 0.92;
                velocity.Z *= 0.92;
            }

            positions[i] = next;
            velocities[i] = velocity;
        }
    }
}
