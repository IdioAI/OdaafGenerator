namespace OdaafGenerator.Core;

public sealed record ShellMenu(string Menu, string Header, string MethodName, string? Submenu = null);

public static class ShellMenus
{
    public static readonly ShellMenu[] Entries =
    [
        ..FileMenu.Commands.Select(command => new ShellMenu("文件", command.Header, command.MethodName)),
        new("文件", "导入配置参数", "ExecuteShowConfigDialog"),
        new("文件", "修改系统参数", "ExecuteShowSystemParameterConfig"),
        new("文件", "保存默认配置", "ExecuteExportConfig"),
        new("文件", "重置系统配置", "ExecuteResetSystemConfig"),
        new("文件", "退出", "ExecuteExit"),

        new("CNC上下文", "打开工程索引文件", "ExecuteOpenProjectIndex"),
        new("CNC上下文", "设置工作区主XML", "ExecuteSetWorkspaceMainXml"),
        new("CNC上下文", "切换到Z抽出工作区", "ExecuteSwitchToZExtractStudio"),
        new("CNC上下文", "保存当前CNC工作区", "ExecuteSaveActiveCncWorkspace"),

        new("视图", "PCB分析器", "ExecuteNavigateToPcbAnalyzer"),
        new("视图", "PCB比较工具", "ExecuteNavigateToPcbComparer"),
        new("视图", "导入XML", "ExecuteNavigateToXmlImporter"),
        new("视图", "CNC转换", "ExecuteNavigateToCncConverter"),
        new("视图", "料号编辑", "ExecuteNavigateToSpecEditor"),
        new("视图", "器件总览", "ExecuteNavigateToDeviceOverview"),
        new("视图", "新料号生成", "ExecuteNavigateToNewSpecCreator"),
        new("视图", "防焊绘制", "ExecuteNavigateToSolderMaskDrawer"),

        new("单文件处理", "导入CNC(在新上下文）", "ExecuteSingleFileImportCnc"),
        new("单文件处理", "附加CNC（单文件）", "ExecuteSingleFileAppendCnc"),
        new("单文件处理", "导入XML(在新上下文）", "ExecuteSingleFileImportXml"),
        new("单文件处理", "附加XML（单文件）", "ExecuteSingleFileAppendXml"),
        new("单文件处理", "附加XML(单文件)-高级", "ExecuteSingleFileAppendXmlAdvanced"),

        new("数据融合检查", "XML与PCB同源检查", "ExecuteCheckPcbXmlHomologous"),
        new("数据融合检查", "缺陷点定位", "ExecuteOpenInspectionDefect"),

        new("工程管理", "工程文件管理与编辑", "ExecuteOpenProjectManagement"),
        new("工程管理", "Panel拼版布局编辑", "ExecuteOpenPanelLayout"),
        new("工程管理", "panel缺陷点导出", "ExecuteOpenAoiDefectMatch"),
        new("工程管理", "Panel CNC与PCB比较", "ExecuteOpenPanelCncPcbCompare"),
        new("工程管理", "缺陷点匹配规则", "ExecuteOpenAoiDefectMatchRules"),
        new("工程管理", "RVS列对应", "ExecuteOpenRvsColumnMapping"),
        new("工程管理", "本文档合法性规则", "ExecuteOpenDocumentPcbLegalityRules"),
        new("工程管理", "合法性规则编辑", "ExecuteOpenPcbLegalityRules"),
        new("工程管理", "Z抽出算法规则", "ExecuteOpenZExtractAlgorithmRules"),
        new("工程管理", "工程索引规则", "ExecuteOpenProjectIndexRules"),
        new("工程管理", "保存项目", "ExecuteSaveProject"),
        new("工程管理", "保存当前CNC工作区", "ExecuteSaveActiveCncWorkspace"),

        new("PCB半自动处理", "加载PCB与XML对", "ExecuteSemiAutoAddGroupingInfo"),
        new("PCB半自动处理", "参数写入PCB", "ExecuteWriteParamsToPcb"),
        new("PCB半自动处理", "为当前PCB添加Z抽出", "ExecuteAddSemiZExtractionToCurrentPcb", "其他操作"),
        new("PCB半自动处理", "为当前PCB添加背钻参数", "ExecuteAddBackdrillParametersToCurrentPcb", "其他操作"),
        new("PCB半自动处理", "清除Z抽出", "ExecuteClearZExtractionInSemiAuto", "其他操作"),
        new("PCB半自动处理", "导出半自动上下文PCB", "ExecuteExportHalfAutoPcb"),
        new("PCB半自动处理", "从半自动工程索引加载", "ExecuteLoadSemiAutoProjectIndex"),
        new("PCB半自动处理", "保存为半自动工程索引", "ExecuteSaveSemiAutoProjectIndex"),
        new("PCB半自动处理", "半自动过程规则", "ExecuteOpenSemiAutoProcessRules"),

        new("添加Z抽出", "Z抽出工作台（RapidCAD）", "ExecuteOpenZExtractStudio"),
        new("添加Z抽出", "UniPcb Z抽出（FDE/Analyzer）", "ExecuteOpenUniPcb"),
        new("添加Z抽出", "Z抽出算法规则", "ExecuteOpenZExtractAlgorithmRules"),
        new("添加Z抽出", "Z抽出写入XML", "ExecuteWriteZExtractXml"),
        new("添加Z抽出", "另存XML", "ExecuteSaveZExtractXmlAs"),
        new("添加Z抽出", "当前工程导出PCB", "ExecuteExportPcbFromZExtractStudio"),

        new("XML转PCB", "新建XML导出PCB工程", "ExecuteCreateXmlToPcbGenProject"),
        new("XML转PCB", "XML导出PCB工具", "ExecuteGeneratePcbFromCurrentXml"),
        new("XML转PCB", "当前信息导出到PCB", "ExecuteExportCurrentInfoToPcb"),
        new("XML转PCB", "同步XML的Z抽出到OMRON格式", "ExecuteSyncZExtractToOmron"),
        new("XML转PCB", "导入XML", "ExecuteImportXmlData"),

        new("PCB文件", "导入PCB", "ExecuteImportPcb"),
        new("PCB文件", "加载PCB上下文工程索引", "ExecuteLoadPcbAnalyzerIndex"),
        new("PCB文件", "加载信号层（文件夹）", "ExecuteLoadSignalLayersFromFolder"),
        new("PCB文件", "加载信号层（文件）", "ExecuteLoadSignalLayersFromFile"),
        new("PCB文件", "从工程索引导入信号层", "ExecuteLoadSignalLayersFromProjectIndex"),
        new("PCB文件", "从 Teaching XML 导入信号层", "ExecuteLoadSignalLayersFromTeachingXml"),
        new("PCB文件", "高级PCB导入", "ExecuteAdvancedImportPcb"),
        new("PCB文件", "保存", "ExecuteSavePcb"),
        new("PCB文件", "导出PCB", "ExecuteExportPcb"),
        new("PCB文件", "另存为模板", "ExecuteSaveAsPcbTemplate"),
        new("PCB文件", "PCB合法性检查", "ExecuteRunPcbLegalityCheck"),
        new("PCB文件", "修复当前PCB", "ExecuteRepairCurrentPcb"),
        new("PCB文件", "按顺位规则重排算法项", "ExecuteReorderCriterionItemsByCanonicalOrder"),
        new("PCB文件", "删除Z抽出项", "ExecuteClearZExtractionInAnalyzerPcb"),
        new("PCB文件", "本文档合法性规则", "ExecuteOpenDocumentPcbLegalityRules"),
        new("PCB文件", "合法性规则编辑", "ExecuteOpenPcbLegalityRules"),
        new("PCB文件", "Z抽出算法规则", "ExecuteOpenZExtractAlgorithmRules"),
        new("PCB文件", "工程索引规则", "ExecuteOpenProjectIndexRules")
    ];

    public static readonly string[] TopMenus =
    [
        "文件", "CNC上下文", "视图", "单文件处理", "数据融合检查", "工程管理",
        "PCB半自动处理", "添加Z抽出", "XML转PCB", "PCB文件"
    ];
}
