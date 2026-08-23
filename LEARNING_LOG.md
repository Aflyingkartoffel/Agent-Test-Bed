# Learning Log

This log records useful programming concepts encountered while building the repository's applications. It is intentionally brief and will grow with the projects.

## 2026-08-21 — Local Git repository and remote repository

- **What it is:** Git tracks local versions of files as commits. A remote repository, such as one on GitHub, stores a copy that can be shared and used as the project history's source of truth.
- **Where we encountered it:** We initialized the repository on `main` and created the first commit. The GitHub account is connected, but the remote repository itself has not yet been created.
- **Why it matters:** Keeping local commits separate from pushing to a remote makes it possible to review changes before sharing them.

## 2026-08-21 — 3D particle model viewer

- **Vertices, faces, and meshes:** An OBJ model stores 3D points (vertices) and the faces that connect them into a mesh.
- **Surface sampling:** The viewer chooses points inside imported triangles using barycentric coordinates, so particles follow the actual model surface.
- **Camera transforms:** A 3D camera position, look direction, and distance determine which part of the scene is visible; mouse movement changes those values for orbiting and zooming.
- **Desktop UI events and state:** WPF button, slider, mouse, and file-dialog events update shared model and visualization state.
- **Billboards and materials:** A billboard is a camera-facing quad; an `ImageBrush` supplies its texture while a `DiffuseMaterial` connects that appearance to WPF 3D geometry.
- **Particle simulation:** Rest positions, restoring forces, damping, and a bounded timestep create a lightweight deformable effect without pretending to be a full physics engine.
- **Collision response:** Ground collision checks particle penetration, corrects the position, reverses downward velocity with a small bounce, and damps horizontal motion.

## 2026-08-21 — Repository structure and documentation

- **What it is:** A repository is a shared project folder whose history can be tracked with Git. Documentation files explain the project's purpose, current state, and working conventions.
- **Where we encountered it:** We created a top-level structure that separates applications, experiments, and broader documentation.
- **Why it matters:** Clear structure and current documentation help both people and AI assistants understand what exists and what should happen next.

## 2026-08-21 — Deformation resistance and simulation state

- **Rest state:** A simulation needs a separate copy of each particle's original position; comparing a particle to its current position produces no restoring force.
- **Parameter separation:** Deformation resistance scales the force pulling particles toward rest, while damping independently reduces velocity and energy over time.
- **Live UI updates:** A WPF slider event can update a simulation parameter during the timer loop without resetting the simulation state.

## 2026-08-21 — HSV color picking and bounded elasticity

- **HSV color model:** Hue, saturation, and value are convenient independent controls for a visual color picker; the renderer still receives a WPF `Color`.
- **Custom WPF control:** A small reusable `UserControl` can own drawing, pointer events, and a `ColorChanged` event without adding a UI library.
- **Approximate volume preservation:** During ground compression, targeting bounded lateral expansion from the rest shape creates a more convincing squish without a full particle-neighbor physics system.

## 2026-08-21 — Render timing, capture, and shared materials

- **Render timing:** WPF's `CompositionTarget.Rendering` event provides a practical render-cadence signal; smoothing several samples makes an FPS display more useful than single-frame timing.
- **Mouse capture:** Capturing the viewport during a drag and releasing it on mouse-up, capture loss, and window deactivation prevents stale interaction state.
- **Shared material:** A reusable brush/material lets a color-only change reach existing geometry without regenerating particle positions or meshes.

## 2026-08-21 — Spring-constrained soft bodies

- **Rest positions and spring constraints:** Neighbor particles are connected using distances measured from the original model, so deformation produces restoring forces between particles instead of independent falling points.
- **Damping and restitution:** Spring/velocity damping removes oscillation over time, while restitution separately controls how much normal collision velocity returns from the ground.
- **Shape preservation:** Rest-position, center-of-mass, and bounded lateral forces help a compressed object recover without preventing all deformation.
- **Substeps and spatial hashing:** Two semi-implicit Euler substeps improve stability, while a spatial hash builds a local graph without an O(n²) every-frame search.

## 2026-08-21 — Soft-body-focused interface

- **Internal versus user-facing representation:** A particle graph can remain the implementation detail while the UI presents the result as one deformable object.
- **Independent simulation inputs:** Drop Height changes initial position, Momentum changes initial velocity, and Chaos adds bounded disturbances; none is merely a relabeled gravity multiplier.
- **Visibility and collision state:** A visible ground plane and its collision toggle should share one clear state so an object is never colliding with an invisible surface.

## 2026-08-21 — Project rename

- **Git-aware renaming:** Renaming a project includes its folder, project file, assembly identity, run commands, application title, and current documentation references; internal technical class names can remain when they still describe their implementation role.

## 2026-08-22 — 3D Bounce Simulator foundation rebuild

- **Scope control:** A reliable foundation should be validated before adding physics; Stage 1 intentionally contains only import, solid mesh rendering, framing, orbit, zoom, and reset view.
- **Format import:** AssimpNet converts OBJ and FBX files into standard triangle data that WPF can render as `MeshGeometry3D`.
- **Validation:** Importers should reject missing files, unsupported extensions, empty meshes, zero-size bounds, and non-finite coordinates before creating viewport geometry.

## 2026-08-21 — Building the calculator

- **Functions:** A function packages a focused action, such as entering a number or clearing the calculator, so the same behavior can be called from both buttons and keyboard input.
- **Events and event handlers:** A browser event represents an interaction, such as a click or key press. An event handler listens for it and runs the matching calculator function.
- **Application state:** Variables such as the current expression and current input remember what the user has entered so the display and next operation stay synchronized.
- **Operator precedence:** The calculator uses separate value and operator stacks so multiplication and division are applied before addition and subtraction.

## Procedural features and visual sampling

A procedural visual feature should store a local rule relative to its parent rather than a final world position. For example: `Head Position + Local Eye Offset = Eye World Position`. When the head moves or rotates, the eye follows automatically because the renderer resolves the local transform against the current parent transform.

Structural nodes can remain low-resolution while the displayed skin is smoothed with interpolated visual samples. The creature still has the same nodes, connections, spacing, and radii; Catmull-Rom samples only refine the rendered silhouette between those authored points.

## Terminal geometry and feature capability

Interpolating through side points is not sufficient for terminal geometry. A terminal node is a volume-bearing part of the creature, so the silhouette must explicitly wrap around its radius with a cap. Body interpolation can remain smooth, but the first and last circles need their own tangent-aware semicircular geometry.

Feature capabilities should belong to the feature type rather than being assumed universal. Eyes are mirrorable, while Forked Tongue is singular. A `SupportsMirroring` rule keeps the editor and renderer from inventing a second visual for a feature that should only exist once.
