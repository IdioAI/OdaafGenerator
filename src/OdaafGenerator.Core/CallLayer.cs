using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace OdaafGenerator.Core;

public sealed record DirectCall(
    string Name,
    string? SyntaxPath,
    string? Signature,
    string? FilePath,
    int StartLine,
    bool Resolved,
    PatternAPhase Phase);

public sealed class CallLayerGraph
{
    public required CodeSymbol Caller { get; init; }
    public required IReadOnlyList<DirectCall> Calls { get; init; }

    public (IReadOnlyList<FunctionCard> Cards, IReadOnlyList<GraphEdge> Edges) Cards(LayoutFile layout)
    {
        var placedCaller = layout.Find(Caller.SyntaxPath);
        var caller = FunctionCard.OffCanvas(Caller, placedCaller, onCanvas: true);
        caller.X = placedCaller?.X ?? 24;
        caller.Y = placedCaller?.Y ?? 36;
        var cards = new List<FunctionCard> { caller };
        var edges = new List<GraphEdge>();
        var lanes = Calls
            .Where(call => !PatternA.IsSyntaxNoise(call.Name))
            .GroupBy(call => call.Phase)
            .ToDictionary(group => group.Key, group => group.ToList());
        foreach (var phase in Enum.GetValues<PatternAPhase>())
        {
            if (!lanes.TryGetValue(phase, out var lane))
                continue;
            var column = 280 + (int)phase * 230;
            for (var i = 0; i < lane.Count; i++)
            {
                var call = lane[i];
                var path = (call.SyntaxPath ?? Caller.SyntaxPath + "." + call.Name) + "@" + phase;
                var placed = layout.Find(path);
                cards.Add(new FunctionCard
                {
                    SyntaxPath = path,
                    Name = call.Name,
                    StepLabel = PatternA.Title(phase),
                    Project = Caller.Project,
                    Signature = call.Signature ?? "",
                    FilePath = call.FilePath ?? "",
                    StartLine = call.StartLine,
                    EndLine = call.StartLine,
                    Materials = SignatureParser.Parameters(call.Signature),
                    Note = call.Resolved ? null : "这一层只列出名字，索引里没有对上。",
                    OnCanvas = true,
                    X = column,
                    Y = 36 + i * 96,
                    Formal = placed?.Formal ?? "",
                    Final = placed?.Final ?? ""
                });
                edges.Add(new GraphEdge(caller.SyntaxPath, path));
            }
        }

        return (cards, edges);
    }

    public static CallLayerGraph Expand(CodeSymbol caller, string sourceRoot, Catalog catalog)
    {
        var names = ReadInvocations(caller, sourceRoot);
        var dialogIndex = -1;
        for (var i = 0; i < names.Count; i++)
        {
            if (names[i] == "ShowDialog")
            {
                dialogIndex = i;
                break;
            }
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var calls = new List<DirectCall>();
        for (var index = 0; index < names.Count; index++)
        {
            var name = names[index];
            var phase = PatternA.Classify(name, index, dialogIndex);
            if (!seen.Add(phase + ":" + name) || PatternA.IsSyntaxNoise(name))
                continue;
            var symbol = catalog.ResolveCall(name, caller.TypeSyntaxPath);
            calls.Add(symbol is null
                ? new DirectCall(name, null, null, null, 0, false, phase)
                : new DirectCall(symbol.SimpleName, symbol.SyntaxPath, symbol.Signature, symbol.FilePath, symbol.StartLine, true, phase));
        }

        return new CallLayerGraph { Caller = caller, Calls = calls };
    }

    public static string? ResolveSource(string sourceRoot, string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return null;
        if (File.Exists(filePath))
            return Path.GetFullPath(filePath);
        var combined = Path.GetFullPath(Path.Combine(sourceRoot, filePath.Replace('/', Path.DirectorySeparatorChar)));
        return File.Exists(combined) ? combined : null;
    }

    private static IReadOnlyList<string> ReadInvocations(CodeSymbol caller, string sourceRoot)
    {
        var path = ResolveSource(sourceRoot, caller.FilePath);
        if (path is null)
            return [];

        var text = File.ReadAllText(path);
        var tree = CSharpSyntaxTree.ParseText(text);
        var root = tree.GetCompilationUnitRoot();
        var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(method => method.Identifier.Text == caller.SimpleName)
            .ToList();
        var method = methods.Count == 1
            ? methods[0]
            : methods.FirstOrDefault(item =>
            {
                var line = tree.GetLineSpan(item.Span).StartLinePosition.Line + 1;
                return line >= caller.StartLine - 1 && line <= caller.EndLine + 1;
            }) ?? methods.FirstOrDefault();
        if (method is null)
            return [];
        SyntaxNode? node = method.Body ?? (SyntaxNode?)method.ExpressionBody;
        if (node is null)
            return [];
        return node.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Select(NameOf)
            .Where(name => !string.IsNullOrEmpty(name) && name != caller.SimpleName)
            .Cast<string>()
            .ToList();
    }

    private static string? NameOf(InvocationExpressionSyntax invocation) =>
        invocation.Expression switch
        {
            IdentifierNameSyntax name => name.Identifier.Text,
            MemberAccessExpressionSyntax member => member.Name.Identifier.Text,
            MemberBindingExpressionSyntax bound => bound.Name.Identifier.Text,
            _ => null
        };
}
