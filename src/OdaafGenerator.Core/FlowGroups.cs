namespace OdaafGenerator.Core;

public static class FlowGroups
{
    public const string Other = "其余";

    private static readonly string[] WriteOrder =
    [
        "归档与顺序", "清洗", "背钻", "Z抽出", "ISU", "Z补正", "写回", Other
    ];

    private static readonly string[] LoadOrder =
    [
        "配对窗", "文档", "导入", "融合匹配", "配盘分组", "清洗", "刷新", Other
    ];

    private static readonly Dictionary<string, string> Write = new(StringComparer.Ordinal)
    {
        ["TryArchiveSemiAutoAssets"] = "归档与顺序",
        ["BeginSemiAutoWorkflow"] = "归档与顺序",
        ["SemiAutoWorkflowEnter"] = "归档与顺序",
        ["SemiAutoWorkflowLeave"] = "归档与顺序",
        ["EndSemiAutoWorkflow"] = "归档与顺序",
        ["BuildSanitizeLevelsFromProcessOptions"] = "清洗",
        ["ApplySemiAutoSanitizeAndArchive"] = "清洗",
        ["LoadPcbDataFromParseResult"] = "清洗",
        ["DisableCustomModeWhenNotAllowed"] = "背钻",
        ["TryGetFirstCriterion8161And8164FromPcb"] = "背钻",
        ["EnsureToolsBound"] = "背钻",
        ["SeedStubCatalogFromPcb"] = "背钻",
        ["AddBackdrillParametersToPadsAsync"] = "背钻",
        ["ApplyZeroZExtractSearchRangesIfEnabled"] = "Z抽出",
        ["ExecuteAddZExtractionToCurrentPcbAsync"] = "Z抽出",
        ["ApplyExpansion"] = "ISU",
        ["ToZCorrectionPlaceOptions"] = "Z补正",
        ["ZCorrectionPlacementService.Place"] = "Z补正",
        ["NotifyNewSpecCreatorToRefresh"] = "写回",
        ["SemiExportPcbFile"] = "写回"
    };

    private static readonly Dictionary<string, string> Load = new(StringComparer.Ordinal)
    {
        ["TryArchiveSemiAutoAssets"] = "配对窗",
        ["BeginSemiAutoWorkflow"] = "配对窗",
        ["SemiAutoWorkflowEnter"] = "配对窗",
        ["SemiAutoWorkflowLeave"] = "配对窗",
        ["FormatSemiAutoSanitizeRules"] = "配对窗",
        ["GetOrCreateContextByFilePath"] = "文档",
        ["SetActiveContext"] = "文档",
        ["ClearContextData"] = "文档",
        ["NavigateToView"] = "文档",
        ["EnsureContextLogicLayer"] = "文档",
        ["SwitchToContext"] = "文档",
        ["GetActiveContext"] = "文档",
        ["ImportXmlFromFile"] = "导入",
        ["PrepareXmlAndPcbForProcessing"] = "导入",
        ["ReportSemiAutoProgress"] = "导入",
        ["CreateFusionContextAndMatchComponents"] = "融合匹配",
        ["PerformLandMatchingAndGrouping"] = "配盘分组",
        ["GetAllPlacements"] = "配盘分组",
        ["BuildSanitizeLevelsFromProcessOptions"] = "清洗",
        ["ApplySemiAutoSanitizeAndArchive"] = "清洗",
        ["LoadPcbDataFromParseResult"] = "清洗",
        ["SetParseResult"] = "清洗",
        ["MarkDataLoaded"] = "清洗",
        ["UpdateFilePath"] = "清洗",
        ["EnsureToolsBound"] = "清洗",
        ["SeedStubCatalogFromPcb"] = "清洗",
        ["ShowHalfAutoGroupingDetails"] = "刷新",
        ["NotifyNewSpecCreatorToRefresh"] = "刷新",
        ["EndSemiAutoWorkflow"] = "刷新"
    };

    public static bool Has(string? handler) =>
        handler is "ExecuteWriteParamsToPcb" or "ExecuteSemiAutoAddGroupingInfo";

    public static IReadOnlyList<string> Order(string handler) =>
        handler == "ExecuteSemiAutoAddGroupingInfo" ? LoadOrder : WriteOrder;

    public static string NameOf(string handler, string? syntaxPath, string name)
    {
        var table = handler == "ExecuteSemiAutoAddGroupingInfo" ? Load : Write;
        var key = KeyOf(syntaxPath, name);
        if (table.TryGetValue(key, out var segment))
            return segment;
        if (table.TryGetValue(name, out segment))
            return segment;
        return Other;
    }

    public static string KeyOf(string? syntaxPath, string name)
    {
        if (!string.IsNullOrEmpty(syntaxPath)
            && syntaxPath.Contains("ZCorrectionPlacementService.Place", StringComparison.Ordinal))
            return "ZCorrectionPlacementService.Place";
        var simple = string.IsNullOrEmpty(syntaxPath) ? name : SyntaxName.Last(syntaxPath);
        var at = simple.IndexOf('@');
        return at > 0 ? simple[..at] : simple;
    }
}
