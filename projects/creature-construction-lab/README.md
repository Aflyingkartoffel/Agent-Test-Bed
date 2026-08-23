# Procedural Creature Construction and Animation Laboratory

Milestone 1 is a lightweight WPF editor foundation. It starts empty and supports Create/Play modes, circular node creation, selection, dragging, deletion, editable node properties, and reset. Play mode is intentionally a placeholder.

Milestone 2 adds one ordered primary chain. Adjacent node centers are always exactly `ChainSettings.Spacing` apart, regardless of their visual radii. Each adjacent pair has a `CreatureConnection` with parent/child IDs, rest length, stiffness, and damping.

In Create Mode, select a node to see the dashed center-spacing construction circle and directional handle. Drag the handle to set rotation, then use `ADD NEXT NODE` to place the next node at exactly one spacing interval. Changing spacing rebuilds the chain while preserving each segment's direction. The root can move the whole chain; constrained child nodes are not freely dragged. Deleting a node removes it and all later descendants, keeping the remaining chain valid. Branching, body-size ramps, constraints, physics, and Play Mode animation remain future milestones.

Milestone 3 adds the procedural Body Size Ramp. The ramp has fixed endpoints at normalized positions 0 and 1 plus editable intermediate points, uses linear interpolation, and clamps values to `0.1..2.0`. Each ordered chain node uses `index / (count - 1)` (or 0 for a one-node creature), samples the ramp, and derives `radius = BaseRadius * rampValue`. The visual radius is never used for structural spacing.

The right inspector includes a compact curve editor: drag points, double-click to add an intermediate point, Delete to remove one, and Reset Curve to restore the tapered default. Edits update node sizes immediately. Adding or deleting chain nodes re-samples the same ramp; Reset returns the editor to an empty creature with the default ramp.

Milestone 4 adds functional Play Mode. The mouse is a target, not a direct attachment: the root uses velocity, limited acceleration, maximum speed, and damping to follow it. The body follows through fixed-step rest-length constraints using connection stiffness and damping. Play positions and velocities live in `CreaturePlayState`; the constructed `CreatureDefinition` is not mutated during simulation. Pause, Resume, Reset Simulation, simulation speed, Max Speed, Acceleration, and Damping are available in the Play panel. Returning to Create restores the exact constructed pose, while the Create-mode Reset/Clear still removes the creature and restores the default ramp.

Milestone 5 adds configurable procedural wave motion. `BodyWaveGenerator` evaluates a sinusoid using simulation time and normalized chain position, then applies the offset along the local perpendicular to each segment. Head influence is zero and tail influence increases with normalized position. Amplitude, Frequency, Phase, Influence, and Wave Enabled are available under BODY MOTION. The wave is folded into constrained child targets, so connection rest lengths remain stable; pause freezes wave time and Reset Simulation resets time while preserving wave settings.

The final prototype polish uses a gentler default wave amplitude of `4` (previously `8`) and a faster default Max Speed of `360` (previously `180`), while retaining the full manual amplitude range. Save and Load use human-readable `.creature.json` files containing the authored node chain, connections, chain settings, Base Radius, and Body Size Ramp. Temporary Play Mode positions, velocities, acceleration, target, and pause state are never serialized. Loading validates JSON, IDs, references, finite numeric values, chain order, spacing, and ramp points; invalid files show an error and leave the current creature unchanged. `NEW` clears the authored creature and restores defaults.

## Run

From this directory:

```powershell
dotnet run
```

Build with `dotnet build`. The project intentionally has no third-party dependencies.
