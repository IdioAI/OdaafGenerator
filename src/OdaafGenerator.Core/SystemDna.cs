using System.Xml.Linq;

namespace OdaafGenerator.Core;

public sealed record DnaLeaf(string Title, string MachineName, string DataType, string DefaultValue, string Range, string Description);

public sealed record DnaGroup(string Title, string MachineName, string Description, IReadOnlyList<DnaLeaf> Leaves);

public static class SystemDnaContext
{
    public const string Id = "system-dna";
    public const string Name = "系统 DNA";
}

public static class SystemDna
{
    public const string Uri = "urn:odaaf:code:system-parameters";
    private static readonly XNamespace Ns = "urn:odaaf:project";

    public static IReadOnlyList<DnaGroup> Load(string path)
    {
        var root = XDocument.Load(path).Root
            ?? throw new InvalidDataException("系统 DNA 文件没有根元素。");
        var graph = root.Element(Ns + "Graph")
            ?? throw new InvalidDataException("系统 DNA 文件没有 Graph。");
        var groups = new List<DnaGroup>();
        foreach (var cls in graph.Element(Ns + "Classes")?.Elements(Ns + "Class") ?? [])
        {
            var leaves = new List<DnaLeaf>();
            foreach (var prop in cls.Element(Ns + "DataProperties")?.Elements(Ns + "DataProperty") ?? [])
            {
                var constraint = prop.Element(Ns + "Constraint");
                leaves.Add(new DnaLeaf(
                    Attr(prop, "label") ?? Attr(prop, "friendlyName") ?? Attr(prop, "name") ?? "",
                    Attr(prop, "machineName") ?? "",
                    Attr(prop, "dataType") ?? "",
                    Attr(constraint, "defaultValue") ?? "",
                    Attr(constraint, "pattern") ?? "",
                    prop.Element(Ns + "Description")?.Value?.Trim() ?? ""));
            }

            groups.Add(new DnaGroup(
                Attr(cls, "label") ?? Attr(cls, "name") ?? "",
                Attr(cls, "machineName") ?? "",
                cls.Element(Ns + "Description")?.Value?.Trim() ?? "",
                leaves));
        }

        return groups;
    }

    private static string? Attr(XElement? element, string name) =>
        element?.Attribute(name)?.Value;
}
