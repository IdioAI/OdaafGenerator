using System.Xml.Linq;

namespace OdaafGenerator.Core;

public sealed record GeneratorContext(string Id, string Name);

public sealed class GeneratorProject
{
    public const string FileName = "rapidcad-zextract.ogproj";
    private static readonly XNamespace Ns = "urn:odaaf:generator";

    public required string FilePath { get; init; }
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string DatabasePath { get; init; }
    public required string SourceRoot { get; init; }
    public required string SolutionFile { get; init; }
    public required string OdaafPath { get; init; }
    public required string OdaafUri { get; init; }
    public required string GraphVersion { get; init; }
    public required string DefaultContextId { get; init; }
    public required IReadOnlyList<GeneratorContext> Contexts { get; init; }

    public string SolutionPath => Path.Combine(SourceRoot, SolutionFile);

    public static string? FindDefaultFile()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, FileName);
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        return null;
    }

    public static GeneratorProject Load(string path)
    {
        var full = Path.GetFullPath(path);
        if (!File.Exists(full))
            throw new FileNotFoundException("找不到工程索引。", full);

        var document = XDocument.Load(full);
        var root = document.Root ?? throw new InvalidDataException("工程索引没有根元素。");
        var directory = Path.GetDirectoryName(full) ?? ".";
        var database = Required(root, "Database", "href");
        var source = Required(root, "Source", "href");
        var solution = Required(root, "Source", "solution");
        var odaaf = Required(root, "Odaaf", "href");

        return new GeneratorProject
        {
            FilePath = full,
            Id = Attr(root, "id") ?? "",
            Name = Attr(root, "name") ?? "",
            DatabasePath = Resolve(directory, database),
            SourceRoot = Resolve(directory, source),
            SolutionFile = solution,
            OdaafPath = Resolve(directory, odaaf),
            OdaafUri = Attr(root.Element(Ns + "Odaaf"), "uri") ?? "",
            GraphVersion = Attr(root.Element(Ns + "Odaaf"), "graphVersion") ?? "",
            DefaultContextId = Attr(root.Element(Ns + "Contexts"), "default") ?? OdaafFileContext.Id,
            Contexts = ReadContexts(root)
        };
    }

    private static IReadOnlyList<GeneratorContext> ReadContexts(XElement root)
    {
        var parent = root.Element(Ns + "Contexts");
        if (parent is null)
            return [];
        return parent.Elements(Ns + "Context")
            .Select(element => new GeneratorContext(Attr(element, "id") ?? "", Attr(element, "name") ?? ""))
            .Where(context => context.Id.Length > 0)
            .ToList();
    }

    private static string Required(XElement root, string element, string attribute)
    {
        var value = Attr(root.Element(Ns + element), attribute);
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidDataException($"工程索引缺少 {element}/@{attribute}。");
        return value;
    }

    private static string? Attr(XElement? element, string name) =>
        element?.Attribute(name)?.Value;

    private static string Resolve(string directory, string href) =>
        Path.GetFullPath(Path.Combine(directory, href.Replace('/', Path.DirectorySeparatorChar)));
}
