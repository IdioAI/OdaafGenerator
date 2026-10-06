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
    PatternAPhase Phase,
    CallKind Kind,
    string Project,
    string Purpose);

public sealed class CallLayerGraph
{
    public required CodeSymbol Caller { get; init; }
    public required IReadOnlyList<DirectCall> Calls { get; init; }
    public bool Menu { get; init; }

    public (IReadOnlyList<FunctionCard> Cards, IReadOnlyList<GraphEdge> Edges) Cards(LayoutFile layout)
    {
        var placedCaller = layout.Find(Caller.SyntaxPath);
        var callerNote = FlowNotes.Find(Caller.SyntaxPath, Caller.SimpleName);
        var caller = FunctionCard.OffCanvas(Caller, placedCaller, onCanvas: true, stepLabel: "执行");
        caller.X = placedCaller?.X ?? 24;
        caller.Y = placedCaller?.Y ?? 36;
        caller.Purpose = callerNote?.Purpose ?? "";
        caller.Documents = callerNote?.Documents ?? "";
        caller.Materials = FlowNotes.WithMeanings(Caller.SyntaxPath, Caller.SimpleName, caller.Materials);
        FunctionBrief.Apply(caller);
        var cards = new List<FunctionCard> { caller };
        var edges = new List<GraphEdge>();
        var work = Calls.Where(call => call.Kind != CallKind.System).ToList();
        var lanes = work.GroupBy(call => call.Phase).ToDictionary(group => group.Key, group => group.ToList());
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

        var systems = Calls.Where(call => call.Kind == CallKind.System).ToList();
        if (systems.Count > 0)
            AddLane(cards, edges, caller, systems, PatternAPhase.Execute, column, main: false, Menu, layout, system: true);

        return (cards, edges);
    }

    public (IReadOnlyList<FunctionCard> Cards, IReadOnlyList<GraphEdge> Edges) Segments(LayoutFile layout)
    {
        var (cards, edges, _) = Begin(layout);
        var execute = Calls.Where(call => call.Kind != CallKind.System && call.Phase == PatternAPhase.Execute).ToList();
        string? previous = null;
        var y = 36;
        foreach (var name in LayerMarkdown.Segments(Caller.SimpleName, this))
        {
            var members = execute.Where(call => FlowGroups.NameOf(Caller.SimpleName, call.SyntaxPath, call.Name) == name).ToList();
            var path = Caller.SyntaxPath + "@segment:" + name;
            var titles = members.Select(call => FlowNotes.Find(call.SyntaxPath, call.Name)?.Title ?? call.Name);
            cards.Add(new FunctionCard
            {
                SyntaxPath = path,
                Name = name,
                FriendlyName = name,
                StepLabel = "执行",
                Project = "",
                Signature = "",
                FilePath = "",
                StartLine = 0,
                EndLine = 0,
                Materials = [],
                Purpose = string.Join("、", titles),
                OnCanvas = true,
                MainFlow = true,
                X = 280,
                Y = y
            });
            if (previous is not null)
                edges.Add(new GraphEdge(previous, path));
            previous = path;
            y += 112;
        }

        return (cards, edges);
    }

    public (IReadOnlyList<FunctionCard> Cards, IReadOnlyList<GraphEdge> Edges) SegmentDetail(LayoutFile layout, string segment)
    {
        var (cards, edges, caller) = Begin(layout);
        var members = Calls.Where(call =>
            call.Kind != CallKind.System
            && call.Phase == PatternAPhase.Execute
            && FlowGroups.NameOf(Caller.SimpleName, call.SyntaxPath, call.Name) == segment).ToList();
        AddLane(cards, edges, caller, members, PatternAPhase.Execute, 280, main: true, Menu, layout);
        return (cards, edges);
    }

    private (List<FunctionCard> Cards, List<GraphEdge> Edges, FunctionCard CallerCard) Begin(LayoutFile layout)
    {
        var placedCaller = layout.Find(Caller.SyntaxPath);
        var callerNote = FlowNotes.Find(Caller.SyntaxPath, Caller.SimpleName);
        var caller = FunctionCard.OffCanvas(Caller, placedCaller, onCanvas: true, stepLabel: "执行");
        caller.X = placedCaller?.X ?? 24;
        caller.Y = placedCaller?.Y ?? 36;
        caller.Purpose = callerNote?.Purpose ?? "";
        caller.Documents = callerNote?.Documents ?? "";
        caller.Materials = FlowNotes.WithMeanings(Caller.SyntaxPath, Caller.SimpleName, caller.Materials);
        FunctionBrief.Apply(caller);
        return ([caller], [], caller);
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
            var system = SystemCalls.TryDescribe(site.Name, site.Receiver, symbol, caller.TypeSyntaxPath, out var systemPurpose);
            if (!system && (FrameworkCalls.Exclude(site.Name, site.Receiver, symbol) || !seen.Add(site.Phase + ":" + site.Name)))
                continue;
            if (system && !seen.Add(site.Phase + ":" + site.Name))
                continue;
            if (system)
            {
                calls.Add(new DirectCall(site.Name, null, null, null, 0, false, site.Phase, CallKind.System, "系统", systemPurpose));
                continue;
            }

            var note = FlowNotes.Find(symbol?.SyntaxPath, site.Name);
            calls.Add(symbol is null
                ? new DirectCall(site.Name, null, null, null, 0, false, site.Phase, CallKind.Unindexed, "", note?.Purpose ?? "")
                : new DirectCall(symbol.SimpleName, symbol.SyntaxPath, symbol.Signature, symbol.FilePath, symbol.StartLine, true, site.Phase, CallKind.Domain, symbol.Project, note?.Purpose ?? ""));
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
        LayoutFile layout,
        bool system = false)
    {
        for (var i = 0; i < lane.Count; i++)
        {
            var call = lane[i];
            var path = (call.SyntaxPath ?? caller.SyntaxPath + "." + call.Name) + "@" + (system ? "system" : phase.ToString());
            var placed = layout.Find(path);
            var note = call.Kind == CallKind.Unindexed ? "未收入本期索引。" : null;
            cards.Add(new FunctionCard
            {
                SyntaxPath = path,
                Name = call.Name,
                StepLabel = system ? "系统" : PatternA.Title(phase, menu),
                Project = call.Project,
                Signature = call.Signature ?? "",
                FilePath = call.FilePath ?? "",
                StartLine = call.StartLine,
                EndLine = call.StartLine,
                Materials = FlowNotes.WithMeanings(call.SyntaxPath, call.Name, SignatureParser.Parameters(call.Signature)),
                Purpose = call.Purpose,
                Note = note,
                OnCanvas = true,
                MainFlow = main && call.Kind != CallKind.System,
                WritesStructure = call.Kind != CallKind.System,
                X = column,
                Y = 36 + i * 112,
                Formal = call.Kind == CallKind.System ? "" : placed?.Formal ?? "",
                Final = placed?.Final ?? ""
            });
            FunctionBrief.Apply(cards[^1]);
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
