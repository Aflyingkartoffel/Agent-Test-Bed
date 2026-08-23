# Project Status

The first application, a browser-based calculator, has been created. The second application is the WPF 3D Bounce Simulator. Its current foundation stage focuses on importing and viewing solid OBJ/FBX meshes; physics and particle features have intentionally not been rebuilt yet.

## Repository

- **Purpose:** Personal AI-assisted application development laboratory
- **Current status:** Scaffolded, initialized as a local Git repository, and ready for GitHub connection
- **Technology/language:** HTML/CSS/vanilla JavaScript for the calculator; C#/.NET 8 WPF with AssimpNet for the 3D Bounce Simulator
- **What currently works:** Calculator core workflow; 3D Bounce Simulator Stage 1 build, actual OBJ/FBX import path, solid mesh rendering, validation, automatic centering/scaling/framing, orbit, zoom, and Reset View
- **What is being worked on:** Interactive Windows testing with cube, complex OBJ, and FBX assets
- **Known problems:** Stage 1 uses one simple material; animation, textures, physics, particles, soft-body behavior, ground collision, and advanced FBX material preservation are not implemented
- **Next logical steps:** Confirm visual OBJ/FBX loading, then add the next feature only after this foundation is stable

## Projects

Current projects:

- `projects/calculator/`
- `projects/3d-bounce-simulator/`

## Creature features and smooth skin

The creature laboratory now defaults to `2.0x` Simulation Speed while retaining Max Speed `720`. Procedural skin uses Catmull-Rom sampled side curves with rounded head/tail caps, derived from the existing node positions and radii. A reusable local-space `CreatureFeature` model replaces the eye-specific definition; Eye is the first supported type, with root parenting, manual local transforms, scale, visibility, mirrored rendering, Save/Load persistence, and migration from older eye JSON. Feature controls are Create-only; Play Mode keeps only visual attachment to simulated parents.

## Creature tongue and skin-cap milestone

Terminal skin geometry now wraps explicitly around the first and last node radii using sampled semicircular caps and one-sided endpoint tangents. The body still uses derived node positions/radii only. `ForkedTongue` extends the local-parent feature architecture with vector stem/fork geometry, length/fork-length/fork-angle controls, root parenting, singular non-mirrored behavior, Play attachment, and Save/Load persistence.
## Creature tongue and skin-cap milestone

Terminal skin geometry now wraps explicitly around the first and last node radii using sampled semicircular caps and one-sided endpoint tangents. The body still uses derived node positions/radii only. `ForkedTongue` extends the local-parent feature architecture with vector stem/fork geometry, length/fork-length/fork-angle controls, root parenting, singular non-mirrored behavior, Play attachment, and Save/Load persistence.

The follow-up cap-orientation fix uses explicit outward tangent vectors so the head cap points opposite the body direction and the tail cap points away from the preceding node. Regression tests cover right-facing heads, left-facing tails, cap midpoints, endpoint radii, and curved chains. Final canvas-level visual confirmation remains user verification in this environment.
