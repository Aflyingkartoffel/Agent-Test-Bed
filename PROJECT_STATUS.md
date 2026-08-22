# Project Status

The first application, a browser-based calculator, has been created. The second application, a WPF 3D particle model viewer, is implemented through the initial tested desktop build.

## Repository

- **Purpose:** Personal AI-assisted application development laboratory
- **Current status:** Scaffolded, initialized as a local Git repository, and ready for GitHub connection
- **Technology/language:** HTML/CSS/vanilla JavaScript for the calculator; C#/.NET 8 WPF for the particle viewer
- **What currently works:** Calculator core workflow; particle viewer build, OBJ parsing, surface-derived particles, color/count/size controls, mouse orbit, zoom, reset camera, and rotation slider
- **What is being worked on:** Interactive Windows testing of the particle viewer with the included sample cube
- **Known problems:** FBX, pan, textures, and GPU point rendering are not implemented; GUI testing must be performed manually on Windows
- **Next logical steps:** Test with a small and complex OBJ, then decide whether a reliable FBX/glTF loader is worth adding

## Projects

Current projects:

- `projects/calculator/`
- `projects/3d-particle-viewer/`
