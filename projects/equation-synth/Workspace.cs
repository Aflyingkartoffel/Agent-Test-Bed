using System.Globalization;
using System.Text.Json.Serialization;

namespace EquationSynth;

public sealed class EquationEntry {
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ExpressionText { get; set; } = "sin(x)";
    public string DisplayName { get; set; } = "sin(x)";
    public string ColorHex { get; set; } = "#69DAFF";
    public bool IsVisible { get; set; } = true;
    public bool IsAudioEnabled { get; set; }
    public bool IsMuted { get; set; }
    public bool IsSolo { get; set; }
    public double AudioFrequency { get; set; } = 440;
    public double AudioVolume { get; set; } = .25;
    public double AudioPan { get; set; }
    public EnvelopeSettings Envelope { get; set; } = new();
    public bool IsSelected { get; set; }
    [JsonIgnore] public Expression? ParsedExpression { get; set; }
    public bool TryParse(out string error) { try { ParsedExpression = Expression.Parse(ExpressionText); DisplayName = ExpressionText; error = ""; return true; } catch (ParseException ex) { error = ex.Message; return false; } }
}

public sealed class EquationDocument {
    public List<EquationEntry> Equations { get; set; } = [];
    public ParameterSet Parameters { get; } = new();
    public Guid? SelectedEquationId { get; set; }
    public EquationEntry? Selected => Equations.FirstOrDefault(x => x.Id == SelectedEquationId);
    public static EquationDocument CreateDefault() { var d = new EquationDocument(); d.Add("sin(x)"); return d; }
    public EquationEntry Add(string text = "sin(x)") { var e = new EquationEntry { ExpressionText = text, ColorHex = Palette[Equations.Count % Palette.Length], IsAudioEnabled = Equations.Count == 0 }; e.TryParse(out _); Equations.Add(e); Select(e); ReconcileParameters(); return e; }
    public void RemoveSelected() { if (Selected is { } e) Equations.Remove(e); if (Equations.Count == 0) Add(); else Select(Equations[Math.Min(Equations.Count - 1, 0)]); ReconcileParameters(); }
    public void Select(EquationEntry e) { foreach (var item in Equations) item.IsSelected = item.Id == e.Id; SelectedEquationId = e.Id; }
    public void ReconcileParameters(IEnumerable<string>? extraNames = null) { var old = Parameters.Items.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase); var names = Equations.Where(x => x.ParsedExpression is not null).SelectMany(x => x.ParsedExpression!.Parameters).Concat(extraNames ?? []).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x); Parameters.Items.Clear(); foreach (var name in names) { if (old.TryGetValue(name, out var p)) Parameters.Items.Add(p); else Parameters.Items.Add(new Parameter(name)); } }
    public static readonly string[] Palette = ["#69DAFF", "#FFB95D", "#74E39B", "#C69CFF", "#FFE36E", "#FF82B2"];
}

public readonly record struct GraphPoint(double X, double Y, bool Valid);
public sealed class GraphCamera {
    public double XMin { get; private set; } = -10; public double XMax { get; private set; } = 10; public double YMin { get; private set; } = -5; public double YMax { get; private set; } = 5;
    public double CenterX => (XMin + XMax) / 2; public double CenterY => (YMin + YMax) / 2; public double Width => XMax - XMin; public double Height => YMax - YMin;
    public void Reset() => Set(-10, 10, -5, 5);
    public bool Set(double xmin, double xmax, double ymin, double ymax) { if (!double.IsFinite(xmin)||!double.IsFinite(xmax)||!double.IsFinite(ymin)||!double.IsFinite(ymax)||xmin>=xmax||ymin>=ymax)return false; XMin=Math.Clamp(xmin,-1e9,1e9);XMax=Math.Clamp(xmax,-1e9,1e9);YMin=Math.Clamp(ymin,-1e9,1e9);YMax=Math.Clamp(ymax,-1e9,1e9);return true; }
    public void Pan(double dx,double dy) { Set(XMin+dx,XMax+dx,YMin+dy,YMax+dy); }
    public void ZoomAt(double factor,double x,double y) { factor=Math.Clamp(factor,.05,20);var nx=x+(XMin-x)*factor;var xx=x+(XMax-x)*factor;var ny=y+(YMin-y)*factor;var yy=y+(YMax-y)*factor;if(xx-nx>.000001&&yy-ny>.000001)Set(nx,xx,ny,yy); }
    public (double X,double Y) ScreenToWorld(double px,double py,double width,double height) => (XMin+px/width*Width,YMax-py/height*Height);
    public (double X,double Y) WorldToScreen(double x,double y,double width,double height) => ((x-XMin)/Width*width,(YMax-y)/Height*height);
}

public static class GraphSampler {
    public static List<GraphPoint> Sample(EquationEntry entry, GraphCamera camera, double time, IReadOnlyDictionary<string,double> parameters, double pixelWidth) { var count=Math.Clamp((int)(pixelWidth*1.5),200,2400);var result=new List<GraphPoint>(count);if(entry.ParsedExpression is null)return result;for(var i=0;i<count;i++){var x=camera.XMin+camera.Width*i/(count-1);var r=entry.ParsedExpression.Evaluate(x,time,parameters);result.Add(r.Status==ValueStatus.Valid&&double.IsFinite(r.Value)&&Math.Abs(r.Value)<1e9?new GraphPoint(x,r.Value,true):new GraphPoint(x,0,false));}return result; }
    public static double NiceSpacing(double range, int targetTicks=10) { if (!(range>0)||!double.IsFinite(range))return 1;var raw=range/Math.Max(1,targetTicks);var power=Math.Pow(10,Math.Floor(Math.Log10(raw)));var n=raw/power;return (n<=1?1:n<=2?2:n<=5?5:10)*power; }
    public static string FormatLabel(double value,double spacing) { var decimals=Math.Max(0,(int)Math.Ceiling(-Math.Log10(Math.Abs(spacing))));return value.ToString("F"+Math.Min(decimals,8),CultureInfo.InvariantCulture); }
}
