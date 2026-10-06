using OdaafGenerator.Core;

namespace OdaafGenerator.Core.Tests;

public class FileNavNextTests
{
    [Fact]
    public void WriteParams_GroupsExecuteIntoSegments()
    {
        var caller = Caller("ExecuteWriteParamsToPcb");
        var layer = new CallLayerGraph
        {
            Caller = caller,
            Menu = true,
            Calls =
            [
                Call("TryArchiveSemiAutoAssets", PatternAPhase.Execute),
                Call("AddBackdrillParametersToPadsAsync", PatternAPhase.Execute),
                Call("ApplyExpansion", PatternAPhase.Execute),
                Call("ShowDialog", PatternAPhase.Check, CallKind.System)
            ]
        };
        var brief = LayerBrief.For(caller.SimpleName, layer);
        Assert.NotNull(brief);
        Assert.Contains("SemiAuto", brief.ContextText);
        Assert.Contains("未在本层直接读取", brief.DnaText);
        Assert.Equal(
            ["TryArchiveSemiAutoAssets", "AddBackdrillParametersToPadsAsync", "ApplyExpansion"],
            brief.Execute.Select(row => row.MethodName));
        Assert.DoesNotContain(brief.Execute, row => row.MethodName == "ShowDialog");

        var markdown = LayerMarkdown.Render(brief, layer);
        Assert.Contains("flowchart LR", markdown);
        Assert.Contains("归档与顺序", markdown);
        Assert.Contains("背钻", markdown);
        Assert.Contains("ISU", markdown);
        Assert.DoesNotContain("ShowDialog", markdown);

        var graph = layer.Segments(new LayoutFile());
        Assert.Equal(["归档与顺序", "背钻", "ISU"], graph.Cards.Skip(1).Select(card => card.Name));
        Assert.Equal(2, graph.Edges.Count);
        Assert.All(graph.Edges, edge => Assert.Contains("@segment:", edge.To));
    }

    [Fact]
    public void AcceptedNote_FillsMissingMethodWithoutReplacingHandTable()
    {
        var path = Path.Combine(Path.GetTempPath(), "og-notes-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            AcceptedNotes.Write(path, new Dictionary<string, FlowNotes.Note>
            {
                ["BrandNewMethod"] = new("一句说明。", new Dictionary<string, string> { ["filePath"] = "路径。" }, "", "新方法"),
                ["TryArchiveSemiAutoAssets"] = new("不该覆盖。", new Dictionary<string, string>(), "", "别的名字")
            });
            FlowNotes.ReplaceAccepted(AcceptedNotes.Read(path));
            var added = FlowNotes.Find(null, "BrandNewMethod");
            Assert.Equal("新方法", added?.Title);
            Assert.Equal("路径。", added?.Parameters["filePath"]);
            var hand = FlowNotes.Find(null, "TryArchiveSemiAutoAssets");
            Assert.Equal("归档现场", hand?.Title);
            Assert.Contains("归档", hand?.Purpose);
        }
        finally
        {
            FlowNotes.ReplaceAccepted(new Dictionary<string, FlowNotes.Note>());
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void Proposal_SkipsNotedAndSystemCalls()
    {
        var caller = Caller("ExecuteWriteParamsToPcb");
        var layer = new CallLayerGraph
        {
            Caller = caller,
            Menu = true,
            Calls =
            [
                Call("TryArchiveSemiAutoAssets", PatternAPhase.Execute),
                Call("ShowDialog", PatternAPhase.Check, CallKind.System),
                Call("NotNotedYet", PatternAPhase.Execute, CallKind.Unindexed)
            ]
        };
        var missing = NameProposals.Missing(layer);
        Assert.Equal(["NotNotedYet"], missing.Select(call => call.Name));
        var parsed = NameProposals.Parse("NotNotedYet\t尚未记下\t还没有中文说明。\nSkipMe\t不要\t无效", missing);
        Assert.Equal("尚未记下", Assert.Single(parsed).Title);
        var fenced = NameProposals.Parse(
            "```\nNotNotedYet | 尚未记下 | 还没有中文说明。第二句。 | missing=忽略\n```",
            missing);
        Assert.Equal("尚未记下", Assert.Single(fenced).Title);
        Assert.Equal("还没有中文说明。", Assert.Single(fenced).Purpose);
        Assert.Empty(Assert.Single(fenced).Parameters);
    }

    private static CodeSymbol Caller(string name) =>
        new("BHEditor", "MainWindowViewModel.cs", 1, "method",
            "BHEditor.ViewModels.MainWindowViewModel." + name,
            "private async void " + name + "()", 1, 2, "BHEditor.ViewModels", "MainWindowViewModel",
            "BHEditor.ViewModels.MainWindowViewModel");

    private static DirectCall Call(string name, PatternAPhase phase, CallKind kind = CallKind.Domain) =>
        new(name, "Some." + name, "void " + name + "()", "MainWindowViewModel.cs", 10, kind == CallKind.Domain, phase, kind,
            kind == CallKind.System ? "系统" : "BHEditor", "");
}
