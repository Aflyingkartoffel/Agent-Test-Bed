# Project Status

## Milestone 1 — Time-Series Visualization Foundation

Complete in this branch: CSV import, column selection, safe validation, sorted processed series, numeric/year/date time parsing, linear interpolation, graph rendering, playhead and marker, current time/value readout, scrubbing, play/pause/reset, speed control, and looping.

Automated coverage includes CSV, raw-data preservation, time parsing, series bounds/policies, interpolation, current state, and timeline behavior.

Not implemented yet: audio, pitch mapping, FFT, icon or color mapping, multiple datasets, persistence, live data, annotations, exports, and templates.

## Milestone 3 — Safe Real-Time Audio Sonification

Implemented on the audio branch: logarithmic pitch mapping, bounded oscillator waveforms, 50 ms pitch glide, volume control, explicit audio enable/start/stop controls, native 48 kHz mono streaming, safe lifecycle states, callback-safe shutdown, device-failure handling, and fake-backend lifecycle tests.

Still deferred: FFT/spectrum, icons, multiple voices, stereo, effects, MIDI, export, and persistence.
