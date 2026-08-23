using System.Numerics;
using CreatureConstructionLab.Model;

namespace CreatureConstructionLab.Editor;

public static class ChainMath
{
    public static Vector2 GetDirectionFromRotation(float degrees)
    {
        var radians = degrees * MathF.PI / 180;
        return new Vector2(MathF.Cos(radians), MathF.Sin(radians));
    }

    public static Vector2 GetPositionAtSpacing(Vector2 origin, Vector2 direction, float spacing)
        => origin + Vector2.Normalize(direction) * spacing;

    public static Vector2 ConstrainToSpacing(Vector2 parentPosition, Vector2 candidatePosition, float spacing, Vector2 fallbackDirection)
    {
        var direction = candidatePosition - parentPosition;
        if (direction.LengthSquared() < 0.0001f) direction = fallbackDirection;
        return GetPositionAtSpacing(parentPosition, direction, spacing);
    }

    public static void RebuildChainSpacing(CreatureDefinition creature)
    {
        var directions = new Vector2[Math.Max(0, creature.Nodes.Count - 1)];
        for (var i = 0; i < directions.Length; i++)
        {
            directions[i] = creature.Nodes[i + 1].Position - creature.Nodes[i].Position;
            if (directions[i].LengthSquared() < 0.0001f) directions[i] = GetDirectionFromRotation(creature.Nodes[i].Rotation);
            else directions[i] = Vector2.Normalize(directions[i]);
        }
        for (var i = 0; i < creature.Nodes.Count - 1; i++)
        {
            var parent = creature.Nodes[i];
            var child = creature.Nodes[i + 1];
            var direction = directions[i];
            child.Position = GetPositionAtSpacing(parent.Position, direction, creature.ChainSettings.Spacing);
            child.Rotation = MathF.Atan2(direction.Y, direction.X) * 180 / MathF.PI;
            var connection = creature.Connections.FirstOrDefault(c => c.ParentNodeId == parent.Id && c.ChildNodeId == child.Id);
            if (connection is not null) connection.RestLength = creature.ChainSettings.Spacing;
        }
    }
}
