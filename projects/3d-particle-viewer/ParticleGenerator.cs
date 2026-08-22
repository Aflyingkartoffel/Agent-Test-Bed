using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace ParticleModelViewer;

public static class ParticleGenerator
{
    public static List<Point3D> SampleSurface(ObjModel model, int count)
    {
        var triangles = model.Triangles
            .Select(triangle => (triangle, area: Area(model, triangle)))
            .Where(item => item.area > 0.0000001)
            .ToList();
        if (triangles.Count == 0) return [];

        var totalArea = triangles.Sum(item => item.area);
        var result = new List<Point3D>(count);
        for (var i = 0; i < count; i++)
        {
            var target = ((i + 0.5) / count) * totalArea;
            var running = 0d;
            var chosen = triangles[^1].triangle;
            foreach (var item in triangles)
            {
                running += item.area;
                if (target <= running) { chosen = item.triangle; break; }
            }

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

    public static MeshGeometry3D CreateParticleMesh(IEnumerable<Point3D> points, double size)
    {
        var mesh = new MeshGeometry3D();
        foreach (var point in points) AddCube(mesh, point, size);
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

    private static double Halton(int index, int basis)
    {
        var result = 0d; var fraction = 1d / basis;
        while (index > 0) { result += (index % basis) * fraction; index /= basis; fraction /= basis; }
        return result;
    }

    private static void AddCube(MeshGeometry3D mesh, Point3D center, double size)
    {
        var h = size / 2; var start = mesh.Positions.Count;
        foreach (var p in new[] { new Point3D(-h,-h,-h), new Point3D(h,-h,-h), new Point3D(h,h,-h), new Point3D(-h,h,-h), new Point3D(-h,-h,h), new Point3D(h,-h,h), new Point3D(h,h,h), new Point3D(-h,h,h) }) mesh.Positions.Add(center + (p - new Point3D()));
        int[] faces = [0,1,2, 0,2,3, 4,6,5, 4,7,6, 0,4,5, 0,5,1, 3,2,6, 3,6,7, 1,5,6, 1,6,2, 0,3,7, 0,7,4];
        foreach (var face in faces) mesh.TriangleIndices.Add(start + face);
    }
}
