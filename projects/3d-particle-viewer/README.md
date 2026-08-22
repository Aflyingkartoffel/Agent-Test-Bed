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
- Choose Cube, Sphere, Tetrahedron, Billboard, or Image Billboard particles
- Load PNG, JPG, or JPEG images for camera-facing image billboards
- Use the built-in `radial_gradient.png` texture for immediate billboard testing
- Adjust particle count and size
- Orbit with mouse drag and zoom with the mouse wheel
- Rotate around the Y axis with a slider
- Keep the original mesh hidden by default, with an optional **Show original mesh** reference toggle
- Run a lightweight soft-body simulation with restoring forces, damping, gravity, and optional ground collision
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

Enable **Soft body** to let particles move with a simple stable simulation. Each particle remembers its sampled rest position and receives a restoring force toward that position, plus gravity and velocity damping. When **Ground plane** is enabled, particles are kept above the configured plane and downward velocity is reduced on contact. **Reset simulation** restores all particles and velocities without reloading the model.

This is intentionally a lightweight shape-preserving effect, not a full physics engine. It does not calculate particle-to-particle springs or rigid-body contacts, so very high deformation or dense models may look approximate.

## Radial-gradient billboard test

`radial_gradient.png` is a 256×256 transparent PNG with a bright center and soft transparent edges. Choose **Image Billboard**, then **Use built-in radial gradient** to test camera-facing texture particles without finding an image manually.

## Known limitations

- OBJ is the only implemented model format. FBX is detected and reported as unsupported; it has not been implemented or tested.
- Geometric particles are combined into one WPF mesh rather than thousands of UI objects, but they are still CPU-side geometry rather than GPU point sprites.
- Low-poly spheres and image billboards can still become expensive at the 6,000-particle maximum; reduce count for complex models if interaction slows.
- Image billboard color is not applied automatically, so the source image keeps its original colors.
- Soft-body updates currently update combined mesh positions on the UI thread; static visualization remains the preferred mode for large models.
- Pan, materials, textures, normals, and animation are not implemented.
- The current environment supports compilation but does not provide a GUI test harness, so interactive viewport testing must be performed by opening the application on Windows.

## Future ideas

- Add a reliable FBX/glTF loader or an established 3D library.
- Use GPU point rendering for much larger particle counts.
- Add pan, presets, model visibility toggles, and a small import preview.
