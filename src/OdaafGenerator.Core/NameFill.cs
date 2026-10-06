namespace OdaafGenerator.Core;

public sealed record FillTarget(string Handler, DirectCall Call);

public sealed record FillPlan(IReadOnlyList<FillTarget> Targets, IReadOnlyList<string> Ambiguous);

public static class NameFill
{
    public const string SemiAutoSkill =
        "半自动是 PcbDocumentContextType.SemiAuto，一块板一份文档，不是系统 DNA。\n" +
        "加载PCB与XML对：导入 XML，匹配分组，不写背钻和 Z。\n" +
        "参数写入PCB：必须先加载配对，再写背钻、Z抽出、ISU、Z补正。\n" +
        "管道中途不再弹出选文件。";

    private static readonly HashSet<string> SemiAutoHandlers = new(StringComparer.Ordinal)
    {
        "ExecuteSemiAutoAddGroupingInfo",
        "ExecuteWriteParamsToPcb",
        "ExecuteAddSemiZExtractionToCurrentPcb",
        "ExecuteAddBackdrillParametersToCurrentPcb",
        "ExecuteClearZExtractionInSemiAuto",
        "ExecuteExportHalfAutoPcb",
        "ExecuteLoadSemiAutoProjectIndex",
        "ExecuteSaveSemiAutoProjectIndex",
        "ExecuteOpenSemiAutoProcessRules"
    };

    public static string? SkillFor(string handler) =>
        SemiAutoHandlers.Contains(handler) ? SemiAutoSkill : null;

    public static FillPlan Collect(Catalog catalog, string sourceRoot, bool onlyMissing = true)
    {
        var chosen = new Dictionary<string, FillTarget>(StringComparer.Ordinal);
        var paths = new Dictionary<string, string?>(StringComparer.Ordinal);
        var ambiguous = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entry in ShellMenus.Entries)
        {
            var symbol = catalog.FindMethod(entry.MethodName);
            if (symbol is not null)
                Consider(chosen, paths, ambiguous, entry.MethodName, FromSymbol(symbol), onlyMissing);
            if (symbol is null)
                continue;
            CallLayerGraph layer;
            try
            {
                layer = CallLayerGraph.Expand(symbol, sourceRoot, catalog);
            }
            catch (IOException)
            {
                continue;
            }

            foreach (var call in layer.Calls)
                Consider(chosen, paths, ambiguous, entry.MethodName, call, onlyMissing);
        }

        return new FillPlan(chosen.Values.ToList(), ambiguous.OrderBy(name => name, StringComparer.Ordinal).ToList());
    }

    private static void Consider(
        Dictionary<string, FillTarget> chosen,
        Dictionary<string, string?> paths,
        HashSet<string> ambiguous,
        string handler,
        DirectCall call,
        bool onlyMissing)
    {
        if (call.Kind == CallKind.System || ambiguous.Contains(call.Name) || (onlyMissing && !Needs(call)))
            return;
        if (paths.TryGetValue(call.Name, out var known))
        {
            if (!SamePath(known, call.SyntaxPath))
            {
                ambiguous.Add(call.Name);
                chosen.Remove(call.Name);
                return;
            }

            if (SkillFor(chosen[call.Name].Handler) is null && SkillFor(handler) is not null)
                chosen[call.Name] = chosen[call.Name] with { Handler = handler };
            return;
        }

        paths[call.Name] = call.SyntaxPath;
        chosen[call.Name] = new FillTarget(handler, call);
    }

    private static bool Needs(DirectCall call)
    {
        var note = FlowNotes.Find(call.SyntaxPath, call.Name);
        return string.IsNullOrWhiteSpace(note?.Title) && string.IsNullOrWhiteSpace(note?.Purpose);
    }

    private static bool SamePath(string? left, string? right) =>
        string.Equals(left ?? "", right ?? "", StringComparison.Ordinal);

    private static DirectCall FromSymbol(CodeSymbol symbol) =>
        new(symbol.SimpleName, symbol.SyntaxPath, symbol.Signature, symbol.FilePath, symbol.StartLine,
            true, PatternAPhase.Execute, CallKind.Domain, symbol.Project, "");
}
