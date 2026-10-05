using OdaafGenerator.Core;

namespace OdaafGenerator.Core.Tests;

public class WorkspaceTests
{
    [Fact]
    public void IndexViews_DoNotListTheFileMenuCommands()
    {
        var titles = WorkspaceViews.For(IndexContext.Id).Select(view => view.Title).ToList();
        Assert.Contains("工程索引", titles);
        Assert.Contains("目录查找", titles);
        Assert.DoesNotContain(FileMenu.Commands.Select(command => command.Header), titles.Contains);
        Assert.Empty(IndexContext.Commands);
    }

    [Fact]
    public void OdaafViews_IncludeFileMenuScatterAndFunctionLayer()
    {
        Assert.Equal(
            ["工程索引", "文件菜单", "Z 抽出", "类散图", "函数层"],
            WorkspaceViews.For(OdaafFileContext.Id).Select(view => view.Title));
    }

    [Fact]
    public void ProjectIndex_ListsOgprojInputsAndOutputs()
    {
        var path = FindIndex();
        Assert.False(string.IsNullOrEmpty(path));
        var project = GeneratorProject.Load(path!);
        var info = ProjectIndexInfo.From(project, Path.Combine(Path.GetTempPath(), "og-layout.json"), 50, 100);

        Assert.Equal(project.Name, info.Name);
        Assert.Equal(FileNavContext.Id, info.DefaultContextId);
        Assert.Contains(info.Contexts, context => context.Id == FileNavContext.Id);
        Assert.Contains(info.Files, file => file.Role == "工程索引" && file.Kind == "输入" && file.Exists);
        Assert.Contains(info.Files, file => file.Role == "代码索引数据库" && file.Exists);
        Assert.Contains(info.Files, file => file.Role == "源码解决方案" && file.Exists);
        Assert.Contains(info.Files, file => file.Role == "同步本体" && file.Kind == "输出" && file.Exists);
        Assert.Contains(info.Files, file => file.Role == "画布布局" && file.Kind == "本地输出");
        Assert.Equal(XmlScene.FocusProjects, info.FocusProjects);
    }

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
