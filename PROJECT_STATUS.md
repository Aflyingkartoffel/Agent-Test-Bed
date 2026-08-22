# Project Status

The first application, a browser-based calculator, has been created. The second application is the WPF 3D Bounce Simulator, which presents an OBJ model as a cohesive soft body with an optional ground simulation.

## Repository

- **Purpose:** Personal AI-assisted application development laboratory
- **Current status:** Scaffolded, initialized as a local Git repository, and ready for GitHub connection
- **Technology/language:** HTML/CSS/vanilla JavaScript for the calculator; C#/.NET 8 WPF for the particle viewer
- **What currently works:** Calculator core workflow; 3D Bounce Simulator build, OBJ parsing, cohesive local spring constraints, black viewport, visible optional ground plane/collision, compact soft-body controls for Chaos/Momentum/Drop Height, reset-to-start behavior, and camera view/rotation controls
- **What is being worked on:** Interactive Windows testing of the simplified interface, cube/brain spring stability, chaos/momentum/drop-height differences, and settling behavior
- **Known problems:** FBX and GPU point rendering are not implemented; spring simulation and combined mesh updates run on the UI thread; anatomical visual testing requires an anatomical model asset not present in this repository
- **Next logical steps:** Test with a small and complex OBJ, then decide whether a reliable FBX/glTF loader is worth adding

## Projects

Current projects:

- `projects/calculator/`
- `projects/3d-bounce-simulator/`
