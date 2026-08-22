# 3D Particle Model Viewer

This Windows desktop application imports an OBJ model and displays its surface as an interactive particle visualization.

## How to run

From the repository root, run:

```powershell
dotnet run --project projects/3d-particle-viewer/3d-particle-viewer.csproj
```

Or open the generated executable under `projects/3d-particle-viewer/bin/Debug/net8.0-windows/` after building.

To test quickly, choose the included `sample-cube.obj` with **Load model**.

## Features

- Load OBJ files, including polygon faces that are triangulated during import
- Normalize and automatically frame imported geometry
- Generate up to 6,000 surface-derived particles
- Change particle color with a hex color value
- Choose particle hue, saturation, and brightness with the built-in color picker, with optional HEX precision input
- Show a smoothed render-cadence FPS counter in the upper-right of the viewport
- Choose Cube, Sphere, Tetrahedron, Billboard, or Image Billboard particles
- Load PNG, JPG, or JPEG images for camera-facing image billboards
- Use the built-in `radial_gradient.png` texture for immediate billboard testing
- Adjust particle count and size
- Orbit with mouse drag and zoom with the mouse wheel
- Orbit with left-drag, pan with right-drag, and zoom with the mouse wheel; drag capture is released safely when focus or buttons change
- Rotate around the Y axis with a slider
- Keep the original mesh hidden by default, with an optional **Show original mesh** reference toggle
- Run a lightweight soft-body simulation with restoring forces, deformation resistance, elasticity, damping, gravity, and optional ground collision
- Preserve cohesion with local rest-position spring constraints built from nearby particles
- Tune spring stiffness and bounce separately from deformation resistance, elasticity, and damping
- Adjust deformation resistance independently from damping while the simulation is running
- Display and adjust an optional ground plane without rotating it with the model
- Reset the camera or visualization
- Graceful messages for unsupported FBX files, invalid OBJ files, invalid colors, and zero-size models

## Technology

- C# and .NET 8 WPF
- WPF `Viewport3D` for the desktop 3D viewport
- No third-party packages or external services

## How particle generation works

The OBJ loader reads vertices and faces. Each face is triangulated, triangles are weighted by surface area, and points are sampled with barycentric coordinates across those triangles. Particle shapes reuse those same surface points. Billboards are camera-facing quads rebuilt when the camera or model rotation changes. Image billboards use an `ImageBrush` with `Stretch.Uniform`, preserving image aspect ratio and PNG transparency.

## Soft-body simulation

Enable **Soft body** to let particles move with a cohesive spring simulation. Each particle remembers its sampled rest position and is connected to a capped set of nearby rest-position neighbors. Those local springs restore rest lengths while a gentle rest-shape and center-of-mass force prevents permanent collapse. **Spring stiffness** ranges from `0.00` to `1.00`; low values feel softer and high values feel more rubbery. **Deformation resistance** scales the global rest-position force, **Elasticity** controls compression recovery and lateral spreading, and **Damping** controls velocity/energy dissipation. **Bounce** controls the bounded ground restitution independently from spring damping. When **Ground plane** and **Ground collision** are enabled, particles are kept above the plane, downward velocity is reflected, and modest friction slows sliding. The soft-body approximation allows compression and recovery without uniformly scaling the model. **Reset simulation** restores all particles, velocities, and spring rest lengths without reloading the model.

This is intentionally a lightweight shape-preserving effect, not a full physics engine. The spring graph is built once with a spatial hash rather than comparing every particle pair each frame. It uses two semi-implicit Euler substeps per timer update to keep reasonable stiffness values stable; very high deformation or dense models may still look approximate.

The FPS indicator measures WPF `CompositionTarget.Rendering` callbacks, which is the closest render cadence available to this architecture. It uses a smoothed rolling measurement and refreshes about four times per second; it is not a hardware/GPU profiler.

## Radial-gradient billboard test

`radial_gradient.png` is a 256×256 transparent PNG with a bright center and soft transparent edges. Choose **Image Billboard**, then **Use built-in radial gradient** to test camera-facing texture particles without finding an image manually.

## Known limitations

- OBJ is the only implemented model format. FBX is detected and reported as unsupported; it has not been implemented or tested.
- Geometric particles are combined into one WPF mesh rather than thousands of UI objects, but they are still CPU-side geometry rather than GPU point sprites.
- Low-poly spheres and image billboards can still become expensive at the 6,000-particle maximum; reduce count for complex models if interaction slows.
- Image billboard color is not applied automatically, so the source image keeps its original colors.
- Soft-body spring updates and combined mesh positions are processed on the UI thread; static visualization remains the preferred mode for large models.
- Pan, materials, textures, normals, and animation are not implemented.
- The current environment supports compilation but does not provide a GUI test harness, so interactive viewport testing must be performed by opening the application on Windows.
- Color selection updates the preview and HEX field while dragging; Apply commits the selected color to a shared particle material without regenerating particle positions or geometry. Image billboards continue to use their source image colors.

## Future ideas

- Add a reliable FBX/glTF loader or an established 3D library.
- Use GPU point rendering for much larger particle counts.
- Add pan, presets, model visibility toggles, and a small import preview.
