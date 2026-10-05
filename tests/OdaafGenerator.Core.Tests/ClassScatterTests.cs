using OdaafGenerator.Core;

namespace OdaafGenerator.Core.Tests;

public class ClassScatterTests
{
    [Fact]
    public void SceneTypes_FormAZExtractCluster_OthersGroupByNamespace()
    {
        var catalog = Catalog.Create(
            ["Baile.XmlGen.Xml", "Baile.XmlGen.Logic", "Baile.XmlGen.Core"],
            [
                Row("Baile.XmlGen.Xml", 1, null, "namespace", "Baile.XmlGen.Xml"),
                Row("Baile.XmlGen.Xml", 2, 1, "class", "Baile.XmlGen.Xml.XmlImporter"),
                Row("Baile.XmlGen.Xml", 3, 2, "method", "Baile.XmlGen.Xml.XmlImporter.ParseZExtractionWindowCollection",
                    "private void ParseZExtractionWindowCollection(XDocument xdoc, XmlImportResult result)"),
                Row("Baile.XmlGen.Core", 4, null, "namespace", "Baile.XmlGen.Core"),
                Row("Baile.XmlGen.Core", 5, 4, "class", "Baile.XmlGen.Core.XmlImportResult"),
                Row("Baile.XmlGen.Core", 6, 5, "method", "Baile.XmlGen.Core.XmlImportResult.HasXmlZExtractionWindows",
                    "public bool HasXmlZExtractionWindows()"),
                Row("Baile.XmlGen.Core", 7, 4, "class", "Baile.XmlGen.Core.Orphan"),
                Row("Baile.XmlGen.Core", 8, 7, "method", "Baile.XmlGen.Core.Orphan.Ping", "void Ping()"),
                Row("Baile.XmlGen.Logic", 9, null, "namespace", "Baile.XmlGen.Logic"),
                Row("Baile.XmlGen.Logic", 10, 9, "class", "Baile.XmlGen.Logic.LogicLayer"),
                Row("Baile.XmlGen.Logic", 11, 10, "method", "Baile.XmlGen.Logic.LogicLayer.ImportXmlData",
                    "public void ImportXmlData(XmlImportResult result)")
            ]);

        var graph = ClassScatterGraph.Build(catalog);
        Assert.Contains(graph.Types, type => type.Name == "XmlImporter" && type.InScene);
        var scene = Assert.Single(graph.Clusters, cluster => cluster.Kind == "场景");
        Assert.Equal("Z抽出", scene.Name);
        Assert.Contains(scene.Types, type => type.Name == "XmlImporter");
        Assert.Contains(scene.Types, type => type.Name == "LogicLayer");
        Assert.Contains(graph.Clusters, cluster => cluster.Types.Any(type => type.Name == "Orphan"));
        var cards = graph.ClusterCards();
        Assert.Contains(cards, card => card.Name == "Z抽出");
        var (members, _) = graph.Members(scene.Id);
        Assert.Contains(members, card => card.Name == "XmlImporter");
    }

    [Fact]
    public void RealIndex_BuildsFocusTypeClusters()
    {
        var path = DeclarationStore.DefaultDatabasePath;
        Assert.True(File.Exists(path), path);
        var (projects, rows, host) = DeclarationStore.Load(path);
        var catalog = Catalog.Create(projects, rows, host);
        var graph = ClassScatterGraph.Build(catalog);

        Assert.True(graph.Types.Count > 20, "types=" + graph.Types.Count);
        Assert.Contains(graph.Clusters, cluster => cluster.Kind == "场景" && cluster.Types.Count >= 2);
        Assert.Equal(graph.Types.Count, graph.Clusters.Sum(cluster => cluster.Types.Count));
        var typeNames = graph.Types.Select(type => type.Name).ToHashSet(StringComparer.Ordinal);
        Assert.DoesNotContain(FileMenu.Commands, command => typeNames.Contains(command.Header));
    }

    private static RawDeclaration Row(
        string project, int start, int? parent, string kind, string path, string signature = "void M()") =>
        new("file.cs", start, parent, kind, path, signature, 10, 12, project);
}
