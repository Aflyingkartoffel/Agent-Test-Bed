using System.Windows;
using System.Windows.Media;
using CreatureConstructionLab.Editor;
using CreatureConstructionLab.Model;

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

        foreach (var node in State.Creature.Nodes) DrawNode(draw, node, node == State.SelectedNode);
        if (State.Mode == EditorMode.Play)
        {
            var text = new FormattedText("PLAY MODE  /  SIMULATION NOT IMPLEMENTED IN MILESTONE 1", System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Consolas"), 14, Brushes.SeaGreen, 1);
            draw.DrawText(text, new Point(24, 24));
        }
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
        if (State.Tool == EditorTool.Node && State.Creature.Nodes.All(n => System.Numerics.Vector2.Distance(n.Position, world) > n.Radius))
        {
            State.CreateNode(world); CaptureMouse(); dragging = State.SelectedNode; dragOffset = new Vector(0, 0); return;
        }
        State.SelectAt(world);
        if (State.SelectedNode is not null) { dragging = State.SelectedNode; dragOffset = new Vector(State.SelectedNode.Position.X - world.X, State.SelectedNode.Position.Y - world.Y); CaptureMouse(); }
    }

    private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (dragging is null || e.LeftButton != System.Windows.Input.MouseButtonState.Pressed || State.Mode != EditorMode.Create) return;
        var world = State.Coordinates.ScreenToWorld(e.GetPosition(this));
        dragging.Position = new System.Numerics.Vector2((float)(world.X + dragOffset.X), (float)(world.Y + dragOffset.Y));
        State.Select(dragging);
    }

    private void OnMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e) { dragging = null; ReleaseMouseCapture(); }
}
