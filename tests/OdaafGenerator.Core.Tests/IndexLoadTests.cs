using OdaafGenerator.Core;

namespace OdaafGenerator.Core.Tests;

public class IndexLoadTests
{
    [Fact]
    public void RealIndex_ResolvesTheXmlContextFunctions()
    {
        var path = DeclarationStore.DefaultDatabasePath;
        Assert.True(File.Exists(path), path);
        var (projects, rows, host) = DeclarationStore.Load(path);
        var catalog = Catalog.Create(projects, rows, host);
        var cards = catalog.BuildXmlContext(new LayoutFile());

        Assert.Equal(6, cards.Count);
        Assert.All(cards, card => Assert.False(string.IsNullOrEmpty(card.FilePath), card.SyntaxPath));
        Assert.Contains(cards, card => card.Name == "TryWriteBack" && card.Materials.Count >= 3);
        Assert.Contains(cards, card => card.Name == "HasXmlZExtractionWindows" && card.Materials.Count == 0);
        Assert.Contains(projects, name => name == "GeneralUtils");
        Assert.Empty(catalog.Projects.Single(project => project.Name == "GeneralUtils").Namespaces);
        Assert.NotEmpty(host);
        Assert.Empty(catalog.Search(SearchLevel.Function, "ExecuteImportXmlData").Hits);
    }
}
