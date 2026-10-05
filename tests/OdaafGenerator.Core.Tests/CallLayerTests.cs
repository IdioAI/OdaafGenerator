using OdaafGenerator.Core;

namespace OdaafGenerator.Core.Tests;

public class CallLayerTests
{
    [Fact]
    public void Expand_ListsDirectCallsOnly()
    {
        var root = Path.Combine(Path.GetTempPath(), "og-call-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var file = Path.Combine(root, "Host.cs");
        File.WriteAllText(file,
            """
            namespace Demo;
            public class Host
            {
                public void Run(string path)
                {
                    Import(path);
                    Log();
                }
                public void Import(string path) { Nested(); }
                public void Log() {}
                public void Nested() {}
            }
            """);
        try
        {
            var catalog = Catalog.Create(
                ["Baile.XmlGen.Core"],
                [
                    new RawDeclaration(file, 1, null, "namespace", "Demo", "", 1, 1, "Baile.XmlGen.Core"),
                    new RawDeclaration(file, 2, 1, "class", "Demo.Host", "", 2, 16, "Baile.XmlGen.Core"),
                    new RawDeclaration(file, 3, 2, "method", "Demo.Host.Run", "public void Run(string path)", 4, 8, "Baile.XmlGen.Core"),
                    new RawDeclaration(file, 4, 2, "method", "Demo.Host.Import", "public void Import(string path)", 9, 10, "Baile.XmlGen.Core"),
                    new RawDeclaration(file, 5, 2, "method", "Demo.Host.Log", "public void Log()", 11, 11, "Baile.XmlGen.Core"),
                    new RawDeclaration(file, 6, 2, "method", "Demo.Host.Nested", "public void Nested()", 12, 12, "Baile.XmlGen.Core")
                ]);
            var run = catalog.Find("Demo.Host.Run");
            Assert.NotNull(run);
            var layer = CallLayerGraph.Expand(run, root, catalog);
            Assert.Equal(["Import", "Log"], layer.Calls.Select(call => call.Name));
            Assert.DoesNotContain(layer.Calls, call => call.Name == "Nested");
            Assert.All(layer.Calls, call => Assert.True(call.Resolved));
            var (cards, edges) = layer.Cards(new LayoutFile());
            Assert.Equal(3, cards.Count);
            Assert.Equal(2, edges.Count);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void RealIndex_ImportXmlExpandsOneLayer()
    {
        var index = FindIndex();
        Assert.False(string.IsNullOrEmpty(index));
        var project = GeneratorProject.Load(index!);
        var (projects, rows, host) = DeclarationStore.Load(project.DatabasePath);
        var catalog = Catalog.Create(projects, rows, host);
        var method = catalog.FindMethod(FileMenu.ImportXmlMethod);
        Assert.NotNull(method);
        var layer = CallLayerGraph.Expand(method, project.SourceRoot, catalog);
        Assert.NotEmpty(layer.Calls);
        Assert.DoesNotContain(layer.Calls, call => call.Name == FileMenu.ImportXmlMethod);
        Assert.Contains(layer.Calls, call => call.Resolved);
    }

    [Fact]
    public void RealIndex_WriteParamsFollowsPatternA()
    {
        var index = FindIndex();
        Assert.False(string.IsNullOrEmpty(index));
        var project = GeneratorProject.Load(index!);
        var (projects, rows, host) = DeclarationStore.Load(project.DatabasePath);
        var catalog = Catalog.Create(projects, rows, host);
        var method = catalog.FindMethod("ExecuteWriteParamsToPcb");
        Assert.NotNull(method);
        var layer = CallLayerGraph.Expand(method, project.SourceRoot, catalog);
        Assert.Equal(PatternAPhase.Dispatch, Phase(layer, "GetFusionContextForSemiAuto"));
        Assert.Equal(PatternAPhase.Dispatch, Phase(layer, "ShowDialog"));
        Assert.Equal(PatternAPhase.Schedule, Phase(layer, "BeginSemiAutoWorkflow"));
        Assert.Equal(PatternAPhase.Execute, Phase(layer, "ApplySemiAutoSanitizeAndArchive"));
        Assert.Equal(PatternAPhase.Result, Phase(layer, "EndSemiAutoWorkflow"));
        Assert.Contains(layer.Calls, call => call.Name == "Show" && call.Phase == PatternAPhase.Check);
        Assert.Contains(layer.Calls, call => call.Name == "Show" && call.Phase == PatternAPhase.Result);
        Assert.DoesNotContain(layer.Calls, call => call.Name is "ToList" or "OfType");
    }

    [Fact]
    public void PatternA_SplitsWriteParamsByPhase()
    {
        var root = Path.Combine(Path.GetTempPath(), "og-pattern-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var file = Path.Combine(root, "Host.cs");
        File.WriteAllText(file,
            """
            namespace Demo;
            public class Host
            {
                public void ExecuteWriteParamsToPcb()
                {
                    GetFusionContextForSemiAuto();
                    if (string.IsNullOrEmpty(path)) { Show(); Warn(); return; }
                    ShowDialog();
                    BeginSemiAutoWorkflow();
                    ApplySemiAutoSanitizeAndArchive();
                    EndSemiAutoWorkflow();
                    Show();
                    ToList();
                }
                string path = "";
            }
            """);
        try
        {
            var catalog = Catalog.Create(["BHEditor"], [], []);
            var method = new CodeSymbol(
                "BHEditor",
                file,
                4,
                "method",
                "Demo.Host.ExecuteWriteParamsToPcb",
                "public void ExecuteWriteParamsToPcb()",
                4,
                16,
                "Demo",
                "Host",
                "Demo.Host");
            var layer = CallLayerGraph.Expand(method, root, catalog);
            Assert.DoesNotContain(layer.Calls, call => call.Name == "ToList");
            Assert.Equal(PatternAPhase.Dispatch, Phase(layer, "GetFusionContextForSemiAuto"));
            Assert.Equal(PatternAPhase.Check, Phase(layer, "IsNullOrEmpty"));
            Assert.Equal(PatternAPhase.Check, Phase(layer, "Warn"));
            Assert.Equal(PatternAPhase.Dispatch, Phase(layer, "ShowDialog"));
            Assert.Equal(PatternAPhase.Schedule, Phase(layer, "BeginSemiAutoWorkflow"));
            Assert.Equal(PatternAPhase.Execute, Phase(layer, "ApplySemiAutoSanitizeAndArchive"));
            Assert.Equal(PatternAPhase.Result, Phase(layer, "EndSemiAutoWorkflow"));
            Assert.Contains(layer.Calls, call => call.Name == "Show" && call.Phase == PatternAPhase.Check);
            Assert.Contains(layer.Calls, call => call.Name == "Show" && call.Phase == PatternAPhase.Result);
            var (cards, _) = layer.Cards(new LayoutFile());
            Assert.Equal(PatternA.Title(PatternAPhase.Execute), cards.Single(card => card.Name == "ApplySemiAutoSanitizeAndArchive").StepLabel);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static PatternAPhase Phase(CallLayerGraph layer, string name) =>
        Assert.Single(layer.Calls, call => call.Name == name).Phase;

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
