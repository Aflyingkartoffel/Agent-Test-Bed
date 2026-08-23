using System.Windows;
using System.Windows.Media;
using CreatureConstructionLab.Editor;
using CreatureConstructionLab.Model;
using System.Windows.Input;

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
        MouseLeftButtonDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseLeftButtonUp += OnMouseUp;
        Focusable = true;
    }

    private CreatureNode? dragging;
    private Vector dragOffset;
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

        var connectionPen = new Pen(new SolidColorBrush(Color.FromRgb(35, 120, 79)), 1.5);
        foreach (var connection in State.Creature.Connections)
        {
            var parent = State.Creature.Nodes.FirstOrDefault(n => n.Id == connection.ParentNodeId);
            var child = State.Creature.Nodes.FirstOrDefault(n => n.Id == connection.ChildNodeId);
            if (parent is not null && child is not null) draw.DrawLine(connectionPen, ToPoint(parent.Position), ToPoint(child.Position));
        }
        if (State.Mode == EditorMode.Create && State.SelectedNode is not null) DrawGizmo(draw, State.SelectedNode, State.Creature.ChainSettings.Spacing);
        foreach (var node in State.Creature.Nodes) DrawNode(draw, node, node == State.SelectedNode);
        if (State.Mode == EditorMode.Play)
        {
            var text = new FormattedText("PLAY MODE  /  SIMULATION NOT IMPLEMENTED IN MILESTONE 1", System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Consolas"), 14, Brushes.SeaGreen, 1);
            draw.DrawText(text, new Point(24, 24));
        }
    }

    private static Point ToPoint(System.Numerics.Vector2 p) => new(p.X, p.Y);

    private static void DrawGizmo(DrawingContext draw, CreatureNode node, float spacing)
    {
        var center = ToPoint(node.Position);
        var guidePen = new Pen(new SolidColorBrush(Color.FromArgb(150, 83, 197, 139)), 1) { DashStyle = DashStyles.Dash };
        draw.DrawEllipse(null, guidePen, center, spacing, spacing);
        var direction = ChainMath.GetDirectionFromRotation(node.Rotation);
        var handle = new Point(center.X + direction.X * spacing, center.Y + direction.Y * spacing);
        draw.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(130, 255, 186)), 2), center, handle);
        draw.DrawEllipse(new SolidColorBrush(Color.FromRgb(130, 255, 186)), null, handle, 5, 5);
    }

    private static void DrawNode(DrawingContext draw, CreatureNode node, bool selected)
    {
        var center = new Point(node.Position.X, node.Position.Y);
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
        if (State.Mode != EditorMode.Create) return;
        if (State.SelectedNode is not null && IsNearDirectionHandle(world, State.SelectedNode)) { rotating = true; CaptureMouse(); UpdateRotation(world); return; }
        if (State.Tool == EditorTool.Node && State.Creature.Nodes.All(n => System.Numerics.Vector2.Distance(n.Position, world) > n.Radius))
        {
            State.CreateNode(world); CaptureMouse(); dragging = State.SelectedNode; dragOffset = new Vector(0, 0); return;
        }
        State.SelectAt(world);
        if (State.SelectedNode is not null) { dragging = State.SelectedNode; dragOffset = new Vector(State.SelectedNode.Position.X - world.X, State.SelectedNode.Position.Y - world.Y); CaptureMouse(); }
    }

    private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || State.Mode != EditorMode.Create) return;
        var world = State.Coordinates.ScreenToWorld(e.GetPosition(this));
        if (rotating && State.SelectedNode is not null) { UpdateRotation(world); return; }
        if (dragging is null) return;
        if (State.Creature.Nodes.IndexOf(dragging) > 0) return;
        var offset = new System.Numerics.Vector2((float)(world.X + dragOffset.X) - dragging.Position.X, (float)(world.Y + dragOffset.Y) - dragging.Position.Y);
        foreach (var node in State.Creature.Nodes) node.Position += offset;
        State.Select(dragging);
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e) { dragging = null; rotating = false; ReleaseMouseCapture(); }

    private bool IsNearDirectionHandle(System.Numerics.Vector2 world, CreatureNode node)
    {
        var handle = node.Position + ChainMath.GetDirectionFromRotation(node.Rotation) * State.Creature.ChainSettings.Spacing;
        return System.Numerics.Vector2.Distance(world, handle) <= 14;
    }

    private void UpdateRotation(System.Numerics.Vector2 world)
    {
        if (State.SelectedNode is null) return;
        var delta = world - State.SelectedNode.Position;
        if (delta.LengthSquared() > 0.001f) State.SetSelectedRotation(MathF.Atan2(delta.Y, delta.X) * 180 / MathF.PI);
    }
}
