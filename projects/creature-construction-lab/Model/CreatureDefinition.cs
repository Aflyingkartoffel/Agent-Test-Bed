namespace CreatureConstructionLab.Model;

public sealed class CreatureDefinition
{
    public List<CreatureNode> Nodes { get; } = [];
    public List<CreatureConnection> Connections { get; } = [];
    public ChainSettings ChainSettings { get; } = new();
    // Reserved for later milestones: branches, size ramps, constraints, and animation settings.
}
