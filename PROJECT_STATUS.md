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

## Appearance, anatomy views, and bend stability

The current creature-lab milestone adds authored skin color with Create-mode color picking and JSON persistence, almond eyes with width/height controls, smooth Play pupil tracking, and mirrored feature rendering. Play Mode defaults to solid body plus features, with optional Skeleton and Muscles diagnostic overlays. Muscle lines are derived from the current chain positions/radii and do not affect simulation.

Play deformation now applies a 75-degree local bend limit after distance following and wave motion, then restores authored connection rest lengths. This is a bend-stability constraint only; self-collision is intentionally not implemented. The lab test harness now has 94 passing tests covering the new appearance and deformation behavior.

## Soft bend and construction-circle visualization

The previous bend solver hard-snapped each violating downstream child to the exact angular boundary, so sequential joints could overwrite one another and form alternating zig-zag bends. The replacement uses signed angles, six small distance/angular iterations, 25% angular stiffness, and lightweight correction sharing between the joint and child. It preserves dynamic Play curvature without targeting authored rotations.

MUSCLES now renders the actual construction circles used by the procedural skin: one transparent radius outline per node. SKELETON remains centerline plus connections, and SKIN remains the smoothed outer envelope. Create and Play have independent display toggles. Full non-adjacent self-collision remains out of scope.

## Editing history, feature clipboard, orb eyes, and fins

Create-mode authoring now has bounded snapshot Undo history with selection restoration and one history entry per grouped drag. Ctrl+C/Ctrl+V duplicates selected features with new IDs and a local offset; Ctrl+Z removes the pasted feature. Eyes are pure-white procedural circles with dark, constrained, smoothly tracked pupils. Skeleton overlays now expose tongue stem/fork bones and fin attachment-to-tip bones.

The new Fin feature attaches to a selected body node and left/right body side. Its tapered shape uses the node radius at the attachment, while Play mode maintains temporary spring-damped angular state with stiffness, damping, a ±120° safety range, pause/reset behavior, and no body-solver participation. Fin authored data is saved; temporary angular state is not. The test runner now has 121 passing tests.
## Fin mirroring, rounded shape, and Bezier handles

The current milestone supports mirrored Fin pairs with quarter-radius attachment, rounded sampled organic outlines, persisted editable Fin Color, and Fin skeleton/hit-test coverage. Body Size Ramp Bezier mode now stores explicit incoming/outgoing authored handles with endpoint rules and automatic migration for older files. Verification: project build succeeded and 126 editor tests passed.
