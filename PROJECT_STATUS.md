# Project Status

The first application, a browser-based calculator, has been created. The second application, a WPF 3D particle model viewer, now has selectable geometric and billboard particle representations plus an optional lightweight soft-body/ground simulation.

## Repository

- **Purpose:** Personal AI-assisted application development laboratory
- **Current status:** Scaffolded, initialized as a local Git repository, and ready for GitHub connection
- **Technology/language:** HTML/CSS/vanilla JavaScript for the calculator; C#/.NET 8 WPF for the particle viewer
- **What currently works:** Calculator core workflow; particle viewer build, OBJ parsing, surface-derived particles, HSV color picker with HEX input, all appearance controls, built-in radial-gradient billboard test asset, adjustable deformation resistance and elasticity, soft-body restoring-force simulation, reset behavior, optional ground plane/collision, and particle-level collision response
- **What is being worked on:** Interactive Windows testing of picker behavior, simulation stability, elasticity tuning, and each particle shape with a representative anatomical OBJ
- **Known problems:** FBX, pan, particle-to-particle springs, and GPU point rendering are not implemented; anatomical visual testing requires an anatomical model asset not present in this repository
- **Next logical steps:** Test with a small and complex OBJ, then decide whether a reliable FBX/glTF loader is worth adding

## Projects

Current projects:

- `projects/calculator/`
- `projects/3d-particle-viewer/`
