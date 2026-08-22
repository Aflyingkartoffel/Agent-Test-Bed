using System.Windows.Media.Media3D;

namespace ParticleModelViewer;

public enum ParticleShape
{
    Cube,
    Sphere,
    Tetrahedron,
    Billboard,
    ImageBillboard
}

public static class ParticleGenerator
{
    public static List<Point3D> SampleSurface(ObjModel model, int count)
    {
        if (count <= 0) return [];
        var triangles = BuildSurfaceTriangles(model);
        if (triangles.Count == 0) return [];

        var totalArea = triangles[^1].CumulativeArea;
        var result = new List<Point3D>(count);
        var areaStep = totalArea / count;
        for (var i = 0; i < count; i++)
        {
            // One evenly spaced area target per particle avoids clumps in large faces.
            var target = (i + 0.5) * areaStep;
            var chosen = triangles[FindTriangle(triangles, target)].Triangle;

            var u = Halton(i + 1, 2);
            var v = Halton(i + 1, 3);
            if (u + v > 1) { u = 1 - u; v = 1 - v; }
            var a = model.Vertices[chosen.A];
            var b = model.Vertices[chosen.B];
            var c = model.Vertices[chosen.C];
            result.Add(new Point3D(a.X + u * (b.X - a.X) + v * (c.X - a.X), a.Y + u * (b.Y - a.Y) + v * (c.Y - a.Y), a.Z + u * (b.Z - a.Z) + v * (c.Z - a.Z)));
        }
        return result;
    }

    public static MeshGeometry3D CreateParticleMesh(IEnumerable<Point3D> points, double size, ParticleShape shape)
    {
        var mesh = new MeshGeometry3D();
        foreach (var point in points)
        {
            switch (shape)
            {
                case ParticleShape.Sphere: AddSphere(mesh, point, size); break;
                case ParticleShape.Tetrahedron: AddTetrahedron(mesh, point, size); break;
                default: AddCube(mesh, point, size); break;
            }
        }
        return mesh;
    }

    public static MeshGeometry3D CreateBillboardMesh(IReadOnlyList<Point3D> points, double size, Point3D cameraPosition, double modelYawDegrees)
    {
        var mesh = new MeshGeometry3D();
        var forward = cameraPosition - new Point3D();
        forward.Normalize();
        var right = Vector3D.CrossProduct(new Vector3D(0, 1, 0), forward);
        right.Normalize();
        var up = Vector3D.CrossProduct(forward, right);
        up.Normalize();
        right = RotateY(right, -modelYawDegrees);
        up = RotateY(up, -modelYawDegrees);
        var halfSize = size / 2;

        foreach (var center in points)
        {
            var start = mesh.Positions.Count;
            mesh.Positions.Add(center - right * halfSize - up * halfSize);
            mesh.Positions.Add(center + right * halfSize - up * halfSize);
            mesh.Positions.Add(center + right * halfSize + up * halfSize);
            mesh.Positions.Add(center - right * halfSize + up * halfSize);
            mesh.TextureCoordinates.Add(new System.Windows.Point(0, 1));
            mesh.TextureCoordinates.Add(new System.Windows.Point(1, 1));
            mesh.TextureCoordinates.Add(new System.Windows.Point(1, 0));
            mesh.TextureCoordinates.Add(new System.Windows.Point(0, 0));
            mesh.TriangleIndices.Add(start); mesh.TriangleIndices.Add(start + 1); mesh.TriangleIndices.Add(start + 2);
            mesh.TriangleIndices.Add(start); mesh.TriangleIndices.Add(start + 2); mesh.TriangleIndices.Add(start + 3);
        }
        return mesh;
    }

    public static MeshGeometry3D CreateSurfaceMesh(ObjModel model)
    {
        var mesh = new MeshGeometry3D();
        foreach (var triangle in model.Triangles)
        {
            var start = mesh.Positions.Count;
            mesh.Positions.Add(model.Vertices[triangle.A]);
            mesh.Positions.Add(model.Vertices[triangle.B]);
            mesh.Positions.Add(model.Vertices[triangle.C]);
            mesh.TriangleIndices.Add(start); mesh.TriangleIndices.Add(start + 1); mesh.TriangleIndices.Add(start + 2);
        }
        return mesh;
    }

    private static double Area(ObjModel model, Triangle triangle)
    {
        var ab = model.Vertices[triangle.B] - model.Vertices[triangle.A];
        var ac = model.Vertices[triangle.C] - model.Vertices[triangle.A];
        return Vector3D.CrossProduct(ab, ac).Length / 2;
    }

    private static List<SurfaceTriangle> BuildSurfaceTriangles(ObjModel model)
    {
        var cumulativeArea = 0d;
        var triangles = new List<SurfaceTriangle>(model.Triangles.Count);
        foreach (var triangle in model.Triangles)
        {
            var area = Area(model, triangle);
            if (area <= 0.0000001) continue;
            cumulativeArea += area;
            triangles.Add(new SurfaceTriangle(triangle, cumulativeArea));
        }
        return triangles;
    }

    private static int FindTriangle(IReadOnlyList<SurfaceTriangle> triangles, double target)
    {
        var low = 0;
        var high = triangles.Count - 1;
        while (low < high)
        {
            var middle = low + (high - low) / 2;
            if (target <= triangles[middle].CumulativeArea) high = middle;
            else low = middle + 1;
        }
        return low;
    }

    private static double Halton(int index, int basis)
    {
        var result = 0d; var fraction = 1d / basis;
        while (index > 0) { result += (index % basis) * fraction; index /= basis; fraction /= basis; }
        return result;
    }

    private static void AddCube(MeshGeometry3D mesh, Point3D center, double size)
    {
        var halfSize = size / 2; var start = mesh.Positions.Count;
        foreach (var point in new[] { new Point3D(-halfSize,-halfSize,-halfSize), new Point3D(halfSize,-halfSize,-halfSize), new Point3D(halfSize,halfSize,-halfSize), new Point3D(-halfSize,halfSize,-halfSize), new Point3D(-halfSize,-halfSize,halfSize), new Point3D(halfSize,-halfSize,halfSize), new Point3D(halfSize,halfSize,halfSize), new Point3D(-halfSize,halfSize,halfSize) }) mesh.Positions.Add(center + (point - new Point3D()));
        int[] faces = [0,1,2, 0,2,3, 4,6,5, 4,7,6, 0,4,5, 0,5,1, 3,2,6, 3,6,7, 1,5,6, 1,6,2, 0,3,7, 0,7,4];
        foreach (var face in faces) mesh.TriangleIndices.Add(start + face);
    }

    private static void AddTetrahedron(MeshGeometry3D mesh, Point3D center, double size)
    {
        var radius = size * 0.58; var start = mesh.Positions.Count;
        foreach (var point in new[] { new Point3D(radius, radius, radius), new Point3D(radius, -radius, -radius), new Point3D(-radius, radius, -radius), new Point3D(-radius, -radius, radius) }) mesh.Positions.Add(center + (point - new Point3D()));
        int[] faces = [0,2,1, 0,1,3, 0,3,2, 1,2,3];
        foreach (var face in faces) mesh.TriangleIndices.Add(start + face);
    }

    private static void AddSphere(MeshGeometry3D mesh, Point3D center, double size)
    {
        var radius = size / 2; var start = mesh.Positions.Count;
        foreach (var point in SpherePoints) mesh.Positions.Add(center + (point * radius));
        foreach (var face in SphereFaces) mesh.TriangleIndices.Add(start + face);
    }

    private static Vector3D RotateY(Vector3D vector, double degrees)
    {
        var radians = degrees * Math.PI / 180;
        return new Vector3D(vector.X * Math.Cos(radians) + vector.Z * Math.Sin(radians), vector.Y, -vector.X * Math.Sin(radians) + vector.Z * Math.Cos(radians));
    }

    private static readonly Vector3D[] SpherePoints =
    [
        new Vector3D(0, 0.525731, 0.850651), new Vector3D(0, 0.525731, -0.850651), new Vector3D(0, -0.525731, 0.850651), new Vector3D(0, -0.525731, -0.850651),
        new Vector3D(0.525731, 0.850651, 0), new Vector3D(-0.525731, 0.850651, 0), new Vector3D(0.525731, -0.850651, 0), new Vector3D(-0.525731, -0.850651, 0),
        new Vector3D(0.850651, 0, 0.525731), new Vector3D(0.850651, 0, -0.525731), new Vector3D(-0.850651, 0, 0.525731), new Vector3D(-0.850651, 0, -0.525731)
    ];

    private static readonly int[] SphereFaces = [0,4,8, 0,8,2, 0,2,10, 0,10,5, 0,5,4, 1,9,3, 1,3,7, 1,7,11, 1,11,5, 1,5,9, 2,8,6, 2,6,7, 2,7,10, 3,9,6, 3,6,2, 3,2,7, 4,5,11, 4,11,9, 4,9,8, 5,10,11, 6,8,9, 6,9,3, 6,3,7, 7,3,11, 8,10,6, 10,7,11];

    private readonly record struct SurfaceTriangle(Triangle Triangle, double CumulativeArea);
}
