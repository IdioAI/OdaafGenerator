namespace OdaafGenerator.Core;

public static class ViewIds
{
    public const string ProjectIndex = "project-index";
    public const string IndexBrowse = "index-browse";
    public const string FileMenu = "file-menu";
    public const string ZExtract = "z-extract";
    public const string ClassScatter = "class-scatter";
    public const string ClassCluster = "class-cluster";
    public const string FunctionLayer = "function-layer";
}

public sealed record WorkspaceView(string Id, string Title, string ContextId);

public static class WorkspaceViews
{
    public static readonly WorkspaceView[] All =
    [
        new(ViewIds.ProjectIndex, "工程索引", IndexContext.Id),
        new(ViewIds.IndexBrowse, "目录查找", IndexContext.Id),
        new(ViewIds.ProjectIndex, "工程索引", OdaafFileContext.Id),
        new(ViewIds.FileMenu, "文件菜单", OdaafFileContext.Id),
        new(ViewIds.ZExtract, "Z 抽出", OdaafFileContext.Id),
        new(ViewIds.ClassScatter, "类散图", OdaafFileContext.Id),
        new(ViewIds.FunctionLayer, "函数层", OdaafFileContext.Id),
        new(ViewIds.FileMenu, "菜单导航", FileNavContext.Id)
    ];

    public static IReadOnlyList<WorkspaceView> For(string contextId) =>
        All.Where(view => view.ContextId == contextId).ToList();

    public static string Title(string viewId) =>
        All.FirstOrDefault(view => view.Id == viewId)?.Title ?? viewId;
}

public sealed record ProjectFileRow(
    string Role,
    string Kind,
    string Path,
    bool Exists,
    long? Bytes);

public sealed class ProjectIndexInfo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string DefaultContextId { get; init; }
    public required IReadOnlyList<GeneratorContext> Contexts { get; init; }
    public required string OdaafUri { get; init; }
    public required string GraphVersion { get; init; }
    public required int ProjectCount { get; init; }
    public required int DeclarationCount { get; init; }
    public required IReadOnlyList<string> FocusProjects { get; init; }
    public required IReadOnlyList<ProjectFileRow> Files { get; init; }

    public static ProjectIndexInfo From(
        GeneratorProject project,
        string layoutPath,
        int projectCount,
        int declarationCount)
    {
        return new ProjectIndexInfo
        {
            Id = project.Id,
            Name = project.Name,
            DefaultContextId = project.DefaultContextId,
            Contexts = project.Contexts,
            OdaafUri = project.OdaafUri,
            GraphVersion = project.GraphVersion,
            ProjectCount = projectCount,
            DeclarationCount = declarationCount,
            FocusProjects = XmlScene.FocusProjects,
            Files =
            [
                Row("工程索引", "输入", project.FilePath),
                Row("代码索引数据库", "输入", project.DatabasePath),
                Row("源码解决方案", "输入", project.SolutionPath),
                Row("源码目录", "输入", project.SourceRoot, directory: true),
                Row("同步本体", "输出", project.OdaafPath),
                Row("画布布局", "本地输出", layoutPath)
            ]
        };
    }

    private static ProjectFileRow Row(string role, string kind, string path, bool directory = false)
    {
        var exists = directory ? Directory.Exists(path) : File.Exists(path);
        long? bytes = !directory && exists ? new FileInfo(path).Length : null;
        return new ProjectFileRow(role, kind, path, exists, bytes);
    }
}
