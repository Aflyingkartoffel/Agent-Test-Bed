using System.Numerics;
using CreatureConstructionLab.Editor;

var passed = 0;
Run("starts empty", () => { var s = new EditorState(); Check(s.Creature.Nodes.Count == 0); });
Run("creates one node at position", () => { var s = new EditorState(); var n = s.CreateNode(new Vector2(40, 60)); Check(s.Creature.Nodes.Count == 1 && n.Position == new Vector2(40, 60) && s.SelectedNode == n); });
Run("selects node", () => { var s = new EditorState(); var n = s.CreateNode(new Vector2(40, 60)); s.SelectAt(new Vector2(45, 60)); Check(s.SelectedNode == n); });
Run("moves node", () => { var s = new EditorState(); var n = s.CreateNode(Vector2.Zero); n.Position = new Vector2(9, 12); Check(n.Position == new Vector2(9, 12)); });
Run("deletes selected node", () => { var s = new EditorState(); s.CreateNode(Vector2.Zero); s.DeleteSelected(); Check(s.Creature.Nodes.Count == 0 && s.SelectedNode is null); });
Run("supports multiple nodes and properties", () => { var s = new EditorState(); var a = s.CreateNode(Vector2.Zero); a.Radius = 12; a.Rotation = 30; s.CreateNode(new Vector2(1, 2)); Check(s.Creature.Nodes.Count == 2 && a.Radius == 12 && a.Rotation == 30); });
Run("mode switching preserves nodes", () => { var s = new EditorState(); s.CreateNode(Vector2.Zero); s.SetMode(EditorMode.Play); s.SetMode(EditorMode.Create); Check(s.Creature.Nodes.Count == 1); });
Run("reset empties creature", () => { var s = new EditorState(); s.CreateNode(Vector2.Zero); s.Reset(); Check(s.Creature.Nodes.Count == 0 && s.Mode == EditorMode.Create); });
Console.WriteLine($"{passed} editor tests passed.");

void Run(string name, Action test) { test(); passed++; Console.WriteLine($"PASS {name}"); }
void Check(bool condition) { if (!condition) throw new InvalidOperationException("assertion failed"); }
