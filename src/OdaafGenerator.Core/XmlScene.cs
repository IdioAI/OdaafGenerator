namespace OdaafGenerator.Core;

public sealed record XmlSlot(
    string SyntaxPath,
    string StepLabel,
    double X,
    double Y,
    string? InterfaceSyntaxPath);

public static class XmlScene
{
    public const string ContextId = "rapidcad-zextract-xml";

    public static readonly string[] FocusProjects =
    [
        "BHEditor.Modules.ZExtractStudio",
        "Baile.XmlGen.Xml",
        "Baile.XmlGen.Core",
        "Baile.XmlGen.Logic"
    ];

    public static readonly XmlSlot[] Slots =
    [
        new(
            "BHEditor.Modules.ZExtractStudio.Services.ZExtractXmlWriteBackService.TryWriteBack",
            "写回",
            24,
            48,
            null),
        new(
            "BHEditor.Modules.ZExtractStudio.Services.ZExtractXmlZWindowMerge.MergeInto",
            "合并窗",
            260,
            48,
            null),
        new(
            "Baile.XmlGen.Xml.XmlImporter.ParseZExtractionWindowCollection",
            "解析窗集合",
            496,
            48,
            null),
        new(
            "Baile.XmlGen.Core.Models.XmlImportResult.HasXmlZExtractionWindows",
            "判定有 Z",
            732,
            48,
            null),
        new(
            "Baile.XmlGen.Logic.LogicLayer.ImportXmlData",
            "导入内存",
            968,
            48,
            "Baile.XmlGen.Core.ILogicLayer.ImportXmlData"),
        new(
            "Baile.XmlGen.Core.EngineDep.ZExtractionFromXml.IsValid",
            "窗有效",
            732,
            200,
            null)
    ];

    public static readonly (string From, string To)[] Edges =
    [
        (Slots[0].SyntaxPath, Slots[1].SyntaxPath),
        (Slots[1].SyntaxPath, Slots[2].SyntaxPath),
        (Slots[2].SyntaxPath, Slots[3].SyntaxPath),
        (Slots[3].SyntaxPath, Slots[4].SyntaxPath),
        (Slots[3].SyntaxPath, Slots[5].SyntaxPath)
    ];

    public static bool IsFocusProject(string project) =>
        FocusProjects.Contains(project, StringComparer.Ordinal);

    public static bool IsCanvas(string syntaxPath) =>
        Slots.Any(slot => slot.SyntaxPath == syntaxPath);
}
