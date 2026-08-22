using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Media.Media3D;

namespace ParticleModelViewer;

public sealed class ObjModel
{
    public List<Point3D> Vertices { get; } = [];
    public List<Triangle> Triangles { get; } = [];

    public static ObjModel Load(string path)
    {
        var model = new ObjModel();
        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            string[] parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) continue;

            if (parts[0] == "v" && parts.Length >= 4)
            {
                model.Vertices.Add(new Point3D(Parse(parts[1]), Parse(parts[2]), Parse(parts[3])));
            }
            else if (parts[0] == "f" && parts.Length >= 4)
            {
                var indices = parts.Skip(1).Select(part => ParseVertexIndex(part, model.Vertices.Count)).ToArray();
                for (var i = 1; i < indices.Length - 1; i++)
                {
                    model.Triangles.Add(new Triangle(indices[0], indices[i], indices[i + 1]));
                }
            }
        }

        if (model.Vertices.Count == 0 || model.Triangles.Count == 0)
            throw new InvalidDataException("The OBJ file does not contain usable vertices and faces.");
        return model;
    }

    private static double Parse(string value) => double.Parse(value, CultureInfo.InvariantCulture);

    private static int ParseVertexIndex(string value, int vertexCount)
    {
        var indexText = value.Split('/')[0];
        var index = int.Parse(indexText, CultureInfo.InvariantCulture);
        var resolved = index < 0 ? vertexCount + index : index - 1;
        if (resolved < 0 || resolved >= vertexCount) throw new InvalidDataException("The OBJ file contains an invalid face index.");
        return resolved;
    }
}

public readonly record struct Triangle(int A, int B, int C);
