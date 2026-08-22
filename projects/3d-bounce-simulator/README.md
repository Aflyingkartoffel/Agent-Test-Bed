# 3D Bounce Simulator

Stage 1 is a clean WPF foundation for loading and viewing solid 3D models.

## Run

```powershell
dotnet run --project projects/3d-bounce-simulator/3d-bounce-simulator.csproj
```

Load the included `sample-cube.obj` to try the foundation.

## Stage 1 features

- OBJ import
- FBX import through AssimpNet 4.1.0
- Solid triangle-mesh rendering with simple lighting
- Automatic validation, centering, scaling, and camera framing
- Left-drag orbit camera
- Mouse-wheel zoom
- Reset View button and `R` keyboard shortcut
- User-friendly import errors for unsupported, corrupt, empty, or invalid models

AssimpNet is used because WPF does not provide a general FBX importer. The library supports Windows and exposes imported OBJ/FBX triangle data that the application converts to standard WPF `MeshGeometry3D` data. No physics, particles, springs, gravity, collision, ground plane, chaos, momentum, or deformation is implemented in this stage.

## Limitations

- The importer is tested at the import/build level in this repository; visual FBX testing requires an FBX asset.
- Materials and textures are intentionally reduced to one simple solid material for this foundation.
- Animation, skeletal data, and advanced material preservation are not implemented.
