using OdaafGenerator.Core;

namespace OdaafGenerator.Core.Tests;

public class GeneratorProjectTests
{
    [Fact]
    public void CommittedIndex_ResolvesDatabaseSourceAndOdaaf()
    {
        var path = FindIndex();
        Assert.False(string.IsNullOrEmpty(path), "找不到 rapidcad-zextract.ogproj");
        var project = GeneratorProject.Load(path!);

        Assert.Equal("rapidcad-zextract-xml", project.Id);
        Assert.Equal("urn:odaaf:code:rapidcad-zextract-xml", project.OdaafUri);
        Assert.Equal("1.0.0", project.GraphVersion);
        Assert.True(File.Exists(project.DatabasePath), project.DatabasePath);
        Assert.True(File.Exists(project.SolutionPath), project.SolutionPath);
        Assert.True(File.Exists(project.OdaafPath), project.OdaafPath);
        Assert.EndsWith("rapidcad-zextract-xml.odaaf", project.OdaafPath, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(FileNavContext.Id, project.DefaultContextId);
        Assert.Equal(["index", "odaaf-file", "file-nav", "system-dna"], project.Contexts.Select(context => context.Id));
        Assert.Equal(["解析入库", "ODAAF", "文件菜单", "系统 DNA"], project.Contexts.Select(context => context.Name));
        Assert.Equal(SystemDna.Uri, project.DnaUri);
        Assert.True(File.Exists(project.DnaPath), project.DnaPath);
        var groups = SystemDna.Load(project.DnaPath);
        Assert.Equal(13, groups.Count);
        Assert.Equal(56, groups.Sum(group => group.Leaves.Count));
        Assert.Contains(groups, group => group.Leaves.Any(leaf => leaf.MachineName == "ToolProcessThresholdOfIsPossiblePreDrill" && leaf.DefaultValue == "2.0"));
    }

    [Fact]
    public void RelativeHrefs_ResolveBesideTheIndexFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "ogproj-" + Guid.NewGuid().ToString("N"));
        var index = Path.Combine(root, "app", "sample.ogproj");
        Directory.CreateDirectory(Path.GetDirectoryName(index)!);
        File.WriteAllText(index,
            """
            <?xml version="1.0" encoding="utf-8"?>
            <og:GeneratorProject xmlns:og="urn:odaaf:generator" id="sample" name="Sample">
              <og:Database href="../data/bheditor.db" />
              <og:Source href="../src" solution="BHEditor.sln" />
              <og:Odaaf href="../onto/rapidcad-zextract-xml.odaaf" uri="urn:odaaf:code:rapidcad-zextract-xml" graphVersion="1.0.0" />
            </og:GeneratorProject>
            """);

        try
        {
            var project = GeneratorProject.Load(index);
            Assert.Equal(Path.GetFullPath(Path.Combine(root, "data", "bheditor.db")), project.DatabasePath);
            Assert.Equal(Path.GetFullPath(Path.Combine(root, "src", "BHEditor.sln")), project.SolutionPath);
            Assert.Equal(Path.GetFullPath(Path.Combine(root, "onto", "rapidcad-zextract-xml.odaaf")), project.OdaafPath);
            Assert.Equal(OdaafFileContext.Id, project.DefaultContextId);
            Assert.Empty(project.Contexts);
            Assert.Equal("", project.DnaPath);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
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
