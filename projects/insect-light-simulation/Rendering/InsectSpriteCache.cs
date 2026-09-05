namespace InsectLightSimulation.Rendering;

public readonly struct SpritePixel
{
    public readonly int X;
    public readonly int Y;
    public readonly int Color;

    public SpritePixel(int x, int y, int color)
    {
        X = x;
        Y = y;
        Color = color;
    }
}

public sealed class InsectSpriteCache
{
    public const int RotationCount = 8;
    private readonly SpritePixel[][][] frames;

    public InsectSpriteCache()
    {
        frames = new SpritePixel[4][][];
        for (int frame = 0; frame < 4; frame++)
        {
            Dictionary<(int X, int Y), int> basePixels = BuildFrame(frame);
            frames[frame] = new SpritePixel[RotationCount][];
            for (int rotation = 0; rotation < RotationCount; rotation++)
                frames[frame][rotation] = Rotate(basePixels, rotation);
        }
    }

    public IReadOnlyList<SpritePixel> GetFrame(int frame, int rotation)
        => frames[frame & 3][rotation & (RotationCount - 1)];

    private static Dictionary<(int X, int Y), int> BuildFrame(int frame)
    {
        var pixels = new Dictionary<(int X, int Y), int>();
        int wingFill = Argb(25, 112, 52);
        int wingEdge = Argb(76, 205, 78);
        int body = Argb(49, 160, 57);
        int bodyHighlight = Argb(112, 225, 76);
        int outline = Argb(40, 116, 44);
        int head = Argb(4, 8, 5);
        int glowOuter = Argb(72, 72, 25);
        int glowInner = Argb(160, 150, 38);
        int abdomen = Argb(255, 255, 224);

        // The reference's four states are top, mid, down, mid. Only wing geometry changes.
        (int frontX, int frontY, int rearX, int rearY) = frame switch
        {
            0 => (1, 7, -2, 5),
            1 => (2, 5, -2, 4),
            2 => (1, 3, -1, 6),
            _ => (2, 5, -2, 4)
        };
        DrawWing(pixels, frontX, -frontY, 6, 2, wingFill, wingEdge);
        DrawWing(pixels, frontX, frontY, 6, 2, wingFill, wingEdge);
        DrawWing(pixels, rearX, -rearY, 5, 2, wingFill, wingEdge);
        DrawWing(pixels, rearX, rearY, 5, 2, wingFill, wingEdge);

        DrawEllipse(pixels, -1, 0, 5, 2, body, outline);
        DrawEllipse(pixels, 1, 0, 3, 1, bodyHighlight, body);
        DrawEllipse(pixels, 7, 0, 3, 3, head, wingEdge);
        DrawEllipse(pixels, -7, 0, 3, 3, glowOuter, glowOuter);
        DrawEllipse(pixels, -7, 0, 2, 2, glowInner, glowInner);
        DrawPixel(pixels, -7, 0, abdomen);
        DrawPixel(pixels, -8, 0, abdomen);
        DrawPixel(pixels, -6, 0, abdomen);

        // Two short antennae lead from the head, matching the simple top-view silhouette.
        DrawLine(pixels, 9, -2, 12, -5, wingEdge);
        DrawLine(pixels, 9, 2, 12, 5, wingEdge);
        DrawPixel(pixels, 13, -6, bodyHighlight);
        DrawPixel(pixels, 13, 6, bodyHighlight);
        return pixels;
    }

    private static SpritePixel[] Rotate(Dictionary<(int X, int Y), int> basePixels, int rotation)
    {
        double angle = rotation * Math.PI * 2 / RotationCount;
        double cosine = Math.Cos(angle);
        double sine = Math.Sin(angle);
        var rotated = new SpritePixel[basePixels.Count];
        int index = 0;
        foreach (KeyValuePair<(int X, int Y), int> pixel in basePixels)
        {
            int x = (int)Math.Round(pixel.Key.X * cosine - pixel.Key.Y * sine);
            int y = (int)Math.Round(pixel.Key.X * sine + pixel.Key.Y * cosine);
            rotated[index++] = new SpritePixel(x, y, pixel.Value);
        }
        return rotated;
    }

    private static void DrawWing(Dictionary<(int X, int Y), int> pixels, int centerX, int centerY, int length, int width, int fill, int edge)
    {
        for (int x = centerX - length; x <= centerX + length; x++)
            for (int y = centerY - width; y <= centerY + width; y++)
            {
                float normalized = MathF.Pow((x - centerX) / (float)length, 2) + MathF.Pow((y - centerY) / (float)width, 2);
                if (normalized <= 1) pixels[(x, y)] = normalized > 0.65f ? edge : fill;
            }
    }

    private static void DrawEllipse(Dictionary<(int X, int Y), int> pixels, int centerX, int centerY, int radiusX, int radiusY, int fill, int edge)
    {
        for (int x = centerX - radiusX; x <= centerX + radiusX; x++)
            for (int y = centerY - radiusY; y <= centerY + radiusY; y++)
            {
                float normalized = MathF.Pow((x - centerX) / (float)radiusX, 2) + MathF.Pow((y - centerY) / (float)radiusY, 2);
                if (normalized <= 1) pixels[(x, y)] = normalized > 0.7f ? edge : fill;
            }
    }

    private static void DrawLine(Dictionary<(int X, int Y), int> pixels, int x0, int y0, int x1, int y1, int color)
    {
        int steps = Math.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0));
        for (int i = 0; i <= steps; i++)
        {
            float amount = steps == 0 ? 0 : i / (float)steps;
            DrawPixel(pixels, (int)Math.Round(x0 + (x1 - x0) * amount), (int)Math.Round(y0 + (y1 - y0) * amount), color);
        }
    }

    private static void DrawPixel(Dictionary<(int X, int Y), int> pixels, int x, int y, int color) => pixels[(x, y)] = color;
    private static int Argb(byte red, byte green, byte blue) => unchecked((int)(0xFF000000u | ((uint)red << 16) | ((uint)green << 8) | blue));
}
