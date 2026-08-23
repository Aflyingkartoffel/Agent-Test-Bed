namespace CreatureConstructionLab.Model;

public sealed class CreatureConnection
{
    public Guid ParentNodeId { get; init; }
    public Guid ChildNodeId { get; init; }
    public float RestLength { get; set; }
    public float Stiffness { get; set; } = 1;
    public float Damping { get; set; } = 0.1f;
}
