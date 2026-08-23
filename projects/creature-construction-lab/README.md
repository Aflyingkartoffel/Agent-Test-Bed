# Procedural Creature Construction and Animation Laboratory

Milestone 1 is a lightweight WPF editor foundation. It starts empty and supports Create/Play modes, circular node creation, selection, dragging, deletion, editable node properties, and reset. Play mode is intentionally a placeholder.

Milestone 2 adds one ordered primary chain. Adjacent node centers are always exactly `ChainSettings.Spacing` apart, regardless of their visual radii. Each adjacent pair has a `CreatureConnection` with parent/child IDs, rest length, stiffness, and damping.

In Create Mode, select a node to see the dashed center-spacing construction circle and directional handle. Drag the handle to set rotation, then use `ADD NEXT NODE` to place the next node at exactly one spacing interval. Changing spacing rebuilds the chain while preserving each segment's direction. The root can move the whole chain; constrained child nodes are not freely dragged. Deleting a node removes it and all later descendants, keeping the remaining chain valid. Branching, body-size ramps, constraints, physics, and Play Mode animation remain future milestones.

Milestone 3 adds the procedural Body Size Ramp. The ramp has fixed endpoints at normalized positions 0 and 1 plus editable intermediate points, uses linear interpolation, and clamps values to `0.1..2.0`. Each ordered chain node uses `index / (count - 1)` (or 0 for a one-node creature), samples the ramp, and derives `radius = BaseRadius * rampValue`. The visual radius is never used for structural spacing.

The right inspector includes a compact curve editor: drag points, double-click to add an intermediate point, Delete to remove one, and Reset Curve to restore the tapered default. Edits update node sizes immediately. Adding or deleting chain nodes re-samples the same ramp; Reset returns the editor to an empty creature with the default ramp.

## Run

From this directory:

```powershell
dotnet run
```

Build with `dotnet build`. The project intentionally has no third-party dependencies.
