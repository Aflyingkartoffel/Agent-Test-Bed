using System.Numerics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using CreatureConstructionLab.Model;

namespace CreatureConstructionLab.IO;

public static class CreatureFileService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };

    public static void Save(string path, CreatureDefinition creature)
    {
        var file = new CreatureFile
        {
            Nodes = creature.Nodes.Select(n => new NodeFile { Id = n.Id.ToString(), PositionX = n.Position.X, PositionY = n.Position.Y, Rotation = n.Rotation }).ToList(),
            Connections = creature.Connections.Select(c => new ConnectionFile { ParentNodeId = c.ParentNodeId.ToString(), ChildNodeId = c.ChildNodeId.ToString(), RestLength = c.RestLength, Stiffness = c.Stiffness, Damping = c.Damping }).ToList(),
            Chain = new ChainFile { Spacing = creature.ChainSettings.Spacing, Stiffness = creature.ChainSettings.Stiffness, Damping = creature.ChainSettings.Damping },
            Body = new BodyFile { BaseRadius = creature.BaseRadius, Interpolation = creature.BodySizeRamp.Interpolation, RampPoints = creature.BodySizeRamp.Points.Select(p => new RampPointFile { Position = p.Position, Value = p.Value }).ToList() }
        };
        File.WriteAllText(path, JsonSerializer.Serialize(file, JsonOptions));
    }

    public static bool TryLoad(string path, out CreatureDefinition? creature, out string error)
    {
        creature = null;
        try
        {
            var file = JsonSerializer.Deserialize<CreatureFile>(File.ReadAllText(path), JsonOptions);
            if (file is null) { error = "The file is empty."; return false; }
            if (file.Nodes is null || file.Connections is null || file.Chain is null || file.Body is null || file.Body.RampPoints is null) { error = "The file is missing required sections."; return false; }
            if (!FinitePositive(file.Chain.Spacing) || !FiniteNonNegative(file.Chain.Stiffness) || !FiniteNonNegative(file.Chain.Damping) || !FinitePositive(file.Body.BaseRadius)) { error = "The file contains invalid settings."; return false; }
            if (file.Body.RampPoints.Count < 2 || file.Body.RampPoints[0].Position != 0 || file.Body.RampPoints[^1].Position != 1) { error = "The ramp must have valid 0 and 1 endpoints."; return false; }
            for (var i = 0; i < file.Body.RampPoints.Count; i++)
            {
                var point = file.Body.RampPoints[i];
                if (!Finite(point.Position) || !Finite(point.Value) || point.Position is < 0 or > 1 || point.Value is < BodySizeRamp.MinValue or > BodySizeRamp.MaxValue || (i > 0 && point.Position <= file.Body.RampPoints[i - 1].Position)) { error = "The file contains invalid ramp points."; return false; }
            }
            var ids = new HashSet<Guid>();
            var nodes = new List<CreatureNode>();
            foreach (var node in file.Nodes)
            {
                if (!Guid.TryParse(node.Id, out var id) || !ids.Add(id) || !Finite(node.PositionX) || !Finite(node.PositionY) || !Finite(node.Rotation)) { error = "The file contains invalid or duplicate nodes."; return false; }
                nodes.Add(new CreatureNode { Id = id, Position = new Vector2(node.PositionX, node.PositionY), Rotation = node.Rotation });
            }
            if (file.Connections.Count != Math.Max(0, nodes.Count - 1)) { error = "The file contains an invalid chain connection count."; return false; }
            var connections = new List<CreatureConnection>();
            for (var i = 0; i < file.Connections.Count; i++)
            {
                var saved = file.Connections[i];
                if (!Guid.TryParse(saved.ParentNodeId, out var parent) || !Guid.TryParse(saved.ChildNodeId, out var child) || !ids.Contains(parent) || !ids.Contains(child) || !FinitePositive(saved.RestLength) || Math.Abs(saved.RestLength - file.Chain.Spacing) > 0.01f || !FiniteNonNegative(saved.Stiffness) || !FiniteNonNegative(saved.Damping) || parent != nodes[i].Id || child != nodes[i + 1].Id) { error = "The file contains invalid chain connections."; return false; }
                connections.Add(new CreatureConnection { ParentNodeId = parent, ChildNodeId = child, RestLength = saved.RestLength, Stiffness = saved.Stiffness, Damping = saved.Damping });
            }
            creature = new CreatureDefinition { BaseRadius = file.Body.BaseRadius };
            creature.Nodes.AddRange(nodes);
            creature.Connections.AddRange(connections);
            creature.ChainSettings.Spacing = file.Chain.Spacing;
            creature.ChainSettings.Stiffness = file.Chain.Stiffness;
            creature.ChainSettings.Damping = file.Chain.Damping;
            creature.BodySizeRamp.Points.Clear();
            creature.BodySizeRamp.Interpolation = file.Body.Interpolation;
            foreach (var point in file.Body.RampPoints) creature.BodySizeRamp.Points.Add(new RampPoint(point.Position, point.Value));
            error = "";
            return true;
        }
        catch (JsonException) { error = "The file is not valid creature JSON."; return false; }
        catch (IOException) { error = "The creature file could not be read."; return false; }
        catch (UnauthorizedAccessException) { error = "The creature file could not be accessed."; return false; }
        catch (Exception) { error = "The creature file has an invalid structure."; return false; }
    }

    private static bool Finite(float value) => float.IsFinite(value);
    private static bool FinitePositive(float value) => Finite(value) && value > 0;
    private static bool FiniteNonNegative(float value) => Finite(value) && value >= 0;

    public sealed class CreatureFile { public List<NodeFile>? Nodes { get; set; } public List<ConnectionFile>? Connections { get; set; } public ChainFile? Chain { get; set; } public BodyFile? Body { get; set; } }
    public sealed class NodeFile { public string? Id { get; set; } public float PositionX { get; set; } public float PositionY { get; set; } public float Rotation { get; set; } }
    public sealed class ConnectionFile { public string? ParentNodeId { get; set; } public string? ChildNodeId { get; set; } public float RestLength { get; set; } public float Stiffness { get; set; } public float Damping { get; set; } }
    public sealed class ChainFile { public float Spacing { get; set; } public float Stiffness { get; set; } public float Damping { get; set; } }
    public sealed class BodyFile { public float BaseRadius { get; set; } public RampInterpolationMode Interpolation { get; set; } = RampInterpolationMode.Linear; public List<RampPointFile>? RampPoints { get; set; } }
    public sealed class RampPointFile { public float Position { get; set; } public float Value { get; set; } }
}
