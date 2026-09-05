using System.Globalization;
using System.IO;
using System.Text;

namespace TimeSeriesSonifier;

public static class CsvImporter
{
    public static RawImportedData Read(string path) => Parse(File.ReadAllText(path), Path.GetFileName(path));

    public static RawImportedData Parse(string csv, string sourceName = "pasted data")
    {
        var rows = new List<RawDataRow>();
        var physicalRow = 0;
        foreach (var line in csv.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None))
        {
            physicalRow++;
            if (string.IsNullOrWhiteSpace(line)) continue;
            rows.Add(new RawDataRow(physicalRow, ParseLine(line)));
        }
        if (rows.Count == 0) throw new InvalidDataException("The CSV does not contain a header row.");
        var headers = rows[0].Values.Select(x => x.Trim()).ToArray();
        if (headers.Length < 2 || headers.Any(string.IsNullOrWhiteSpace)) throw new InvalidDataException("The CSV header must contain at least two named columns.");
        return new RawImportedData { Headers = headers, Rows = rows.Skip(1).ToArray(), SourceName = sourceName };
    }

    static IReadOnlyList<string> ParseLine(string line)
    {
        var values = new List<string>(); var field = new StringBuilder(); var quoted = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"') { if (quoted && i + 1 < line.Length && line[i + 1] == '"') { field.Append('"'); i++; } else quoted = !quoted; }
            else if (c == ',' && !quoted) { values.Add(field.ToString().Trim()); field.Clear(); }
            else field.Append(c);
        }
        values.Add(field.ToString().Trim());
        return values;
    }
}

public static class DataSeriesBuilder
{
    public static SeriesBuildResult Build(RawImportedData raw, int timeColumn, int valueColumn)
    {
        if (timeColumn < 0 || valueColumn < 0 || timeColumn >= raw.Headers.Count || valueColumn >= raw.Headers.Count) return new(null, 0, raw.Rows.Count, "Select valid time and value columns.");
        var points = new List<DataPoint>(); var skipped = 0;
        foreach (var row in raw.Rows)
        {
            if (row.Values.Count <= Math.Max(timeColumn, valueColumn) || !TimeValueParser.TryParse(row.Values[timeColumn], out var time) || !double.TryParse(row.Values[valueColumn], NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || !double.IsFinite(value)) { skipped++; continue; }
            points.Add(new DataPoint(time, value, row.OriginalRowIndex, row.Values[timeColumn]));
        }
        points.Sort((a, b) => { var compare = a.Time.CompareTo(b.Time); return compare != 0 ? compare : a.OriginalRowIndex.CompareTo(b.OriginalRowIndex); });
        var unique = new List<DataPoint>();
        foreach (var point in points) { if (unique.Count > 0 && point.Time == unique[^1].Time) { skipped++; continue; } unique.Add(point); }
        if (unique.Count < 2) return new(null, unique.Count, skipped, "Need at least two valid data points.");
        return new(new DataSeries { Name = raw.SourceName, Points = unique }, unique.Count, skipped, null);
    }
}
