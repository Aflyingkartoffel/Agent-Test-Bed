using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using CreatureConstructionLab.Editor;
using CreatureConstructionLab.Model;

namespace CreatureConstructionLab.Rendering;

public sealed class BodySizeRampCanvas : FrameworkElement
{
    private readonly DrawingVisual visual = new();
    private readonly EditorState state;
    private RampPoint? selectedPoint;
    private bool dragging;

    public BodySizeRampCanvas(EditorState state)
    {
        this.state = state;
        AddVisualChild(visual);
        AddLogicalChild(visual);
        state.Changed += InvalidateVisual;
        Focusable = true;
        MouseLeftButtonDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseLeftButtonUp += OnMouseUp;
        MouseLeftButtonDown += OnDoubleClick;
        KeyDown += OnKeyDown;
    }

    protected override int VisualChildrenCount => 1;
    protected override Visual GetVisualChild(int index) => visual;

    protected override void OnRender(DrawingContext context)
    {
        base.OnRender(context);
        using var draw = visual.RenderOpen();
        var background = new SolidColorBrush(Color.FromRgb(4, 16, 11));
        draw.DrawRectangle(background, new Pen(new SolidColorBrush(Color.FromRgb(39, 122, 81)), 1), new Rect(RenderSize));
        var gridPen = new Pen(new SolidColorBrush(Color.FromRgb(14, 53, 34)), 1);
        for (var i = 1; i < 5; i++)
        {
            var x = ActualWidth * i / 5;
            draw.DrawLine(gridPen, new Point(x, 0), new Point(x, ActualHeight));
        }
        var curvePen = new Pen(new SolidColorBrush(Color.FromRgb(100, 230, 156)), 2);
        var points = state.Creature.BodySizeRamp.Points;
        for (var i = 1; i < points.Count; i++) draw.DrawLine(curvePen, ToCanvas(points[i - 1]), ToCanvas(points[i]));
        foreach (var point in points)
        {
            var p = ToCanvas(point);
            draw.DrawEllipse(new SolidColorBrush(point == selectedPoint ? Color.FromRgb(220, 255, 225) : Color.FromRgb(100, 230, 156)), new Pen(new SolidColorBrush(Color.FromRgb(4, 16, 11)), 1), p, point == selectedPoint ? 5 : 4, point == selectedPoint ? 5 : 4);
        }
    }

    private Point ToCanvas(RampPoint point) => new(point.Position * Math.Max(1, ActualWidth), ActualHeight - ValueToY(point.Value));
    private double ValueToY(float value) => (value - BodySizeRamp.MinValue) / (BodySizeRamp.MaxValue - BodySizeRamp.MinValue) * Math.Max(1, ActualHeight);
    private (float Position, float Value) FromCanvas(Point point)
    {
        var position = Math.Clamp((float)(point.X / Math.Max(1, ActualWidth)), 0, 1);
        var value = BodySizeRamp.MinValue + (float)((ActualHeight - point.Y) / Math.Max(1, ActualHeight)) * (BodySizeRamp.MaxValue - BodySizeRamp.MinValue);
        return (position, Math.Clamp(value, BodySizeRamp.MinValue, BodySizeRamp.MaxValue));
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (state.Mode != EditorMode.Create) return;
        Focus();
        var mouse = e.GetPosition(this);
        selectedPoint = state.Creature.BodySizeRamp.Points.OrderBy(p => (ToCanvas(p) - mouse).Length).FirstOrDefault();
        if (selectedPoint is not null && (ToCanvas(selectedPoint) - mouse).Length <= 12) { dragging = true; CaptureMouse(); }
        else selectedPoint = null;
        InvalidateVisual();
    }

    private void OnDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (state.Mode != EditorMode.Create || e.ClickCount != 2) return;
        var value = FromCanvas(e.GetPosition(this));
        selectedPoint = state.AddRampPoint(value.Position, value.Value);
        InvalidateVisual();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (state.Mode != EditorMode.Create || !dragging || selectedPoint is null || e.LeftButton != MouseButtonState.Pressed) return;
        var value = FromCanvas(e.GetPosition(this));
        state.SetRampPoint(selectedPoint, value.Position, value.Value);
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e) { dragging = false; ReleaseMouseCapture(); }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (state.Mode == EditorMode.Create && (e.Key is Key.Delete or Key.Back) && selectedPoint is not null)
        {
            if (state.RemoveRampPoint(selectedPoint)) selectedPoint = null;
            e.Handled = true;
        }
    }
}
