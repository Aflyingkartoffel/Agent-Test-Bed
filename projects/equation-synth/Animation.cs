namespace EquationSynth;

public enum AutomationMode { Off, Sine, Cosine, Expression }
public sealed class ParameterAutomation {
    public AutomationMode Mode { get; set; }
    public double Center { get; set; } = 1; public double Amplitude { get; set; } = 1; public double Frequency { get; set; } = .5; public double Phase { get; set; }
    public string ExpressionText { get; set; } = "0";
    [System.Text.Json.Serialization.JsonIgnore] public Expression? ParsedExpression { get; set; }
    public bool TryParse(out string error) { if (Mode != AutomationMode.Expression) { error=""; return true; } try { ParsedExpression=EquationSynth.Expression.Parse(ExpressionText); error=""; return true; } catch(ParseException ex){error=ex.Message;return false;} }
}

public sealed class AutomationResult { public Dictionary<string,double> Values { get; }=new(StringComparer.OrdinalIgnoreCase); public string? Error { get; internal set; } }
public sealed class AutomationEngine {
    public AutomationResult Evaluate(double time, IEnumerable<Parameter> parameters) {
        var list=parameters.ToDictionary(x=>x.Name,StringComparer.OrdinalIgnoreCase);var result=new AutomationResult();var visiting=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var done=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        double Resolve(string name){if(done.Contains(name))return result.Values[name];if(!list.TryGetValue(name,out var p))return double.NaN;if(!visiting.Add(name)){result.Error=$"Automation dependency cycle involving {name}";return double.NaN;}double value=p.ManualValue;switch(p.Automation.Mode){case AutomationMode.Sine:value=p.Automation.Center+Math.Sin(Math.Tau*p.Automation.Frequency*time+p.Automation.Phase)*p.Automation.Amplitude;break;case AutomationMode.Cosine:value=p.Automation.Center+Math.Cos(Math.Tau*p.Automation.Frequency*time+p.Automation.Phase)*p.Automation.Amplitude;break;case AutomationMode.Expression:if(p.Automation.ParsedExpression is null&&!p.Automation.TryParse(out var error)){result.Error=$"Automation error for {name}: {error}";break;}var refs=p.Automation.ParsedExpression!.Parameters.ToDictionary(x=>x,Resolve,StringComparer.OrdinalIgnoreCase);value=p.Automation.ParsedExpression.Evaluate(0,time,refs).Value;break;}if(!double.IsFinite(value))value=p.ManualValue;result.Values[name]=Math.Clamp(value,p.Minimum,p.Maximum);visiting.Remove(name);done.Add(name);return result.Values[name];}
        foreach(var p in list.Values)Resolve(p.Name);return result;
    }
}

public sealed class TrailManager {
    readonly Queue<(double Time,List<GraphPoint> Points)> history=new(); public bool Enabled {get;set;} public int Count {get;set;}=5; public double Interval {get;set;}=.1; double last=double.NegativeInfinity; public IReadOnlyCollection<(double Time,List<GraphPoint> Points)> History=>history;
    public void Record(double time,List<GraphPoint> points){if(!Enabled||time-last<Interval)return;last=time;history.Enqueue((time,points));while(history.Count>Math.Clamp(Count,1,20))history.Dequeue();}public void Clear(){history.Clear();last=double.NegativeInfinity;}
}
