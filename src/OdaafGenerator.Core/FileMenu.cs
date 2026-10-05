namespace OdaafGenerator.Core;

public sealed record GraphEdge(string From, string To);

public sealed record MenuCommand(string Header, string MethodName, string Group, double X, double Y);

public static class IndexContext
{
    public const string Id = "index";
    public static IReadOnlyList<string> Commands { get; } = [];
}

public static class OdaafFileContext
{
    public const string Id = "odaaf-file";
}

public static class FileMenu
{
    public const string ImportXmlMethod = "ExecuteImportXmlData";

    public static readonly MenuCommand[] Commands =
    [
        new("导入CNC", "ExecuteImportCnc", "导入 CNC", 24, 36),
        new("导入背钻和通钻", "ExecuteImportBackdrillAndThroughHole", "导入 CNC", 24, 132),
        new("导入XML", ImportXmlMethod, "导入与导出 XML", 280, 36),
        new("上次导入的 XML 摘要", "ExecuteShowLastXmlImportInfo", "导入与导出 XML", 280, 132),
        new("导入SMT XML", "ExecuteImportSmtXmlData", "导入与导出 XML", 280, 228),
        new("导出XML", "ExecuteExportXmlData", "导入与导出 XML", 280, 324),
        new("导出到资产目录", "ExecuteExportXmlToAssetDirectory", "导入与导出 XML", 280, 420),
        new("仅导出人工元件到XML", "ExecuteExportXmlOnlyCustomComponents", "导入与导出 XML", 280, 516),
        new("高级导出XML", "ExecuteAdvancedExportXml", "导入与导出 XML", 280, 612),
        new("高级导入", "ExecuteAdvancedImport", "高级导入与生成 OMR", 536, 36),
        new("生成OMR", "ExecuteGenerateOmr", "高级导入与生成 OMR", 536, 132)
    ];

    public static (IReadOnlyList<FunctionCard> Cards, IReadOnlyList<GraphEdge> Edges) Build(Catalog catalog, LayoutFile layout)
    {
        var cards = new List<FunctionCard>(Commands.Length);
        foreach (var command in Commands)
        {
            var symbol = catalog.FindMethod(command.MethodName);
            var syntaxPath = symbol?.SyntaxPath ?? "BHEditor.ViewModels.MainWindowViewModel." + command.MethodName;
            var placed = layout.Find(syntaxPath);
            cards.Add(new FunctionCard
            {
                SyntaxPath = syntaxPath,
                Name = command.Header,
                StepLabel = command.Group,
                Project = symbol?.Project ?? "",
                Signature = symbol?.Signature ?? "",
                FilePath = symbol?.FilePath ?? "",
                StartLine = symbol?.StartLine ?? 0,
                EndLine = symbol?.EndLine ?? 0,
                Materials = SignatureParser.Parameters(symbol?.Signature),
                Note = symbol is null ? "索引里没有对上这个方法。" : null,
                OnCanvas = true,
                OpensZExtract = command.MethodName == ImportXmlMethod,
                X = placed?.X ?? command.X,
                Y = placed?.Y ?? command.Y,
                Formal = placed?.Formal ?? "",
                Final = placed?.Final ?? ""
            });
        }

        var edges = new List<GraphEdge>();
        foreach (var group in cards.GroupBy(card => card.StepLabel))
        {
            var ordered = group.OrderBy(card => card.Y).ToList();
            for (var i = 1; i < ordered.Count; i++)
                edges.Add(new GraphEdge(ordered[i - 1].SyntaxPath, ordered[i].SyntaxPath));
        }

        return (cards, edges);
    }
}
