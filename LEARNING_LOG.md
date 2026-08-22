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

## 2026-08-21 — Repository structure and documentation

- **What it is:** A repository is a shared project folder whose history can be tracked with Git. Documentation files explain the project's purpose, current state, and working conventions.
- **Where we encountered it:** We created a top-level structure that separates applications, experiments, and broader documentation.
- **Why it matters:** Clear structure and current documentation help both people and AI assistants understand what exists and what should happen next.

## 2026-08-21 — Building the calculator

- **Functions:** A function packages a focused action, such as entering a number or clearing the calculator, so the same behavior can be called from both buttons and keyboard input.
- **Events and event handlers:** A browser event represents an interaction, such as a click or key press. An event handler listens for it and runs the matching calculator function.
- **Application state:** Variables such as the current expression and current input remember what the user has entered so the display and next operation stay synchronized.
- **Operator precedence:** The calculator uses separate value and operator stacks so multiplication and division are applied before addition and subtraction.
