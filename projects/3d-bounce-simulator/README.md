# 3D Bounce Simulator

Stage 1 is a clean WPF foundation for loading and viewing solid 3D models.

## Run from source

```powershell
dotnet run --project projects/3d-bounce-simulator/3d-bounce-simulator.csproj
```

The application starts as an empty 3D viewport. No model, sample cube, particles, or physics are loaded automatically. Click **Load Model...** to choose an OBJ or FBX file.

The repository includes `sample-cube.obj` as an optional import fixture for testing; it is never loaded automatically.

## Build standalone Windows application

From the repository root, run:

```powershell
dotnet publish "projects/3d-bounce-simulator/3d-bounce-simulator.csproj" -c Release -r win-x64 --self-contained true -o "projects/3d-bounce-simulator/publish"
```

This creates a self-contained Windows x64 application. The published folder includes the .NET runtime and Assimp native runtime files, so a separate .NET installation is not required.

## Run standalone application

Open `projects/3d-bounce-simulator/publish/` in File Explorer and double-click **3D Bounce Simulator.exe**. No terminal, PowerShell, Visual Studio, or VS Code is required.

## Stage 1 features

- OBJ import
- FBX import through AssimpNet 4.1.0
- Solid triangle-mesh rendering with simple lighting
- Automatic validation, centering, scaling, and camera framing
- Left-drag orbit camera
- Mouse-wheel zoom
- Reset View button and `R` keyboard shortcut
- User-friendly import errors for unsupported, corrupt, empty, or invalid models

When no model is loaded, **Reset View** restores the default empty-scene camera. After a successful import, it frames the loaded model again.

AssimpNet is used because WPF does not provide a general FBX importer. The library supports Windows and exposes imported OBJ/FBX triangle data that the application converts to standard WPF `MeshGeometry3D` data. No physics, particles, springs, gravity, collision, ground plane, chaos, momentum, or deformation is implemented in this stage.

## Limitations

- The importer is tested at the import/build level in this repository; visual FBX testing requires an FBX asset.
- Materials and textures are intentionally reduced to one simple solid material for this foundation.
- Animation, skeletal data, and advanced material preservation are not implemented.
