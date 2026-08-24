using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Numerics;
using CreatureConstructionLab.Editor;
using CreatureConstructionLab.Model;

namespace CreatureConstructionLab.Rendering;

public sealed class BodySizeRampCanvas : FrameworkElement
{
    private readonly DrawingVisual visual = new();
    private readonly EditorState state;
    private RampPoint? selectedPoint;
    private bool dragging;
    private bool draggingHandle;
    private bool draggingOutgoing;
    private RampPoint? hoveredHandlePoint;
    private bool hoveredHandleOutgoing;

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
        var curve = new StreamGeometry();
        using (var curveContext = curve.Open())
        {
            curveContext.BeginFigure(ToCanvasValue(0), false, false);
            for (var i = 1; i <= 120; i++)
            {
                var t = i / 120f;
                curveContext.LineTo(ToCanvasValue(t), true, false);
            }
        }
        curve.Freeze();
        draw.DrawGeometry(null, curvePen, curve);
        foreach (var point in points)
        {
            var p = ToCanvas(point);
            if (state.Creature.BodySizeRamp.Interpolation == RampInterpolationMode.Bezier)
            {
                DrawHandle(draw, point, point.InHandle, false);
                DrawHandle(draw, point, point.OutHandle, true);
            }
            draw.DrawEllipse(new SolidColorBrush(point == selectedPoint ? Color.FromRgb(220, 255, 225) : Color.FromRgb(100, 230, 156)), new Pen(new SolidColorBrush(Color.FromRgb(4, 16, 11)), 1), p, point == selectedPoint ? 5 : 4, point == selectedPoint ? 5 : 4);
        }
    }

    private void DrawHandle(DrawingContext draw, RampPoint point, Vector2? offset, bool outgoing)
    {
        if (!offset.HasValue) return;
        var anchor = ToCanvas(point);
        var handle = ToCanvasHandle(point, offset.Value);
        draw.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(91, 165, 122)), 1), anchor, handle);
        var highlighted = point == hoveredHandlePoint && outgoing == hoveredHandleOutgoing;
        var selected = point == selectedPoint && draggingHandle && outgoing == draggingOutgoing;
        var color = selected ? Color.FromRgb(255, 245, 170) : highlighted ? Color.FromRgb(225, 255, 230) : outgoing ? Color.FromRgb(255, 215, 120) : Color.FromRgb(150, 205, 255);
        draw.DrawEllipse(new SolidColorBrush(color), null, handle, highlighted || selected ? 5 : 4, highlighted || selected ? 5 : 4);
    }

    private Point ToCanvas(RampPoint point) => new(point.Position * Math.Max(1, ActualWidth), ActualHeight - ValueToY(point.Value));
    private Point ToCanvasHandle(RampPoint point, Vector2 offset) => new((point.Position + offset.X) * Math.Max(1, ActualWidth), ActualHeight - ValueToY(point.Value + offset.Y));
    private Point ToCanvasValue(float position) => new(position * Math.Max(1, ActualWidth), ActualHeight - ValueToY(state.Creature.BodySizeRamp.Sample(position)));
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
        if (state.Creature.BodySizeRamp.Interpolation == RampInterpolationMode.Bezier)
        {
            foreach (var point in state.Creature.BodySizeRamp.Points)
            {
                if (point.OutHandle is Vector2 outgoing && (ToCanvasHandle(point, outgoing) - mouse).Length <= 10)
                { selectedPoint = point; draggingHandle = true; draggingOutgoing = true; state.BeginHistoryGroup(); CaptureMouse(); InvalidateVisual(); return; }
                if (point.InHandle is Vector2 incoming && (ToCanvasHandle(point, incoming) - mouse).Length <= 10)
                { selectedPoint = point; draggingHandle = true; draggingOutgoing = false; state.BeginHistoryGroup(); CaptureMouse(); InvalidateVisual(); return; }
            }
        }
        selectedPoint = state.Creature.BodySizeRamp.Points.OrderBy(p => (ToCanvas(p) - mouse).Length).FirstOrDefault();
        if (selectedPoint is not null && (ToCanvas(selectedPoint) - mouse).Length <= 12) { state.BeginHistoryGroup(); dragging = true; CaptureMouse(); }
        else selectedPoint = null;
        InvalidateVisual();
    }

    private bool TryGetHandleAt(Point mouse, out RampPoint? point, out bool outgoing)
    {
        point = null;
        outgoing = false;
        if (state.Creature.BodySizeRamp.Interpolation != RampInterpolationMode.Bezier) return false;
        foreach (var candidate in state.Creature.BodySizeRamp.Points)
        {
            if (candidate.OutHandle is Vector2 outOffset && (ToCanvasHandle(candidate, outOffset) - mouse).Length <= 10)
            { point = candidate; outgoing = true; return true; }
            if (candidate.InHandle is Vector2 inOffset && (ToCanvasHandle(candidate, inOffset) - mouse).Length <= 10)
            { point = candidate; return true; }
        }
        return false;
    }

    private void OnDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (state.Mode != EditorMode.Create || e.ClickCount != 2) return;
        if (state.Creature.BodySizeRamp.Interpolation == RampInterpolationMode.Bezier && state.Creature.BodySizeRamp.Points.Any(p => (ToCanvas(p) - e.GetPosition(this)).Length <= 12 || (p.OutHandle is Vector2 o && (ToCanvasHandle(p, o) - e.GetPosition(this)).Length <= 10) || (p.InHandle is Vector2 i && (ToCanvasHandle(p, i) - e.GetPosition(this)).Length <= 10))) return;
        var value = FromCanvas(e.GetPosition(this));
        selectedPoint = state.AddRampPoint(value.Position, value.Value);
        InvalidateVisual();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (state.Mode != EditorMode.Create) return;
        if (!dragging && !draggingHandle && e.LeftButton != MouseButtonState.Pressed)
        {
            hoveredHandlePoint = TryGetHandleAt(e.GetPosition(this), out var point, out var outgoing) ? point : null;
            hoveredHandleOutgoing = outgoing;
            Cursor = hoveredHandlePoint is null ? Cursors.Arrow : Cursors.Hand;
            InvalidateVisual();
            return;
        }
        if ((!dragging && !draggingHandle) || selectedPoint is null || e.LeftButton != MouseButtonState.Pressed) return;
        if (draggingHandle)
        {
            var value = FromCanvas(e.GetPosition(this));
            state.SetRampHandle(selectedPoint, draggingOutgoing, new Vector2(value.Position - selectedPoint.Position, value.Value - selectedPoint.Value));
        }
        else
        {
            var value = FromCanvas(e.GetPosition(this));
            state.SetRampPoint(selectedPoint, value.Position, value.Value);
        }
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e) { if (dragging || draggingHandle) state.EndHistoryGroup(); dragging = false; draggingHandle = false; ReleaseMouseCapture(); Cursor = Cursors.Arrow; }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (state.Mode == EditorMode.Create && (e.Key is Key.Delete or Key.Back) && selectedPoint is not null)
        {
            if (state.RemoveRampPoint(selectedPoint)) selectedPoint = null;
            e.Handled = true;
        }
    }
}
