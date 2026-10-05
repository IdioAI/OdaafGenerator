namespace OdaafGenerator.Core;

public enum SearchLevel
{
    Project,
    Namespace,
    Class,
    Function
}

public sealed record SearchHit(
    SearchLevel Level,
    string Title,
    string Subtitle,
    string? SyntaxPath,
    bool OnXmlCanvas);

public sealed record SearchResult(IReadOnlyList<SearchHit> Hits, bool Truncated);

public sealed record TypeEntry(
    string Name,
    string SyntaxPath,
    string Kind,
    string Project,
    string NamespaceName,
    IReadOnlyList<CodeSymbol> Functions);

public sealed record NamespaceEntry(
    string Name,
    string Project,
    IReadOnlyList<TypeEntry> Types);

public sealed record ProjectEntry(
    string Name,
    bool IsFocus,
    IReadOnlyList<NamespaceEntry> Namespaces);

public sealed class Catalog
{
    private readonly List<CodeSymbol> _symbols;
    private readonly List<CodeSymbol> _host;

    private Catalog(IReadOnlyList<ProjectEntry> projects, List<CodeSymbol> symbols, List<CodeSymbol> host)
    {
        Projects = projects;
        _symbols = symbols;
        _host = host;
    }

    public IReadOnlyList<ProjectEntry> Projects { get; }

    public static Catalog Create(
        IReadOnlyList<string> projectNames,
        IReadOnlyList<RawDeclaration> focusRows,
        IReadOnlyList<RawDeclaration>? hostRows = null)
    {
        var symbols = ToSymbols(focusRows);
        var host = ToSymbols(hostRows ?? []);

        var entries = projectNames
            .Distinct(StringComparer.Ordinal)
            .Select(name => Entry(name, symbols))
            .OrderBy(entry => entry.IsFocus ? 0 : 1)
            .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return new Catalog(entries, symbols, host);
    }

    public CodeSymbol? FindMethod(string simpleName) =>
        _host.FirstOrDefault(symbol => SymbolKinds.IsFunction(symbol.Kind) && symbol.SimpleName == simpleName)
        ?? _symbols.FirstOrDefault(symbol => SymbolKinds.IsFunction(symbol.Kind) && symbol.SimpleName == simpleName);

    public CodeSymbol? Find(string syntaxPath) =>
        _symbols.FirstOrDefault(symbol => symbol.SyntaxPath == syntaxPath);

    public IReadOnlyList<FunctionCard> BuildXmlContext(LayoutFile layout)
    {
        var cards = new List<FunctionCard>(XmlScene.Slots.Length);
        foreach (var slot in XmlScene.Slots)
        {
            var symbol = Find(slot.SyntaxPath);
            var interfaceSymbol = slot.InterfaceSyntaxPath is null ? null : Find(slot.InterfaceSyntaxPath);
            cards.Add(FunctionCard.FromScene(slot, symbol, interfaceSymbol, layout.Find(slot.SyntaxPath)));
        }

        return cards;
    }

    public FunctionCard BranchFor(CodeSymbol symbol, LayoutFile layout)
    {
        var scene = XmlScene.Slots.FirstOrDefault(slot => slot.SyntaxPath == symbol.SyntaxPath);
        if (scene is not null)
            return FunctionCard.FromScene(scene, symbol, scene.InterfaceSyntaxPath is null ? null : Find(scene.InterfaceSyntaxPath), layout.Find(symbol.SyntaxPath));
        return FunctionCard.OffCanvas(symbol, layout.Find(symbol.SyntaxPath));
    }

    public SearchResult Search(SearchLevel level, string? query, int limit = 200)
    {
        var text = query?.Trim() ?? "";
        if (text.Length == 0)
            return new SearchResult([], false);

        IEnumerable<SearchHit> hits = level switch
        {
            SearchLevel.Project => Projects
                .Where(project => project.Name.Contains(text, StringComparison.OrdinalIgnoreCase))
                .Select(project => new SearchHit(
                    SearchLevel.Project,
                    project.Name,
                    project.IsFocus ? "XML 上下文，可展开到函数" : "辅助工程，只到项目",
                    null,
                    false)),
            SearchLevel.Namespace => Projects.Where(project => project.IsFocus)
                .SelectMany(project => project.Namespaces)
                .Where(space => space.Name.Contains(text, StringComparison.OrdinalIgnoreCase))
                .Select(space => new SearchHit(
                    SearchLevel.Namespace,
                    space.Name,
                    space.Project,
                    null,
                    false)),
            SearchLevel.Class => Projects.Where(project => project.IsFocus)
                .SelectMany(project => project.Namespaces)
                .SelectMany(space => space.Types)
                .Where(type =>
                    type.Name.Contains(text, StringComparison.OrdinalIgnoreCase)
                    || type.SyntaxPath.Contains(text, StringComparison.OrdinalIgnoreCase))
                .Select(type => new SearchHit(
                    SearchLevel.Class,
                    type.Name,
                    $"{type.NamespaceName} · {type.Project}",
                    type.SyntaxPath,
                    false)),
            _ => _symbols.Where(symbol =>
                    SymbolKinds.IsFunction(symbol.Kind)
                    && (symbol.SimpleName.Contains(text, StringComparison.OrdinalIgnoreCase)
                        || symbol.SyntaxPath.Contains(text, StringComparison.OrdinalIgnoreCase)))
                .Select(symbol => new SearchHit(
                    SearchLevel.Function,
                    symbol.SimpleName,
                    $"{symbol.TypeName} · {symbol.Project}",
                    symbol.SyntaxPath,
                    XmlScene.IsCanvas(symbol.SyntaxPath)))
        };

        var taken = hits.Take(limit + 1).ToList();
        var truncated = taken.Count > limit;
        if (truncated)
            taken.RemoveAt(taken.Count - 1);
        return new SearchResult(taken, truncated);
    }

    private static List<CodeSymbol> ToSymbols(IReadOnlyList<RawDeclaration> rows)
    {
        var map = new Dictionary<(string File, int Start), RawDeclaration>();
        foreach (var row in rows)
            map[(row.FilePath, row.StartByte)] = row;

        var symbols = new List<CodeSymbol>();
        foreach (var row in rows)
        {
            if (!SymbolKinds.IsType(row.Kind) && !SymbolKinds.IsFunction(row.Kind))
                continue;
            var typeDecl = ImmediateType(row, map);
            var typePath = SymbolKinds.IsType(row.Kind) ? row.SyntaxPath : typeDecl?.SyntaxPath ?? "";
            symbols.Add(new CodeSymbol(
                row.ProjectName,
                row.FilePath,
                row.StartByte,
                row.Kind,
                row.SyntaxPath,
                row.Signature,
                row.StartLine,
                row.EndLine,
                NamespaceOf(row, map),
                SyntaxName.Last(typePath),
                typePath));
        }

        return symbols;
    }

    private static ProjectEntry Entry(string name, List<CodeSymbol> symbols)
    {
        if (!XmlScene.IsFocusProject(name))
            return new ProjectEntry(name, false, []);

        var mine = symbols.Where(symbol => symbol.Project == name).ToList();
        var namespaces = mine
            .Where(symbol => SymbolKinds.IsType(symbol.Kind))
            .GroupBy(symbol => symbol.Namespace, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new NamespaceEntry(
                string.IsNullOrEmpty(group.Key) ? "(全局)" : group.Key,
                name,
                group.OrderBy(type => type.SimpleName, StringComparer.OrdinalIgnoreCase)
                    .Select(type => new TypeEntry(
                        type.SimpleName,
                        type.SyntaxPath,
                        type.Kind,
                        name,
                        string.IsNullOrEmpty(group.Key) ? "(全局)" : group.Key,
                        mine.Where(fn =>
                                SymbolKinds.IsFunction(fn.Kind)
                                && fn.TypeSyntaxPath == type.SyntaxPath)
                            .OrderBy(fn => fn.SimpleName, StringComparer.OrdinalIgnoreCase)
                            .ToList()))
                    .ToList()))
            .ToList();
        return new ProjectEntry(name, true, namespaces);
    }

    private static string NamespaceOf(
        RawDeclaration row,
        Dictionary<(string File, int Start), RawDeclaration> map)
    {
        var current = row;
        var guard = 0;
        while (guard++ < 64)
        {
            if (current.Kind == "namespace")
                return current.SyntaxPath;
            if (current.ParentStartByte is int parentStart
                && map.TryGetValue((current.FilePath, parentStart), out var parent))
            {
                current = parent;
                continue;
            }

            break;
        }

        var type = ImmediateType(row, map);
        if (type is null)
            return "";
        var dot = type.SyntaxPath.LastIndexOf('.');
        return dot < 0 ? "" : type.SyntaxPath[..dot];
    }

    private static RawDeclaration? ImmediateType(
        RawDeclaration row,
        Dictionary<(string File, int Start), RawDeclaration> map)
    {
        if (SymbolKinds.IsType(row.Kind))
            return row;
        if (row.ParentStartByte is int parentStart
            && map.TryGetValue((row.FilePath, parentStart), out var parent)
            && SymbolKinds.IsType(parent.Kind))
            return parent;
        return null;
    }
}
