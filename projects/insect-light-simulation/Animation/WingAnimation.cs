namespace InsectLightSimulation.Animation;

public static class WingAnimation
{
    public const int FrameCount = 4;

    public static float AdvancePhase(float phase, float speed, float deltaTime)
    {
        float next = phase + speed * deltaTime;
        return next - MathF.Floor(next / FrameCount) * FrameCount;
    }

    public static int GetFrameIndex(float phase)
    {
        int frame = (int)MathF.Floor(phase);
        return ((frame % FrameCount) + FrameCount) % FrameCount;
    }
}
