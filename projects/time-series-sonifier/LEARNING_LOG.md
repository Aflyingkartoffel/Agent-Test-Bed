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
