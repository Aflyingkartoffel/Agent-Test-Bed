using System.Numerics;

namespace InsectLightSimulation.Rendering;

public sealed class Camera2D
{
    public const float MinimumZoom = 0.5f;
    public const float MaximumZoom = 4f;
    public float Zoom { get; private set; } = 1f;
    public Vector2 Center { get; private set; }
    public Vector2 ViewportSize { get; private set; }
    public Vector2 WorldSize { get; private set; }

    public void Resize(float viewportWidth, float viewportHeight, float worldWidth, float worldHeight)
    {
        ViewportSize = new Vector2(Math.Max(1, viewportWidth), Math.Max(1, viewportHeight));
        WorldSize = new Vector2(Math.Max(1, worldWidth), Math.Max(1, worldHeight));
        if (Center == Vector2.Zero) Center = WorldSize * 0.5f;
        ClampCenter();
    }

    public Vector2 WorldToScreen(Vector2 worldPosition)
        => (worldPosition - Center) * Zoom + ViewportSize * 0.5f;

    public Vector2 ScreenToWorld(Vector2 screenPosition)
        => (screenPosition - ViewportSize * 0.5f) / Zoom + Center;

    public void ZoomAt(Vector2 screenPosition, float zoom)
    {
        Vector2 worldUnderCursor = ScreenToWorld(screenPosition);
        Zoom = Math.Clamp(zoom, MinimumZoom, MaximumZoom);
        Center = worldUnderCursor - (screenPosition - ViewportSize * 0.5f) / Zoom;
        ClampCenter();
    }

    public void Reset()
    {
        Zoom = 1f;
        Center = WorldSize * 0.5f;
        ClampCenter();
    }

    private void ClampCenter()
    {
        float halfVisibleWidth = ViewportSize.X / (2 * Zoom);
        float halfVisibleHeight = ViewportSize.Y / (2 * Zoom);
        float minX = Math.Min(halfVisibleWidth, WorldSize.X * 0.5f);
        float maxX = Math.Max(WorldSize.X - halfVisibleWidth, WorldSize.X * 0.5f);
        float minY = Math.Min(halfVisibleHeight, WorldSize.Y * 0.5f);
        float maxY = Math.Max(WorldSize.Y - halfVisibleHeight, WorldSize.Y * 0.5f);
        Center = new Vector2(Math.Clamp(Center.X, minX, maxX), Math.Clamp(Center.Y, minY, maxY));
    }
}
