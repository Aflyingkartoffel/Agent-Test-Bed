using System.Numerics;
using CreatureConstructionLab.Model;

namespace CreatureConstructionLab.Simulation;

public sealed class CreatureSimulator
{
    public CreaturePlayState State { get; } = new();
    public void Reset(CreatureDefinition definition) => State.ResetFromDefinition(definition);
    public void Update(CreatureDefinition definition, float elapsedSeconds) => State.Advance(definition, elapsedSeconds);
    public void SetTarget(Vector2 target) => State.SetTarget(target);
}
