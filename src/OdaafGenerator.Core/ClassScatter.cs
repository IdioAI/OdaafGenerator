using System.Text.RegularExpressions;

namespace OdaafGenerator.Core;

public sealed record TypeNode(
    string SyntaxPath,
    string Name,
    string Project,
    string Namespace,
    string Kind,
    bool InScene);

public sealed record TypeCluster(
    string Id,
    string Name,
    string Kind,
    IReadOnlyList<TypeNode> Types);

public sealed class ClassScatterGraph
{
    private static readonly Regex Identifier = new(@"[A-Za-z_][A-Za-z0-9_]*", RegexOptions.Compiled);
    private static readonly HashSet<string> Skip = new(StringComparer.Ordinal)
    {
        "void", "bool", "byte", "char", "decimal", "double", "float", "int", "long", "object",
        "sbyte", "short", "string", "uint", "ulong", "ushort", "var", "dynamic",
        "public", "private", "protected", "internal", "static", "async", "override", "virtual",
        "abstract", "sealed", "partial", "readonly", "required", "new", "this", "base",
        "out", "ref", "in", "params", "true", "false", "null", "Task", "ValueTask",
        "List", "IReadOnlyList", "IEnumerable", "Dictionary", "CancellationToken"
    };

    public required IReadOnlyList<TypeNode> Types { get; init; }
    public required IReadOnlyList<TypeCluster> Clusters { get; init; }
    public required IReadOnlyList<GraphEdge> TypeEdges { get; init; }

    public IReadOnlyList<FunctionCard> ClusterCards()
    {
        var cards = new List<FunctionCard>(Clusters.Count);
        for (var i = 0; i < Clusters.Count; i++)
        {
            var cluster = Clusters[i];
            cards.Add(new FunctionCard
            {
                SyntaxPath = cluster.Id,
                Name = cluster.Name,
                StepLabel = $"{cluster.Kind} · {cluster.Types.Count} 类",
                Project = cluster.Types[0].Project,
                Signature = string.Join("、", cluster.Types.Select(type => type.Name).Take(8)),
                FilePath = "",
                StartLine = 0,
                EndLine = 0,
                Materials = [],
                Note = cluster.Kind,
                OnCanvas = true,
                X = 24 + i % 3 * 256,
                Y = 36 + i / 3 * 96
            });
        }

        return cards;
    }

    public (IReadOnlyList<FunctionCard> Cards, IReadOnlyList<GraphEdge> Edges) Members(string clusterId)
    {
        var cluster = Clusters.FirstOrDefault(item => item.Id == clusterId);
        if (cluster is null)
            return ([], []);

        var cards = new List<FunctionCard>(cluster.Types.Count);
        for (var i = 0; i < cluster.Types.Count; i++)
        {
            var type = cluster.Types[i];
            cards.Add(new FunctionCard
            {
                SyntaxPath = type.SyntaxPath,
                Name = type.Name,
                StepLabel = type.InScene ? "Z 抽出" : type.Kind,
                Project = type.Project,
                Signature = type.Namespace,
                FilePath = "",
                StartLine = 0,
                EndLine = 0,
                Materials = [],
                Note = type.Namespace,
                OnCanvas = true,
                X = 24 + i % 4 * 240,
                Y = 36 + i / 4 * 96
            });
        }

        var known = cluster.Types.Select(type => type.SyntaxPath).ToHashSet(StringComparer.Ordinal);
        var edges = TypeEdges
            .Where(edge => known.Contains(edge.From) && known.Contains(edge.To))
            .ToList();
        return (cards, edges);
    }

    public static ClassScatterGraph Build(Catalog catalog)
    {
        var types = catalog.FocusTypes()
            .GroupBy(type => type.SyntaxPath, StringComparer.Ordinal)
            .Select(group => group.First())
            .Select(type => new TypeNode(
                type.SyntaxPath,
                type.Name,
                type.Project,
                type.NamespaceName,
                type.Kind,
                IsSceneType(type.SyntaxPath)))
            .ToList();
        var byName = types
            .GroupBy(type => type.Name, StringComparer.Ordinal)
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.First().SyntaxPath, StringComparer.Ordinal);
        var edges = new List<GraphEdge>();
        foreach (var type in catalog.FocusTypes())
        {
            foreach (var function in type.Functions)
            {
                foreach (var name in Identifiers(function.Signature))
                {
                    if (name == type.Name || !byName.TryGetValue(name, out var target))
                        continue;
                    if (target == type.SyntaxPath)
                        continue;
                    edges.Add(new GraphEdge(type.SyntaxPath, target));
                }
            }
        }

        foreach (var (from, to) in XmlScene.Edges)
        {
            var left = OwnerType(from);
            var right = OwnerType(to);
            if (left != right)
                edges.Add(new GraphEdge(left, right));
        }

        edges = edges.Distinct().ToList();
        var clusters = Cluster(types, edges);
        return new ClassScatterGraph
        {
            Types = types,
            Clusters = clusters,
            TypeEdges = edges
        };
    }

    private static IReadOnlyList<TypeCluster> Cluster(List<TypeNode> types, List<GraphEdge> edges)
    {
        var parent = types.ToDictionary(type => type.SyntaxPath, type => type.SyntaxPath, StringComparer.Ordinal);
        string Find(string id)
        {
            while (parent[id] != id)
                id = parent[id] = parent[parent[id]];
            return id;
        }

        void Union(string left, string right)
        {
            left = Find(left);
            right = Find(right);
            if (left != right)
                parent[right] = left;
        }

        foreach (var edge in edges)
        {
            if (parent.ContainsKey(edge.From) && parent.ContainsKey(edge.To))
                Union(edge.From, edge.To);
        }

        var components = types
            .GroupBy(type => Find(type.SyntaxPath), StringComparer.Ordinal)
            .Select(group => group.ToList())
            .ToList();

        var clusters = new List<TypeCluster>();
        var leftovers = new List<TypeNode>();
        var scene = 0;
        var extra = 0;
        foreach (var component in components)
        {
            if (component.Any(type => type.InScene))
            {
                clusters.Add(new TypeCluster(
                    "cluster-z-extract" + (scene == 0 ? "" : "-" + scene),
                    scene == 0 ? "Z抽出" : "Z抽出 " + (scene + 1),
                    "场景",
                    component.OrderBy(type => type.Name, StringComparer.OrdinalIgnoreCase).ToList()));
                scene++;
                continue;
            }

            if (component.Count >= 2)
            {
                extra++;
                var title = component[0].Namespace;
                if (title.Length == 0)
                    title = component[0].Project;
                clusters.Add(new TypeCluster(
                    "cluster-rel-" + extra,
                    SyntaxName.Last(title),
                    "引用",
                    component.OrderBy(type => type.Name, StringComparer.OrdinalIgnoreCase).ToList()));
                continue;
            }

            leftovers.AddRange(component);
        }

        foreach (var group in leftovers.GroupBy(type => type.Namespace + "\n" + type.Project, StringComparer.Ordinal))
        {
            var members = group.OrderBy(type => type.Name, StringComparer.OrdinalIgnoreCase).ToList();
            var space = members[0].Namespace;
            clusters.Add(new TypeCluster(
                "cluster-ns-" + members[0].Project + "-" + space,
                string.IsNullOrEmpty(space) ? members[0].Project : SyntaxName.Last(space),
                "命名空间",
                members));
        }

        return clusters
            .OrderBy(cluster => cluster.Kind == "场景" ? 0 : cluster.Kind == "引用" ? 1 : 2)
            .ThenBy(cluster => cluster.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IEnumerable<string> Identifiers(string signature) =>
        Identifier.Matches(signature ?? "")
            .Select(match => match.Value)
            .Where(name => !Skip.Contains(name));

    private static bool IsSceneType(string syntaxPath) =>
        XmlScene.Slots.Any(slot =>
            OwnerType(slot.SyntaxPath) == syntaxPath
            || slot.InterfaceSyntaxPath is string iface && OwnerType(iface) == syntaxPath);

    private static string OwnerType(string syntaxPath)
    {
        var dot = syntaxPath.LastIndexOf('.');
        return dot < 0 ? syntaxPath : syntaxPath[..dot];
    }
}
