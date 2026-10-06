namespace OdaafGenerator.Core;

public sealed record ExecuteRow(string FriendlyName, string MethodName, string Purpose, string SyntaxPath, bool Unindexed);

public sealed class LayerBrief
{
    public required string Handler { get; init; }
    public required string MenuTitle { get; init; }
    public required string ContextText { get; init; }
    public required string MachineInput { get; init; }
    public required string BoardInput { get; init; }
    public required string ChoiceInput { get; init; }
    public required string DnaText { get; init; }
    public required IReadOnlyList<ExecuteRow> Execute { get; init; }

    public static LayerBrief? For(string handler, CallLayerGraph layer)
    {
        if (!FlowGroups.Has(handler))
            return null;

        var write = handler == "ExecuteWriteParamsToPcb";
        var rows = layer.Calls
            .Where(call => call.Phase == PatternAPhase.Execute && call.Kind != CallKind.System)
            .Select(call => Row(layer.Caller.SyntaxPath, call))
            .ToList();
        return new LayerBrief
        {
            Handler = handler,
            MenuTitle = write ? "参数写入PCB" : "加载PCB与XML对",
            ContextText = write
                ? "工作在 SemiAuto。一份文档只处理一块板。必须先完成「加载PCB与XML对」，使用已有融合结果。管道中途不再选文件。"
                : "工作在 SemiAuto。一份文档只处理一块板。这一步导入 XML、匹配并分组，不写背钻和 Z。管道中途不再选文件。",
            MachineInput = "semi_auto_process_config.json 是本机对话框的默认值，不是这块板的事实。",
            BoardInput = ".prodat4semi、已打开的 SemiAuto 文档，以及融合结果里的 PCB 路径和 XML 路径，属于这一对板。加载配对写入它们。参数写入只读，路径在窗上只显示。",
            ChoiceInput = "ProcessInputOptions 是这一次人在过程输入窗里的勾选：清洗、背钻、Z 抽出、ISU 扩展、Z 补正。",
            DnaText = "未在本层直接读取。系统 DNA（properties.csv 的 MachineVariable）是全机配置，不是 SemiAuto 上下文。",
            Execute = rows
        };
    }

    public static string CardPath(string callerPath, DirectCall call) =>
        (call.SyntaxPath ?? callerPath + "." + call.Name) + "@" + call.Phase;

    private static ExecuteRow Row(string callerPath, DirectCall call)
    {
        var note = FlowNotes.Find(call.SyntaxPath, call.Name);
        var unindexed = call.Kind == CallKind.Unindexed;
        return new ExecuteRow(
            string.IsNullOrWhiteSpace(note?.Title) ? call.Name : note.Title,
            call.Name,
            unindexed ? "未收入本期索引。" : note?.Purpose ?? call.Purpose,
            CardPath(callerPath, call),
            unindexed);
    }
}
