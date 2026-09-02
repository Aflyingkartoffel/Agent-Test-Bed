# Time-Series Sonifier

Time-Series Sonifier is a standalone .NET 8/WPF desktop tool for presenting time-series data visually. Milestone 1 imports one CSV, lets you choose time and value columns, builds a validated series, and plays a ten-second presentation timeline with a moving graph playhead and interpolated current-value marker.

## Run

```powershell
dotnet run --project projects/time-series-sonifier/time-series-sonifier.csproj
```

CSV files need a header row and at least two columns. Time values may be numeric, integer years, or parseable dates. Values must be finite numbers. Blank, malformed, duplicate-time, and invalid rows are skipped with a status summary; at least two valid points are required.

## Architecture

`CsvImporter` keeps `RawImportedData` and original rows separate from `DataSeriesBuilder`, which produces sorted `DataPoint` values. `TimelineEngine` owns play/pause/reset/seek, speed, loop, and a ten-second presentation duration. `SeriesInterpolator` produces the authoritative `CurrentDataState` used by `GraphRenderer` and the readout. `GraphSurface` renders one geometry rather than creating a WPF element per point.

Milestone 1 intentionally has no audio, icon mapping, FFT, persistence, live external data, or expression mapping. Those are planned later systems that will consume `CurrentDataState`.
