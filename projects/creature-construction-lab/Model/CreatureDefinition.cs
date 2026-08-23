namespace CreatureConstructionLab.Model;

public sealed class CreatureDefinition
{
    public List<CreatureNode> Nodes { get; } = [];
    public List<CreatureConnection> Connections { get; } = [];
    public ChainSettings ChainSettings { get; } = new();
    public BodySizeRamp BodySizeRamp { get; } = new();
    public float BaseRadius { get; set; } = 24;
    public List<CreatureFeature> Features { get; } = [];
    // Reserved for later milestones: branches, size ramps, constraints, and animation settings.
}
