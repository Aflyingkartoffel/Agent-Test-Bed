# Changelog

## 2026-09-02

- Added Time-Series Sonifier Milestone 5 live FFT spectrum visualization, bounded audio sample tap, safe FFT controls, spectrum renderer, and regression coverage.

## 2026-09-02

- Completed Equation Synth Milestone 7 procedural outputs and updated the roadmap indicator to 7 / 7 (100%).

## 2026-09-02

- Added Equation Synth Milestone 7 procedural expression outputs: reusable output bindings, 2D procedural objects, transform/color targets, combination modes, shared parameters, time-driven preview, bounded trails, persistence, and regression coverage.

## 2026-09-02

- Added Equation Synth Milestone 6 multi-oscillator mixer foundations: independent layer audio state and voices, stereo equal-power panning, mute/solo, ADSR, master limiting, mixer persistence, and regression coverage.

## 2026-09-02

- Completed Equation Synth V1 integration: authored Undo/Redo, grouped continuous edits, saved-state dirty tracking, shared unsaved-change protection, parameter metadata/automation editing, atomic validated loads, and expanded regression coverage.

## 2026-09-01

- Added `projects/equation-synth/`, a .NET 8 WPF Equation Synth Milestone 1 with a restricted expression AST, automatic parameters, graph pan/zoom, time animation, normalized waveform preview, conservative SoundPlayer playback, presets, and JSON save/load.
- Added `projects/equation-synth-tests/` with dependency-free silent expression, safety, waveform, parameter, time, and preset checks.
- Extended Equation Synth with a multi-equation graph workspace, per-entry colors and visibility, shared parameters, centralized camera/range controls, cursor coordinate readout, adaptive grid labels, viewport-aware sampling, and backward-compatible preset fields.
- Added Equation Synth Milestone 3 procedural animation: looping/reverse/scrubbable time, stepping, sine/cosine/expression parameter automation, dependency cycle detection, effective-value clamping, selected-equation trails, and procedural animation presets.
- Replaced Equation Synth's active SoundPlayer synthesis path with a 48 kHz native `waveOut` streaming oscillator using persistent phase, interpolated 2048-sample tables, crossfaded live updates, smoothed frequency/gain, and safe float output limiting.
- Added Equation Synth V1 workflow polish: project validation, bounded undo/redo primitives, project toolbar actions, dirty/status indicators, keyboard shortcuts, function-reference guidance, auto-fit/panic controls, Release build documentation, and expanded regression coverage.

## 2026-08-23

- Added `projects/creature-construction-lab/`, a WPF Milestone 1 foundation for procedural creature construction.
- Added separate model, editor state, coordinate conversion, rendering, UI, and test-runner layers.
- Added 8 silent editor tests covering empty startup, node operations, properties, modes, and reset.
- Added Milestone 2 chain construction with center-to-center spacing, connection data, construction gizmos, rotation-based placement, spacing rebuilds, and descendant-safe deletion.
- Added Milestone 3 procedural body sizing with editable linear ramp control points, derived node radii, normalized chain sampling, and real-time curve updates.
- Added Milestone 4 functional Play Mode with separated simulation state, mouse following, fixed-step movement, chain constraints, pause/resume, and simulation reset.
- Added Milestone 5 procedural body wave motion with local-orientation offsets, amplitude/frequency/phase/influence controls, and stable rest-length integration.
- Added final prototype polish: gentler default wave, faster default follow speed, validated `.creature.json` Save/Load, NEW/SAVE/LOAD controls, and reduced per-frame UI refresh work.
- Added the curve-and-turning upgrade: 270-degree child construction arcs, rear-sector clamping, Linear/Smooth/Bezier ramp interpolation, sampled curve rendering, and backward-compatible interpolation persistence.
- Added procedural skin and mirrored eyes, independent visual toggles, strict Create/Play UI separation, and final tuning defaults of Max Speed 720 with Wave disabled.

## 2026-08-21

- Created the repository documentation and AI-agent guidance.
- Added the `projects/`, `experiments/`, and `docs/` directory structure.
- Added a conservative `.gitignore` for the Windows/VS Code development environment and common application outputs.
- Confirmed that no sample application was created.
- Initialized the local Git repository on `main` and created the initial commit `4ce0a3c`.
- Confirmed that no GitHub remote is currently configured.
- Created the first application in `projects/calculator/` using HTML, CSS, and vanilla JavaScript.
- Added arithmetic operations, decimals, sign toggling, clear/backspace, keyboard input, precedence, and graceful error handling.
- Created `projects/3d-particle-viewer/`, a .NET 8 WPF desktop viewer for OBJ surface particles.
- Added OBJ face triangulation, area-weighted barycentric surface sampling, particle controls, camera orbit/zoom, and a Y-rotation slider.
- Added `sample-cube.obj` as a repeatable import fixture. FBX remains planned, not implemented.
- Added selectable Cube, Sphere, Tetrahedron, Billboard, and Image Billboard particle shapes.
- Hid the original mesh by default and added an optional reference-mesh toggle to prevent mesh occlusion.
- Added PNG/JPG/JPEG image loading for camera-facing billboards with aspect-ratio preservation.
- Added a built-in transparent `radial_gradient.png` billboard test texture and one-click selection.
- Added a lightweight soft-body mode with rest-position restoring forces, damping, gravity, reset behavior, optional ground plane, and particle-level ground collision.
- Added focused simulation/scene UI sections and reduced per-particle template allocations during dynamic mesh updates.
- Added a live `0.00`–`1.00` deformation-resistance slider that scales rest-position restoring force independently from damping.
- Fixed the simulation to retain separate rest positions so deformation resistance affects actual particle recovery.
- Set the Particle Shape selector and its options to black text for readability.
- Added a dependency-free HSV particle color picker with hue, saturation/value, brightness, preview, live HEX display, and optional precise HEX apply input.
- Added a separate normalized Elasticity control and bounded ground-contact squish, lateral spreading, recovery, and bounce behavior.
- Added an explicit Ground collision toggle while preserving the existing ground plane behavior by default.
- Added a smoothed FPS counter based on WPF render callbacks, refreshed approximately four times per second.
- Replaced the color picker's hue strip with a radial HSV wheel; hue is selected by angle, saturation by radius, and brightness by the separate value slider.
- Fixed HEX input to accept both `#RRGGBB` and `RRGGBB`, and made Apply update the shared solid material without rebuilding particle geometry.
- Added right-drag camera panning and robust mouse-capture cleanup for release, focus loss, and window deactivation.
- Reduced per-frame simulation enumeration and reused the solid particle material for color-only updates.
- Added a spatial-hash-built local spring graph from original particle positions to preserve soft-body cohesion.
- Added spring stiffness and bounce controls, semi-implicit two-substep integration, center/rest-shape preservation, bounded restitution, and ground friction.
- Removed a redundant particle reset during visualization rebuild so the sampled particle set and spring graph are initialized once.
- Simplified the viewer UI around the soft-body object by hiding particle color, shape, image, count, size, billboard, and radial-gradient controls.
- Changed the main viewport to pure black and made the optional ground a solid, lit neutral plane enabled by default.
- Added user-facing Chaos, Momentum, and Drop Height controls with reset behavior that preserves the loaded model and selected settings.
- Connected Ground Plane visibility directly to floor collision eligibility.
- Renamed the application and project from **3D Particle Model Viewer** to **3D Bounce Simulator**, including its folder, project file, run command, title, and current documentation references.
- Abandoned the unreliable soft-body implementation and rebuilt `projects/3d-bounce-simulator/` as a clean Stage 1 WPF foundation with solid OBJ/FBX mesh import, automatic framing, orbit, zoom, and Reset View.
- Added AssimpNet 4.1.0 as the minimal importer dependency required for actual OBJ and FBX support.
- Corrected Stage 1 startup so the 3D Bounce Simulator opens as an empty viewport; the optional sample cube is never loaded automatically, and failed imports restore the empty-scene camera.
- Added a self-contained Release `win-x64` publish workflow and documented direct launch of `3D Bounce Simulator.exe` without a separate .NET runtime.

## 2026-08-22

- Created `projects/insect-light-simulation/` as a separate .NET 8 WPF milestone-one prototype.
- Added vector-based agents, composable light attraction, coherent wander, bounded turning/speed, configurable boundaries, deterministic reset, and a single `WriteableBitmap` pixel renderer.
- Added retro green technical controls, pause/resume, reset, simulation speed, insect count, light/behavior settings, and actual runtime statistics.
- Added `projects/insect-light-simulation-tests/` with seven dependency-free simulation-layer tests.
- Extended the simulation to support 1–16 independent light sources with summed attraction forces, selected-light controls, nearest-light hit testing, mouse dragging, add/remove actions, per-light glow rendering, and smoothed FPS measurement.
- Replaced the three editable light attribute sliders with one selected-light LIGHT POWER slider while preserving separate attraction, radius, and visual intensity properties per light.
- Performed a UI and performance polish pass: consistent dark-green ComboBox/Button/Slider styling, clearer sections and spacing, cached insect patterns, loop-based hot-path calculations, and throttled statistics refreshes.
- Replaced the minimal insect mark with a cached four-frame top-down pixel insect adapted from `projects/insect-light-simulation/Ref/Insect Ref.png`; added per-agent deterministic wing phases, flap-speed variation, eight cached heading rotations, and a glowing abdomen while preserving procedural movement.
- Added 0.5x–4x cursor-centered camera zoom with inverse light-drag coordinates, reduced default insect display scale to 0.5, and added smooth approach-aware speed variation based on the strongest light influence without changing the agent population during zoom.

## Creature feature attachments and smooth skin

- Doubled the creature lab default Simulation Speed from `1.0x` to `2.0x`.
- Smoothed the derived skin with Catmull-Rom samples and rounded caps without changing structural chain geometry.
- Replaced special-purpose eye data with local-parented `CreatureFeature` attachments; Eye is the first supported type.
- Added Create-only feature add/select/drag/edit/delete controls, mirrored Eye rendering, Play attachment behavior, and feature Save/Load migration.
## Creature tongue and skin-cap milestone

- Fixed terminal skin inversion/pinching with explicit radius-bearing head and tail caps.
- Added one-sided endpoint tangent handling for stable body interpolation.
- Added singular vector `ForkedTongue` feature with local parenting, length, fork length, fork angle, scale, Play attachment, and Save/Load support.

## Skin cap orientation correction

- Corrected head/tail cap orientation using explicit outward vectors and an unambiguous semicircle equation.
- Added regression coverage for left-facing tails, outward cap midpoints, endpoint radii, and exact body/cap connection points.

## Appearance, anatomy views, and bend stability

- Added authored skin color with Create-mode color picker, opaque color persistence, and backward-compatible defaults.
- Reworked eyes as almond outlines with configurable width, height, smooth Play pupil tracking, and mirrored support.
- Added Play-only Skeleton and Muscles diagnostic overlays while defaulting Play to a solid body with features visible.
- Added a 75-degree local bend constraint and rest-length reapplication after wave/deformation updates.
- Added regression tests for appearance persistence, backward compatibility, bend clamping, rest lengths, and long-chain stability.

## Soft bend and construction-circle visualization

- Replaced hard downstream bend snaps with six iterative signed-angle corrections at 25% stiffness.
- Distributed angular correction between the incoming joint and outgoing child while retaining rest-length solving and gentle velocity damping.
- Replaced arbitrary Muscle offset lines with one thin transparent construction circle per structural node.
- Added independent Create/Play display toggles and regression coverage for circles, signed bend continuity, S-curves, and wave interaction.

## Fin mirroring, rounded geometry, and Bezier handles

- Added mirrored Fin rendering, skeletons, hit testing, and separate spring state.
- Replaced triangular Fins with rounded sampled organic outlines attached at one quarter of the parent node radius.
- Added persisted Fin Color, explicit Bezier handles with legacy auto-generation, grouped handle Undo, and black-on-white dropdown styling.

## Fin placement lock and Bezier handle interaction

- Disabled generic canvas translation for Fins while preserving click selection and parent-driven movement.
- Normalized legacy Fin local placement and removed generic Fin paste offsets.
- Added dedicated Bezier handle drag priority, live updates, directional constraints, hover feedback, and grouped Undo.

## Editing history, feature clipboard, orb eyes, and fins

- Added bounded Create-mode snapshot Undo with selection restoration and grouped drag edits.
- Added internal Ctrl+C/Ctrl+V feature duplication for Eye, Forked Tongue, and Fin features with new IDs and undo support.
- Replaced almond eyes with pure-white circular orbs and small dark constrained pupils retaining smooth tracking.
- Added tongue stem/fork and fin attachment/tip skeleton overlays.
- Added parent-local left/right Fin geometry with authored dimensions and Play-only spring-damped angular inertia.
