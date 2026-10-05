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
    public bool Menu { get; init; }

    public (IReadOnlyList<FunctionCard> Cards, IReadOnlyList<GraphEdge> Edges) Cards(LayoutFile layout)
    {
        var placedCaller = layout.Find(Caller.SyntaxPath);
        var caller = FunctionCard.OffCanvas(Caller, placedCaller, onCanvas: true, stepLabel: "执行");
        caller.X = placedCaller?.X ?? 24;
        caller.Y = placedCaller?.Y ?? 36;
        var cards = new List<FunctionCard> { caller };
        var edges = new List<GraphEdge>();
        var lanes = Calls.GroupBy(call => call.Phase).ToDictionary(group => group.Key, group => group.ToList());
        if (lanes.TryGetValue(PatternAPhase.Execute, out var execute))
            AddLane(cards, edges, caller, execute, PatternAPhase.Execute, 280, main: true, Menu, layout);
        var column = 560;
        foreach (var phase in Enum.GetValues<PatternAPhase>())
        {
            if (phase == PatternAPhase.Execute || !lanes.TryGetValue(phase, out var lane))
                continue;
            AddLane(cards, edges, caller, lane, phase, column, main: false, Menu, layout);
            column += 230;
        }

        return (cards, edges);
    }

    public static CallLayerGraph Expand(CodeSymbol caller, string sourceRoot, Catalog catalog)
    {
        var menu = FrameworkCalls.IsMenuHandler(caller.SimpleName);
        var sites = ReadSites(caller, sourceRoot, menu);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var calls = new List<DirectCall>();
        foreach (var site in sites)
        {
            var symbol = catalog.ResolveCall(site.Name, caller.TypeSyntaxPath);
            if (FrameworkCalls.Exclude(site.Name, site.Receiver, symbol) || !seen.Add(site.Phase + ":" + site.Name))
                continue;
            calls.Add(symbol is null
                ? new DirectCall(site.Name, null, null, null, 0, false, site.Phase)
                : new DirectCall(symbol.SimpleName, symbol.SyntaxPath, symbol.Signature, symbol.FilePath, symbol.StartLine, true, site.Phase));
        }

        return new CallLayerGraph { Caller = caller, Calls = calls, Menu = menu };
    }

    private static void AddLane(
        List<FunctionCard> cards,
        List<GraphEdge> edges,
        FunctionCard caller,
        IReadOnlyList<DirectCall> lane,
        PatternAPhase phase,
        int column,
        bool main,
        bool menu,
        LayoutFile layout)
    {
        for (var i = 0; i < lane.Count; i++)
        {
            var call = lane[i];
            var path = (call.SyntaxPath ?? caller.SyntaxPath + "." + call.Name) + "@" + phase;
            var placed = layout.Find(path);
            cards.Add(new FunctionCard
            {
                SyntaxPath = path,
                Name = call.Name,
                StepLabel = PatternA.Title(phase, menu),
                Project = caller.Project,
                Signature = call.Signature ?? "",
                FilePath = call.FilePath ?? "",
                StartLine = call.StartLine,
                EndLine = call.StartLine,
                Materials = SignatureParser.Parameters(call.Signature),
                Note = call.Resolved ? null : "这一层只列出名字，索引里没有对上。",
                OnCanvas = true,
                MainFlow = main,
                X = column,
                Y = 36 + i * 96,
                Formal = placed?.Formal ?? "",
                Final = placed?.Final ?? ""
            });
            if (main)
                edges.Add(new GraphEdge(caller.SyntaxPath, path));
        }
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

    private readonly record struct CallSite(string Name, string? Receiver, PatternAPhase Phase);

    private static List<CallSite> ReadSites(CodeSymbol caller, string sourceRoot, bool menu)
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

        var invocations = node.DescendantNodes().OfType<InvocationExpressionSyntax>().ToList();
        var dialogIndex = -1;
        for (var i = 0; i < invocations.Count; i++)
        {
            if (NameOf(invocations[i]) == "ShowDialog")
            {
                dialogIndex = i;
                break;
            }
        }

        var sites = new List<CallSite>();
        for (var index = 0; index < invocations.Count; index++)
        {
            var invocation = invocations[index];
            var name = NameOf(invocation);
            if (string.IsNullOrEmpty(name) || name == caller.SimpleName)
                continue;
            var phase = menu
                ? PatternA.Classify(name, index, dialogIndex)
                : PatternB.Region(invocation, method.Body);
            sites.Add(new CallSite(name, ReceiverOf(invocation), phase));
        }

        return sites;
    }

    private static string? NameOf(InvocationExpressionSyntax invocation) =>
        invocation.Expression switch
        {
            IdentifierNameSyntax name => name.Identifier.Text,
            MemberAccessExpressionSyntax member => member.Name.Identifier.Text,
            MemberBindingExpressionSyntax bound => bound.Name.Identifier.Text,
            _ => null
        };

    private static string? ReceiverOf(InvocationExpressionSyntax invocation) =>
        invocation.Expression switch
        {
            MemberAccessExpressionSyntax member => ReceiverText(member.Expression),
            _ => null
        };

    private static string? ReceiverText(ExpressionSyntax expression) =>
        expression switch
        {
            IdentifierNameSyntax name => name.Identifier.Text,
            MemberAccessExpressionSyntax member => member.Name.Identifier.Text,
            GenericNameSyntax generic => generic.Identifier.Text,
            _ => null
        };
}
