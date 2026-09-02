using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;

namespace EquationSynth;

public sealed class EquationProject {
    public EquationDocument Document { get; set; } = EquationDocument.CreateDefault();
    public GraphProjectSettings Graph { get; set; } = new();
    public AudioProjectSettings Audio { get; set; } = new();
    public TimeProjectSettings Time { get; set; } = new();
    public TrailProjectSettings Trails { get; set; } = new();
    public string? FilePath { get; set; }
    [JsonIgnore] public bool IsDirty { get; set; }
    public static EquationProject CreateNew() => new();
    public void Rebuild() { foreach (var entry in Document.Equations) entry.TryParse(out _); Document.ReconcileParameters(); }
}
public sealed class GraphProjectSettings { public bool GridEnabled { get; set; } = true; public bool AxesEnabled { get; set; } = true; public double XMin { get; set; } = -10; public double XMax { get; set; } = 10; public double YMin { get; set; } = -5; public double YMax { get; set; } = 5; }
public sealed class AudioProjectSettings { public double Frequency { get; set; } = 440; public double Volume { get; set; } = .18; }
public sealed class TimeProjectSettings { public double CurrentTime { get; set; } public double TimeScale { get; set; } = 1; public double TimelineStart { get; set; } public double TimelineEnd { get; set; } = 10; public bool LoopEnabled { get; set; } }
public sealed class TrailProjectSettings { public bool Enabled { get; set; } public int Count { get; set; } = 5; }
public static class ProjectValidator {
    public static IReadOnlyList<string> Validate(EquationProject project) { var errors=new List<string>();var ids=new HashSet<Guid>();foreach(var e in project.Document.Equations){if(!ids.Add(e.Id))errors.Add($"Duplicate equation ID: {e.Id}");if(!e.TryParse(out var parse))errors.Add($"Equation '{e.ExpressionText}': {parse}");}if(project.Document.SelectedEquationId is { } selected&&!ids.Contains(selected))errors.Add("Selected equation does not exist.");var validParameters=true;foreach(var p in project.Document.Parameters.Items){if(!p.IsMetadataValid){validParameters=false;errors.Add($"Parameter '{p.Name}' must have Minimum < Maximum and Step > 0.");}if(!double.IsFinite(p.ManualValue))errors.Add($"Parameter '{p.Name}' value must be finite.");if(!double.IsFinite(p.DefaultValue)||!double.IsFinite(p.Automation.Center)||!double.IsFinite(p.Automation.Amplitude)||!double.IsFinite(p.Automation.Frequency)||!double.IsFinite(p.Automation.Phase))errors.Add($"Parameter '{p.Name}' automation values must be finite.");if(p.Automation.Mode==AutomationMode.Expression&&!p.Automation.TryParse(out var automationError))errors.Add($"Parameter '{p.Name}' automation: {automationError}");}if(validParameters){var automationResult=new AutomationEngine().Evaluate(0,project.Document.Parameters.Items);if(automationResult.Error is not null)errors.Add(automationResult.Error);}if(!double.IsFinite(project.Time.TimelineStart)||!double.IsFinite(project.Time.TimelineEnd)||project.Time.TimelineStart>=project.Time.TimelineEnd)errors.Add("Timeline start must be less than end.");if(!double.IsFinite(project.Audio.Frequency)||project.Audio.Frequency<=0)errors.Add("Audio frequency must be positive and finite.");if(!double.IsFinite(project.Audio.Volume)||project.Audio.Volume<0||project.Audio.Volume>.5)errors.Add("Audio volume must be between 0 and 0.5.");return errors; }
}
public sealed class ProjectHistory<T> {
    readonly int capacity; readonly Stack<T> undo=new(); readonly Stack<T> redo=new(); public ProjectHistory(int capacity=100)=>this.capacity=capacity; public bool CanUndo=>undo.Count>0;public bool CanRedo=>redo.Count>0;public int Count=>undo.Count;
    public void Record(T snapshot){undo.Push(snapshot);while(undo.Count>capacity){var keep=undo.ToArray().Take(capacity).Reverse().ToArray();undo.Clear();foreach(var item in keep)undo.Push(item);}redo.Clear();}public T? Undo(T current){if(!CanUndo)return default;redo.Push(current);return undo.Pop();}public T? Redo(T current){if(!CanRedo)return default;undo.Push(current);return redo.Pop();}public void Clear(){undo.Clear();redo.Clear();}
}
public static class ProjectFileService { public static void Save(string path,EquationProject project){var errors=ProjectValidator.Validate(project);if(errors.Count>0)throw new InvalidDataException(string.Join(Environment.NewLine,errors));File.WriteAllText(path,JsonSerializer.Serialize(project,new JsonSerializerOptions{WriteIndented=true}));}public static EquationProject Load(string path){var project=JsonSerializer.Deserialize<EquationProject>(File.ReadAllText(path))??throw new InvalidDataException("Project file was empty.");project.Rebuild();var errors=ProjectValidator.Validate(project);if(errors.Count>0)throw new InvalidDataException(string.Join(Environment.NewLine,errors));project.FilePath=path;project.IsDirty=false;return project;} }

public static class AuthoredStateCodec {
    static readonly JsonSerializerOptions Options = new() { WriteIndented = false };
    public static string Serialize(PresetState state) => JsonSerializer.Serialize(state, Options);
    public static PresetState Deserialize(string text) => JsonSerializer.Deserialize<PresetState>(text) ?? throw new InvalidDataException("Project state was empty.");
    public static bool Equal(string? left, string? right) => string.Equals(left, right, StringComparison.Ordinal);
}
