# Learning Log

## Milestone 1

- A raw imported table should remain intact; a processed `DataSeries` is a separate validated view.
- A neutral `CurrentDataState` prevents graph and future audio systems from calculating different values.
- A presentation duration lets datasets spanning decades play in seconds without changing their chronological relationships.
- WPF drawing geometry scales better than creating one control per data point.

## Milestone 3

- The UI publishes a target frequency; the audio callback owns sample-rate work.
- Logarithmic frequency mapping better matches perceived pitch across a wide range.
- Native audio resources need one owner and a stop order that prevents callbacks from touching released buffers.
- A small backend interface makes lifecycle behavior testable without requiring a real audio device.
## 2026-09-03 — Audio compatibility and icon layers

- A device-facing audio format should be broadly compatible even when synthesis is internally mono; duplicating the generated sample to stereo PCM16 keeps the synth simple while matching common Windows playback paths.
- Visualization layers should consume shared data state independently: a data-driven icon can stay centered while its scale changes, so graph coordinate transforms remain solely a graph concern.

## 2026-09-03 — Presentation rendering and export

- A reusable scene renderer keeps the on-screen preview and exported frames driven by the same layout and state, while output profiles change composition rather than stretching pixels.
- Offline frame timing and offline oscillator samples share presentation time, which avoids realtime playback speed and device timing affecting exported synchronization.

## 2026-09-03 — Continuous audio transport

- A native streaming callback must requeue the exact completed buffer; selecting one by wall-clock time can leave another buffer unfilled and cause audible starvation.
- Pause can preserve phase and device continuity by keeping the stream alive and rendering zero-gain samples, while Reset remains the explicit teardown operation.

## 2026-09-03 — Shared labels and pitch targets

- A target published by the UI is not enough by itself: the audio render path must consume it without rebuilding the smoother, otherwise the oscillator can remain at its initial frequency.
- Semantic metadata belongs in the shared presentation scene so on-screen previews and exported frames cannot drift from Setup labels.

## 2026-09-03 — Full data versus display data

- Large-data visualization can remain accurate by retaining the full series for state and interpolation while using a screen-space min/max envelope only for drawing.
- A cached forward display representation removes repeated source-point work during playback; axis ticks and FPS measurement should be shared logic rather than values tied to a single timer interval.

## 2026-09-03 — Static versus dynamic WPF visuals

- A cached geometry still costs work if the entire drawing surface is invalidated every tick. Separate visuals let the compositor retain static graph content while only the playhead layer changes.
- Hidden tabs should not be invalidated by the active tab’s clock; expensive preview rendering belongs behind an active-view boundary.

## 2026-09-03 — Theme resources and progressive reveal

- Semantic WPF brushes can be updated in place so ordinary controls follow a live theme without duplicating styles.
- A reveal effect can be a presentation concern: retain the complete sampled geometry and move only a clip boundary with timeline progress, preserving cache reuse and deterministic export.

## 2026-09-03 — Frozen resources and expanding viewports

- WPF `Freezable` resources may be frozen after being consumed by controls; live theme updates must replace frozen instances rather than assign to their properties.
- A growing visualization can preserve layout stability by clipping a fixed final composition and drawing its moving boundary dynamically.

## 2026-09-03 — Dynamic time domains

- A live chart effect is better modeled by changing coordinate transforms than by changing the physical frame: historical points naturally compress as the visible time maximum grows.
- Prefix extrema make progressive Y autoscaling cheap because the revealed range always starts at the first sample; the same domain calculation can drive interactive and offline rendering.
