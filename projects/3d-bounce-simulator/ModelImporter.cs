using Assimp;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using AssimpVector3D = Assimp.Vector3D;

namespace BounceSimulator;

public static class ModelImporter
{
    public static ImportedModel Load(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("The selected model could not be found.", path);
        var extension = System.IO.Path.GetExtension(path).ToLowerInvariant();
        if (extension is not ".obj" and not ".fbx") throw new NotSupportedException("Choose an OBJ or FBX model.");

        var importer = new AssimpContext();
        var scene = importer.ImportFile(path,
            PostProcessSteps.Triangulate |
            PostProcessSteps.JoinIdenticalVertices |
            PostProcessSteps.GenerateSmoothNormals |
            PostProcessSteps.ImproveCacheLocality);
        if (scene is null || scene.MeshCount == 0) throw new InvalidDataException("The model contains no mesh data.");

        var rawVertices = new List<AssimpVector3D>();
        var rawIndices = new List<int>();
        foreach (var mesh in scene.Meshes)
        {
            if (mesh.VertexCount == 0) continue;
            var offset = rawVertices.Count;
            rawVertices.AddRange(mesh.Vertices);
            foreach (var face in mesh.Faces)
            {
                if (face.IndexCount != 3) continue;
                rawIndices.Add(offset + face.Indices[0]);
                rawIndices.Add(offset + face.Indices[1]);
                rawIndices.Add(offset + face.Indices[2]);
            }
        }
        if (rawVertices.Count == 0 || rawIndices.Count == 0) throw new InvalidDataException("The model contains no renderable triangles.");

        var min = new AssimpVector3D(float.MaxValue, float.MaxValue, float.MaxValue);
        var max = new AssimpVector3D(float.MinValue, float.MinValue, float.MinValue);
        foreach (var point in rawVertices)
        {
            if (!IsFinite(point.X) || !IsFinite(point.Y) || !IsFinite(point.Z)) throw new InvalidDataException("The model contains invalid vertex coordinates.");
            min.X = Math.Min(min.X, point.X); min.Y = Math.Min(min.Y, point.Y); min.Z = Math.Min(min.Z, point.Z);
            max.X = Math.Max(max.X, point.X); max.Y = Math.Max(max.Y, point.Y); max.Z = Math.Max(max.Z, point.Z);
        }

        var center = new AssimpVector3D((min.X + max.X) / 2, (min.Y + max.Y) / 2, (min.Z + max.Z) / 2);
        var largestDimension = Math.Max(max.X - min.X, Math.Max(max.Y - min.Y, max.Z - min.Z));
        if (!IsFinite(largestDimension) || largestDimension <= 0) throw new InvalidDataException("The model has no measurable size.");
        var scale = 4 / largestDimension;
        var geometry = new MeshGeometry3D();
        foreach (var point in rawVertices)
            geometry.Positions.Add(new Point3D((point.X - center.X) * scale, (point.Y - center.Y) * scale, (point.Z - center.Z) * scale));
        foreach (var index in rawIndices) geometry.TriangleIndices.Add(index);
        geometry.Freeze();

        var radius = geometry.Positions.Max(point => (point - new Point3D()).Length);
        if (!IsFinite(radius) || radius <= 0) throw new InvalidDataException("The model bounds are invalid.");
        return new ImportedModel { Geometry = geometry, Radius = radius, VertexCount = geometry.Positions.Count, TriangleCount = geometry.TriangleIndices.Count / 3 };
    }

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
