using System.Numerics;
using CreatureConstructionLab.Model;

namespace CreatureConstructionLab.Editor;

public sealed class EditorState
{
    public CreatureDefinition Creature { get; } = new();
    public CoordinateSystem Coordinates { get; } = new();
    public EditorMode Mode { get; private set; } = EditorMode.Create;
    public EditorTool Tool { get; set; } = EditorTool.Select;
    public CreatureNode? SelectedNode { get; private set; }
    public string? StatusMessage { get; private set; }

    public event Action? Changed;

    public CreatureNode CreateNode(Vector2 position)
    {
        var node = new CreatureNode { Position = position };
        Creature.Nodes.Add(node);
        SelectedNode = node;
        Changed?.Invoke();
        return node;
    }

    public CreatureNode? AddNextNode()
    {
        if (SelectedNode is null) return null;
        var index = Creature.Nodes.IndexOf(SelectedNode);
        if (index != Creature.Nodes.Count - 1) return null;
        var position = ChainMath.GetPositionAtSpacing(SelectedNode.Position, ChainMath.GetDirectionFromRotation(SelectedNode.Rotation), Creature.ChainSettings.Spacing);
        var child = new CreatureNode { Position = position, Rotation = SelectedNode.Rotation };
        Creature.Nodes.Add(child);
        Creature.Connections.Add(new CreatureConnection { ParentNodeId = SelectedNode.Id, ChildNodeId = child.Id, RestLength = Creature.ChainSettings.Spacing, Stiffness = Creature.ChainSettings.Stiffness, Damping = Creature.ChainSettings.Damping });
        SelectedNode = child;
        StatusMessage = null;
        Changed?.Invoke();
        return child;
    }

    public void SetSpacing(float spacing)
    {
        Creature.ChainSettings.Spacing = Math.Max(4, spacing);
        ChainMath.RebuildChainSpacing(Creature);
        Changed?.Invoke();
    }

    public void SetChainSettings(float stiffness, float damping)
    {
        Creature.ChainSettings.Stiffness = Math.Max(0, stiffness);
        Creature.ChainSettings.Damping = Math.Max(0, damping);
        foreach (var connection in Creature.Connections) { connection.Stiffness = Creature.ChainSettings.Stiffness; connection.Damping = Creature.ChainSettings.Damping; }
        Changed?.Invoke();
    }

    public void SetSelectedRotation(float rotation)
    {
        if (SelectedNode is null) return;
        SelectedNode.Rotation = rotation;
        Changed?.Invoke();
    }

    public void ClearStatus() { StatusMessage = null; Changed?.Invoke(); }
    public void SetStatus(string message) { StatusMessage = message; Changed?.Invoke(); }

    public void SelectAt(Vector2 position)
    {
        SelectedNode = Creature.Nodes
            .OrderByDescending(node => Vector2.DistanceSquared(node.Position, position) <= node.Radius * node.Radius)
            .ThenBy(node => Vector2.DistanceSquared(node.Position, position))
            .FirstOrDefault(node => Vector2.DistanceSquared(node.Position, position) <= node.Radius * node.Radius);
        Changed?.Invoke();
    }

    public void Select(CreatureNode? node) { SelectedNode = node; Changed?.Invoke(); }

    public void DeleteSelected()
    {
        if (SelectedNode is null) return;
        var index = Creature.Nodes.IndexOf(SelectedNode);
        var removed = Creature.Nodes.Skip(index).ToHashSet();
        Creature.Nodes.RemoveRange(index, Creature.Nodes.Count - index);
        Creature.Connections.RemoveAll(c => removed.Any(n => n.Id == c.ParentNodeId || n.Id == c.ChildNodeId));
        SelectedNode = null;
        Changed?.Invoke();
    }

    public void SetMode(EditorMode mode) { Mode = mode; Changed?.Invoke(); }

    public void Reset()
    {
        Creature.Nodes.Clear();
        Creature.Connections.Clear();
        SelectedNode = null;
        Mode = EditorMode.Create;
        Tool = EditorTool.Select;
        Changed?.Invoke();
    }
}
