using System.Numerics;
using CreatureConstructionLab.Model;
using CreatureConstructionLab.Simulation;

namespace CreatureConstructionLab.Editor;

public sealed class EditorState
{
    private const int MaxHistory = 150;
    private readonly List<AuthoringSnapshot> undoHistory = [];
    private readonly List<AuthoringSnapshot> redoHistory = [];
    private AuthoringSnapshot? historyGroupStart;
    private bool historyGroupDirty;
    private bool restoringHistory;
    private CreatureFeature? clipboardFeature;
    public CreatureDefinition Creature { get; } = new();
    public CoordinateSystem Coordinates { get; } = new();
    public EditorMode Mode { get; private set; } = EditorMode.Create;
    public EditorTool Tool { get; set; } = EditorTool.Select;
    public CreatureNode? SelectedNode { get; private set; }
    public CreatureFeature? SelectedFeature { get; private set; }
    public string? StatusMessage { get; private set; }
    public CreatureSimulator Simulator { get; } = new();
    public DisplaySettings Display { get; } = new();

    public event Action? Changed;
    public event Action? SimulationUpdated;
    public int UndoCount => undoHistory.Count;

    public CreatureNode CreateNode(Vector2 position)
    {
        CaptureEdit();
        var node = new CreatureNode { Position = position };
        Creature.Nodes.Add(node);
        SelectedNode = node;
        SelectedFeature = null;
        RecalculateBodySizes();
        Changed?.Invoke();
        return node;
    }

    public CreatureNode? AddNextNode()
    {
        if (SelectedNode is null) return null;
        var index = Creature.Nodes.IndexOf(SelectedNode);
        if (index != Creature.Nodes.Count - 1) return null;
        CaptureEdit();
        var constructionRotation = ChainMath.ClampConstructionRotation(Creature, index, SelectedNode.Rotation);
        var position = ChainMath.GetPositionAtSpacing(SelectedNode.Position, ChainMath.GetDirectionFromRotation(constructionRotation), Creature.ChainSettings.Spacing);
        var child = new CreatureNode { Position = position, Rotation = constructionRotation };
        Creature.Nodes.Add(child);
        Creature.Connections.Add(new CreatureConnection { ParentNodeId = SelectedNode.Id, ChildNodeId = child.Id, RestLength = Creature.ChainSettings.Spacing, Stiffness = Creature.ChainSettings.Stiffness, Damping = Creature.ChainSettings.Damping });
        SelectedNode = child;
        SelectedFeature = null;
        RecalculateBodySizes();
        StatusMessage = null;
        Changed?.Invoke();
        return child;
    }

    public void SetSpacing(float spacing)
    {
        CaptureEdit();
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
        CaptureEdit();
        Creature.BaseRadius = Math.Clamp(float.IsFinite(radius) ? radius : 24, 4, 100);
        RecalculateBodySizes();
    }

    public void ResetBodySizeRamp()
    {
        CaptureEdit();
        Creature.BodySizeRamp.Reset();
        RecalculateBodySizes();
    }

    public void SetRampPoint(RampPoint point, float position, float value)
    {
        CaptureEdit();
        if (Creature.BodySizeRamp.Points[0] == point) position = 0;
        if (Creature.BodySizeRamp.Points[^1] == point) position = 1;
        point.Position = position;
        point.Value = value;
        Creature.BodySizeRamp.SortAndClamp();
        RecalculateBodySizes();
    }

    public RampPoint? AddRampPoint(float position, float value)
    {
        CaptureEdit();
        var point = Creature.BodySizeRamp.AddPoint(position, value);
        if (point is not null) RecalculateBodySizes();
        return point;
    }

    public void SetRampInterpolation(RampInterpolationMode mode)
    {
        CaptureEdit();
        Creature.BodySizeRamp.Interpolation = mode;
        if (mode == RampInterpolationMode.Bezier) Creature.BodySizeRamp.EnsureHandles();
        RecalculateBodySizes();
    }

    public void SetRampHandle(RampPoint point, bool outgoing, Vector2 offset)
    {
        CaptureEdit();
        var horizontalLimit = outgoing ? Math.Max(0.01f, 1f - point.Position) : Math.Max(0.01f, point.Position);
        var horizontal = outgoing ? Math.Clamp(offset.X, 0.01f, horizontalLimit) : Math.Clamp(offset.X, -horizontalLimit, -0.01f);
        var clamped = new Vector2(horizontal, Math.Clamp(offset.Y, -1f, 1f));
        if (outgoing) point.OutHandle = clamped; else point.InHandle = clamped;
        RecalculateBodySizes();
    }

    public bool RemoveRampPoint(RampPoint point)
    {
        CaptureEdit();
        var removed = Creature.BodySizeRamp.RemovePoint(point);
        if (removed) RecalculateBodySizes();
        return removed;
    }

    public void SetChainSettings(float stiffness, float damping)
    {
        CaptureEdit();
        Creature.ChainSettings.Stiffness = Math.Max(0, stiffness);
        Creature.ChainSettings.Damping = Math.Max(0, damping);
        foreach (var connection in Creature.Connections) { connection.Stiffness = Creature.ChainSettings.Stiffness; connection.Damping = Creature.ChainSettings.Damping; }
        Changed?.Invoke();
    }

    public void SetSelectedRotation(float rotation)
    {
        if (SelectedNode is null) return;
        CaptureEdit();
        var index = Creature.Nodes.IndexOf(SelectedNode);
        SelectedNode.Rotation = ChainMath.ClampConstructionRotation(Creature, index, rotation);
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
        SelectedFeature = null;
        Changed?.Invoke();
    }

    public void Select(CreatureNode? node) { SelectedNode = node; if (node is not null) SelectedFeature = null; Changed?.Invoke(); }

    public void DeleteSelected()
    {
        if (SelectedFeature is not null) { DeleteSelectedFeature(); return; }
        if (SelectedNode is null) return;
        CaptureEdit();
        var index = Creature.Nodes.IndexOf(SelectedNode);
        var removed = Creature.Nodes.Skip(index).ToHashSet();
        Creature.Nodes.RemoveRange(index, Creature.Nodes.Count - index);
        Creature.Connections.RemoveAll(c => removed.Any(n => n.Id == c.ParentNodeId || n.Id == c.ChildNodeId));
        Creature.Features.RemoveAll(feature => removed.Any(n => n.Id == feature.ParentNodeId));
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
    public void UpdateSimulation(float elapsedSeconds) { if (Mode == EditorMode.Play) { Simulator.Update(Creature, elapsedSeconds); SimulationUpdated?.Invoke(); } }
    public void SetPaused(bool paused) { Simulator.State.SetPaused(paused); Changed?.Invoke(); }
    public void ResetSimulation() { Simulator.Reset(Creature); Changed?.Invoke(); }
    public void SetPlaySettings(float speed, float maxSpeed, float acceleration, float damping)
    {
        Simulator.State.SimulationSpeed = Math.Clamp(speed, 0.25f, 4);
        Simulator.State.MaxSpeed = Math.Clamp(maxSpeed, 10, 1000);
        Simulator.State.AccelerationStrength = Math.Clamp(acceleration, 10, 2000);
        Simulator.State.Damping = Math.Clamp(damping, 0, 20);
        Changed?.Invoke();
    }

    public void SetWaveSettings(bool enabled, float amplitude, float frequency, float phase, float influence)
    {
        var wave = Simulator.State.Wave;
        wave.Enabled = enabled;
        wave.Amplitude = Math.Clamp(float.IsFinite(amplitude) ? amplitude : 4, 0, Creature.ChainSettings.Spacing * 0.45f);
        wave.Frequency = Math.Clamp(float.IsFinite(frequency) ? frequency : 1.2f, 0, 10);
        wave.Phase = Math.Clamp(float.IsFinite(phase) ? phase : 2.8f, 0, 20);
        wave.Influence = Math.Clamp(float.IsFinite(influence) ? influence : 0.75f, 0, 1);
        Changed?.Invoke();
    }

    public void SetDisplay(bool showNodes, bool showSkin, bool showMuscles, bool showEyes)
    {
        if (Mode == EditorMode.Create) { Display.CreateShowNodes = showNodes; Display.CreateShowSkin = showSkin; Display.CreateShowMuscles = showMuscles; Display.CreateShowFeatures = showEyes; }
        else { Display.PlayShowNodes = showNodes; Display.PlayShowSkin = showSkin; Display.PlayShowFeatures = showEyes; }
        Changed?.Invoke();
    }

    public void SetPlayDisplay(bool showSkin, bool solidBody, bool showSkeleton, bool showMuscles, bool showFeatures)
    {
        Display.PlayShowSkin = showSkin;
        Display.PlaySolidBody = solidBody;
        Display.PlayShowSkeleton = showSkeleton;
        Display.PlayShowMuscles = showMuscles;
        Display.PlayShowFeatures = showFeatures;
        Changed?.Invoke();
    }

    public void SetSkinColor(uint argb)
    {
        CaptureEdit();
        Creature.SkinColorArgb = argb | 0xFF000000;
        Changed?.Invoke();
    }

    public void SetFinColor(uint argb)
    {
        CaptureEdit();
        Creature.FinColorArgb = argb | 0xFF000000;
        Changed?.Invoke();
    }

    public CreatureFeature AddFeature(CreatureFeatureType type = CreatureFeatureType.Eye)
    {
        CaptureEdit();
        var parent = SelectedNode ?? Creature.Nodes.FirstOrDefault();
        var feature = new CreatureFeature { Type = type, ParentNodeId = parent?.Id ?? Guid.Empty, Mirrored = type == CreatureFeatureType.Eye, FinSide = FinSide.Right };
        Creature.Features.Add(feature);
        SelectedFeature = feature;
        SelectedNode = null;
        Changed?.Invoke();
        return feature;
    }

    public void SelectFeature(CreatureFeature? feature)
    {
        SelectedFeature = feature;
        if (feature is not null) SelectedNode = null;
        Changed?.Invoke();
    }

    public void DeleteSelectedFeature()
    {
        if (SelectedFeature is null) return;
        CaptureEdit();
        Creature.Features.Remove(SelectedFeature);
        SelectedFeature = null;
        Changed?.Invoke();
    }

    public void SetSelectedFeature(CreatureFeatureType type, Guid parentNodeId, Vector2 localPosition, float rotation, float scale, bool mirrored, bool visible, float eyeSize, float eyeWidth, float eyeHeight, float trackingStrength, float tongueLength, float tongueForkLength, float tongueForkAngle, FinSide finSide, float finLength, float finWidth, float finBaseAngle, float finStiffness, float finDamping)
    {
        if (SelectedFeature is null) return;
        CaptureEdit();
        SelectedFeature.Type = type;
        SelectedFeature.ParentNodeId = Creature.Nodes.Any(n => n.Id == parentNodeId) ? parentNodeId : Creature.Nodes.FirstOrDefault()?.Id ?? Guid.Empty;
        SelectedFeature.LocalPosition = type == CreatureFeatureType.Fin ? Vector2.Zero : localPosition;
        SelectedFeature.LocalRotation = type == CreatureFeatureType.Fin ? 0 : (float.IsFinite(rotation) ? rotation : 0);
        SelectedFeature.Scale = Math.Clamp(float.IsFinite(scale) ? scale : 1, 0.1f, 10);
        SelectedFeature.Mirrored = SelectedFeature.SupportsMirroring && mirrored;
        SelectedFeature.Visible = visible;
        SelectedFeature.EyeSize = Math.Clamp(float.IsFinite(eyeSize) ? eyeSize : 5, 1, 20);
        SelectedFeature.EyeWidth = Math.Clamp(float.IsFinite(eyeWidth) ? eyeWidth : 16, 6, 40);
        SelectedFeature.EyeHeight = Math.Clamp(float.IsFinite(eyeHeight) ? eyeHeight : 9, 3, 24);
        SelectedFeature.EyeTrackingStrength = Math.Clamp(float.IsFinite(trackingStrength) ? trackingStrength : 0.5f, 0, 1);
        SelectedFeature.TongueLength = Math.Clamp(float.IsFinite(tongueLength) ? tongueLength : 28, 2, 200);
        SelectedFeature.TongueForkLength = Math.Clamp(float.IsFinite(tongueForkLength) ? tongueForkLength : 12, 2, 100);
        SelectedFeature.TongueForkAngle = Math.Clamp(float.IsFinite(tongueForkAngle) ? tongueForkAngle : 28, 5, 75);
        SelectedFeature.FinSide = finSide;
        SelectedFeature.FinLength = Math.Clamp(float.IsFinite(finLength) ? finLength : 34, 2, 200);
        SelectedFeature.FinWidth = Math.Clamp(float.IsFinite(finWidth) ? finWidth : 16, 2, 100);
        SelectedFeature.FinBaseAngle = Math.Clamp(float.IsFinite(finBaseAngle) ? finBaseAngle : 0, -120, 120);
        SelectedFeature.FinAngularStiffness = Math.Clamp(float.IsFinite(finStiffness) ? finStiffness : 12, 0, 40);
        SelectedFeature.FinAngularDamping = Math.Clamp(float.IsFinite(finDamping) ? finDamping : 4, 0, 20);
        Changed?.Invoke();
    }

    public void SetSelectedFeatureLocalPosition(Vector2 position)
    {
        if (SelectedFeature is null) return;
        CaptureEdit();
        SelectedFeature.LocalPosition = position;
        Changed?.Invoke();
    }

    public void SetSelectedNodeProperties(Vector2 position, float rotation)
    {
        if (SelectedNode is null) return;
        CaptureEdit();
        SelectedNode.Position = position;
        SelectedNode.Rotation = float.IsFinite(rotation) ? rotation : SelectedNode.Rotation;
        Changed?.Invoke();
    }

    public void BeginHistoryGroup()
    {
        if (Mode != EditorMode.Create || historyGroupStart is not null) return;
        historyGroupStart = CaptureSnapshot();
        historyGroupDirty = true;
    }

    public void EndHistoryGroup()
    {
        if (historyGroupStart is null) return;
        if (historyGroupDirty) AddUndo(historyGroupStart);
        historyGroupStart = null;
        historyGroupDirty = false;
    }

    public void Undo()
    {
        if (Mode != EditorMode.Create || undoHistory.Count == 0) return;
        var current = CaptureSnapshot();
        var previous = undoHistory[^1];
        undoHistory.RemoveAt(undoHistory.Count - 1);
        redoHistory.Add(current);
        restoringHistory = true;
        LoadDefinition(previous.Definition.Clone());
        SelectedNode = Creature.Nodes.FirstOrDefault(node => node.Id == previous.SelectedNodeId);
        SelectedFeature = Creature.Features.FirstOrDefault(feature => feature.Id == previous.SelectedFeatureId);
        restoringHistory = false;
        Changed?.Invoke();
    }

    public void CopySelectedFeature() => clipboardFeature = SelectedFeature?.Clone();

    public bool PasteFeature()
    {
        if (clipboardFeature is null || Creature.Nodes.Count == 0) return false;
        CaptureEdit();
        var pasted = clipboardFeature.Clone(true);
        if (!Creature.Nodes.Any(node => node.Id == pasted.ParentNodeId)) pasted.ParentNodeId = Creature.Nodes[0].Id;
        if (pasted.Type != CreatureFeatureType.Fin) pasted.LocalPosition += new Vector2(8, 8);
        else { pasted.LocalPosition = Vector2.Zero; pasted.LocalRotation = 0; }
        Creature.Features.Add(pasted);
        SelectedFeature = pasted;
        SelectedNode = null;
        Changed?.Invoke();
        return true;
    }

    public void LoadDefinition(CreatureDefinition loaded)
    {
        Creature.Nodes.Clear();
        Creature.Connections.Clear();
        Creature.Nodes.AddRange(loaded.Nodes);
        Creature.Connections.AddRange(loaded.Connections);
        Creature.ChainSettings.Spacing = loaded.ChainSettings.Spacing;
        Creature.ChainSettings.Stiffness = loaded.ChainSettings.Stiffness;
        Creature.ChainSettings.Damping = loaded.ChainSettings.Damping;
        Creature.BaseRadius = loaded.BaseRadius;
        Creature.SkinColorArgb = loaded.SkinColorArgb;
        Creature.FinColorArgb = loaded.FinColorArgb;
        Creature.Features.Clear();
        Creature.Features.AddRange(loaded.Features);
        Creature.BodySizeRamp.Points.Clear();
        Creature.BodySizeRamp.Interpolation = loaded.BodySizeRamp.Interpolation;
        Creature.BodySizeRamp.Points.AddRange(loaded.BodySizeRamp.Points.Select(p => new RampPoint(p.Position, p.Value) { InHandle = p.InHandle, OutHandle = p.OutHandle }));
        Creature.BodySizeRamp.EnsureHandles();
        SelectedNode = null;
        SelectedFeature = null;
        Mode = EditorMode.Create;
        Tool = EditorTool.Select;
        StatusMessage = null;
        RecalculateBodySizes();
        Simulator.Reset(Creature);
        Changed?.Invoke();
    }

    private void CaptureEdit()
    {
        if (restoringHistory) return;
        if (historyGroupStart is not null) { historyGroupDirty = true; return; }
        AddUndo(CaptureSnapshot());
    }

    private void AddUndo(AuthoringSnapshot snapshot)
    {
        undoHistory.Add(snapshot);
        if (undoHistory.Count > MaxHistory) undoHistory.RemoveAt(0);
        redoHistory.Clear();
    }

    private AuthoringSnapshot CaptureSnapshot() => new(Creature.Clone(), SelectedNode?.Id, SelectedFeature?.Id);

    private sealed record AuthoringSnapshot(CreatureDefinition Definition, Guid? SelectedNodeId, Guid? SelectedFeatureId);

    public void Reset()
    {
        CaptureEdit();
        Creature.Nodes.Clear();
        Creature.Connections.Clear();
        Creature.BodySizeRamp.Reset();
        Creature.BaseRadius = 24;
        Creature.SkinColorArgb = 0xFF2E8B57;
        Creature.FinColorArgb = 0xFF9BE7B0;
        Creature.Features.Clear();
        Display.CreateShowNodes = true;
        Display.CreateShowSkin = true;
        Display.CreateShowMuscles = false;
        Display.CreateShowFeatures = true;
        Display.PlayShowNodes = false;
        Display.PlayShowSkin = true;
        Display.PlaySolidBody = true;
        Display.PlayShowSkeleton = false;
        Display.PlayShowMuscles = false;
        Display.PlayShowFeatures = true;
        Simulator.ResetSettings();
        SelectedNode = null;
        SelectedFeature = null;
        Mode = EditorMode.Create;
        Simulator.Reset(Creature);
        Tool = EditorTool.Select;
        Changed?.Invoke();
    }
}
