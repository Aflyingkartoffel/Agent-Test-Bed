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

    public event Action? Changed;

    public CreatureNode CreateNode(Vector2 position)
    {
        var node = new CreatureNode { Position = position };
        Creature.Nodes.Add(node);
        SelectedNode = node;
        Changed?.Invoke();
        return node;
    }

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
        Creature.Nodes.Remove(SelectedNode);
        SelectedNode = null;
        Changed?.Invoke();
    }

    public void SetMode(EditorMode mode) { Mode = mode; Changed?.Invoke(); }

    public void Reset()
    {
        Creature.Nodes.Clear();
        SelectedNode = null;
        Mode = EditorMode.Create;
        Tool = EditorTool.Select;
        Changed?.Invoke();
    }
}
