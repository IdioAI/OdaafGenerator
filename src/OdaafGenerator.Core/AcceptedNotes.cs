using System.Text.Json;

namespace OdaafGenerator.Core;

public static class AcceptedNotes
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public sealed class FileNote
    {
        public string Title { get; set; } = "";
        public string Purpose { get; set; } = "";
        public Dictionary<string, string> Parameters { get; set; } = new();
    }

    public static Dictionary<string, FlowNotes.Note> Read(string path)
    {
        var result = new Dictionary<string, FlowNotes.Note>(StringComparer.Ordinal);
        if (!File.Exists(path))
            return result;
        var stored = JsonSerializer.Deserialize<Dictionary<string, FileNote>>(File.ReadAllText(path));
        if (stored is null)
            return result;
        foreach (var (key, note) in stored)
        {
            result[key] = new FlowNotes.Note(
                note.Purpose ?? "",
                note.Parameters ?? new Dictionary<string, string>(),
                "",
                note.Title ?? "");
        }

        return result;
    }

    public static void Write(string path, IReadOnlyDictionary<string, FlowNotes.Note> notes)
    {
        var stored = notes.ToDictionary(
            pair => pair.Key,
            pair => new FileNote
            {
                Title = pair.Value.Title,
                Purpose = pair.Value.Purpose,
                Parameters = pair.Value.Parameters.ToDictionary(item => item.Key, item => item.Value)
            },
            StringComparer.Ordinal);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        File.WriteAllText(path, JsonSerializer.Serialize(stored, Json));
    }
}
