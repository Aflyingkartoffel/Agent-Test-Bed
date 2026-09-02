# Equation Synth

Equation Synth is a .NET 8 WPF desktop application that parses one restricted mathematical expression and reuses its AST for a live graph and normalized audio wavetable.

## Architecture

`Core.cs` contains the UI-independent expression AST/parser, safe evaluator, parameter model, time engine, waveform generator, audio adapter, and JSON preset model. `MainWindow.xaml.cs` only coordinates those systems and renders sampled geometry on WPF canvases.

Supported operators are `+`, `-`, `*`, `/`, `^`, unary `-`, and parentheses. Built-ins are `x`, `t`, `pi`, `e`, plus `sin`, `cos`, `tan`, `abs`, `sqrt`, `pow`, `log`, `exp`, `floor`, `ceil`, `min`, and `max`. Any other identifier becomes a parameter with a default range of -10 to 10.

The graph samples x from -10 to 10 and breaks lines at invalid/non-finite values. Pan with left-drag and zoom around the cursor with the mouse wheel. The audio view samples one cycle with x from 0 to 2π using 2048 samples. Invalid values become zero; the table is normalized, softly limited, and played through a conservative volume setting. `sin(x)` is an oscillator whose pitch is controlled independently by Frequency.

Milestone 2 supports multiple `EquationEntry` records. Each has an ID, parsed runtime expression, color, visibility, and selection state. Shared identifiers across entries reconcile into one project parameter while preserving existing values. The selected entry alone feeds the audio waveform and SoundPlayer; this is intentionally not a multi-oscillator mixer. The graph uses a centralized `GraphCamera`, cursor-anchored zoom, pan, editable validated ranges, adaptive 1/2/5 grid spacing, numeric labels, viewport-aware sampling, and screen-space discontinuity breaks. Save files store authored entries and rebuild their ASTs on load; old Milestone 1 files with only `Equation` remain supported.

Time Play/Pause/Reset controls `t`; it is separate from Play Sound/Stop Sound. Presets are available in the selector. Save and Load use human-readable `.equation.json` files containing equation, parameter metadata/values, audio settings, time speed, and graph domain.

Milestone 3 adds a reusable `TimeEngine` with a 0–10 timeline, scrubbing, play/pause, reverse playback, looping, and 1/60-second step buttons. `AutomationEngine` supports OFF, SINE, COSINE, and AST-backed EXPRESSION modes. Sine/cosine frequency is in cycles per second (`sin(2πft + phase)`). Automation resolves parameter dependencies safely and reports cycles. Parameters retain `ManualValue` separately from clamped `EffectiveValue`. Optional selected-equation trails use bounded, interval-spaced history. Animation settings are part of the JSON schema; older files default to automation off, looping off, and the standard timeline. SoundPlayer remains the simple Milestone 1 backend and is not rebuilt into a streaming synth here.

Milestone 4 replaces SoundPlayer as the active synth path with `AudioEngine` using Windows `waveOut`, mono 32-bit float PCM at 48 kHz, three 512-sample buffers, and a persistent oscillator phase. The callback only performs interpolated wavetable reads, phase advancement, smoothing, limiting, and buffer writes. Completed 2048-sample tables are published from the UI side and crossfaded over approximately 20 ms, so time, automation, frequency, selection, and parameter changes do not reset phase. Frequency and gain are smoothed; non-finite samples become zero, DC is removed/normalized by the waveform stage, and output is softly limited. The selected equation remains the sole audio source; multiple-oscillator mixing and a larger streaming DSP architecture are intentionally deferred. SoundPlayer is retained only as the legacy compatibility class.

## Run and test

From the repository root:

```powershell
dotnet run --project projects/equation-synth/equation-synth.csproj
dotnet run --project projects/equation-synth-tests/equation-synth-tests.csproj
```

The normal build output is `projects/equation-synth/bin/Debug/net8.0-windows/Equation Synth.exe`. It can be launched directly from Explorer after building. No audio starts automatically. Audible output requires pressing PLAY SOUND and was not verified by automated tests.

## V1 workflow

Use NEW, SAVE, SAVE AS, and LOAD from the project toolbar. The status area shows equation/parameter counts, time, audio state, and unsaved state. Keyboard shortcuts include Ctrl+N, Ctrl+S, Ctrl+Shift+S, Ctrl+O, Ctrl+D, Delete, Space, Escape, F, and R when focus is not in a text field. The function reference is the supported syntax section above; presets include basic, motion, nested, and automation-oriented examples.

The project model validates equation IDs, selected entries, parameter metadata, time ranges, audio settings, and parsed expressions before accepting files. Save files use readable `.equation.json` JSON and missing optional fields retain safe defaults. Release output is `projects/equation-synth/bin/Release/net8.0-windows/Equation Synth.exe`. A framework-dependent publish can be produced with `dotnet publish projects/equation-synth/equation-synth.csproj -c Release`.

Known limitations: routine slider/history integration is intentionally lightweight, speaker/device behavior requires manual verification, and the current streaming audio engine remains a single selected-equation source without mixing, effects, or recording.
