# Project Status

## Milestone 1 — Time-Series Visualization Foundation

Complete in this branch: CSV import, column selection, safe validation, sorted processed series, numeric/year/date time parsing, linear interpolation, graph rendering, playhead and marker, current time/value readout, scrubbing, play/pause/reset, speed control, and looping.

Automated coverage includes CSV, raw-data preservation, time parsing, series bounds/policies, interpolation, current state, and timeline behavior.

Not implemented yet: audio, pitch mapping, FFT, icon or color mapping, multiple datasets, persistence, live data, annotations, exports, and templates.

## Milestone 3 — Safe Real-Time Audio Sonification

Implemented on the audio branch: logarithmic pitch mapping, bounded oscillator waveforms, 50 ms pitch glide, volume control, explicit audio enable/start/stop controls, native 48 kHz mono streaming, safe lifecycle states, callback-safe shutdown, device-failure handling, and fake-backend lifecycle tests.

Still deferred: FFT/spectrum, icons, multiple voices, stereo, effects, MIDI, export, and persistence.
## Audio device and icon overlay update — 2026-09-03

- Uses the Windows default `WAVE_MAPPER` with stereo PCM16 and 48 kHz/44.1 kHz fallback; the oscillator follows the actual opened sample rate and failures retain translated diagnostics.
- Icons are centered transparent overlays with independent normalized scaling and no graph-coordinate attachment.

## Presentation and video export — 2026-09-03

- Added a white/blue/green theme, Setup/Pre-Visualization/Final Output workflow, reusable presentation rendering, vertical/square/horizontal output profiles, and a programmatic gray cube placeholder.
- Added deterministic frame timing, offline WAV generation, FFmpeg MP4 command integration, export progress/status, and safe unavailable-FFmpeg handling.

## Audio continuity and FFT theme — 2026-09-03

- Corrected native buffer requeueing to use the completed `WAVEHDR`, added three reusable 512-frame buffers, and removed render-buffer allocation from the callback path.
- Added explicit audio pause/resume semantics that keep the backend open and render silence while paused.
- Updated the FFT renderer to a light plot with subtle gray grid lines, readable labels, and a green spectrum accent.

## Dynamic labels and live pitch tracking — 2026-09-03

- Selected time/value column names now update current-data labels and flow through shared presentation-scene metadata into preview and export rendering.
- Fixed live pitch tracking: the audio render path now consumes `TargetFrequency` through the persistent smoother, so interpolated normalized data changes oscillator pitch without restarting it.
- Added progression and live-target regression coverage; the focused runner now passes 167 checks.

## Large dataset rendering and graph axes — 2026-09-03

- Kept full mapped data authoritative while adding viewport-scaled first/min/max/last bucket sampling and cached display points for dense graph rendering.
- Replaced linear interpolation neighbor scans with binary search and added shared nice-number X/Y ticks, mapped-value ranges, selected axis titles, and readable formatting.
- Added a real WPF composition-based smoothed FPS counter and synthetic 100k-point regression coverage. The focused runner now passes 174 checks.

## High-performance visualization pipeline — 2026-09-03

- Split the Setup graph into cached static and dynamic WPF visuals. Frozen graph geometry and axis drawing are rebuilt only when data, viewport, or labels change; playback updates only the playhead/marker visual.
- Limited presentation redraws to the active workflow tab, throttled FFT/readout work, and retained a separate output-resolution cache for export.
- GUI playback profiling on 1k–250k datasets was not available in this environment; synthetic structural benchmarks remain automated coverage.
