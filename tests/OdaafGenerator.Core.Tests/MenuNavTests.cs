using OdaafGenerator.Core;

namespace OdaafGenerator.Core.Tests;

public class MenuNavTests
{
    [Fact]
    public void Build_PutsHandlerUnderEachMenuItem()
    {
        var catalog = Catalog.Create(
            ["BHEditor"],
            [],
            [
                new RawDeclaration(
                    "MainWindowViewModel.cs",
                    10,
                    null,
                    "method",
                    "BHEditor.ViewModels.MainWindowViewModel.ExecuteImportXmlData",
                    "private async void ExecuteImportXmlData()",
                    20,
                    40,
                    "BHEditor")
            ]);
        var roots = MenuNavTree.Build(catalog);

        Assert.Equal(ShellMenus.TopMenus, roots.Select(node => node.Title));
        Assert.Equal("PCB文件", roots[^1].Title);
        var menus = roots.SelectMany(CommandsOf).ToList();
        Assert.Equal(ShellMenus.Entries.Select(entry => entry.Header), menus.Select(node => node.Title));
        Assert.Contains(menus, menu => menu.Title == "导入PCB" && menu.MethodName == "ExecuteImportPcb");
        Assert.Contains(menus, menu => menu.Title == "导入配置参数");
        var nested = roots.Single(node => node.Title == "PCB半自动处理").Children
            .Single(node => node.Title == "其他操作");
        Assert.Contains(nested.Children, menu => menu.Title == "为当前PCB添加Z抽出");
        Assert.All(menus, menu =>
        {
            var handler = Assert.Single(menu.Children);
            Assert.Equal(menu.MethodName, handler.MethodName);
            Assert.Equal(NavNode.Function, handler.Kind);
        });
        var import = MenuNavTree.FindHandler(roots, FileMenu.ImportXmlMethod);
        Assert.NotNull(import);
        Assert.NotNull(import.Symbol);
        Assert.Contains(import.Children, child => child.Kind == NavNode.More);
    }

    [Fact]
    public void Expand_AddsOneLayerOfCalls()
    {
        var root = Path.Combine(Path.GetTempPath(), "og-nav-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var file = Path.Combine(root, "Host.cs");
        File.WriteAllText(file,
            """
            namespace Demo;
            public class Host
            {
                public void ExecuteImportXmlData()
                {
                    Import();
                    Log();
                }
                public void Import() { Nested(); }
                public void Log() {}
                public void Nested() {}
            }
            """);
        try
        {
            var catalog = Catalog.Create(
                ["Baile.XmlGen.Core"],
                [
                    Row(file, 1, null, "namespace", "Demo"),
                    Row(file, 2, 1, "class", "Demo.Host"),
                    Row(file, 3, 2, "method", "Demo.Host.ExecuteImportXmlData", "public void ExecuteImportXmlData()", 4, 8),
                    Row(file, 4, 2, "method", "Demo.Host.Import", "public void Import()", 9, 9),
                    Row(file, 5, 2, "method", "Demo.Host.Log", "public void Log()", 10, 10),
                    Row(file, 6, 2, "method", "Demo.Host.Nested", "public void Nested()", 11, 11)
                ]);
            var node = new NavNode("ExecuteImportXmlData", NavNode.Function, "ExecuteImportXmlData",
                "Demo.Host.ExecuteImportXmlData", catalog.Find("Demo.Host.ExecuteImportXmlData"), mainFlow: true);
            node.Children.Add(NavNode.Placeholder());
            MenuNavTree.Expand(node, catalog, root);
            var execute = Assert.Single(node.Children);
            Assert.Equal("执行", execute.Title);
            Assert.True(execute.MainFlow);
            Assert.Equal(["Import", "Log"], execute.Children.Select(child => child.Title));
            Assert.All(execute.Children, child => Assert.True(child.MainFlow));
            Assert.DoesNotContain(execute.Children, child => child.Title == "Nested");
            Assert.True(node.CallsLoaded);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void RealIndex_ImportXmlHandlerExpands()
    {
        var index = FindIndex();
        Assert.False(string.IsNullOrEmpty(index));
        var project = GeneratorProject.Load(index!);
        var (projects, rows, host) = DeclarationStore.Load(project.DatabasePath);
        var catalog = Catalog.Create(projects, rows, host);
        var roots = MenuNavTree.Build(catalog);
        var handler = MenuNavTree.FindHandler(roots, FileMenu.ImportXmlMethod);
        Assert.NotNull(handler);
        Assert.NotNull(handler.Symbol);
        MenuNavTree.Expand(handler, catalog, project.SourceRoot);
        Assert.NotEmpty(handler.Children);
        Assert.DoesNotContain(handler.Children, child => child.Title == FileMenu.ImportXmlMethod);
    }

    private static IEnumerable<NavNode> CommandsOf(NavNode node) =>
        node.Children.SelectMany(child =>
            child.MethodName is null ? CommandsOf(child) : [child]);

    private static RawDeclaration Row(
        string file, int start, int? parent, string kind, string path, string signature = "", int startLine = 1, int endLine = 1) =>
        new(file, start, parent, kind, path, signature, startLine, endLine, "Baile.XmlGen.Core");

    private static string? FindIndex()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, GeneratorProject.FileName);
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        return null;
    }
}
