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
- Adjust particle count and size
- Orbit with mouse drag and zoom with the mouse wheel
- Rotate around the Y axis with a slider
- Keep the original mesh hidden by default, with an optional **Show original mesh** reference toggle
- Reset the camera or visualization
- Graceful messages for unsupported FBX files, invalid OBJ files, invalid colors, and zero-size models

## Technology

- C# and .NET 8 WPF
- WPF `Viewport3D` for the desktop 3D viewport
- No third-party packages or external services

## How particle generation works

The OBJ loader reads vertices and faces. Each face is triangulated, triangles are weighted by surface area, and points are sampled with barycentric coordinates across those triangles. Particle shapes reuse those same surface points. Billboards are camera-facing quads rebuilt when the camera or model rotation changes. Image billboards use an `ImageBrush` with `Stretch.Uniform`, preserving image aspect ratio and PNG transparency.

## Known limitations

- OBJ is the only implemented model format. FBX is detected and reported as unsupported; it has not been implemented or tested.
- Geometric particles are combined into one WPF mesh rather than thousands of UI objects, but they are still CPU-side geometry rather than GPU point sprites.
- Low-poly spheres and image billboards can still become expensive at the 6,000-particle maximum; reduce count for complex models if interaction slows.
- Image billboard color is not applied automatically, so the source image keeps its original colors.
- Pan, materials, textures, normals, and animation are not implemented.
- The current environment supports compilation but does not provide a GUI test harness, so interactive viewport testing must be performed by opening the application on Windows.

## Future ideas

- Add a reliable FBX/glTF loader or an established 3D library.
- Use GPU point rendering for much larger particle counts.
- Add pan, presets, model visibility toggles, and a small import preview.
