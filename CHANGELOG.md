# Changelog

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

## Creature feature attachments and smooth skin

- Doubled the creature lab default Simulation Speed from `1.0x` to `2.0x`.
- Smoothed the derived skin with Catmull-Rom samples and rounded caps without changing structural chain geometry.
- Replaced special-purpose eye data with local-parented `CreatureFeature` attachments; Eye is the first supported type.

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

## Soft bend and construction-circle visualization

- Replaced hard downstream bend snaps with six iterative signed-angle corrections at 25% stiffness.
- Distributed angular correction between the incoming joint and outgoing child while retaining rest-length solving and gentle velocity damping.
- Replaced arbitrary Muscle offset lines with one thin transparent construction circle per structural node.
- Added independent Create/Play display toggles and regression coverage for circles, signed bend continuity, S-curves, and wave interaction.

## Editing history, feature clipboard, orb eyes, and fins

- Added bounded Create-mode snapshot Undo with selection restoration and grouped drag edits.
- Added internal Ctrl+C/Ctrl+V feature duplication for Eye, Forked Tongue, and Fin features with new IDs and undo support.
- Replaced almond eyes with pure-white circular orbs and small dark constrained pupils retaining smooth tracking.
- Added tongue stem/fork and fin attachment/tip skeleton overlays.
- Added parent-local left/right Fin geometry with authored dimensions and Play-only spring-damped angular inertia.
## Fin mirroring, rounded geometry, and Bezier handles

- Added mirrored Fin rendering, skeletons, hit testing, and separate spring state.
- Replaced triangular Fins with rounded sampled organic outlines attached at one quarter of the parent node radius.
- Added persisted Fin Color, explicit Bezier handles with legacy auto-generation, grouped handle Undo, and black-on-white dropdown styling.
## Fin placement lock and Bezier handle interaction

- Disabled generic canvas translation for Fins while preserving click selection and parent-driven movement.
- Normalized legacy Fin local placement and removed generic Fin paste offsets.
- Added dedicated Bezier handle drag priority, live updates, directional constraints, hover feedback, and grouped Undo.
