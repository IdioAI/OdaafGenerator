namespace OdaafGenerator.Core;

public static class FlowNotes
{
    public sealed record Note(string Purpose, IReadOnlyDictionary<string, string> Parameters, string Documents, string Title = "");

    private static readonly Dictionary<string, string> Titles = new(StringComparer.Ordinal)
    {
        ["ExecuteSemiAutoAddGroupingInfo"] = "加载配对",
        ["ExecuteWriteParamsToPcb"] = "参数写入",
        ["TryArchiveSemiAutoAssets"] = "归档现场",
        ["BeginSemiAutoWorkflow"] = "开始顺序",
        ["ApplySemiAutoSanitizeAndArchive"] = "清洗归档",
        ["EndSemiAutoWorkflow"] = "结束顺序",
        ["SemiExportPcbFile"] = "导出PCB",
        ["GetFusionContextForSemiAuto"] = "取半自动融合",
        ["GetFusionContextForActive"] = "取当前融合",
        ["GetActiveContext"] = "取当前文档",
        ["GetOrCreateContextByFilePath"] = "按路径取文档",
        ["SetActiveContext"] = "设为当前",
        ["ClearContextData"] = "清空上下文",
        ["NavigateToView"] = "打开视图",
        ["EnsureContextLogicLayer"] = "备好逻辑层",
        ["SwitchToContext"] = "切换上下文",
        ["ReportSemiAutoProgress"] = "报告进度",
        ["ImportXmlFromFile"] = "导入XML",
        ["PrepareXmlAndPcbForProcessing"] = "补齐文件",
        ["CreateFusionContextAndMatchComponents"] = "融合匹配",
        ["PerformLandMatchingAndGrouping"] = "配盘分组",
        ["GetAllPlacements"] = "取元件放置",
        ["SetParseResult"] = "记下解析",
        ["LoadPcbDataFromParseResult"] = "装入PCB",
        ["MarkDataLoaded"] = "标记已装入",
        ["UpdateFilePath"] = "记下路径",
        ["BuildSanitizeLevelsFromProcessOptions"] = "转清洗级别",
        ["FormatSemiAutoSanitizeRules"] = "写下清洗规则",
        ["FormatSemiAutoWriteRules"] = "写下写入规则",
        ["SemiAutoWorkflowEnter"] = "标记开始",
        ["SemiAutoWorkflowLeave"] = "标记结束",
        ["EnsureToolsBound"] = "绑定刀具",
        ["SeedStubCatalogFromPcb"] = "播种算法",
        ["DisableCustomModeWhenNotAllowed"] = "关闭自定义",
        ["TryGetFirstCriterion8161And8164FromPcb"] = "读取背钻底稿",
        ["AddBackdrillParametersToPadsAsync"] = "写入背钻",
        ["ApplyZeroZExtractSearchRangesIfEnabled"] = "搜索范围填零",
        ["ShowHalfAutoGroupingDetails"] = "显示分组",
        ["NotifyNewSpecCreatorToRefresh"] = "刷新视图",
        ["ExecuteAddZExtractionToCurrentPcbAsync"] = "放置Z抽出",
        ["ApplyExpansion"] = "扩展ISU",
        ["ZCorrectionPlacementService.Place"] = "放置Z补正",
        ["ToZCorrectionPlaceOptions"] = "转补正选项",
        ["ApplyToTagParams"] = "改写算法项",
        ["BuildCreateOrGetZExtractCriterionItemCallback"] = "准备算法项",
        ["UpdateNodeTextRecursive"] = "写回文本",
        ["SavePcbFileDirectly"] = "直接保存",
        ["ShowZExtractionPlacementFailureMessage"] = "提示失败",
        ["ReadVersionFromFile"] = "读出版本"
    };

    private static readonly Dictionary<string, Note> ByName = new(StringComparer.Ordinal)
    {
        ["ExecuteSemiAutoAddGroupingInfo"] = new(
            "加载 PCB 与 XML 对：导入 XML，解析 PCB，匹配元件并按刀具分组。这一步不写背钻和 Z。",
            new Dictionary<string, string>(),
            "半自动说明 pcb-semi-auto。配对窗确认后不再中途选文件。背钻、Z 抽出、ISU 留到「参数写入PCB」。"),
        ["ExecuteWriteParamsToPcb"] = new(
            "把已加载配对上的背钻、Z 抽出、ISU 和 Z 补正写入当前 PCB。",
            new Dictionary<string, string>(),
            "半自动说明 pcb-semi-auto。参数写入须先完成「加载PCB与XML对」。这一步不在中途再选文件。"),
        ["TryArchiveSemiAutoAssets"] = new(
            "写入开始前，按原因把当前 XML 和 PCB 归档。目录没配则跳过。",
            new Dictionary<string, string>
            {
                ["reason"] = "归档原因，例如 before-start:WriteParamsToPcb。",
                ["xmlFilePath"] = "当前 XML 路径，只用于归档。",
                ["originalPcbPath"] = "写入前的 PCB 路径。",
                ["generatedZPcbPath"] = "已经生成的 Z 抽出 PCB。没有则为空。"
            },
            ""),
        ["BeginSemiAutoWorkflow"] = new(
            "按顺序记下这一次半自动要走的步骤。这里没有排程器。",
            new Dictionary<string, string>
            {
                ["reason"] = "进度窗标题。",
                ["mode"] = "这一次是加载分组还是参数写入。"
            },
            ""),
        ["ApplySemiAutoSanitizeAndArchive"] = new(
            "按对话框勾选清洗 PCB，并把清洗后的文件存到 origin。",
            new Dictionary<string, string>
            {
                ["dataManager"] = "当前半自动文档的 PCB 数据。",
                ["parseResult"] = "融合文档上的解析结果。",
                ["pcbFilePath"] = "要归档和清洗的 PCB 路径。",
                ["levels"] = "由对话框勾选转成的清洗级别。",
                ["archiveFixed"] = "是否把清洗后的副本写入 origin。",
                ["fusionDoc"] = "这一对 PCB 与 XML 的融合结果。"
            },
            ""),
        ["EndSemiAutoWorkflow"] = new(
            "结束这一次顺序标记，并留下进度窗一会儿。",
            new Dictionary<string, string>
            {
                ["keepVisibleSeconds"] = "进度窗再留几秒。"
            },
            ""),
        ["SemiExportPcbFile"] = new(
            "人确认保存后，把当前半自动上下文写成 PCB 文件。",
            new Dictionary<string, string>
            {
                ["fusionDoc"] = "这一对 PCB 与 XML 的融合结果。"
            },
            ""),
        ["GetFusionContextForSemiAuto"] = new(
            "取出已经加载好的半自动融合结果。没有它就不能写参数。",
            new Dictionary<string, string>(),
            ""),
        ["GetFusionContextForActive"] = new(
            "取当前文档上的融合结果，用来对上半自动文档名。",
            new Dictionary<string, string>(),
            ""),
        ["GetActiveContext"] = new(
            "取当前打开的文档上下文。",
            new Dictionary<string, string>(),
            ""),
        ["GetOrCreateContextByFilePath"] = new(
            "按 PCB 路径取得或新建半自动文档。一个文档只处理一块板。",
            new Dictionary<string, string>
            {
                ["contextType"] = "这里是半自动。",
                ["filePath"] = "PCB 文件路径。"
            },
            ""),
        ["SetActiveContext"] = new(
            "把这份半自动文档设为当前文档。",
            new Dictionary<string, string>
            {
                ["context"] = "要激活的文档。"
            },
            ""),
        ["ClearContextData"] = new(
            "清掉该上下文里上一次留下的数据。",
            new Dictionary<string, string>
            {
                ["contextType"] = "要清空的上下文类型。"
            },
            ""),
        ["NavigateToView"] = new(
            "打开新料号视图，用来看这一对 PCB 和 XML。",
            new Dictionary<string, string>
            {
                ["viewName"] = "要打开的视图名。",
                ["contextType"] = "视图所属的上下文。"
            },
            ""),
        ["EnsureContextLogicLayer"] = new(
            "保证这个文档已经有逻辑层，后面的导入和匹配都写在这层上。",
            new Dictionary<string, string>
            {
                ["context"] = "半自动文档。",
                ["contextType"] = "半自动。"
            },
            ""),
        ["SwitchToContext"] = new(
            "切到半自动上下文。路径为空时用当前已有的那一份。",
            new Dictionary<string, string>
            {
                ["contextType"] = "要切入的上下文类型。",
                ["documentId"] = "文档名。为空时用该类型下已有的一份。"
            },
            ""),
        ["ReportSemiAutoProgress"] = new(
            "更新半自动进度条上的百分比和文字。",
            new Dictionary<string, string>
            {
                ["percent"] = "进度，0 到 100。",
                ["text"] = "当前步骤说明。"
            },
            ""),
        ["ImportXmlFromFile"] = new(
            "把 XML 导入指定上下文。半自动这里固定导入 SemiAuto。",
            new Dictionary<string, string>
            {
                ["filePath"] = "XML 文件路径。",
                ["clearExistingData"] = "导入前是否清掉已有数据。",
                ["contextType"] = "导入到哪个上下文。"
            },
            ""),
        ["PrepareXmlAndPcbForProcessing"] = new(
            "对话框没有给 PCB 路径时，补齐 XML 和 PCB，供后面融合使用。",
            new Dictionary<string, string>
            {
                ["logicLayer"] = "当前半自动逻辑层。",
                ["configurationService"] = "上次打开目录等配置。",
                ["importXmlFromFile"] = "把 XML 导入半自动上下文的适配函数。"
            },
            ""),
        ["CreateFusionContextAndMatchComponents"] = new(
            "解析 PCB，并按元件名和 XML 做匹配。",
            new Dictionary<string, string>
            {
                ["pcbFilePath"] = "要解析的 PCB。",
                ["logicLayer"] = "承接解析结果的逻辑层。",
                ["currentFusionContext"] = "输出的融合上下文，供后面按文档保存。",
                ["onPcbLoaded"] = "PCB 装入后的回调，用来分开加载和名称匹配。"
            },
            ""),
        ["PerformLandMatchingAndGrouping"] = new(
            "按元件配焊盘，再按刀具分组。",
            new Dictionary<string, string>
            {
                ["fusionDoc"] = "这一对 PCB 与 XML 的融合结果。",
                ["businessPlacements"] = "逻辑层里的元件放置。",
                ["matchedComponentNames"] = "已经对上名的元件。",
                ["logicLayer"] = "当前半自动逻辑层。",
                ["createLandMatchPairs"] = "按元件创建焊盘配对的函数。",
                ["exportXmlData"] = "刀具缺少 SID 时导出 XML 的函数。",
                ["progressNote"] = "配对过程中的进度文字。",
                ["onLandPairsCreated"] = "配对完成后的回调，用来进入刀具分组。"
            },
            ""),
        ["GetAllPlacements"] = new(
            "取出逻辑层里的元件放置，供焊盘配对使用。",
            new Dictionary<string, string>(),
            ""),
        ["SetParseResult"] = new(
            "把融合得到的解析结果交给半自动文档。",
            new Dictionary<string, string>
            {
                ["parseResult"] = "PCB 解析结果。"
            },
            ""),
        ["LoadPcbDataFromParseResult"] = new(
            "用解析结果装入 PCB 数据，供后面的清洗和写入使用。",
            new Dictionary<string, string>
            {
                ["parseResult"] = "已经融合好的 PCB 解析结果。"
            },
            ""),
        ["MarkDataLoaded"] = new(
            "标记这份 PCB 数据已经装入，避免再清空重载。",
            new Dictionary<string, string>(),
            ""),
        ["UpdateFilePath"] = new(
            "把当前 PCB 路径记到半自动文档上。",
            new Dictionary<string, string>
            {
                ["filePath"] = "PCB 文件路径。",
                ["pcbFilePath"] = "PCB 文件路径。"
            },
            ""),
        ["BuildSanitizeLevelsFromProcessOptions"] = new(
            "把对话框里的清洗勾选变成清洗级别。",
            new Dictionary<string, string>
            {
                ["options"] = "过程输入窗返回的选项。"
            },
            ""),
        ["FormatSemiAutoSanitizeRules"] = new(
            "把清洗勾选写成一行，记进顺序日志。",
            new Dictionary<string, string>
            {
                ["options"] = "过程输入窗返回的选项。"
            },
            ""),
        ["FormatSemiAutoWriteRules"] = new(
            "把背钻、Z 抽出、ISU、Z 补正的勾选写成一行。",
            new Dictionary<string, string>
            {
                ["options"] = "参数写入窗返回的选项。"
            },
            ""),
        ["SemiAutoWorkflowEnter"] = new(
            "标记顺序里的一步开始。",
            new Dictionary<string, string>
            {
                ["nodeId"] = "步骤编号，例如 SA.dialog。",
                ["note"] = "这一步在进度里显示的名称。"
            },
            ""),
        ["SemiAutoWorkflowLeave"] = new(
            "标记这一步结束，并写下结果。",
            new Dictionary<string, string>
            {
                ["nodeId"] = "步骤编号。",
                ["status"] = "完成、跳过或失败。",
                ["note"] = "结果摘要，例如文件名或焊盘数。"
            },
            ""),
        ["EnsureToolsBound"] = new(
            "让 stub 目录绑上当前刀具，写入前刀具和算法项对得上。",
            new Dictionary<string, string>
            {
                ["tools"] = "当前逻辑层里的刀具。"
            },
            ""),
        ["SeedStubCatalogFromPcb"] = new(
            "从 PCB 里已有的 8161 和 8164 给 stub 目录播种。",
            new Dictionary<string, string>
            {
                ["catalog"] = "刀具 stub 目录。",
                ["dataManager"] = "当前文档的 PCB 数据。",
                ["pcbVersion"] = "PCB 版本，用来取变量表。",
                ["overwriteEmptyOnly"] = "为真时只填 stub 里还空着的槽。"
            },
            ""),
        ["DisableCustomModeWhenNotAllowed"] = new(
            "不允许自定义背钻时，去掉刀具上的自定义模式，焊盘仍共用原来的算法组。",
            new Dictionary<string, string>
            {
                ["tools"] = "准备写入的刀具。",
                ["allowCustomBackdrill"] = "工程是否允许自定义背钻。"
            },
            ""),
        ["TryGetFirstCriterion8161And8164FromPcb"] = new(
            "读取 PCB 里第一项背钻算法 8161 和正钻算法 8164，作为没有 stub 时的底稿。",
            new Dictionary<string, string>
            {
                ["dataManager"] = "当前文档的 PCB 数据。",
                ["first8161"] = "输出的第一项背钻算法 8161。",
                ["first8164"] = "输出的第一项正钻算法 8164。"
            },
            ""),
        ["AddBackdrillParametersToPadsAsync"] = new(
            "按刀具把 8161 和 8164 写到焊盘上。",
            new Dictionary<string, string>
            {
                ["fusionDoc"] = "融合结果，含焊盘和刀具组。",
                ["tools"] = "要写入的刀具。",
                ["logicLayer"] = "当前半自动逻辑层。",
                ["pcbFilePath"] = "当前 PCB 路径。",
                ["contextType"] = "写入所在的上下文，这里是半自动。",
                ["useUnifyStrategy"] = "是否用统一策略。半自动这里为假。",
                ["toolParametersDict"] = "每把刀具对应的 8161 和 8164 参数。"
            },
            ""),
        ["ApplyZeroZExtractSearchRangesIfEnabled"] = new(
            "勾选了 Z 抽出数据填零时，写入前把搜索范围填成零。",
            new Dictionary<string, string>
            {
                ["tools"] = "要回填的刀具。",
                ["pcbVersion"] = "PCB 版本。"
            },
            ""),
        ["ShowHalfAutoGroupingDetails"] = new(
            "在系统信息窗显示这次分组的元件数、刀具组和焊盘对数。",
            new Dictionary<string, string>
            {
                ["details"] = "分组结果摘要。"
            },
            ""),
        ["NotifyNewSpecCreatorToRefresh"] = new(
            "让当前新料号视图按逻辑层重新装入并重画。",
            new Dictionary<string, string>
            {
                ["contextType"] = "要刷新的上下文，这里是半自动。"
            },
            ""),
        ["ExecuteAddZExtractionToCurrentPcbAsync"] = new(
            "按刀具在当前 PCB 上放置 Z 抽出窗。优先用 XML 里已有的窗，没有再走算法。",
            new Dictionary<string, string>
            {
                ["fusionDoc"] = "融合结果。",
                ["logicLayer"] = "当前半自动逻辑层。",
                ["configurationService"] = "板厚等配置。",
                ["pcbFilePath"] = "当前 PCB 路径。",
                ["shouldClearZExtraction"] = "放置前是否清掉已有 Z 抽出。",
                ["contextType"] = "半自动。",
                ["createOrGetCriterionItemCallback"] = "按刀具取得或新建 Z 抽出算法项。",
                ["updateNodeTextCallback"] = "写回节点文本。",
                ["savePcbFileCallback"] = "中途保存 PCB 的函数。半自动这里最终另存。",
                ["finalResultSavePath"] = "指定最终保存路径。半自动这里为空。",
                ["zExtractWinWidthMm"] = "Z 抽出窗宽，毫米。",
                ["zExtractWinHeightMm"] = "Z 抽出窗高，毫米。",
                ["preferXmlZExtraction"] = "是否优先使用 XML 里的 Z 抽出窗。",
                ["xmlZExtractionWindows"] = "从 XML 解析出的 Z 抽出窗。没有则为空。",
                ["zExtractionWindowsPerIsu"] = "每个 ISU 放几个 Z 抽出窗。"
            },
            ""),
        ["ApplyExpansion"] = new(
            "把参与配对的 ISU 向外扩大。未参与配对的 ISU 不动。",
            new Dictionary<string, string>
            {
                ["fusionDoc"] = "已经配对分组的融合结果。",
                ["expandMm"] = "向外扩展量，毫米。"
            },
            ""),
        ["ZCorrectionPlacementService.Place"] = new(
            "按背钻分布在 PCB 上放置 Z 补正点。",
            new Dictionary<string, string>
            {
                ["parseResult"] = "当前 PCB 解析结果。",
                ["dataManager"] = "当前文档的 PCB 数据。",
                ["options"] = "清除原有、插入或追加等放置选项。"
            },
            ""),
        ["ToZCorrectionPlaceOptions"] = new(
            "把参数写入窗里的 Z 补正勾选转成放置选项。",
            new Dictionary<string, string>(),
            ""),
        ["ApplyToTagParams"] = new(
            "自定义背钻开启时，按刀具方向改写 8161 和 8164。关闭时原样返回。",
            new Dictionary<string, string>
            {
                ["tool"] = "当前刀具。",
                ["tagParams"] = "这把刀要写入的 8161 和 8164。"
            },
            ""),
        ["BuildCreateOrGetZExtractCriterionItemCallback"] = new(
            "准备按刀具创建或取得 Z 抽出算法项的函数。写入前必须带上 PCB 版本。",
            new Dictionary<string, string>
            {
                ["tools"] = "当前刀具。",
                ["pcbVersion"] = "PCB 版本。"
            },
            ""),
        ["UpdateNodeTextRecursive"] = new(
            "按融合结果把节点文本写回，供保存 PCB 使用。",
            new Dictionary<string, string>
            {
                ["nodes"] = "要更新的节点。",
                ["node"] = "当前这一个节点。",
                ["fusionDoc"] = "融合结果，用来对上焊盘。",
                ["updatedCount"] = "已经更新的节点数。",
                ["pairByWinId"] = "按窗号索引的焊盘配对。"
            },
            ""),
        ["SavePcbFileDirectly"] = new(
            "按给定路径把解析结果写成 PCB 文件。",
            new Dictionary<string, string>
            {
                ["parseResult"] = "要保存的解析结果。",
                ["filePath"] = "保存路径。",
                ["contextType"] = "保存时所在的上下文。",
                ["fusionDoc"] = "融合结果。可以为空。"
            },
            ""),
        ["ShowZExtractionPlacementFailureMessage"] = new(
            "弹出 Z 抽出放置失败的提示。人只看结果。",
            new Dictionary<string, string>
            {
                ["errorMessage"] = "失败原因。"
            },
            ""),
        ["ReadVersionFromFile"] = new(
            "从 PCB 文件头读出版本号。融合结果里没有有效版本时用它。",
            new Dictionary<string, string>
            {
                ["pcbFilePath"] = "PCB 文件路径。"
            },
            "")
    };

    public static Note? Find(string? syntaxPath, string name)
    {
        if (!string.IsNullOrEmpty(syntaxPath))
        {
            var simple = SyntaxName.Last(syntaxPath);
            var at = simple.IndexOf('@');
            if (at > 0)
                simple = simple[..at];
            var head = at > 0 ? syntaxPath[..syntaxPath.LastIndexOf('@')] : syntaxPath;
            var dot = head.LastIndexOf('.');
            if (dot > 0)
            {
                var typeDot = head.LastIndexOf('.', dot - 1);
                if (typeDot >= 0)
                {
                    var qualifiedName = head[(typeDot + 1)..];
                    if (ByName.TryGetValue(qualifiedName, out var qualified))
                        return WithTitle(qualifiedName, qualified);
                }
            }

            if (ByName.TryGetValue(simple, out var byPath))
                return WithTitle(simple, byPath);
        }

        return ByName.TryGetValue(name, out var note) ? WithTitle(name, note) : null;
    }

    private static Note WithTitle(string key, Note note) =>
        string.IsNullOrEmpty(note.Title) && Titles.TryGetValue(key, out var title)
            ? note with { Title = title }
            : note;

    public static IReadOnlyList<ParameterLeaf> WithMeanings(string? syntaxPath, string name, IReadOnlyList<ParameterLeaf> leaves)
    {
        var note = Find(syntaxPath, name);
        if (note is null || note.Parameters.Count == 0)
            return leaves;
        return leaves.Select(leaf => note.Parameters.TryGetValue(leaf.Name, out var meaning)
            ? leaf with { Meaning = meaning }
            : leaf).ToList();
    }
}
