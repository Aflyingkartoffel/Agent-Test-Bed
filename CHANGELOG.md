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
