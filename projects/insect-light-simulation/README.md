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

Optional self-contained Windows publish:

```powershell
dotnet publish projects/insect-light-simulation/insect-light-simulation.csproj -c Release -r win-x64 --self-contained true -o projects/insect-light-simulation/publish
```

## Architecture

- `SimulationEngine` owns time, agents, the light, settings, and fixed-size updates.
- `Agent` stores position, velocity, acceleration, heading, and small individual variations. It has no rendering code.
- `IBehavior` makes each force independently composable. Attraction, temporally coherent wander, and soft boundary steering are the first behaviors.
- `PixelRenderer` converts floating-point positions into one cached `WriteableBitmap`. It draws a tiny green insect pattern and a yellow-white glow without creating a WPF object per insect.
- `MainWindow` translates controls into `SimulationSettings` and displays actual runtime statistics.

The movement model is position + velocity + acceleration. A behavior returns a steering force; the engine combines those forces, limits the turn rate, limits speed, integrates position, and applies the selected boundary mode. The simulation never reads rendered pixels: the light is a position, radius, and attraction strength in simulation space.

The update is driven by a stable 16 ms UI tick and clamps unusually large frame gaps. The speed control scales elapsed simulation time, while the underlying behavior equations stay unchanged.

## Current milestone

Implemented: pause/resume, reset, 10–2000 insects, speed multiplier, attraction strength, influence radius, light intensity, base speed, turn rate, wander strength, wraparound/soft-bounce boundaries, deterministic seed, pixel rendering, and actual FPS/average-speed/time statistics.

Not implemented yet: neighbor grids, draggable light, trails, debug vectors, full flocking, GPU rendering, and a general-purpose procedural animation framework.
