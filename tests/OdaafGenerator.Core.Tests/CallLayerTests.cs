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
        Assert.True(layer.Menu);
        Assert.Equal(PatternAPhase.Dispatch, Phase(layer, "GetFusionContextForSemiAuto"));
        Assert.Equal(PatternAPhase.Dispatch, Phase(layer, "ShowDialog"));
        Assert.Equal(PatternAPhase.Schedule, Phase(layer, "BeginSemiAutoWorkflow"));
        Assert.Equal(PatternAPhase.Execute, Phase(layer, "ApplySemiAutoSanitizeAndArchive"));
        Assert.Equal(PatternAPhase.Result, Phase(layer, "EndSemiAutoWorkflow"));
        Assert.DoesNotContain(layer.Calls, call => call.Name is "Warn" or "Info" or "Error" or "ToList" or "OfType" or "SemiAutoWorkflowLog");
        Assert.Equal(CallKind.System, layer.Calls.First(call => call.Name == "ShowDialog").Kind);
        Assert.Contains(layer.Calls, call => call.Name == "IsNullOrEmpty" && call.Kind == CallKind.System);
        Assert.Contains("pcb-semi-auto", FlowNotes.Find(method.SyntaxPath, method.SimpleName)!.Documents);
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
                    Math.Min(1, 2);
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
            Assert.True(layer.Menu);
            Assert.DoesNotContain(layer.Calls, call => call.Name is "ToList" or "Warn");
            Assert.Equal(CallKind.System, layer.Calls.First(call => call.Name == "Show").Kind);
            Assert.Equal(CallKind.System, layer.Calls.Single(call => call.Name == "IsNullOrEmpty").Kind);
            Assert.Equal(CallKind.System, layer.Calls.Single(call => call.Name == "ShowDialog").Kind);
            Assert.Equal(PatternAPhase.Dispatch, Phase(layer, "GetFusionContextForSemiAuto"));
            Assert.Equal(PatternAPhase.Dispatch, Phase(layer, "ShowDialog"));
            Assert.Equal(PatternAPhase.Schedule, Phase(layer, "BeginSemiAutoWorkflow"));
            Assert.Equal(PatternAPhase.Execute, Phase(layer, "ApplySemiAutoSanitizeAndArchive"));
            Assert.Equal(PatternAPhase.Result, Phase(layer, "EndSemiAutoWorkflow"));
            var (cards, edges) = layer.Cards(new LayoutFile());
            var handler = cards.Single(card => card.Name == "ExecuteWriteParamsToPcb");
            Assert.Contains("背钻", handler.Purpose);
            Assert.Contains("pcb-semi-auto", handler.Documents);
            var work = cards.Single(card => card.Name == "ApplySemiAutoSanitizeAndArchive");
            Assert.Equal("执行", work.StepLabel);
            Assert.True(work.MainFlow);
            var dialog = cards.Single(card => card.Name == "ShowDialog");
            Assert.False(dialog.MainFlow);
            Assert.Equal("系统", dialog.Project);
            Assert.False(dialog.WritesStructure);
            Assert.Contains("确认或取消", dialog.Purpose);
            var min = cards.Single(card => card.Name == "Min");
            Assert.Equal("系统", min.StepLabel);
            Assert.False(min.MainFlow);
            Assert.DoesNotContain(edges, edge => edge.To == min.SyntaxPath);
            Assert.Contains(edges, edge => edge.To == work.SyntaxPath);
            Assert.DoesNotContain(edges, edge => edge.To.Contains("ShowDialog", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void PatternB_KeepsExecuteAsMainFlow()
    {
        var root = Path.Combine(Path.GetTempPath(), "og-pattern-b-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var file = Path.Combine(root, "Host.cs");
        File.WriteAllText(file,
            """
            namespace Demo;
            public class Host
            {
                public void Place(string input)
                {
                    if (input == null) { Reject(); return; }
                    Logger.Info("x");
                    Build();
                    if (!ok) { Fail(); return; }
                }
                public void Reject() {}
                public void Build() {}
                public void Fail() {}
                bool ok;
            }
            """);
        try
        {
            var catalog = Catalog.Create(["BHEditor"], [], []);
            var method = new CodeSymbol(
                "BHEditor", file, 4, "method", "Demo.Host.Place", "public void Place(string input)",
                4, 12, "Demo", "Host", "Demo.Host");
            var layer = CallLayerGraph.Expand(method, root, catalog);
            Assert.False(layer.Menu);
            Assert.DoesNotContain(layer.Calls, call => call.Name == "Info");
            Assert.Equal(PatternAPhase.Check, Phase(layer, "Reject"));
            Assert.Equal(PatternAPhase.Execute, Phase(layer, "Build"));
            Assert.Equal(PatternAPhase.Result, Phase(layer, "Fail"));
            var (cards, edges) = layer.Cards(new LayoutFile());
            Assert.True(cards.Single(card => card.Name == "Build").MainFlow);
            Assert.False(cards.Single(card => card.Name == "Reject").MainFlow);
            Assert.False(cards.Single(card => card.Name == "Fail").MainFlow);
            Assert.Equal(["Build"], edges.Select(edge => cards.Single(card => card.SyntaxPath == edge.To).Name));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Brief_FillsPurposeAndParameterMeaning()
    {
        var card = new FunctionCard
        {
            SyntaxPath = "BHEditor.ViewModels.MainWindowViewModel.TryArchiveSemiAutoAssets@Execute",
            Name = "TryArchiveSemiAutoAssets",
            StepLabel = "执行",
            Project = "BHEditor",
            Signature = "private void TryArchiveSemiAutoAssets(string reason, string xmlFilePath, string originalPcbPath, string generatedZPcbPath)",
            FilePath = "",
            StartLine = 1,
            EndLine = 1,
            Materials = SignatureParser.Parameters("private void TryArchiveSemiAutoAssets(string reason, string xmlFilePath, string originalPcbPath, string generatedZPcbPath)")
        };
        FunctionBrief.Apply(card);
        Assert.Contains("归档", card.Purpose);
        Assert.Contains("归档原因", card.Materials.Single(leaf => leaf.Name == "reason").Meaning);
        Assert.Contains("写入前", card.Materials.Single(leaf => leaf.Name == "originalPcbPath").Meaning);
    }

    [Fact]
    public void Brief_UsesSignatureParameterNames()
    {
        var enter = new FunctionCard
        {
            SyntaxPath = "BHEditor.ViewModels.MainWindowViewModel.SemiAutoWorkflowEnter@Execute",
            Name = "SemiAutoWorkflowEnter",
            StepLabel = "执行",
            Project = "BHEditor",
            Signature = "private void SemiAutoWorkflowEnter(string nodeId, string note)",
            FilePath = "",
            StartLine = 1,
            EndLine = 1,
            Materials = SignatureParser.Parameters("private void SemiAutoWorkflowEnter(string nodeId, string note)")
        };
        FunctionBrief.Apply(enter);
        Assert.Contains("进度", enter.Materials.Single(leaf => leaf.Name == "note").Meaning);

        var place = new FunctionCard
        {
            SyntaxPath = "Baile.Pcb.Logic.Services.ZCorrection.ZCorrectionPlacementService.Place@Execute",
            Name = "Place",
            StepLabel = "执行",
            Project = "Baile.Pcb.Logic",
            Signature = "public static ZCorrectionPlaceResult Place(IPcbParseResult parseResult, PcbDataManager dataManager, ZCorrectionPlaceOptions options)",
            FilePath = "",
            StartLine = 1,
            EndLine = 1,
            Materials = SignatureParser.Parameters("public static ZCorrectionPlaceResult Place(IPcbParseResult parseResult, PcbDataManager dataManager, ZCorrectionPlaceOptions options)")
        };
        FunctionBrief.Apply(place);
        Assert.Contains("Z 补正", place.Purpose);
        Assert.Contains("放置选项", place.Materials.Single(leaf => leaf.Name == "options").Meaning);
    }

    [Fact]
    public void XmlDoc_FillsSummaryWhenNoteIsMissing()
    {
        var root = Path.Combine(Path.GetTempPath(), "og-doc-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var file = Path.Combine(root, "Host.cs");
        File.WriteAllText(file,
            """
            namespace Demo;
            public class Host
            {
                /// <summary>把焊盘按刀具分组。</summary>
                /// <param name="fusionDoc">融合结果。</param>
                public void Group(string fusionDoc) {}
            }
            """);
        try
        {
            var card = new FunctionCard
            {
                SyntaxPath = "Demo.Host.Group",
                Name = "Group",
                StepLabel = "执行",
                Project = "BHEditor",
                Signature = "public void Group(string fusionDoc)",
                FilePath = file,
                StartLine = 6,
                EndLine = 6,
                Materials = SignatureParser.Parameters("public void Group(string fusionDoc)")
            };
            FunctionBrief.Apply(card);
            Assert.Contains("刀具分组", card.Purpose);
            Assert.Contains("融合结果", card.Materials.Single().Meaning);
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
