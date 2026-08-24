# Procedural Creature Construction and Animation Laboratory

Milestone 1 is a lightweight WPF editor foundation. It starts empty and supports Create/Play modes, circular node creation, selection, dragging, deletion, editable node properties, and reset. Play mode is intentionally a placeholder.

Milestone 2 adds one ordered primary chain. Adjacent node centers are always exactly `ChainSettings.Spacing` apart, regardless of their visual radii. Each adjacent pair has a `CreatureConnection` with parent/child IDs, rest length, stiffness, and damping.

In Create Mode, select a node to see the dashed center-spacing construction circle and directional handle. Drag the handle to set rotation, then use `ADD NEXT NODE` to place the next node at exactly one spacing interval. Changing spacing rebuilds the chain while preserving each segment's direction. The root can move the whole chain; constrained child nodes are not freely dragged. Deleting a node removes it and all later descendants, keeping the remaining chain valid. Branching, body-size ramps, constraints, physics, and Play Mode animation remain future milestones.

Milestone 3 adds the procedural Body Size Ramp. The ramp has fixed endpoints at normalized positions 0 and 1 plus editable intermediate points, uses linear interpolation, and clamps values to `0.1..2.0`. Each ordered chain node uses `index / (count - 1)` (or 0 for a one-node creature), samples the ramp, and derives `radius = BaseRadius * rampValue`. The visual radius is never used for structural spacing.

The right inspector includes a compact curve editor: drag points, double-click to add an intermediate point, Delete to remove one, and Reset Curve to restore the tapered default. Edits update node sizes immediately. Adding or deleting chain nodes re-samples the same ramp; Reset returns the editor to an empty creature with the default ramp.

Milestone 4 adds functional Play Mode. The mouse is a target, not a direct attachment: the root uses velocity, limited acceleration, maximum speed, and damping to follow it. The body follows through fixed-step rest-length constraints using connection stiffness and damping. Play positions and velocities live in `CreaturePlayState`; the constructed `CreatureDefinition` is not mutated during simulation. Pause, Resume, Reset Simulation, simulation speed, Max Speed, Acceleration, and Damping are available in the Play panel. Returning to Create restores the exact constructed pose, while the Create-mode Reset/Clear still removes the creature and restores the default ramp.

Milestone 5 adds configurable procedural wave motion. `BodyWaveGenerator` evaluates a sinusoid using simulation time and normalized chain position, then applies the offset along the local perpendicular to each segment. Head influence is zero and tail influence increases with normalized position. Amplitude, Frequency, Phase, Influence, and Wave Enabled are available under BODY MOTION. The wave is folded into constrained child targets, so connection rest lengths remain stable; pause freezes wave time and Reset Simulation resets time while preserving wave settings.

The final prototype polish uses a gentler default wave amplitude of `4` (previously `8`) and a faster default Max Speed of `360` (previously `180`), while retaining the full manual amplitude range. Save and Load use human-readable `.creature.json` files containing the authored node chain, connections, chain settings, Base Radius, and Body Size Ramp. Temporary Play Mode positions, velocities, acceleration, target, and pause state are never serialized. Loading validates JSON, IDs, references, finite numeric values, chain order, spacing, and ramp points; invalid files show an error and leave the current creature unchanged. `NEW` clears the authored creature and restores defaults.

The curve-and-turning upgrade limits new child construction to a 270-degree arc centered on the incoming segment direction (`-135°..+135°`), leaving a blocked rear 90-degree sector. The root remains unrestricted, and older authored geometry is preserved while future extension directions are clamped. Body Size supports up to 64 points and Linear, Smooth, or automatic-handle Bezier interpolation. The curve editor renders the sampled curve, updates radii immediately, and saves interpolation mode with backward-compatible Linear fallback.

The visual/editor milestone adds a procedural outer skin, mirrored head eyes, display toggles, and strict mode-specific panels. Skin side points are derived as `node center ± local perpendicular × node radius`, so the Body Size Ramp directly controls silhouette width in both Create and Play Mode. Nodes, skin, and eyes can be toggled independently without changing the definition. Eyes store one size/spacing/forward-offset rule and derive the mirrored pair from the head orientation; those settings persist in JSON, while older files use eye defaults. Create Mode exposes construction, chain, curve, appearance, and eye tools. Play Mode exposes only simulation, movement, body-motion, and display controls. New defaults are Max Speed `720` and Wave disabled.

The features-and-skin milestone doubles the default Simulation Speed from `1.0x` to `2.0x` while keeping Max Speed at `720`. The skin now uses Catmull-Rom interpolation with eight rendering samples per structural segment; nodes and radii remain the only authored geometry, and rounded caps close the head and tail. Play Mode rebuilds this derived skin from simulated positions without changing the authored chain.

Eyes are now the first member of a general `CreatureFeature` system. A feature stores an ID, type, parent node ID, local position, local rotation, scale, mirror state, visibility, and Eye size. Features are parent-local rules: `head position + local eye offset = eye world position`, so movement and rotation carry the feature automatically. Create Mode provides add/select/edit/delete controls and canvas dragging; Play Mode renders features from simulated parent transforms but hides editing controls. Feature collections save to `.creature.json`; older files without `Features` remain valid, and the previous `Eyes` section migrates to one mirrored Eye feature when possible.

The tongue-and-cap milestone replaces the endpoint line/arc transition with explicit sampled semicircular caps. The head cap uses the outgoing tangent and the actual head radius; the tail cap uses the incoming tangent and actual tail radius. A one-node creature renders a complete circle, while a two-node creature becomes a capsule-like outline. Body-side interpolation uses one-sided Hermite endpoint tangents to avoid pulling terminal samples inward.

`ForkedTongue` is the second supported `CreatureFeature` type. It stores local transform, length, fork length, fork angle, scale, and visibility, and is singular: its mirror capability is unavailable even if older data requests mirroring. The vector renderer draws one stem and two symmetric fork lines from the head surface. Tongue features follow the simulated head in Play Mode, do not affect simulation, and persist through Save/Load.

The endpoint-orientation fix defines caps from explicit outward vectors: the head uses `-normalize(Node1 - Node0)`, while the tail uses `normalize(Tail - Previous)`. Each cap uses `center + outward * cos(theta) * radius + normal * sin(theta) * radius` for `theta` from `-π/2` to `+π/2`, so its midpoint always bulges away from the body. Body-side samples retain exact structural cap endpoints.

## Run

From this directory:

```powershell
dotnet run
```

Build with `dotnet build`. The project intentionally has no third-party dependencies.

The appearance and anatomy-debug milestone adds authored skin color with a WPF Create-mode color picker, opaque `#AARRGGBB` persistence, almond-shaped eyes with width/height controls, smooth pupil tracking, and mirrored rendering. Play Mode now defaults to a solid colored body with features visible; Skeleton and Muscles are optional visual overlays and never participate in simulation. The muscle overlay is derived from simulated chain positions and radii.

Play deformation now applies a local bend limit of 75 degrees after distance following and wave motion, then restores connection rest lengths. This is a stability constraint rather than self-collision: overlapping body segments are still allowed, while abrupt local turns are softened.

The test harness covers the new appearance defaults, eye and skin persistence, backward-compatible color defaults, bend clamping, rest-length preservation, and long-chain finite-state behavior.

The soft-bend and construction-visualization milestone replaces the former one-step angular snap with six small solver iterations. Each pass solves distances, applies a signed soft angular correction at 25% stiffness, and solves distances again. Corrections are shared between the incoming joint and outgoing child, preserving bend direction and allowing the root to lead while the body follows progressively. The 75-degree value remains a maximum local bend, not an authored pose target.

The MUSCLES overlay now means construction circles: one transparent outline per structural node, centered at the current authored or simulated node position and using the exact derived node radius. SKELETON remains the centerline and connections; SKIN remains the smoothed envelope. Create and Play expose independent Skin, Skeleton, and Muscles display choices.

The editing-and-fins milestone adds bounded snapshot Undo history for authored Create-mode edits, grouped drag history, internal Ctrl+C/Ctrl+V feature duplication, pure-white circular orb eyes with dark tracked pupils, and feature skeletons for tongue stems/forks and fins. `Fin` is a parent-local feature with left/right side attachment, procedural tapered geometry, authored length/width/base angle, and Play-only spring-damped angular inertia. Fin current angle and angular velocity are temporary and are not saved.
