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
- Added dynamic selected-column labels to current data, preview, final output, and exported presentation frames.
- Fixed the live pitch handoff so target frequencies update the persistent smoother and follow interpolated normalized data.
- Added viewport-aware min/max envelope sampling and cached display points for large graph datasets without changing authoritative data.
- Added binary-search interpolation, shared numeric/time axes, selected axis titles, and a composition-timing FPS counter.
- Added frozen static graph/axis visuals, dynamic-only playhead updates, active-tab presentation invalidation, FFT/readout throttling, and separate export caching.
- Added centralized Light/Dark semantic theming across the application and presentation renderer.
- Added Progressive/Full Graph selection with reusable timeline-driven reveal clipping over the cached graph geometry; export uses the same deterministic reveal state.
- Fixed Dark Mode switching by replacing frozen theme brushes instead of mutating them.
- Changed Progressive reveal from data-line-only clipping to a 4%-to-100% expanding visualization frame with a moving right edge and fixed WPF layout.
- Replaced the expanding-frame effect with Dynamic Timeline: a fixed full-size graph whose X domain grows from the first 3% of time to the complete dataset.
- Added prefix-based visible Y scaling, future-point filtering, interpolated current endpoints, and full-frame remapping for dynamic presentation rendering.
- Removed the redundant vertical playhead from Dynamic Timeline while retaining the endpoint marker and Full Graph playhead.
