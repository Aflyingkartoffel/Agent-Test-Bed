# Equation Synth

Equation Synth is a .NET 8 WPF desktop application that parses one restricted mathematical expression and reuses its AST for a live graph and normalized audio wavetable.

## Architecture

`Core.cs` contains the UI-independent expression AST/parser, safe evaluator, parameter model, time engine, waveform generator, audio adapter, and JSON preset model. `MainWindow.xaml.cs` only coordinates those systems and renders sampled geometry on WPF canvases.

Supported operators are `+`, `-`, `*`, `/`, `^`, unary `-`, and parentheses. Built-ins are `x`, `t`, `pi`, `e`, plus `sin`, `cos`, `tan`, `abs`, `sqrt`, `pow`, `log`, `exp`, `floor`, `ceil`, `min`, and `max`. Any other identifier becomes a parameter with a default range of -10 to 10.

The graph samples x from -10 to 10 and breaks lines at invalid/non-finite values. Pan with left-drag and zoom around the cursor with the mouse wheel. The audio view samples one cycle with x from 0 to 2π using 2048 samples. Invalid values become zero; the table is normalized, softly limited, and played through a conservative volume setting. `sin(x)` is an oscillator whose pitch is controlled independently by Frequency.

Milestone 2 supports multiple `EquationEntry` records. Each has an ID, parsed runtime expression, color, visibility, and selection state. Shared identifiers across entries reconcile into one project parameter while preserving existing values. The selected entry alone feeds the audio waveform and SoundPlayer; this is intentionally not a multi-oscillator mixer. The graph uses a centralized `GraphCamera`, cursor-anchored zoom, pan, editable validated ranges, adaptive 1/2/5 grid spacing, numeric labels, viewport-aware sampling, and screen-space discontinuity breaks. Save files store authored entries and rebuild their ASTs on load; old Milestone 1 files with only `Equation` remain supported.

Time Play/Pause/Reset controls `t`; it is separate from Play Sound/Stop Sound. Presets are available in the selector. Save and Load use human-readable `.equation.json` files containing equation, parameter metadata/values, audio settings, time speed, and graph domain.

Milestone 3 adds a reusable `TimeEngine` with a 0–10 timeline, scrubbing, play/pause, reverse playback, looping, and 1/60-second step buttons. `AutomationEngine` supports OFF, SINE, COSINE, and AST-backed EXPRESSION modes. Sine/cosine frequency is in cycles per second (`sin(2πft + phase)`). Automation resolves parameter dependencies safely and reports cycles. Parameters retain `ManualValue` separately from clamped `EffectiveValue`. Optional selected-equation trails use bounded, interval-spaced history. Animation settings are part of the JSON schema; older files default to automation off, looping off, and the standard timeline. SoundPlayer remains the simple Milestone 1 backend and is not rebuilt into a streaming synth here.

## Run and test

From the repository root:

```powershell
dotnet run --project projects/equation-synth/equation-synth.csproj
dotnet run --project projects/equation-synth-tests/equation-synth-tests.csproj
```

The normal build output is `projects/equation-synth/bin/Debug/net8.0-windows/Equation Synth.exe`. It can be launched directly from Explorer after building. No audio starts automatically. Audible output requires pressing PLAY SOUND and was not verified by automated tests.
