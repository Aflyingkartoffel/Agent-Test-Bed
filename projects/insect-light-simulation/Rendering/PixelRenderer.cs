using System.Numerics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using InsectLightSimulation.Animation;
using InsectLightSimulation.Simulation;

namespace InsectLightSimulation.Rendering;

public sealed class PixelRenderer
{
    private WriteableBitmap? bitmap;
    private int[] pixels = Array.Empty<int>();
    private int width;
    private int height;
    private readonly InsectSpriteCache spriteCache = new();

    public WriteableBitmap? Bitmap => bitmap;

    public const float DefaultInsectScale = 0.5f;

    public void Render(SimulationEngine simulation, Camera2D camera, int selectedLightIndex)
    {
        int nextWidth = Math.Max(1, (int)Math.Round(simulation.Width));
        int nextHeight = Math.Max(1, (int)Math.Round(simulation.Height));
        if (bitmap is null || nextWidth != width || nextHeight != height)
        {
            width = nextWidth;
            height = nextHeight;
            bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            pixels = new int[width * height];
        }

        Array.Clear(pixels);
        for (int i = 0; i < simulation.Lights.Count; i++)
        {
            LightSource light = simulation.Lights[i];
            DrawGlow(camera.WorldToScreen(light.Position), light.InfluenceRadius * 0.28f * camera.Zoom, light.VisualIntensity);
            if (i == selectedLightIndex) DrawSelectionRing(camera.WorldToScreen(light.Position));
        }
        foreach (Agent insect in simulation.Agents)
            DrawInsect(insect, camera);

        bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);
    }

    private void DrawGlow(Vector2 center, float radius, float intensity)
    {
        int minX = Math.Max(0, (int)(center.X - radius));
        int maxX = Math.Min(width - 1, (int)(center.X + radius));
        int minY = Math.Max(0, (int)(center.Y - radius));
        int maxY = Math.Min(height - 1, (int)(center.Y + radius));
        float inverseRadius = 1f / Math.Max(1, radius);
        for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                float distance = Vector2.Distance(center, new Vector2(x, y));
                float glow = Math.Clamp(1f - distance * inverseRadius, 0, 1);
                glow *= glow * Math.Clamp(intensity, 0.1f, 2f);
                int red = (int)Math.Clamp(255 * glow, 0, 255);
                int green = (int)Math.Clamp(240 * glow, 0, 255);
                int blue = (int)Math.Clamp(110 * glow, 0, 255);
                pixels[y * width + x] = (255 << 24) | (red << 16) | (green << 8) | blue;
            }

        Plot((int)center.X, (int)center.Y, Color.FromRgb(255, 255, 235));
        Plot((int)center.X + 1, (int)center.Y, Color.FromRgb(255, 255, 235));
        Plot((int)center.X, (int)center.Y + 1, Color.FromRgb(255, 255, 235));
    }

    private void DrawSelectionRing(Vector2 center)
    {
        int x = (int)center.X;
        int y = (int)center.Y;
        for (int i = 0; i < 12; i++)
        {
            double angle = i * Math.PI / 6;
            Plot(x + (int)(Math.Cos(angle) * 8), y + (int)(Math.Sin(angle) * 8), Color.FromRgb(255, 255, 150));
        }
    }

    private void DrawInsect(Agent insect, Camera2D camera)
    {
        int direction = (int)MathF.Round(insect.Heading / MathF.Tau * InsectSpriteCache.RotationCount) & (InsectSpriteCache.RotationCount - 1);
        int frame = WingAnimation.GetFrameIndex(insect.AnimationPhase);
        IReadOnlyList<SpritePixel> pattern = spriteCache.GetFrame(frame, direction);
        Vector2 screenPosition = camera.WorldToScreen(insect.Position);
        float scale = DefaultInsectScale * camera.Zoom;
        if (screenPosition.X < -16 || screenPosition.X > width + 16 || screenPosition.Y < -16 || screenPosition.Y > height + 16)
            return;
        for (int i = 0; i < pattern.Count; i++)
        {
            SpritePixel pixel = pattern[i];
            int x = (int)MathF.Round(screenPosition.X + pixel.X * scale);
            int y = (int)MathF.Round(screenPosition.Y + pixel.Y * scale);
            int blockSize = Math.Max(1, (int)MathF.Round(scale));
            PlotBlock(x, y, blockSize, pixel.Color);
        }
    }

    private void Plot(int x, int y, Color color)
    {
        if ((uint)x >= (uint)width || (uint)y >= (uint)height) return;
        pixels[y * width + x] = (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;
    }

    private void Plot(int x, int y, int color)
    {
        if ((uint)x >= (uint)width || (uint)y >= (uint)height) return;
        pixels[y * width + x] = color;
    }

    private void PlotBlock(int x, int y, int size, int color)
    {
        for (int offsetY = 0; offsetY < size; offsetY++)
            for (int offsetX = 0; offsetX < size; offsetX++)
                Plot(x + offsetX, y + offsetY, color);
    }
}
