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
    private readonly Dictionary<string, Vector2> pupilOffsets = [];

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
        if (ShowSkin && positions.Length > 0) DrawSkin(draw, CreatureSkinGeometry.Build(positions, radii), State.Mode != EditorMode.Play || State.Display.PlaySolidBody);
        if (State.Mode == EditorMode.Create) DrawConnections(draw, positions, new Pen(new SolidColorBrush(Color.FromRgb(35, 120, 79)), 1.5));
        if (State.Mode == EditorMode.Play && State.Display.PlayShowSkeleton) DrawSkeleton(draw, positions, radii);
        if ((State.Mode == EditorMode.Play && State.Display.PlayShowMuscles) || (State.Mode == EditorMode.Create && State.Display.CreateShowMuscles)) DrawMuscles(draw, positions, radii);
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

    private void DrawConnections(DrawingContext draw, System.Numerics.Vector2[] positions, Pen pen)
    {
        for (var i = 1; i < positions.Length; i++) draw.DrawLine(pen, ToPoint(positions[i - 1]), ToPoint(positions[i]));
    }

    private static void DrawSkeleton(DrawingContext draw, System.Numerics.Vector2[] positions, float[] radii)
    {
        var pen = new Pen(new SolidColorBrush(Color.FromRgb(90, 220, 150)), 1.5);
        for (var i = 1; i < positions.Length; i++) draw.DrawLine(pen, ToPoint(positions[i - 1]), ToPoint(positions[i]));
        for (var i = 0; i < positions.Length; i++) draw.DrawEllipse(new SolidColorBrush(Color.FromRgb(90, 220, 150)), null, ToPoint(positions[i]), Math.Max(2, Math.Min(5, radii[i] * 0.18)), Math.Max(2, Math.Min(5, radii[i] * 0.18)));
    }

    private static void DrawMuscles(DrawingContext draw, System.Numerics.Vector2[] positions, float[] radii)
    {
        var pen = new Pen(new SolidColorBrush(Color.FromArgb(155, 255, 210, 110)), 1);
        foreach (var circle in ConstructionCircleGeometry.Build(positions, radii))
        {
            draw.DrawEllipse(null, pen, ToPoint(circle.Center), circle.Radius, circle.Radius);
        }
    }

    private void DrawSkin(DrawingContext draw, CreatureSkinGeometry skin, bool solid)
    {
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            if (skin.Outline.Length == 0) return;
            context.BeginFigure(ToPoint(skin.Outline[0]), true, true);
            for (var i = 1; i < skin.Outline.Length; i++) context.LineTo(ToPoint(skin.Outline[i]), true, false);
        }
        geometry.Freeze();
        var color = Color.FromArgb((byte)((State.Creature.SkinColorArgb >> 24) & 0xFF), (byte)((State.Creature.SkinColorArgb >> 16) & 0xFF), (byte)((State.Creature.SkinColorArgb >> 8) & 0xFF), (byte)(State.Creature.SkinColorArgb & 0xFF));
        draw.DrawGeometry(solid ? new SolidColorBrush(color) : null, new Pen(Brushes.White, 2), geometry);
    }

    private void DrawFeatures(DrawingContext draw, System.Numerics.Vector2[] positions, float[] radii)
    {
        foreach (var feature in State.Creature.Features)
        {
            if (!feature.Visible || feature.ParentNodeId == Guid.Empty) continue;
            var parentIndex = State.Creature.Nodes.FindIndex(n => n.Id == feature.ParentNodeId);
            if (parentIndex < 0 || parentIndex >= positions.Length) continue;
            var transform = CreatureFeatureTransform.ToWorld(feature, positions[parentIndex], GetNodeHeading(parentIndex), false);
            if (feature.Type == CreatureFeatureType.Eye) DrawEye(draw, feature, transform, false);
            else if (feature.Type == CreatureFeatureType.ForkedTongue) DrawForkedTongue(draw, feature, transform, radii[parentIndex]);
            if (feature.Type == CreatureFeatureType.Eye && feature.Mirrored)
            {
                var mirrored = CreatureFeatureTransform.ToWorld(feature, positions[parentIndex], GetNodeHeading(parentIndex), true);
                if (feature.Type == CreatureFeatureType.Eye) DrawEye(draw, feature, mirrored, true);
            }
        }
    }

    private static void DrawForkedTongue(DrawingContext draw, CreatureFeature feature, FeatureWorldTransform transform, float headRadius)
    {
        var tongue = ForkedTongueGeometry.Build(feature, transform, headRadius);
        var pen = new Pen(Brushes.White, 2);
        draw.DrawLine(pen, ToPoint(tongue.Start), ToPoint(tongue.Junction));
        draw.DrawLine(pen, ToPoint(tongue.Junction), ToPoint(tongue.UpperTip));
        draw.DrawLine(pen, ToPoint(tongue.Junction), ToPoint(tongue.LowerTip));
    }

    private void DrawEye(DrawingContext draw, CreatureFeature feature, FeatureWorldTransform transform, bool mirrored)
    {
        var angle = transform.Rotation * MathF.PI / 180;
        var forward = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        var side = new Vector2(-forward.Y, forward.X);
        var width = feature.EyeWidth * transform.Scale;
        var height = feature.EyeHeight * transform.Scale;
        var center = transform.Position;
        var left = ToPoint(center - side * width * 0.5f);
        var right = ToPoint(center + side * width * 0.5f);
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(left, false, true);
            context.BezierTo(ToPoint(center + forward * height * 0.5f), ToPoint(center + forward * height * 0.5f), right, true, false);
            context.BezierTo(ToPoint(center - forward * height * 0.5f), ToPoint(center - forward * height * 0.5f), left, true, false);
        }
        geometry.Freeze();
        draw.DrawGeometry(null, new Pen(Brushes.White, 2), geometry);
        var key = $"{feature.Id}:{mirrored}";
        var target = State.Mode == EditorMode.Play ? State.Simulator.State.TargetPosition : center;
        var delta = target - center;
        var localTarget = delta.LengthSquared() < 0.0001f ? Vector2.Zero : new Vector2(Vector2.Dot(delta, side), Vector2.Dot(delta, forward));
        var maxX = width * 0.24f;
        var maxY = height * 0.24f;
        var desired = localTarget.LengthSquared() < 0.0001f ? Vector2.Zero : Vector2.Normalize(localTarget) * Math.Min(localTarget.Length(), Math.Min(maxX, maxY)) * feature.EyeTrackingStrength;
        desired.X = Math.Clamp(desired.X, -maxX, maxX);
        desired.Y = Math.Clamp(desired.Y, -maxY, maxY);
        var current = pupilOffsets.TryGetValue(key, out var stored) ? stored : Vector2.Zero;
        current = Vector2.Lerp(current, desired, 1 - MathF.Exp(-8 * (1f / 60f)));
        pupilOffsets[key] = new Vector2(Math.Clamp(current.X, -maxX, maxX), Math.Clamp(current.Y, -maxY, maxY));
        var pupil = center + side * pupilOffsets[key].X + forward * pupilOffsets[key].Y;
        draw.DrawEllipse(Brushes.White, null, ToPoint(pupil), Math.Max(1.5, height * 0.13), Math.Max(1.5, height * 0.13));
        draw.DrawEllipse(Brushes.Black, null, ToPoint(pupil), Math.Max(0.6, height * 0.06), Math.Max(0.6, height * 0.06));
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
