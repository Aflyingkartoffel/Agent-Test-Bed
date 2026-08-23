using System.Numerics;
using CreatureConstructionLab.Editor;

var passed = 0;
Run("starts empty", () => { var s = new EditorState(); Check(s.Creature.Nodes.Count == 0 && s.Creature.Connections.Count == 0); });
Run("first node can be created", () => { var s = new EditorState(); s.CreateNode(new Vector2(40, 60)); Check(s.Creature.Nodes.Count == 1); });
Run("next node adds connection and selects child", () => { var s = new EditorState(); var root = s.CreateNode(Vector2.Zero); var child = s.AddNextNode(); Check(child is not null && s.Creature.Nodes.Count == 2 && s.Creature.Connections.Count == 1 && s.SelectedNode == child && s.Creature.Connections[0].ParentNodeId == root.Id); });
Run("spacing is center to center regardless of radius", () => { var s = new EditorState(); var root = s.CreateNode(Vector2.Zero); root.Radius = 10; var child = s.AddNextNode()!; child.Radius = 100; Check(Math.Abs(Vector2.Distance(root.Position, child.Position) - s.Creature.ChainSettings.Spacing) < 0.001f); });
Run("rotation zero points right", () => { var s = new EditorState(); s.CreateNode(Vector2.Zero).Rotation = 0; var child = s.AddNextNode()!; Check(Vector2.Distance(child.Position, new Vector2(50, 0)) < 0.001f); });
Run("rotation ninety points down", () => { var s = new EditorState(); s.CreateNode(Vector2.Zero).Rotation = 90; var child = s.AddNextNode()!; Check(Vector2.Distance(child.Position, new Vector2(0, 50)) < 0.001f); });
Run("repeated creation is ordered", () => { var s = new EditorState(); var root = s.CreateNode(Vector2.Zero); var first = s.AddNextNode()!; var second = s.AddNextNode()!; Check(s.Creature.Nodes[0] == root && s.Creature.Nodes[1] == first && s.Creature.Nodes[2] == second && s.Creature.Connections.Count == 2); });
Run("node indices are list order", () => { var s = new EditorState(); s.CreateNode(Vector2.Zero); s.AddNextNode(); s.AddNextNode(); Check(s.Creature.Nodes.Select((node, index) => s.Creature.Nodes.IndexOf(node) == index).All(x => x)); });
Run("spacing rebuild updates all rest lengths", () => { var s = new EditorState(); s.CreateNode(Vector2.Zero); s.AddNextNode(); s.AddNextNode(); s.SetSpacing(80); Check(s.Creature.Connections.All(c => c.RestLength == 80) && s.Creature.Nodes.Zip(s.Creature.Nodes.Skip(1)).All(pair => Math.Abs(Vector2.Distance(pair.First.Position, pair.Second.Position) - 80) < 0.001f)); });
Run("spacing rebuild preserves directions", () => { var s = new EditorState(); s.CreateNode(Vector2.Zero); s.AddNextNode()!.Rotation = 45; s.AddNextNode(); var before = Vector2.Normalize(s.Creature.Nodes[2].Position - s.Creature.Nodes[1].Position); s.SetSpacing(100); var after = Vector2.Normalize(s.Creature.Nodes[2].Position - s.Creature.Nodes[1].Position); Check(Vector2.Distance(before, after) < 0.001f); });
Run("end deletion removes connection", () => { var s = new EditorState(); s.CreateNode(Vector2.Zero); s.AddNextNode(); s.DeleteSelected(); Check(s.Creature.Nodes.Count == 1 && s.Creature.Connections.Count == 0); });
Run("middle deletion removes descendants", () => { var s = new EditorState(); s.CreateNode(Vector2.Zero); var middle = s.AddNextNode()!; s.AddNextNode(); s.Select(middle); s.DeleteSelected(); Check(s.Creature.Nodes.Count == 1 && s.Creature.Connections.Count == 0); });
Run("mode switching preserves chain", () => { var s = new EditorState(); s.CreateNode(Vector2.Zero); s.AddNextNode(); s.SetMode(EditorMode.Play); s.SetMode(EditorMode.Create); Check(s.Creature.Nodes.Count == 2 && s.Creature.Connections.Count == 1); });
Run("reset removes nodes and connections", () => { var s = new EditorState(); s.CreateNode(Vector2.Zero); s.AddNextNode(); s.Reset(); Check(s.Creature.Nodes.Count == 0 && s.Creature.Connections.Count == 0); });
Run("direction helper normalizes construction", () => { var result = ChainMath.GetPositionAtSpacing(Vector2.Zero, new Vector2(3, 4), 10); Check(Vector2.Distance(result, new Vector2(6, 8)) < 0.001f); });
Run("constrain helper ignores candidate distance", () => { var result = ChainMath.ConstrainToSpacing(Vector2.Zero, new Vector2(100, 0), 50, Vector2.UnitY); Check(Vector2.Distance(result, new Vector2(50, 0)) < 0.001f); });
Console.WriteLine($"{passed} editor tests passed.");

void Run(string name, Action test) { test(); passed++; Console.WriteLine($"PASS {name}"); }
void Check(bool condition) { if (!condition) throw new InvalidOperationException("assertion failed"); }
