using System.Numerics;
using System.Windows;

namespace CreatureConstructionLab.Editor;

public sealed class CoordinateSystem
{
    public Vector2 ScreenToWorld(Point position) => new((float)position.X, (float)position.Y);
}
