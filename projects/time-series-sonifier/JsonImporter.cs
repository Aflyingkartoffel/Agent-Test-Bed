using System.IO;
using System.Text.Json;

namespace TimeSeriesSonifier;

public static class JsonImporter
{
    public static RawImportedData Read(string path) => Parse(File.ReadAllText(path), Path.GetFileName(path));
    public static RawImportedData Parse(string json, string sourceName = "pasted data")
    {
        using var document = JsonDocument.Parse(json); var array = document.RootElement.ValueKind == JsonValueKind.Array ? document.RootElement : document.RootElement.EnumerateObject().FirstOrDefault(p => p.Value.ValueKind == JsonValueKind.Array).Value;
        if (array.ValueKind != JsonValueKind.Array || array.GetArrayLength() == 0) throw new InvalidDataException("JSON must contain a non-empty array of objects.");
        var objects = array.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.Object).ToArray(); if (objects.Length == 0) throw new InvalidDataException("JSON array must contain objects.");
        var headers = objects.SelectMany(x => x.EnumerateObject().Select(p => p.Name)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(); if (headers.Length < 2) throw new InvalidDataException("JSON objects must contain at least two properties.");
        var rows = objects.Select((item, i) => { var values = headers.Select(h => item.TryGetProperty(h, out var value) && value.ValueKind != JsonValueKind.Null ? ToRaw(value) : "").ToArray(); return new RawDataRow(i + 1, values); }).ToArray();
        return new RawImportedData { Headers = headers, Rows = rows, SourceName = sourceName };
    }
    static string ToRaw(JsonElement value) => value.ValueKind switch { JsonValueKind.String => value.GetString() ?? "", JsonValueKind.Number => value.GetRawText(), JsonValueKind.True => "true", JsonValueKind.False => "false", _ => value.GetRawText() };
}
