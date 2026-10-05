using OdaafGenerator.Core;

namespace OdaafGenerator.Core.Tests;

public class FileMenuTests
{
    [Fact]
    public void Commands_MatchTheElevenFileMenuHeaders()
    {
        Assert.Equal(
            [
                "导入CNC",
                "导入背钻和通钻",
                "导入XML",
                "上次导入的 XML 摘要",
                "导入SMT XML",
                "导出XML",
                "导出到资产目录",
                "仅导出人工元件到XML",
                "高级导出XML",
                "高级导入",
                "生成OMR"
            ],
            FileMenu.Commands.Select(command => command.Header));
        Assert.Empty(IndexContext.Commands);
        Assert.DoesNotContain(FileMenu.Commands, command => IndexContext.Commands.Contains(command.Header));
    }

    [Fact]
    public void ImportXml_ReadsSignatureParametersFromTheHostMethod()
    {
        const string signature = "private async void ExecuteImportXmlData(string path, bool reload)";
        var host = new[]
        {
            new RawDeclaration(
                "MainWindowViewModel.cs",
                20,
                null,
                "method",
                "BHEditor.ViewModels.MainWindowViewModel.ExecuteImportXmlData",
                signature,
                10,
                40,
                "BHEditor")
        };
        var catalog = Catalog.Create(["BHEditor"], [], host);
        var (cards, _) = FileMenu.Build(catalog, new LayoutFile());
        var import = Assert.Single(cards, card => card.Name == "导入XML");

        Assert.Equal(SignatureParser.Parameters(signature), import.Materials);
        Assert.Equal([new ParameterLeaf("string", "path"), new ParameterLeaf("bool", "reload")], import.Materials);
        Assert.True(import.OpensZExtract);
    }

    [Fact]
    public void RealIndex_ResolvesExecuteImportXmlDataParameters()
    {
        var path = DeclarationStore.DefaultDatabasePath;
        Assert.True(File.Exists(path), path);
        var (projects, rows, host) = DeclarationStore.Load(path);
        var catalog = Catalog.Create(projects, rows, host);
        var method = catalog.FindMethod(FileMenu.ImportXmlMethod);

        Assert.NotNull(method);
        var (cards, edges) = FileMenu.Build(catalog, new LayoutFile());
        var import = Assert.Single(cards, card => card.Name == "导入XML");
        Assert.Equal(SignatureParser.Parameters(method.Signature), import.Materials);
        Assert.Contains("ExecuteImportXmlData", method.Signature, StringComparison.Ordinal);
        Assert.Equal(11, cards.Count);
        Assert.Equal(8, edges.Count);
        Assert.All(cards.Where(card => card.Name != "导入XML"), card => Assert.False(card.OpensZExtract));
        var indexedFunctions = catalog.Projects
            .SelectMany(project => project.Namespaces)
            .SelectMany(space => space.Types)
            .SelectMany(type => type.Functions)
            .Select(symbol => symbol.SimpleName);
        Assert.DoesNotContain(indexedFunctions, name => FileMenu.Commands.Any(command => command.MethodName == name));
    }
}
