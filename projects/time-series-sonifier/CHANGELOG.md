# Changelog

## Milestone 1

- Added standalone Time-Series Sonifier WPF application.
- Added CSV import with selectable time/value columns and nonfatal validation.
- Added chronological series construction, linear interpolation, and current-data state.
- Added responsive graph geometry, playhead, marker, timeline controls, speed, loop, and readouts.
- Added non-UI automated tests for import, series, interpolation, and timeline behavior.

## Milestone 3

- Added safe real-time mono sonification from normalized mapped values.
- Added logarithmic pitch mapping, pitch glide, volume, and four oscillator waveforms.
- Added explicit audio lifecycle and callback-safe native WaveOut backend.
- Added device failure handling and lifecycle regression coverage.
## 2026-09-03

- Improved default Windows audio compatibility with PCM16 stereo, deterministic sample-rate fallback, and `waveOut` diagnostics.
- Changed icon visualization to a centered transparent overlay independent of graph/playhead coordinates.
- Added white presentation UI, horizontal preview, vertical final-output preview, reusable scene rendering, gray cube placeholder, offline audio, and FFmpeg MP4 export integration.

- Fixed continuous audio transport by requeueing the completed native buffer, reusing the render scratch buffer, and adding silent pause/resume behavior without reopening the device.
- Restyled the live FFT panel for the light application theme.
