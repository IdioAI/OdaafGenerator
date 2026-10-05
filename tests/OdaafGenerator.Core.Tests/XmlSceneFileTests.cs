using System.Xml.Linq;
using OdaafGenerator.Core;

namespace OdaafGenerator.Core.Tests;

public class XmlSceneFileTests
{
    [Fact]
    public void SceneFileMembers_MatchTheCanvasSlots()
    {
        var path = FindScene();
        Assert.False(string.IsNullOrEmpty(path), "找不到 rapidcad-zextract-xml.codeup");
        var document = XDocument.Load(path!);
        XNamespace ns = "urn:odaaf:code-upward";
        var refs = document.Descendants(ns + "Ref").ToList();
        Assert.NotEmpty(refs);

        foreach (var reference in refs)
        {
            var syntax = (string?)reference.Attribute("syntaxPath") ?? "";
            var project = (string?)reference.Attribute("project") ?? "";
            if (project == "BHEditor.Modules.UniPcb")
            {
                Assert.False(XmlScene.IsCanvas(syntax));
                continue;
            }

            if (syntax == "Baile.XmlGen.Core.ILogicLayer.ImportXmlData")
            {
                Assert.False(XmlScene.IsCanvas(syntax));
                Assert.Contains(XmlScene.Slots, slot => slot.InterfaceSyntaxPath == syntax);
                continue;
            }

            Assert.True(XmlScene.IsCanvas(syntax), syntax);
        }

        Assert.Equal(6, XmlScene.Slots.Length);
        Assert.Equal(5, XmlScene.Edges.Length);
    }

    private static string? FindScene()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(
                dir.FullName,
                "OntoLib",
                "BzoxOdaaf",
                "omron",
                "from-code",
                "contexts",
                "rapidcad-zextract-xml.codeup");
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        return null;
    }
}
