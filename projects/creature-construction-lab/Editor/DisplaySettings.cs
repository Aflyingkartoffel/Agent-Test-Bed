namespace CreatureConstructionLab.Editor;

public sealed class DisplaySettings
{
    public bool CreateShowNodes { get; set; } = true;
    public bool CreateShowSkin { get; set; } = true;
    public bool CreateShowFeatures { get; set; } = true;
    public bool CreateShowMuscles { get; set; }
    public bool PlayShowNodes { get; set; }
    public bool PlayShowSkin { get; set; } = true;
    public bool PlaySolidBody { get; set; } = true;
    public bool PlayShowSkeleton { get; set; }
    public bool PlayShowMuscles { get; set; }
    public bool PlayShowFeatures { get; set; } = true;
}
