namespace CreatureConstructionLab.Model;

public sealed class CreatureDefinition
{
    public List<CreatureNode> Nodes { get; } = [];
    public List<CreatureConnection> Connections { get; } = [];
    public ChainSettings ChainSettings { get; } = new();
    public BodySizeRamp BodySizeRamp { get; } = new();
    public float BaseRadius { get; set; } = 24;
    public uint SkinColorArgb { get; set; } = 0xFF2E8B57;
    public uint FinColorArgb { get; set; } = 0xFF9BE7B0;
    public List<CreatureFeature> Features { get; } = [];
    // Reserved for later milestones: branches, size ramps, constraints, and animation settings.

    public CreatureDefinition Clone()
    {
        var copy = new CreatureDefinition { BaseRadius = BaseRadius, SkinColorArgb = SkinColorArgb, FinColorArgb = FinColorArgb };
        copy.ChainSettings.Spacing = ChainSettings.Spacing;
        copy.ChainSettings.Stiffness = ChainSettings.Stiffness;
        copy.ChainSettings.Damping = ChainSettings.Damping;
        copy.BodySizeRamp.Interpolation = BodySizeRamp.Interpolation;
        copy.BodySizeRamp.Points.Clear();
        foreach (var point in BodySizeRamp.Points) copy.BodySizeRamp.Points.Add(new RampPoint(point.Position, point.Value) { InHandle = point.InHandle, OutHandle = point.OutHandle });
        foreach (var node in Nodes) copy.Nodes.Add(new CreatureNode { Id = node.Id, Position = node.Position, Rotation = node.Rotation, Radius = node.Radius, NormalizedPosition = node.NormalizedPosition, RampValue = node.RampValue });
        foreach (var connection in Connections) copy.Connections.Add(new CreatureConnection { ParentNodeId = connection.ParentNodeId, ChildNodeId = connection.ChildNodeId, RestLength = connection.RestLength, Stiffness = connection.Stiffness, Damping = connection.Damping });
        foreach (var feature in Features) copy.Features.Add(feature.Clone());
        return copy;
    }
}
