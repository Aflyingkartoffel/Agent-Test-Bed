using System.Windows.Media.Media3D;

namespace BounceSimulator;

public sealed class ImportedModel
{
    public required MeshGeometry3D Geometry { get; init; }
    public required double Radius { get; init; }
    public required int VertexCount { get; init; }
    public required int TriangleCount { get; init; }
}
