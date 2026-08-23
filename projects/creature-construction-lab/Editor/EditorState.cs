using System.Numerics;
using CreatureConstructionLab.Model;
using CreatureConstructionLab.Simulation;

namespace CreatureConstructionLab.Editor;

public sealed class EditorState
{
    public CreatureDefinition Creature { get; } = new();
    public CoordinateSystem Coordinates { get; } = new();
    public EditorMode Mode { get; private set; } = EditorMode.Create;
    public EditorTool Tool { get; set; } = EditorTool.Select;
    public CreatureNode? SelectedNode { get; private set; }
    public string? StatusMessage { get; private set; }
    public CreatureSimulator Simulator { get; } = new();

    public event Action? Changed;

    public CreatureNode CreateNode(Vector2 position)
    {
        var node = new CreatureNode { Position = position };
        Creature.Nodes.Add(node);
        SelectedNode = node;
        RecalculateBodySizes();
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
        RecalculateBodySizes();
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

    public void RecalculateBodySizes()
    {
        for (var i = 0; i < Creature.Nodes.Count; i++)
        {
            var node = Creature.Nodes[i];
            node.NormalizedPosition = Creature.Nodes.Count > 1 ? (float)i / (Creature.Nodes.Count - 1) : 0;
            node.RampValue = Creature.BodySizeRamp.Sample(node.NormalizedPosition);
            node.Radius = Math.Max(2, Creature.BaseRadius * node.RampValue);
        }
        Changed?.Invoke();
    }

    public void SetBaseRadius(float radius)
    {
        Creature.BaseRadius = Math.Clamp(float.IsFinite(radius) ? radius : 24, 4, 100);
        RecalculateBodySizes();
    }

    public void ResetBodySizeRamp()
    {
        Creature.BodySizeRamp.Reset();
        RecalculateBodySizes();
    }

    public void SetRampPoint(RampPoint point, float position, float value)
    {
        if (Creature.BodySizeRamp.Points[0] == point) position = 0;
        if (Creature.BodySizeRamp.Points[^1] == point) position = 1;
        point.Position = position;
        point.Value = value;
        Creature.BodySizeRamp.SortAndClamp();
        RecalculateBodySizes();
    }

    public RampPoint? AddRampPoint(float position, float value)
    {
        var point = Creature.BodySizeRamp.AddPoint(position, value);
        RecalculateBodySizes();
        return point;
    }

    public bool RemoveRampPoint(RampPoint point)
    {
        var removed = Creature.BodySizeRamp.RemovePoint(point);
        if (removed) RecalculateBodySizes();
        return removed;
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

    public void SetMode(EditorMode mode)
    {
        if (Mode == mode) return;
        if (mode == EditorMode.Play) Simulator.Reset(Creature);
        else Simulator.Reset(Creature);
        Mode = mode;
        Changed?.Invoke();
    }

    public void SetPlayTarget(Vector2 target) => Simulator.SetTarget(target);
    public void UpdateSimulation(float elapsedSeconds) { if (Mode == EditorMode.Play) { Simulator.Update(Creature, elapsedSeconds); Changed?.Invoke(); } }
    public void SetPaused(bool paused) { Simulator.State.SetPaused(paused); Changed?.Invoke(); }
    public void ResetSimulation() { Simulator.Reset(Creature); Changed?.Invoke(); }
    public void SetPlaySettings(float speed, float maxSpeed, float acceleration, float damping)
    {
        Simulator.State.SimulationSpeed = Math.Clamp(speed, 0.25f, 4);
        Simulator.State.MaxSpeed = Math.Clamp(maxSpeed, 10, 500);
        Simulator.State.AccelerationStrength = Math.Clamp(acceleration, 10, 2000);
        Simulator.State.Damping = Math.Clamp(damping, 0, 20);
        Changed?.Invoke();
    }

    public void SetWaveSettings(bool enabled, float amplitude, float frequency, float phase, float influence)
    {
        var wave = Simulator.State.Wave;
        wave.Enabled = enabled;
        wave.Amplitude = Math.Clamp(float.IsFinite(amplitude) ? amplitude : 8, 0, Creature.ChainSettings.Spacing * 0.45f);
        wave.Frequency = Math.Clamp(float.IsFinite(frequency) ? frequency : 1.2f, 0, 10);
        wave.Phase = Math.Clamp(float.IsFinite(phase) ? phase : 2.8f, 0, 20);
        wave.Influence = Math.Clamp(float.IsFinite(influence) ? influence : 0.75f, 0, 1);
        Changed?.Invoke();
    }

    public void Reset()
    {
        Creature.Nodes.Clear();
        Creature.Connections.Clear();
        Creature.BodySizeRamp.Reset();
        Creature.BaseRadius = 24;
        SelectedNode = null;
        Mode = EditorMode.Create;
        Simulator.Reset(Creature);
        Tool = EditorTool.Select;
        Changed?.Invoke();
    }
}
