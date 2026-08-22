# 3D Bounce Simulator

This Windows desktop application imports an OBJ model and presents it as a deformable soft body. Particles remain the internal representation, but the visible interface focuses on the object and its simulation.

## How to run

From the repository root:

```powershell
dotnet run --project projects/3d-bounce-simulator/3d-bounce-simulator.csproj
```

Load the included `sample-cube.obj` to try the simulation.

## Features

- Load and normalize OBJ models with triangulated polygon faces
- Compact soft-body workflow with **Soft Body**, **Ground Plane**, **Chaos**, **Momentum**, **Drop Height**, and **Reset Simulation** controls
- Black viewport with a visible solid neutral ground plane
- Ground visibility and floor collision share the Ground Plane state
- Left-drag orbit, right-drag pan, mouse-wheel zoom, Reset View, and Reset Rotation
- Smoothed render-cadence FPS indicator
- Internal surface-derived particle representation with local spring constraints
- Graceful messages for unsupported FBX files, invalid OBJ files, invalid colors, and zero-size models

Particle color, shape, image, count, size, billboard, and radial-gradient controls remain internal and are hidden from the soft-body-focused interface.

## Technology

- C# and .NET 8 WPF
- WPF `Viewport3D`
- No third-party packages or external services

## Soft-body simulation

Enable **Soft Body** to let the model fall as a cohesive spring-constrained object.

- **Ground Plane:** shows the solid collision surface and enables floor collision while Soft Body is active. Turning it off hides the plane and disables floor collision.
- **Chaos:** adds bounded, time-varying disturbance forces. It does not randomize positions or disconnect particles.
- **Momentum:** seeds the initial downward velocity when the simulation is reset.
- **Drop Height:** offsets the initial object position above the ground when the simulation is reset.

Each particle remembers its sampled rest position and is connected to a capped set of nearby rest-position neighbors. Local springs restore rest lengths while rest-shape and center-of-mass forces resist permanent collapse. The implementation uses a spatial hash to build the graph once and two semi-implicit Euler substeps per timer update. Existing spring stiffness, deformation resistance, elasticity, damping, and bounce values remain internal tuning parameters.

**Reset Simulation** stops the run, restores the selected starting height and momentum, resets orientation, and preserves the loaded model and visible settings.

The ground response corrects penetration, reflects downward velocity according to restitution, and applies modest friction. This is a lightweight shape-preserving approximation rather than a full physics engine; high particle counts and extreme settings may still look approximate.

## Known limitations

- OBJ is the only implemented model format. FBX is detected and reported as unsupported.
- Spring simulation and combined mesh updates run on the UI thread.
- Geometric particles are CPU-side combined WPF geometry rather than GPU point sprites.
- The current environment supports compilation but does not provide a GUI test harness, so interactive cube/brain testing must be performed on Windows.

## Future ideas

- Add a reliable FBX/glTF loader.
- Move soft-body updates and rendering to a more scalable GPU-oriented path.
- Add model presets and a small import preview.
