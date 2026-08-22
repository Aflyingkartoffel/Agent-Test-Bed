using System.Windows.Media.Media3D;

namespace ParticleModelViewer;

public sealed class ParticleSimulation
{
    private readonly List<Point3D> restPositions = [];
    private readonly List<Point3D> positions = [];
    private readonly List<Vector3D> velocities = [];
    private readonly List<Vector3D> forces = [];
    private readonly List<Spring> springs = [];
    private Point3D restCenter;
    private double restHeight;
    private double simulationTime;

    private readonly record struct Spring(int A, int B, double RestLength, double Stiffness, double Damping);

    public IReadOnlyList<Point3D> Positions => positions;
    public int SpringCount => springs.Count;

    public void Reset(IReadOnlyList<Point3D> newRestPositions) => Reset(newRestPositions, 0, 0);

    public void Reset(IReadOnlyList<Point3D> newRestPositions, double dropHeight, double momentum)
    {
        restPositions.Clear();
        positions.Clear();
        velocities.Clear();
        forces.Clear();
        springs.Clear();
        simulationTime = 0;
        restPositions.AddRange(newRestPositions);
        foreach (var point in newRestPositions)
        {
            positions.Add(new Point3D(point.X, point.Y + Math.Max(0, dropHeight), point.Z));
            velocities.Add(new Vector3D(0, -Math.Clamp(momentum, 0, 1) * 3.5, 0));
        }
        forces.AddRange(Enumerable.Repeat(new Vector3D(), newRestPositions.Count));
        CalculateRestShape();
        BuildSpringGraph();
    }

    public void Step(double timeStep, double strength, double deformationResistance, double elasticity, double springStiffness, double bounce, double damping, double chaos, bool groundEnabled, double groundHeight, double particleRadius)
    {
        if (positions.Count == 0) return;
        var substeps = 2;
        var subTimeStep = Math.Clamp(timeStep, 0.005, 0.033) / substeps;
        var boundedResistance = Math.Clamp(deformationResistance, 0, 1);
        var boundedElasticity = Math.Clamp(elasticity, 0, 1);
        var boundedSpringStiffness = Math.Clamp(springStiffness, 0, 1);
        var boundedBounce = Math.Clamp(bounce, 0, 0.8);
        var velocityDamping = Math.Clamp(damping, 0.75, 0.999);
        var gravity = new Vector3D(0, -4.2, 0);
        var shapeStrength = Math.Clamp(strength, 0, 20) * boundedResistance;
        var springStrength = 18 + boundedSpringStiffness * 72;
        var springDamping = 0.8 + boundedSpringStiffness * 1.8;

        for (var substep = 0; substep < substeps; substep++)
        {
            CalculateCenterAndBounds(out var currentCenter, out var currentMinY, out var currentMaxY);
            var currentHeight = currentMaxY - currentMinY;
            var compression = groundEnabled && restHeight > 0.0001 && currentMinY <= groundHeight + particleRadius + 0.04
                ? Math.Clamp((restHeight - currentHeight) / restHeight, 0, 0.55)
                : 0;
            var lateralScale = 1 + compression * boundedElasticity * 0.28;

            for (var i = 0; i < forces.Count; i++)
            {
                var rest = restPositions[i];
                var current = positions[i];
                var translatedRest = new Point3D(
                    rest.X + currentCenter.X - restCenter.X,
                    rest.Y + currentCenter.Y - restCenter.Y,
                    rest.Z + currentCenter.Z - restCenter.Z);
                var shapeForce = (translatedRest - current) * shapeStrength;
                var centerForce = new Vector3D(restCenter.X - currentCenter.X, 0, restCenter.Z - currentCenter.Z) * (shapeStrength * 0.35);
                var lateralTarget = new Point3D(
                    currentCenter.X + (rest.X - restCenter.X) * lateralScale,
                    currentCenter.Y + (rest.Y - restCenter.Y),
                    currentCenter.Z + (rest.Z - restCenter.Z) * lateralScale);
                var volumeForce = new Vector3D(lateralTarget.X - current.X, 0, lateralTarget.Z - current.Z) * (compression * boundedElasticity * 4);
                var phase = simulationTime * (1.2 + (i % 7) * 0.11) + i * 0.73;
                var disturbance = new Vector3D(Math.Sin(phase), Math.Cos(phase * 0.83), Math.Sin(phase * 1.17)) * (Math.Clamp(chaos, 0, 1) * 2.2);
                forces[i] = gravity + shapeForce + centerForce + volumeForce + disturbance;
            }

            foreach (var spring in springs)
            {
                var delta = positions[spring.B] - positions[spring.A];
                var length = delta.Length;
                if (length < 0.000001) continue;
                var direction = delta / length;
                var relativeVelocity = velocities[spring.B] - velocities[spring.A];
                var extension = length - spring.RestLength;
                var forceMagnitude = extension * springStrength * spring.Stiffness + Vector3D.DotProduct(relativeVelocity, direction) * springDamping * spring.Damping;
                forceMagnitude = Math.Clamp(forceMagnitude, -45, 45);
                var springForce = direction * forceMagnitude;
                forces[spring.A] += springForce;
                forces[spring.B] -= springForce;
            }

            for (var i = 0; i < positions.Count; i++)
            {
                var velocity = (velocities[i] + forces[i] * subTimeStep) * velocityDamping;
                var next = positions[i] + velocity * subTimeStep;
                if (groundEnabled && next.Y - particleRadius < groundHeight)
                {
                    next.Y = groundHeight + particleRadius;
                    if (velocity.Y < 0) velocity.Y = -velocity.Y * boundedBounce;
                    var friction = 0.12;
                    velocity.X *= 1 - friction;
                    velocity.Z *= 1 - friction;
                }
                positions[i] = next;
                velocities[i] = velocity;
            }
            simulationTime += subTimeStep;
        }
    }

    private void CalculateRestShape()
    {
        if (restPositions.Count == 0)
        {
            restCenter = new Point3D();
            restHeight = 0;
            return;
        }
        var minY = double.MaxValue;
        var maxY = double.MinValue;
        var centerX = 0d; var centerY = 0d; var centerZ = 0d;
        foreach (var point in restPositions)
        {
            centerX += point.X; centerY += point.Y; centerZ += point.Z;
            minY = Math.Min(minY, point.Y); maxY = Math.Max(maxY, point.Y);
        }
        restCenter = new Point3D(centerX / restPositions.Count, centerY / restPositions.Count, centerZ / restPositions.Count);
        restHeight = maxY - minY;
    }

    private void CalculateCenterAndBounds(out Point3D center, out double minY, out double maxY)
    {
        var centerX = 0d; var centerY = 0d; var centerZ = 0d;
        minY = double.MaxValue; maxY = double.MinValue;
        foreach (var point in positions)
        {
            centerX += point.X; centerY += point.Y; centerZ += point.Z;
            minY = Math.Min(minY, point.Y); maxY = Math.Max(maxY, point.Y);
        }
        center = new Point3D(centerX / positions.Count, centerY / positions.Count, centerZ / positions.Count);
    }

    private void BuildSpringGraph()
    {
        if (restPositions.Count < 2) return;
        var min = restPositions[0]; var max = restPositions[0];
        foreach (var point in restPositions)
        {
            min.X = Math.Min(min.X, point.X); min.Y = Math.Min(min.Y, point.Y); min.Z = Math.Min(min.Z, point.Z);
            max.X = Math.Max(max.X, point.X); max.Y = Math.Max(max.Y, point.Y); max.Z = Math.Max(max.Z, point.Z);
        }
        var largestDimension = Math.Max(max.X - min.X, Math.Max(max.Y - min.Y, max.Z - min.Z));
        var searchRadius = Math.Clamp(largestDimension * (3.2 / Math.Sqrt(restPositions.Count)), largestDimension * 0.035, largestDimension * 0.25);
        var cells = new Dictionary<(int X, int Y, int Z), List<int>>();
        for (var i = 0; i < restPositions.Count; i++)
        {
            var cell = GetCell(restPositions[i], searchRadius);
            if (!cells.TryGetValue(cell, out var members)) cells[cell] = members = [];
            members.Add(i);
        }

        var edges = new HashSet<long>();
        for (var i = 0; i < restPositions.Count; i++)
        {
            var point = restPositions[i];
            var candidates = new List<(int Index, double Distance)>();
            var cell = GetCell(point, searchRadius);
            for (var x = cell.X - 1; x <= cell.X + 1; x++)
            for (var y = cell.Y - 1; y <= cell.Y + 1; y++)
            for (var z = cell.Z - 1; z <= cell.Z + 1; z++)
            {
                if (!cells.TryGetValue((x, y, z), out var members)) continue;
                foreach (var j in members)
                {
                    if (j == i) continue;
                    var distance = (restPositions[j] - point).Length;
                    if (distance <= searchRadius * 1.35) candidates.Add((j, distance));
                }
            }
            candidates.Sort((left, right) => left.Distance.CompareTo(right.Distance));
            var connections = Math.Min(8, candidates.Count);
            for (var candidateIndex = 0; candidateIndex < connections; candidateIndex++)
            {
                var j = candidates[candidateIndex].Index;
                var a = Math.Min(i, j); var b = Math.Max(i, j);
                var key = ((long)a << 32) | (uint)b;
                if (edges.Add(key)) springs.Add(new Spring(a, b, candidates[candidateIndex].Distance, 1, 1));
            }
        }
    }

    private static (int X, int Y, int Z) GetCell(Point3D point, double cellSize) =>
        ((int)Math.Floor(point.X / cellSize), (int)Math.Floor(point.Y / cellSize), (int)Math.Floor(point.Z / cellSize));
}
