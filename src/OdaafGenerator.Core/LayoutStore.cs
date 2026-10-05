using System.Text.Json;

namespace OdaafGenerator.Core;

public sealed class LayoutNode
{
    public string SyntaxPath { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }
    public string Formal { get; set; } = "";
    public string Final { get; set; } = "";
}

public sealed class LayoutFile
{
    public List<LayoutNode> Nodes { get; set; } = [];

    public LayoutNode? Find(string syntaxPath) =>
        Nodes.FirstOrDefault(node => node.SyntaxPath == syntaxPath);

    public void Upsert(FunctionCard card)
    {
        var node = Find(card.SyntaxPath);
        if (node is null)
        {
            node = new LayoutNode { SyntaxPath = card.SyntaxPath };
            Nodes.Add(node);
        }

        node.X = card.X;
        node.Y = card.Y;
        node.Formal = card.Formal;
        node.Final = card.Final;
    }
}

public static class LayoutStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static LayoutFile Load(string path)
    {
        if (!File.Exists(path))
            return new LayoutFile();
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<LayoutFile>(json, Options) ?? new LayoutFile();
    }

    public static void Save(string path, LayoutFile layout)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        File.WriteAllText(path, JsonSerializer.Serialize(layout, Options));
    }
}
