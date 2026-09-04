# Time-Series Sonifier

Current workflow is Setup followed by Final Output. Audio and FFT analysis default on but remain idle until playback starts; the app does not auto-load or auto-play. Final Output uses responsive vertical, square, or horizontal layouts with a large graph, compact readouts/spectrum, friendly export controls, and dataset-derived presentation titles shared with offline export.

Time-Series Sonifier is a standalone .NET 8/WPF desktop tool for presenting time-series data visually. Milestone 1 imports one CSV, lets you choose time and value columns, builds a validated series, and plays a ten-second presentation timeline with a moving graph playhead and interpolated current-value marker.

## Run

```powershell
dotnet run --project projects/time-series-sonifier/time-series-sonifier.csproj
```

CSV files need a header row and at least two columns. Time values may be numeric, integer years, or parseable dates. Values must be finite numbers. Blank, malformed, duplicate-time, and invalid rows are skipped with a status summary; at least two valid points are required.

## Architecture

`CsvImporter` keeps `RawImportedData` and original rows separate from `DataSeriesBuilder`, which produces sorted `DataPoint` values. `TimelineEngine` owns play/pause/reset/seek, speed, loop, and a ten-second presentation duration. `SeriesInterpolator` produces the authoritative `CurrentDataState` used by `GraphRenderer` and the readout. `GraphSurface` renders one geometry rather than creating a WPF element per point.

Milestone 5 adds an optional live FFT spectrum downstream of synthesis. `AudioEngine` writes its post-volume, finite output samples to a bounded single-producer ring buffer; `SpectrumAnalyzer` consumes the latest 2048 samples on the UI update path, applies a precomputed Hann window, runs an internal radix-2 FFT, and exposes only bins from 0 Hz through Nyquist in `SpectrumFrame`. Magnitudes are normalized and clamped to -100..0 dB. A lightweight smoothing step stabilizes the display, and `SpectrumRenderer` draws a logarithmic-frequency, dB-guided panel without creating controls per bin.

The Windows `waveOut` backend targets `WAVE_MAPPER` (the current default playback device), using stereo 16-bit PCM at 48 kHz with a deterministic 44.1 kHz fallback. The oscillator uses the rate selected by the backend, and diagnostic failures include the MMRESULT and translated Windows message. The generated mono sample is duplicated to left and right; no panning is introduced here.

The native stream keeps three reusable 512-frame buffers (about 10.7 ms each at 48 kHz). `WOM_DONE` callbacks identify and refill the completed buffer, while a reusable render array avoids per-callback allocation. Oscillator phase remains continuous and target-frequency changes are smoothed sample by sample. Pause keeps the stream open but renders silence; Resume reuses it, while Reset and non-looping completion stop it. The live FFT remains outside the audio callback and uses a white/light-gray plot with readable labels and a green spectrum accent.

Icons are independent transparent overlays centered in the main graph viewport. Their alpha channel and aspect ratio are preserved, while `CurrentDataState.CurrentNormalizedValue` drives a smoothed scale between the validated minimum and maximum. The overlay is hit-test transparent and does not use graph/playhead coordinates.

The presentation workflow adds Setup, Pre-Visualization, and Final Output views. The preview uses a reusable `PresentationScene`/`PresentationRenderer` in a horizontal 16:9 layout; Final Output defaults to vertical 1080×1920 and also supports square and horizontal profiles. A programmatic gray cube is used when no custom image is loaded. Export renders deterministic frames at 30 or 60 FPS and offline WAV audio into a temporary directory, then invokes FFmpeg for H.264/AAC MP4 with `yuv420p`. FFmpeg is detected from PATH and is never downloaded automatically.

FFT is disabled by default and never runs in the audio callback. Enabling/disabling or changing among 1024/2048/4096 points does not recreate the audio device. Stopping audio clears the sample buffer and the spectrum; restarting safely resumes analysis. This is an analysis-only layer: the existing data normalization and pitch mapping remain authoritative, while volume changes appear in the post-volume spectrum. No persistence, spectrogram, effects, or multiple voices are included in this milestone.

Selected time and value column names are carried as `PresentationScene` metadata, so Setup readouts, Pre-Visualization, Final Output, and exported frames use the same labels. Labels are formatted for display without changing source-column semantics. Live pitch follows interpolated `CurrentDataState.CurrentNormalizedValue`: the audio render path consumes the thread-safe target through the persistent 50 ms smoother, preserving phase while allowing the expected 110/220/440/880/1760 Hz progression. A simple 0–100 increasing dataset is useful for checking target/current frequency readouts.

Graph rendering keeps the full mapped series for interpolation, audio, readouts, and export. The screen graph uses a cached display representation sized from viewport width; dense buckets retain first, last, local minimum, and local maximum points so spikes remain visible without drawing every source point. The interpolator uses binary search for neighboring points. Axes use shared nice 1/2/5 tick spacing, mapped-value Y ranges, readable numeric formatting, time-aware X labels, and selected column titles. The header FPS counter measures WPF composition frames and applies a rolling half-second smoothing window.

The interactive graph separates static and dynamic visuals: frozen `StreamGeometry` and axis `DrawingGroup` content are rebuilt only after dataset, viewport, or axis changes, while playback redraws only the playhead and current marker. Presentation updates are limited to the active workflow tab; FFT updates are capped at a visualization-friendly rate, and human-readable readouts are throttled while authoritative state continues at timeline/audio cadence. Export keeps its own output-resolution cache and does not use interactive quality limits.

The presentation polish adds a centralized Light/Dark semantic palette. Switching appearance updates application panels, graph axes, spectrum, preview, final output, and exported frames without changing image pixels. The graph defaults to Progressive reveal: its complete cached geometry remains intact while a reusable X-axis clip exposes 1% at the start through 100% at the end. Full Graph is available when a static complete line is preferred; both preview and export use the same normalized timeline state.

Dynamic Timeline replaces frame growth with a fixed-size graph and expanding time domain. The visible end advances from the first 3% of the time span to the full series, so early history fills the complete plot and compresses as later data arrives. The visible Y range is calculated from the revealed prefix with padding and a zero baseline for positive absolute data; axes use the existing nice tick generator. The interpolated current state is appended as the leading endpoint, and future points remain excluded. Full Graph retains the complete domain immediately.

Dynamic Timeline uses the endpoint marker as the current-time indicator and does not draw a separate vertical playhead through the graph. Full Graph continues to show the moving playhead because its complete time domain remains visible. The bottom scrub slider remains collapsed for Dynamic Timeline and available for Full Graph.

Fresh startup defaults to the Light theme and Progressive/Dynamic Timeline mode. Progressive mode shows only the current endpoint marker; Full Graph retains the conventional moving playhead.

Image updates use a normalized opacity value in the 0–1 range and a persistent centered `ScaleTransform`. The live Setup image consumes the same interpolated `CurrentDataState.CurrentNormalizedValue` used by sonification; Preview, Final Output, and export calculate scale from their frame state rather than relying on the live WPF control.

Theme changes replace frozen resource brushes safely instead of mutating them. Progressive mode now uses an expanding visualization viewport: the visible frame starts at 4% of the final plot width and follows `0.04 + 0.96 * timelineProgress`. The fixed WPF layout remains unchanged; one clip reveals the graph frame, axes/grid, graph, and centered image while the dynamic border’s right edge follows the same boundary. Full Graph remains unclipped.

## Milestone 3 audio

Milestone 3 adds one safe mono oscillator driven by `CurrentDataState.CurrentNormalizedValue`. `PitchMapper` uses `minFrequency * pow(maxFrequency / minFrequency, normalized)` with defaults of 110–1760 Hz, so the midpoint is approximately 440 Hz. `ParameterSmoother` glides frequency over about 50 ms. Sine, triangle, square, and saw waveforms share a continuous oscillator phase at 48 kHz.

Audio is off at startup and starts only when requested. `AudioEngine` owns lifecycle state and accepts an `IAudioBackend`, while the native `WaveOutBackend` owns the device, buffers, callback, and pinned memory. Stop is idempotent: callbacks stop requeueing before buffers are reset and released. Device failures become a readable status and do not close the app. Persistence, effects, and multiple voices remain deferred.
