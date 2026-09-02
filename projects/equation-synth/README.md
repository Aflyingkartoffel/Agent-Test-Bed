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

Known limitations: speaker/device behavior requires manual verification; the mixer remains intentionally basic without effects, recording, or export.

## Milestone 6 — multi-oscillator mixer

Equation entries now separate graph visibility from audio participation. Each layer stores Audio Enabled, Mute, Solo, frequency, volume, pan, and editable Attack/Decay/Sustain/Release settings. The mixer uses one ID-keyed runtime voice per layer, independent oscillator phase, equal-power stereo pan, solo/mute resolution, voice-count attenuation, master gain, and a bounded soft limiter. PLAY SOUND gates enabled voices; STOP SOUND gates them off through the ADSR release stage. Mute changes gain without retriggering a voice.

Layer settings are part of project Save/Load and the existing authored Undo/Redo snapshots. Older V1 projects migrate conservatively by enabling only the selected/first layer. Runtime phase, envelope stage, audio buffers, and meter state are not persisted. MIDI, sequencing, effects, recording/export, and VST integration remain outside Milestone 6.
## Milestone 5B — V1 integration completion

The editor now routes authored changes through a bounded 150-operation undo/redo history. Slider drags commit once on release, no-op edits are ignored, and undo/redo compares authored snapshots so returning to the saved snapshot clears the dirty marker. Runtime time progression, effective automation values, audio buffers, and playback state never enter history.

New, Open, and window close share a Save / Don't Save / Cancel workflow. Save uses the existing path when available; Save As is used when needed, and failed or cancelled saves preserve the active project. Loads deserialize and validate a temporary state before replacing the current document.

Expanded parameter rows expose manual value, minimum, maximum, step, reset, and OFF/SINE/COSINE/EXPRESSION automation controls. Manual and Effective values remain separate, and invalid metadata or automation is reported without crashing. Equation ordering, visibility, colors, duplication, graph view, timeline, loop, audio controls, and parameter edits participate in authored history.

Validation: Debug and Release builds plus the console regression suite pass. WPF startup was build-validated but not launched headlessly because this environment has no safe desktop GUI automation; audible playback remains not manually verified.

## Milestone 7 — procedural expression outputs

The procedural workspace adds UI-independent `ExpressionOutputBinding`, `ProceduralObject`, `ProceduralScene`, and `ProceduralOutputEvaluator` types. Bindings target Position X/Y, Rotation, Scale, Opacity, or RGB channels and apply sequentially with Replace, Add, or Multiply semantics. Position values use centered scene units (pixels), rotation uses degrees, scale is a multiplier, and opacity/color channels use 0–1.

Base/authored transforms remain separate from evaluated runtime values. Output expressions share the existing parameters, EffectiveValue automation, and global `TimeEngine`; invalid or non-finite output falls back to the previous value and is safely clamped. The preview supports multiple objects, ordered bindings, object duplication/deletion, live output inspection, and bounded motion trails. Procedural authored state is persisted and participates in existing snapshot history; parsed ASTs, evaluated transforms, and trail samples are runtime-only.

The roadmap indicator is complete at 7 / 7 (100%).
