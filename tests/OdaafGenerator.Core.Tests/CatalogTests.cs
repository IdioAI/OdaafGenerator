using OdaafGenerator.Core;

namespace OdaafGenerator.Core.Tests;

public class CatalogTests
{
    [Fact]
    public void FocusProjectExpands_HelperProjectStopsAtTheName()
    {
        var catalog = Catalog.Create(
            ["GeneralUtils", "Baile.XmlGen.Xml"],
            [
                Row("Baile.XmlGen.Xml", 1, null, "namespace", "Baile.XmlGen.Xml"),
                Row("Baile.XmlGen.Xml", 2, 1, "class", "Baile.XmlGen.Xml.XmlImporter"),
                Row("Baile.XmlGen.Xml", 3, 2, "method", "Baile.XmlGen.Xml.XmlImporter.ParseZExtractionWindowCollection",
                    "private void ParseZExtractionWindowCollection(XDocument xdoc, XmlImportResult result)")
            ]);

        var helper = Assert.Single(catalog.Projects, project => project.Name == "GeneralUtils");
        Assert.False(helper.IsFocus);
        Assert.Empty(helper.Namespaces);

        var xml = Assert.Single(catalog.Projects, project => project.Name == "Baile.XmlGen.Xml");
        var type = Assert.Single(Assert.Single(xml.Namespaces).Types);
        var function = Assert.Single(type.Functions);
        Assert.Equal("ParseZExtractionWindowCollection", function.SimpleName);
        Assert.Equal("Baile.XmlGen.Xml", function.Namespace);

        Assert.Equal("GeneralUtils", Assert.Single(catalog.Search(SearchLevel.Project, "general").Hits).Title);
        Assert.Equal("XmlImporter", Assert.Single(catalog.Search(SearchLevel.Class, "Importer").Hits).Title);
        Assert.Equal("Baile.XmlGen.Xml", Assert.Single(catalog.Search(SearchLevel.Namespace, "XmlGen").Hits).Title);
        var found = Assert.Single(catalog.Search(SearchLevel.Function, "ParseZ").Hits);
        Assert.True(found.OnXmlCanvas);
        Assert.Empty(catalog.Search(SearchLevel.Function, "GeneralUtils").Hits);
    }

    [Fact]
    public void XmlContextCardsUseSignatureParameters_AndLeaveCausesBlank()
    {
        var import = "Baile.XmlGen.Logic.LogicLayer.ImportXmlData";
        var catalog = Catalog.Create(
            ["Baile.XmlGen.Logic", "Baile.XmlGen.Core"],
            [
                Row("Baile.XmlGen.Core", 1, null, "namespace", "Baile.XmlGen.Core"),
                Row("Baile.XmlGen.Core", 2, 1, "interface", "Baile.XmlGen.Core.ILogicLayer"),
                Row("Baile.XmlGen.Core", 3, 2, "method", "Baile.XmlGen.Core.ILogicLayer.ImportXmlData",
                    "void ImportXmlData(XmlImportResult result, bool clearExistingData = true)"),
                Row("Baile.XmlGen.Logic", 4, null, "namespace", "Baile.XmlGen.Logic"),
                Row("Baile.XmlGen.Logic", 5, 4, "class", "Baile.XmlGen.Logic.LogicLayer"),
                Row("Baile.XmlGen.Logic", 6, 5, "method", import,
                    "public void ImportXmlData(XmlImportResult result, bool clearExistingData = true)")
            ]);

        var cards = catalog.BuildXmlContext(new LayoutFile());
        Assert.Equal(6, cards.Count);
        var importCard = Assert.Single(cards, card => card.SyntaxPath == import);
        Assert.Equal("", importCard.Formal);
        Assert.Equal("", importCard.Final);
        Assert.Equal(
            [new ParameterLeaf("XmlImportResult", "result"), new ParameterLeaf("bool", "clearExistingData")],
            importCard.Materials);
        Assert.Contains("ILogicLayer.ImportXmlData", importCard.Note);
        Assert.False(XmlScene.IsCanvas("Baile.XmlGen.Core.ILogicLayer.ImportXmlData"));
        Assert.False(XmlScene.IsCanvas("BHEditor.Modules.UniPcb.Services.ZExtractXmlWriteBackService.TryWriteBack"));
    }

    private static RawDeclaration Row(
        string project, int start, int? parent, string kind, string path, string signature = "void M()") =>
        new("file.cs", start, parent, kind, path, signature, 10, 12, project);
}
