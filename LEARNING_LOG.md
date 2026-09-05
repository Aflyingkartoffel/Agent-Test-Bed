# Learning Log

## 2026-09-02 — Time-Series Sonifier FFT spectrum

- FFT visualization belongs downstream of synthesis: a bounded sample tap lets the audio callback return quickly while a separate update path performs Hann windowing, radix-2 FFT, dB conversion, smoothing, and rendering.
- A real-valued signal only needs bins 0 through Nyquist. A logarithmic display axis makes musical frequency regions easier to inspect, while volume remains visible because samples are tapped after gain application.

## 2026-09-02 — Equation Synth roadmap completion

- A development progress indicator should represent roadmap state, not runtime evaluation state; it can remain static and responsive without entering project persistence or undo history.

## 2026-09-02 — Equation Synth procedural outputs

- A target-independent output binding lets the expression system produce numbers without knowing whether a WPF transform, color channel, or future simulation property will consume them.
- Separating base values from evaluated values preserves authored intent while time and automation continuously update the preview.
- Sequential binding order makes conflicts deterministic: each Replace/Add/Multiply operation receives the result of the preceding binding.

## 2026-09-02 — Equation Synth mixer

- Graph visibility and audio participation are separate authored concerns; one equation can be hidden visually while remaining audible.
- A mixer should own one persistent phase and envelope state per layer so changing another layer cannot reset an oscillator.
- Equal-power panning preserves perceived loudness better than linear left/right scaling, while a voice-count-aware attenuation and soft limiter protect summed output.

## 2026-09-02 — Equation Synth V1 integration

- Undo is most reliable when history stores structural authored snapshots and dirty state compares the current snapshot with the last saved snapshot, rather than using an undo-count heuristic.
- Continuous controls need explicit begin/live/end transactions so many pointer events become one user-level operation.
- A safe load pipeline validates a temporary model before replacing the live model; this keeps malformed files from partially destroying an active session.

## 2026-09-01 — Equation Synth foundation

- A restricted parser turns text into a reusable AST, so graph and audio evaluate the same expression without reparsing each frame.
- Invalid and non-finite values are explicit evaluation results; visualization can break a line while audio can replace a sample with zero.
- A wavetable treats the equation as one cycle from 0 to 2π, while oscillator frequency independently controls pitch.

## 2026-09-01 — Interactive graph workspace

- A graph camera centralizes world/screen transforms, making cursor-anchored zoom and pan testable without WPF event code.
- Multiple equations can share one parameter dictionary while retaining independent parsed ASTs, colors, visibility, and selection.
- Sampling at roughly one to two points per screen pixel adapts graph quality to the viewport while a hard maximum protects the render loop.

## 2026-09-01 — Procedural animation

- Keeping manual and effective parameter values separate means automation can animate safely without destroying the user’s authored value.
- Expression-driven parameters form a dependency graph; resolving dependencies with cycle detection prevents self-reference and mutual-reference hangs.
- A bounded trail queue and interval-based recording expose time history without allocating unbounded geometry every render frame.

## 2026-09-01 — Streaming audio

- Real-time audio should receive precomputed wavetable data; expression parsing and AST evaluation do not belong in a time-sensitive audio callback.
- A persistent oscillator phase preserves continuity when frequency, parameters, time, or the selected expression changes.
- Double/triple buffering and short table crossfades separate device timing from UI updates while reducing audible discontinuities.

## 2026-09-02 — V1 workflow

- Persistent application state should have one validation boundary before serialization or replacement, so malformed projects cannot partially destroy the current session.
- A bounded snapshot history is a practical starting point for desktop undo/redo because it keeps restoration logic simple and prevents unbounded memory growth.

## 2026-08-23 — Creature editor foundation

- A `CreatureDefinition` owns an arbitrary list of `CreatureNode` objects, keeping data independent from WPF rendering.
- `System.Numerics.Vector2` is used for positions so later distance, direction, and constraint calculations have a natural foundation.
- An editor state object centralizes mode, tool, selection, and mutation events; the canvas only handles input and drawing.
- Structural spacing belongs to node centers, not visual circle radii: every adjacent pair satisfies `distance(parent.Position, child.Position) = ChainSpacing`.
- Rebuilding a chain must snapshot segment directions before moving any node; otherwise moving an upstream parent can accidentally change downstream directions.
- Procedural construction stores a rule instead of unrelated results: `normalized chain position → sample BodySizeRamp → BaseRadius × ramp value → node radius`.
- The chain’s structural order, not world-space distance, determines normalized body position, so adding or deleting nodes correctly re-samples the profile.
- The creature definition describes what was constructed; `CreaturePlayState` describes where it is temporarily during simulation. Keeping those separate allows Play Mode movement without destroying the authored pose.
- A fixed timestep makes the simulation update consistently across render-frame timing, while the root’s target steering and the forward rest-length solver keep movement simple and stable.
- A traveling sine wave can be written as `sin(time × frequency − normalizedPosition × phase)`. Applying it along each segment’s local perpendicular makes the motion follow the creature’s current orientation instead of world X/Y.
- Weighting wave influence by normalized chain position keeps the head stable while giving the tail more expressive motion; using the wave as a constrained target preserves structural rest lengths.
- Save/load should persist the authored procedural definition—nodes, connections, spacing, ramp, and base radius—not temporary simulation results. Derived radii and Play Mode positions can then be rebuilt safely after loading.
- Separating simulation redraw notifications from inspector-state changes avoids rebuilding UI text on every fixed-step frame while keeping the canvas responsive.
- Constraining a child’s construction direction to `-135°..+135°` relative to its incoming segment prevents immediate 180-degree folding while preserving a broad 270-degree creative range.
- One set of ramp control points can describe different silhouettes depending on interpolation: linear segments, smoothstep transitions, or automatic-tangent Bezier segments.
- A visual skin can be derived from the same procedural node data: `center + local perpendicular × radius` gives one edge and the mirrored negative offset gives the other, so the size ramp automatically controls silhouette width.
- Mirrored features are best stored as one rule—eye size, spacing, and forward offset—and rendered as symmetric counterparts rather than separately authored world positions.

## 3D Bounce Simulator: explicit empty-scene state

- A viewer can represent “nothing loaded” as a real application state: keep lights and camera active, but add no geometry until the user imports a file.
- Reset behavior should depend on that state, so an empty scene returns to its own default camera instead of retaining a previously loaded model’s framing distance.

## Standalone WPF publishing

- A self-contained publish bundles the .NET runtime and native dependencies with the application, allowing a Windows user to launch the executable directly without installing the runtime or using a development tool.
- Generated publish folders are build artifacts and should be ignored by Git while the project source remains tracked.

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
# Insect Light Simulation milestone

- A `Vector2` represents direction and magnitude together, so attraction can use a normalized direction without separately managing X and Y math.
- Behaviors return steering forces; the simulation combines them before updating velocity and position. This keeps UI, rendering, and movement rules separate.
- A fixed-size update and bounded turn rate make motion stable and explainable: an insect is influenced by the light but cannot instantly rotate or exceed its speed limit.
- `WriteableBitmap` is a practical WPF pixel surface for many simple agents because the application updates one image instead of creating one control per insect.

## Multi-light force fields

- A single light can be generalized into a collection without changing the agent model: each light contributes a vector and the attraction behavior adds those vectors into one total force.
- `Total Attraction = Light A Attraction + Light B Attraction + ...` is useful because it is the same composable pattern future repulsion, gravity, vortex, and flow-field behaviors can use.
- Mouse input remains a UI concern until it is converted into simulation coordinates; after that conversion, dragging changes `LightSource.Position` just like any other simulation state.
- A UI abstraction can simplify control without collapsing the model: `Power` scales three separate light properties, so the interface stays compact while simulation attraction and rendering intensity remain conceptually independent.
- UI refresh frequency is a separate concern from simulation frequency. Keeping pixel rendering continuous while refreshing formatted statistics five times per second avoids needless string work without changing the simulation timestep.
- Static procedural sprite data belongs outside the per-agent render method. Reusing cached patterns reduces repeated setup work while preserving the same pixel output.
- Simulation animation and visual animation are separate concerns: vector behaviors generate an agent's position and heading, while the renderer layers a time-based wing frame and cached rotation on top of that state.
- A small sprite cache can combine a few animation frames with a few heading angles. This avoids generating or transforming an image for every insect every frame while retaining visible orientation and wing variation.
- Simulation state and camera state should remain separate. Zoom changes the world-to-screen transform and its inverse for input; it does not resize, respawn, cull, or otherwise modify the agent collection.
- A desired speed is a target, not an instant velocity assignment. Proximity and velocity alignment choose the target, while bounded acceleration lets the existing turn and movement system approach it smoothly.

## Procedural features and visual sampling

A procedural visual feature should store a local rule relative to its parent rather than a final world position. For example: `Head Position + Local Eye Offset = Eye World Position`. When the head moves or rotates, the eye follows automatically because the renderer resolves the local transform against the current parent transform.

Structural nodes can remain low-resolution while the displayed skin is smoothed with interpolated visual samples. The creature still has the same nodes, connections, spacing, and radii; Catmull-Rom samples only refine the rendered silhouette between those authored points.
## Terminal geometry and feature capability

Interpolating through side points is not sufficient for terminal geometry. A terminal node is a volume-bearing part of the creature, so the silhouette must explicitly wrap around its radius with a cap. Body interpolation can remain smooth, but the first and last circles need their own tangent-aware semicircular geometry.

Feature capabilities should belong to the feature type rather than being assumed universal. Eyes are mirrorable, while Forked Tongue is singular. A `SupportsMirroring` rule keeps the editor and renderer from inventing a second visual for a feature that should only exist once.

For terminal geometry, direction should be named explicitly. The head cap points outward opposite the first body segment; the tail cap points along the final segment away from its predecessor. Building both from `outward + perpendicular` avoids relying on arc sweep or left/right ordering, which can silently turn a rounded cap into an inward crescent.

## Mirrored procedural geometry and authored Bezier handles

Mirroring can remain a render/simulation variant of one authored feature: the pair shares dimensions and spring parameters, while temporary angular state is keyed by feature ID plus mirror side. Rounded appendages can stay dependency-free by sampling cubic curves and a tip arc into a closed outline. Explicit handle offsets make Bezier editing predictable, while missing handles can still be generated automatically for old files.

Interaction rules should match the data model: when a Fin is structurally attached to a parent, the canvas must select it without exposing generic translation. For curve editors, handle hit testing needs its own priority and state so a handle drag cannot accidentally move the control point; grouping the pointer sequence keeps Undo aligned with the user’s intent.

## Appearance and stability constraints

Authored appearance data belongs in the creature definition, while debug visibility belongs in display settings. That separation lets Save/Load preserve the chosen skin color without saving temporary Skeleton/Muscles visibility choices as geometry.

A bend limit is a local constraint: measure the signed angle between adjacent segment directions, clamp it, then restore each segment's rest length. It improves deformation stability without pretending to solve self-collision or changing the authored chain.

An angular limit should be solved as an error over time, not as an immediate positional snap. Small signed corrections in repeated distance/angular passes let a flexible chain bend dynamically while avoiding the neighboring-joint sign flips that a sequential hard clamp can create.

Debug visualizations should expose the same intermediate data that produces the final result. For this creature, Muscles is most useful when it draws the exact node centers and derived radii used to build Skin, while Skeleton separately exposes only the centerline constraints.

For a small editor, immutable snapshots are a practical Undo architecture: copy authored data before each mutation, keep a bounded history, and restore selection IDs alongside the definition. Drag operations need an explicit begin/end group so continuous pointer motion becomes one logical edit.

Secondary appendages should keep authored rest parameters separate from temporary Play state. A Fin stores side, dimensions, and spring settings in the definition; its current angle and angular velocity live only in `CreaturePlayState`, allowing inertia without polluting Save/Load or the body constraint solver.
