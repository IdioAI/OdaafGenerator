namespace OdaafGenerator.Core;

public static class LayerMarkdown
{
    public static string Render(LayerBrief brief, CallLayerGraph layer)
    {
        var lines = new List<string>
        {
            $"# {brief.MenuTitle}",
            "",
            brief.ContextText,
            "",
            "## 输入",
            "",
            $"- 全局，本机：{brief.MachineInput}",
            $"- 上下文，这一对板：{brief.BoardInput}",
            $"- 这一次勾选：{brief.ChoiceInput}",
            "",
            "## DNA",
            "",
            brief.DnaText,
            "",
            "## 执行",
            ""
        };
        foreach (var row in brief.Execute)
            lines.Add($"- {row.FriendlyName}（`{row.MethodName}`）：{row.Purpose}");
        lines.Add("");
        lines.Add("```mermaid");
        lines.Add("flowchart LR");
        var segments = Segments(brief.Handler, layer);
        if (segments.Count == 0)
            lines.Add("  empty[\"无执行段\"]");
        else
        {
            for (var i = 0; i < segments.Count; i++)
                lines.Add($"  seg{i}[\"{segments[i]}\"]");
            for (var i = 0; i < segments.Count - 1; i++)
                lines.Add($"  seg{i} --> seg{i + 1}");
        }

        lines.Add("```");
        lines.Add("");
        return string.Join(Environment.NewLine, lines);
    }

    public static string FileName(string handler) =>
        handler == "ExecuteWriteParamsToPcb" ? "execute-write-params.md" : "execute-load-pair.md";

    public static IReadOnlyList<string> Segments(string handler, CallLayerGraph layer)
    {
        var present = layer.Calls
            .Where(call => call.Phase == PatternAPhase.Execute && call.Kind != CallKind.System)
            .Select(call => FlowGroups.NameOf(handler, call.SyntaxPath, call.Name))
            .ToHashSet(StringComparer.Ordinal);
        return FlowGroups.Order(handler).Where(present.Contains).ToList();
    }

    public static void Write(string directory, LayerBrief brief, CallLayerGraph layer)
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, FileName(brief.Handler)), Render(brief, layer));
    }
}
