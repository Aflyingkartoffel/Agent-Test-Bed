using System.Windows;
using System.Windows.Media;
using System.Numerics;
using CreatureConstructionLab.Editor;
using CreatureConstructionLab.Model;
using System.Windows.Input;
using CreatureConstructionLab.Simulation;

namespace CreatureConstructionLab.Rendering;

public sealed class CreatureCanvas : FrameworkElement
{
    private readonly DrawingVisual visual = new();
    public EditorState State { get; }

    public CreatureCanvas(EditorState state)
    {
        State = state;
        AddVisualChild(visual);
        AddLogicalChild(visual);
        State.Changed += InvalidateVisual;
        State.SimulationUpdated += InvalidateVisual;
        MouseLeftButtonDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseLeftButtonUp += OnMouseUp;
        Focusable = true;
    }

    private CreatureNode? dragging;
    private CreatureFeature? draggingFeature;
    private System.Windows.Vector dragOffset;
    private bool rotating;

    protected override int VisualChildrenCount => 1;
    protected override Visual GetVisualChild(int index) => visual;

    protected override void OnRender(DrawingContext context)
    {
        base.OnRender(context);
        using var draw = visual.RenderOpen();
        draw.DrawRectangle(new SolidColorBrush(Color.FromRgb(3, 10, 7)), null, new Rect(RenderSize));
        var gridPen = new Pen(new SolidColorBrush(Color.FromRgb(9, 35, 24)), 1);
        for (double x = 0; x < ActualWidth; x += 32) draw.DrawLine(gridPen, new Point(x, 0), new Point(x, ActualHeight));
        for (double y = 0; y < ActualHeight; y += 32) draw.DrawLine(gridPen, new Point(0, y), new Point(ActualWidth, y));

        var positions = new System.Numerics.Vector2[State.Creature.Nodes.Count];
        var radii = new float[State.Creature.Nodes.Count];
        for (var i = 0; i < State.Creature.Nodes.Count; i++) { positions[i] = GetPosition(State.Creature.Nodes[i]); radii[i] = State.Creature.Nodes[i].Radius; }
        if (ShowSkin && positions.Length > 0) DrawSkin(draw, CreatureSkinGeometry.Build(positions, radii));
        var connectionPen = new Pen(new SolidColorBrush(Color.FromRgb(35, 120, 79)), 1.5);
        foreach (var connection in State.Creature.Connections)
        {
            var parent = State.Creature.Nodes.FirstOrDefault(n => n.Id == connection.ParentNodeId);
            var child = State.Creature.Nodes.FirstOrDefault(n => n.Id == connection.ChildNodeId);
            if (parent is not null && child is not null) draw.DrawLine(connectionPen, ToPoint(GetPosition(parent)), ToPoint(GetPosition(child)));
        }
        if (State.Mode == EditorMode.Create && State.SelectedNode is not null && State.SelectedFeature is null) DrawGizmo(draw, State.SelectedNode, State.Creature.ChainSettings.Spacing);
        if (ShowNodes) foreach (var node in State.Creature.Nodes) DrawNode(draw, node, node == State.SelectedNode, GetPosition(node));
        if (ShowFeatures) DrawFeatures(draw, positions, radii);
        if (State.Mode == EditorMode.Play)
        {
            var text = new FormattedText(State.Simulator.State.Positions.Count == 0 ? "PLAY MODE  /  CREATE A CREATURE FIRST" : "PLAY MODE  /  MOUSE TARGET ACTIVE", System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Consolas"), 14, Brushes.SeaGreen, 1);
            draw.DrawText(text, new Point(24, 24));
            if (State.Simulator.State.Positions.Count > 0)
            {
                var target = ToPoint(State.Simulator.State.TargetPosition);
                draw.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromRgb(80, 180, 120)), 1), target, 8, 8);
                draw.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(80, 180, 120)), 1), new Point(target.X - 12, target.Y), new Point(target.X + 12, target.Y));
                draw.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(80, 180, 120)), 1), new Point(target.X, target.Y - 12), new Point(target.X, target.Y + 12));
            }
        }
    }

    private bool ShowNodes => State.Mode == EditorMode.Create ? State.Display.CreateShowNodes : State.Display.PlayShowNodes;
    private bool ShowSkin => State.Mode == EditorMode.Create ? State.Display.CreateShowSkin : State.Display.PlayShowSkin;
    private bool ShowFeatures => State.Mode == EditorMode.Create ? State.Display.CreateShowFeatures : State.Display.PlayShowFeatures;

    private static void DrawSkin(DrawingContext draw, CreatureSkinGeometry skin)
    {
        if (skin.Left.Length == 1)
        {
            var center = new Point((skin.Left[0].X + skin.Right[0].X) / 2, (skin.Left[0].Y + skin.Right[0].Y) / 2);
            var radius = Vector2.Distance(skin.Left[0], skin.Right[0]) / 2;
            draw.DrawEllipse(new SolidColorBrush(Color.FromArgb(45, 85, 220, 140)), new Pen(new SolidColorBrush(Color.FromRgb(72, 190, 125)), 2), center, radius, radius);
            return;
        }
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(ToPoint(skin.Left[0]), true, true);
            for (var i = 1; i < skin.Left.Length; i++) context.LineTo(ToPoint(skin.Left[i]), true, false);
            context.ArcTo(ToPoint(skin.Right[^1]), new Size(skin.Radii[^1], skin.Radii[^1]), 0, false, SweepDirection.Clockwise, true, false);
            for (var i = skin.Right.Length - 2; i >= 0; i--) context.LineTo(ToPoint(skin.Right[i]), true, false);
            context.ArcTo(ToPoint(skin.Left[0]), new Size(skin.Radii[0], skin.Radii[0]), 0, false, SweepDirection.Clockwise, true, false);
        }
        geometry.Freeze();
        draw.DrawGeometry(new SolidColorBrush(Color.FromArgb(45, 85, 220, 140)), new Pen(new SolidColorBrush(Color.FromRgb(72, 190, 125)), 2), geometry);
    }

    private void DrawFeatures(DrawingContext draw, System.Numerics.Vector2[] positions, float[] radii)
    {
        foreach (var feature in State.Creature.Features)
        {
            if (!feature.Visible || feature.ParentNodeId == Guid.Empty) continue;
            var parentIndex = State.Creature.Nodes.FindIndex(n => n.Id == feature.ParentNodeId);
            if (parentIndex < 0 || parentIndex >= positions.Length) continue;
            var transform = CreatureFeatureTransform.ToWorld(feature, positions[parentIndex], GetNodeHeading(parentIndex), false);
            if (feature.Type == CreatureFeatureType.Eye) DrawEye(draw, transform.Position, feature.EyeSize * transform.Scale);
            if (feature.Mirrored)
            {
                var mirrored = CreatureFeatureTransform.ToWorld(feature, positions[parentIndex], GetNodeHeading(parentIndex), true);
                if (feature.Type == CreatureFeatureType.Eye) DrawEye(draw, mirrored.Position, feature.EyeSize * mirrored.Scale);
            }
        }
    }

    private void DrawEye(DrawingContext draw, System.Numerics.Vector2 position, float size)
    {
        var center = ToPoint(position);
        draw.DrawEllipse(Brushes.White, new Pen(Brushes.DarkGreen, 1), center, size, size);
        draw.DrawEllipse(Brushes.Black, null, center, size * 0.42, size * 0.42);
    }

    private System.Numerics.Vector2 GetHeadDirection()
    {
        if (State.Mode == EditorMode.Create && State.Creature.Nodes.Count > 0) return ChainMath.GetDirectionFromRotation(State.Creature.Nodes[0].Rotation);
        var velocity = State.Simulator.State.Velocities.Count > 0 ? State.Simulator.State.Velocities[0] : Vector2.Zero;
        if (velocity.LengthSquared() > 0.0001f) return Vector2.Normalize(velocity);
        if (State.Creature.Nodes.Count > 1) { var direction = GetPosition(State.Creature.Nodes[1]) - GetPosition(State.Creature.Nodes[0]); if (direction.LengthSquared() > 0.0001f) return Vector2.Normalize(direction); }
        return ChainMath.GetDirectionFromRotation(State.Creature.Nodes[0].Rotation);
    }

    private System.Numerics.Vector2 GetNodeHeading(int index)
    {
        if (State.Mode == EditorMode.Play && index < State.Simulator.State.Velocities.Count && State.Simulator.State.Velocities[index].LengthSquared() > 0.0001f) return Vector2.Normalize(State.Simulator.State.Velocities[index]);
        if (index + 1 < State.Creature.Nodes.Count)
        {
            var direction = GetPosition(State.Creature.Nodes[index + 1]) - GetPosition(State.Creature.Nodes[index]);
            if (direction.LengthSquared() > 0.0001f) return Vector2.Normalize(direction);
        }
        return ChainMath.GetDirectionFromRotation(State.Creature.Nodes[index].Rotation);
    }

    private System.Numerics.Vector2 GetPosition(CreatureNode node)
    {
        var index = State.Creature.Nodes.IndexOf(node);
        return State.Mode == EditorMode.Play && index >= 0 && index < State.Simulator.State.Positions.Count ? State.Simulator.State.Positions[index] : node.Position;
    }

    private static Point ToPoint(System.Numerics.Vector2 p) => new(p.X, p.Y);

    private void DrawGizmo(DrawingContext draw, CreatureNode node, float spacing)
    {
        var center = ToPoint(node.Position);
        var guidePen = new Pen(new SolidColorBrush(Color.FromArgb(150, 83, 197, 139)), 1) { DashStyle = DashStyles.Dash };
        var index = State.Creature.Nodes.IndexOf(node);
        var reference = ChainMath.GetConstructionReferenceDegrees(State.Creature, index);
        if (index == 0) draw.DrawEllipse(null, guidePen, center, spacing, spacing);
        else
        {
            DrawArc(draw, center, spacing, reference - 135, 270, guidePen);
            DrawArc(draw, center, spacing, reference + 135, 90, new Pen(new SolidColorBrush(Color.FromArgb(110, 180, 70, 70)), 1) { DashStyle = DashStyles.Dot });
        }
        var direction = ChainMath.GetDirectionFromRotation(ChainMath.GetEffectiveConstructionRotation(State.Creature, index));
        var handle = new Point(center.X + direction.X * spacing, center.Y + direction.Y * spacing);
        draw.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(130, 255, 186)), 2), center, handle);
        draw.DrawEllipse(new SolidColorBrush(Color.FromRgb(130, 255, 186)), null, handle, 5, 5);
    }

    private static void DrawArc(DrawingContext draw, Point center, double radius, double startDegrees, double sweepDegrees, Pen pen)
    {
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            var start = PointOnCircle(center, radius, startDegrees);
            context.BeginFigure(start, false, false);
            var points = Math.Max(8, (int)Math.Abs(sweepDegrees) / 5);
            for (var i = 1; i <= points; i++) context.LineTo(PointOnCircle(center, radius, startDegrees + sweepDegrees * i / points), true, false);
        }
        geometry.Freeze();
        draw.DrawGeometry(null, pen, geometry);
    }

    private static Point PointOnCircle(Point center, double radius, double degrees)
    {
        var radians = degrees * Math.PI / 180;
        return new Point(center.X + Math.Cos(radians) * radius, center.Y + Math.Sin(radians) * radius);
    }

    private static void DrawNode(DrawingContext draw, CreatureNode node, bool selected, System.Numerics.Vector2 position)
    {
        var center = new Point(position.X, position.Y);
        var brush = new SolidColorBrush(selected ? Color.FromArgb(70, 100, 255, 160) : Color.FromArgb(40, 40, 150, 90));
        var pen = new Pen(new SolidColorBrush(selected ? Color.FromRgb(115, 255, 180) : Color.FromRgb(53, 166, 110)), selected ? 2.5 : 1.5);
        draw.DrawEllipse(brush, pen, center, node.Radius, node.Radius);
        var angle = node.Rotation * Math.PI / 180;
        draw.DrawLine(new Pen(pen.Brush, 2), center, new Point(center.X + Math.Cos(angle) * node.Radius, center.Y + Math.Sin(angle) * node.Radius));
    }

    private void OnMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        Focus();
        var world = State.Coordinates.ScreenToWorld(e.GetPosition(this));
        if (State.Mode != EditorMode.Create) { State.SetPlayTarget(world); InvalidateVisual(); return; }
        var hitFeature = FindFeatureAt(world);
        if (hitFeature is not null)
        {
            State.SelectFeature(hitFeature);
            draggingFeature = hitFeature;
            CaptureMouse();
            return;
        }
        if (State.SelectedNode is not null && IsNearDirectionHandle(world, State.SelectedNode)) { rotating = true; CaptureMouse(); UpdateRotation(world); return; }
        if (State.Tool == EditorTool.Node && State.Creature.Nodes.All(n => System.Numerics.Vector2.Distance(n.Position, world) > n.Radius))
        {
            State.CreateNode(world); CaptureMouse(); dragging = State.SelectedNode; dragOffset = new System.Windows.Vector(0, 0); return;
        }
        State.SelectAt(world);
        if (State.SelectedNode is not null) { dragging = State.SelectedNode; dragOffset = new System.Windows.Vector(State.SelectedNode.Position.X - world.X, State.SelectedNode.Position.Y - world.Y); CaptureMouse(); }
    }

    private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (State.Mode == EditorMode.Play)
        {
            State.SetPlayTarget(State.Coordinates.ScreenToWorld(e.GetPosition(this)));
            InvalidateVisual();
            return;
        }
        if (e.LeftButton != MouseButtonState.Pressed || State.Mode != EditorMode.Create) return;
        var world = State.Coordinates.ScreenToWorld(e.GetPosition(this));
        if (rotating && State.SelectedNode is not null) { UpdateRotation(world); return; }
        if (draggingFeature is not null)
        {
            var parentIndex = State.Creature.Nodes.FindIndex(n => n.Id == draggingFeature.ParentNodeId);
            if (parentIndex >= 0)
            {
                var parentPosition = State.Creature.Nodes[parentIndex].Position;
                var heading = GetNodeHeading(parentIndex);
                var right = new System.Numerics.Vector2(-heading.Y, heading.X);
                var delta = world - parentPosition;
                State.SetSelectedFeatureLocalPosition(new System.Numerics.Vector2(System.Numerics.Vector2.Dot(delta, heading), System.Numerics.Vector2.Dot(delta, right)));
            }
            return;
        }
        if (dragging is null) return;
        if (State.Creature.Nodes.IndexOf(dragging) > 0) return;
        var offset = new System.Numerics.Vector2((float)(world.X + dragOffset.X) - dragging.Position.X, (float)(world.Y + dragOffset.Y) - dragging.Position.Y);
        foreach (var node in State.Creature.Nodes) node.Position += offset;
        State.Select(dragging);
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e) { dragging = null; draggingFeature = null; rotating = false; ReleaseMouseCapture(); }

    private CreatureFeature? FindFeatureAt(System.Numerics.Vector2 world)
    {
        foreach (var feature in State.Creature.Features.AsEnumerable().Reverse())
        {
            if (!feature.Visible) continue;
            var parentIndex = State.Creature.Nodes.FindIndex(n => n.Id == feature.ParentNodeId);
            if (parentIndex < 0) continue;
            var position = CreatureFeatureTransform.ToWorld(feature, State.Creature.Nodes[parentIndex].Position, GetNodeHeading(parentIndex), false).Position;
            if (System.Numerics.Vector2.DistanceSquared(position, world) <= MathF.Pow(feature.EyeSize * feature.Scale * 1.5f, 2)) return feature;
            if (feature.Mirrored)
            {
                position = CreatureFeatureTransform.ToWorld(feature, State.Creature.Nodes[parentIndex].Position, GetNodeHeading(parentIndex), true).Position;
                if (System.Numerics.Vector2.DistanceSquared(position, world) <= MathF.Pow(feature.EyeSize * feature.Scale * 1.5f, 2)) return feature;
            }
        }
        return null;
    }

    private bool IsNearDirectionHandle(System.Numerics.Vector2 world, CreatureNode node)
    {
        var index = State.Creature.Nodes.IndexOf(node);
        var handle = node.Position + ChainMath.GetDirectionFromRotation(ChainMath.GetEffectiveConstructionRotation(State.Creature, index)) * State.Creature.ChainSettings.Spacing;
        return System.Numerics.Vector2.Distance(world, handle) <= 14;
    }

    private void UpdateRotation(System.Numerics.Vector2 world)
    {
        if (State.SelectedNode is null) return;
        var delta = world - State.SelectedNode.Position;
        if (delta.LengthSquared() > 0.001f) State.SetSelectedRotation(MathF.Atan2(delta.Y, delta.X) * 180 / MathF.PI);
    }
}
