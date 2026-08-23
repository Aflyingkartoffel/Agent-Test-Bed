# Insect Light Simulation

Milestone one is a small .NET 8 WPF procedural simulation: hundreds of pixel insects move through a black 2D world, combining steering forces to wander toward a mathematical light source.

## Run

From the repository root:

```powershell
dotnet restore projects/insect-light-simulation/insect-light-simulation.csproj
dotnet build projects/insect-light-simulation/insect-light-simulation.csproj
dotnet run --project projects/insect-light-simulation/insect-light-simulation.csproj
```

Run the simulation-layer tests:

```powershell
dotnet run --project projects/insect-light-simulation-tests/insect-light-simulation-tests.csproj
```

Run the headless render/update stress benchmark:

```powershell
dotnet run --project projects/insect-light-simulation-tests/insect-light-simulation-tests.csproj -- --benchmark
```

Optional self-contained Windows publish:

```powershell
dotnet publish projects/insect-light-simulation/insect-light-simulation.csproj -c Release -r win-x64 --self-contained true -o projects/insect-light-simulation/publish
```

## Architecture

- `SimulationEngine` owns time, agents, a bounded collection of lights, settings, and fixed-size updates.
- `Agent` stores position, velocity, acceleration, heading, and small individual variations. It has no rendering code.
- `IBehavior` makes each force independently composable. Attraction, temporally coherent wander, and soft boundary steering are the first behaviors.
- `PixelRenderer` converts floating-point positions into one cached `WriteableBitmap`. It draws a tiny green insect pattern and a yellow-white glow without creating a WPF object per insect.
- `InsectSpriteCache` contains four reference-based top-view wing frames and eight precomputed heading rotations. The runtime draws cached packed pixels directly into the shared bitmap.
- `MainWindow` translates controls into shared simulation state, provides add/remove/select/drag interactions for lights, and displays actual runtime statistics.

The movement model is position + velocity + acceleration. A behavior returns a steering force; the engine combines those forces, limits the turn rate, limits speed, integrates position, and applies the selected boundary mode. The attraction behavior sums the vector contribution from every light whose influence radius contains the insect. The simulation never reads rendered pixels: each light has a position, radius, attraction strength, and visual intensity in simulation space.

The LIGHT POWER slider is a UI abstraction for the selected light. Power ranges from 0 to 2, with 1 as the default. It scales that light's attraction strength, influence radius, and visual intensity while keeping those three underlying properties separate for the simulation and renderer. Selecting a different light loads that light's independent power value.

The supplied design reference is `Ref/Insect Ref.png`. Its top view and four labeled wing positions (top, mid, down, mid) were adapted into a small procedural pixel sprite under `Rendering/InsectSpriteCache.cs`; the reference sheet itself is not rendered by the application. Each agent receives a deterministic animation phase and small flap-speed variation. Wing animation is visual state layered on top of procedural flight: position and heading still come from vectors and behaviors, while the renderer selects the current wing frame and cached rotation.

The update is driven by a stable 16 ms UI tick and clamps unusually large frame gaps. The speed control scales elapsed simulation time, while the underlying behavior equations stay unchanged.

## Current milestone

Implemented: pause/resume, reset, 10–2000 insects, speed multiplier, 1–16 selectable lights, add/remove controls, nearest-light hit testing, mouse dragging in simulation coordinates, selected-light power control, independent derived attraction/radius/intensity values, base speed, turn rate, wander strength, wraparound/soft-bounce boundaries, deterministic seed, pixel rendering, and smoothed actual FPS/average-speed/time statistics.

Not implemented yet: neighbor grids, trails, debug vectors, full flocking, GPU rendering, and a general-purpose procedural animation framework.

The current polish pass caches the reference-based sprite frames and rotations, avoids LINQ in average-speed and light-ID calculations, and refreshes statistics five times per second instead of formatting them on every render frame. The pixel buffer and `WriteableBitmap` are still reused across frames.
