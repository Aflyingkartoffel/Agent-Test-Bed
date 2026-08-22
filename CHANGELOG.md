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
