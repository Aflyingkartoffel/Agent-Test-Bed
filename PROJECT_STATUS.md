# Project Status

## Time-Series Sonifier Milestone 5 complete — 2026-09-02

- Added optional live FFT spectrum visualization with post-volume audio sample tap, bounded ring buffer, Hann-windowed radix-2 FFT, finite dB frame model, smoothing, logarithmic frequency display, and safe stop/restart behavior.
- Debug/Release builds and 130 silent regression checks pass. GUI and audible waveform comparison remain unverified in this headless environment.

## Equation Synth Milestone 7 complete — 2026-09-02

- Procedural expression outputs, object preview, shared parameters, ordered bindings, persistence, Undo/Redo, and safe runtime evaluation are complete.
- Roadmap progress is now 7 / 7 (100%). GUI, startup, and audible behavior remain unverified where desktop automation is unavailable.

## Equation Synth Milestone 7 — 2026-09-02

- Added a reusable procedural output architecture with expression context, binding targets, Replace/Add/Multiply combination, safe evaluation, multiple objects, ordered bindings, and bounded trails.
- Added a compact procedural object preview/editor and procedural presets without replacing the graph workspace.
- Added procedural persistence and shared-parameter integration. Debug/Release builds and 93 silent regression checks pass; GUI/startup/audio remain unverified.

## Equation Synth Milestone 6 — 2026-09-02

- Added multi-layer audio state, per-layer runtime voices, stereo mixer math, equal-power pan, mute/solo, master gain, limiter, and ADSR envelope support.
- Added layer mixer controls to the equation cards and persisted mixer settings through the existing validated project workflow and authored history.
- Debug/Release builds and 81 silent regression checks pass. Desktop GUI and audible stereo behavior remain unverified in this environment.

## Equation Synth V1 completion pass — 2026-09-02

- Integrated authored-state Undo/Redo with grouped slider and graph edits, redo invalidation, no-op suppression, and saved-state equality.
- Added shared Save / Don't Save / Cancel protection for New, Open, and close, plus temporary-state validation before load replacement.
- Completed parameter metadata and automation controls with finite/range validation and separate Manual/Effective values.
- Debug/Release builds and 71 silent regression checks pass. WPF startup and speakers remain unverified because headless GUI/audio validation is unavailable in this environment.

The first application, a browser-based calculator, has been created. The second application is the WPF 3D Bounce Simulator. Its current foundation stage focuses on importing and viewing solid OBJ/FBX meshes; physics and particle features have intentionally not been rebuilt yet.

## Repository

- **Purpose:** Personal AI-assisted application development laboratory
- **Current status:** Scaffolded, initialized as a local Git repository, and ready for GitHub connection
- **Technology/language:** HTML/CSS/vanilla JavaScript for the calculator; C#/.NET 8 WPF with AssimpNet for the 3D Bounce Simulator
- **What currently works:** Calculator core workflow; 3D Bounce Simulator Stage 1 build, self-contained win-x64 publish, empty-scene startup, actual OBJ/FBX import path, solid mesh rendering, validation, automatic centering/scaling/framing, orbit, zoom, and Reset View
- **What is being worked on:** Interactive Windows testing with cube, complex OBJ, and FBX assets
- **Known problems:** Stage 1 uses one simple material; animation, textures, physics, particles, soft-body behavior, ground collision, and advanced FBX material preservation are not implemented
- **Next logical steps:** Confirm visual OBJ/FBX loading, then add the next feature only after this foundation is stable

## Projects

The fifth application, `projects/equation-synth/`, is a Milestone 1 WPF equation visualizer and waveform synthesizer. It has a restricted reusable AST parser, automatic parameters, graph and waveform canvases, time controls, safe wavetable audio playback, presets, JSON save/load, and a separate silent test runner.

Equation Synth Milestone 2 adds the interactive graph workspace: multiple colored equation entries, visibility and selection, shared parameter reconciliation, centralized graph camera with pan/anchor zoom/reset, editable ranges, mouse coordinate readout, adaptive labels/grid, viewport-aware sampling, and selected-equation-only audio preservation.

Equation Synth Milestone 3 adds the procedural animation layer: a reusable looping/reversible timeline with scrubbing and stepping, OFF/SINE/COSINE/EXPRESSION parameter automation, dependency-cycle safety, manual/effective parameter values, bounded selected-equation graph trails, procedural animation presets, and animation-aware persistence. The existing SoundPlayer audio architecture remains intentionally unchanged for the future streaming-audio milestone.

Equation Synth Milestone 4 replaces the active SoundPlayer path with a native Windows `waveOut` streaming engine: persistent phase, 48 kHz mono float buffers, interpolated oscillator reads, table crossfades, smoothed frequency/gain, safe limiting, and lifecycle/error handling. The selected equation remains the only audio source; multi-oscillator mixing remains deferred.

Equation Synth Milestone 5 adds V1 workflow polish: explicit project validation/history services, New/Save/Save As/Load toolbar actions, dirty/status indicators, function/reference documentation, V1 shortcuts, procedural preset coverage, auto-fit and panic-stop shortcuts, and Debug/Release build validation.

Current projects:

- `projects/calculator/`
- `projects/3d-bounce-simulator/`

The third application, `projects/insect-light-simulation/`, is now implemented through its multi-light, UI polish, animated insect, speed-variation, and camera-zoom milestones: a .NET 8 WPF pixel simulation with composable steering behaviors, selectable/draggable light sources, independent per-light power controls, consistent retro controls, smoothed FPS, a cached four-frame insect sprite, world/screen camera transforms, and a separate simulation test runner.

The fourth application, `projects/creature-construction-lab/`, now has its Milestone 1 WPF editor foundation: an empty startup canvas, Create/Play modes, circular node creation/selection/movement/deletion, editable node properties, reset, a Vector2 data model, and an isolated editor test runner. Procedural chains, constraints, physics, and animation are intentionally not implemented.

Milestone 2 extends the creature lab with one ordered primary chain, center-to-center equal spacing, connection data, spacing rebuilds, a construction circle, rotation handle, add-next-node workflow, connection lines, and safe descendant deletion. Body-size ramps and Play Mode animation remain intentionally deferred.

Milestone 3 adds a procedural Body Size Ramp with linear sampling, normalized chain positions, derived radii, an interactive curve editor, base-radius control, real-time updates, and endpoint protection. Play Mode animation and branching remain intentionally deferred.

Milestone 4 makes Play Mode functional with separated temporary simulation state, mouse-target root following, velocity/acceleration/max-speed/damping controls, fixed-step updates, forward rest-length chain constraints, pause/resume, simulation reset, and exact restoration of the constructed pose when returning to Create Mode. Procedural wave motion, branching, and advanced physics remain deferred.

Milestone 5 adds modular procedural body wave motion using local perpendicular offsets, time/frequency/phase controls, head-to-tail influence weighting, stable post-constraint targets, and BODY MOTION controls. Branching and advanced physics remain deferred.

The final prototype polish reduces default wave amplitude from 8 to 4, increases default follow speed from 180 to 360, adds validated human-readable JSON Save/Load for authored creature definitions, adds NEW/SAVE/LOAD controls, and separates simulation rendering invalidation from inspector refreshes. Temporary Play Mode state is not saved.

The curve-and-turning upgrade adds a 270-degree child construction arc with a blocked rear sector, clamped rotation handles, up to 64 Body Size points, Linear/Smooth/Bezier interpolation, sampled curve rendering, real-time derived-radius updates, and interpolation persistence with Linear fallback for older files.

The visual/editor milestone adds a radius-derived procedural skin, mirrored head eyes with persisted settings, independent Nodes/Skin/Eyes display toggles, Create/Play-specific panels and overlays, default Max Speed `720`, and Wave disabled by default. Structural chain data and Play Mode physics remain unchanged.

## Creature features and smooth skin

The creature laboratory now defaults to `2.0x` Simulation Speed while retaining Max Speed `720`. Procedural skin uses Catmull-Rom sampled side curves with rounded head/tail caps, derived from the existing node positions and radii. A reusable local-space `CreatureFeature` model replaces the eye-specific definition; Eye is the first supported type, with root parenting, manual local transforms, scale, visibility, mirrored rendering, Save/Load persistence, and migration from older eye JSON. Feature controls are Create-only; Play Mode keeps only visual attachment to simulated parents.
## Creature tongue and skin-cap milestone

Terminal skin geometry now wraps explicitly around the first and last node radii using sampled semicircular caps and one-sided endpoint tangents. The body still uses derived node positions/radii only. `ForkedTongue` extends the local-parent feature architecture with vector stem/fork geometry, length/fork-length/fork-angle controls, root parenting, singular non-mirrored behavior, Play attachment, and Save/Load persistence.

The follow-up cap-orientation fix uses explicit outward tangent vectors so the head cap points opposite the body direction and the tail cap points away from the preceding node. Regression tests cover right-facing heads, left-facing tails, cap midpoints, endpoint radii, and curved chains. Final canvas-level visual confirmation remains user verification in this environment.

## Appearance, anatomy views, and bend stability

The current creature-lab milestone adds authored skin color with Create-mode color picking and JSON persistence, almond eyes with width/height controls, smooth Play pupil tracking, and mirrored feature rendering. Play Mode defaults to solid body plus features, with optional Skeleton and Muscles diagnostic overlays. Muscle lines are derived from the current chain positions/radii and do not affect simulation.

Play deformation now applies a 75-degree local bend limit after distance following and wave motion, then restores authored connection rest lengths. This is a bend-stability constraint only; self-collision is intentionally not implemented. The lab test harness now has 94 passing tests covering the new appearance and deformation behavior.

## Soft bend and construction-circle visualization

The previous bend solver hard-snapped each violating downstream child to the exact angular boundary, so sequential joints could overwrite one another and form alternating zig-zag bends. The replacement uses signed angles, six small distance/angular iterations, 25% angular stiffness, and lightweight correction sharing between the joint and child. It preserves dynamic Play curvature without targeting authored rotations.

MUSCLES now renders the actual construction circles used by the procedural skin: one transparent radius outline per node. SKELETON remains centerline plus connections, and SKIN remains the smoothed outer envelope. Create and Play have independent display toggles. Full non-adjacent self-collision remains out of scope.

## Fin mirroring, rounded shape, and Bezier handles

The current milestone supports mirrored Fin pairs with quarter-radius attachment, rounded sampled organic outlines, persisted editable Fin Color, and Fin skeleton/hit-test coverage. Body Size Ramp Bezier mode now stores explicit incoming/outgoing authored handles with endpoint rules and automatic migration for older files. Verification: project build succeeded and 126 editor tests passed.

## Fin placement lock and Bezier handle interaction

Fin canvas selection no longer starts generic feature dragging. Fin roots are derived only from their parent node, side, radius, quarter-radius attachment, mirror state, and authored Fin parameters; legacy local translation/rotation and paste offsets are ignored. Bezier handles now have dedicated priority hit testing, live mouse dragging, sensible directional bounds, hover/drag feedback, and grouped Undo. Verification: build succeeded, 130 editor tests passed, and the built WPF process reached its running startup state.

## Editing history, feature clipboard, orb eyes, and fins

Create-mode authoring now has bounded snapshot Undo history with selection restoration and one history entry per grouped drag. Ctrl+C/Ctrl+V duplicates selected features with new IDs and a local offset; Ctrl+Z removes the pasted feature. Eyes are pure-white procedural circles with dark, constrained, smoothly tracked pupils. Skeleton overlays now expose tongue stem/fork bones and fin attachment-to-tip bones.

The new Fin feature attaches to a selected body node and left/right body side. Its tapered shape uses the node radius at the attachment, while Play mode maintains temporary spring-damped angular state with stiffness, damping, a ±120° safety range, pause/reset behavior, and no body-solver participation. Fin authored data is saved; temporary angular state is not. The test runner now has 121 passing tests.
