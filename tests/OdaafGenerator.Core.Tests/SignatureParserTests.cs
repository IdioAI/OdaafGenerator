using OdaafGenerator.Core;

namespace OdaafGenerator.Core.Tests;

public class SignatureParserTests
{
    [Fact]
    public void SplitsParameters_AndDropsModifiersAndDefaults()
    {
        var signature =
            "public static bool TryWriteBack( ILogicLayer logicLayer, string targetPath, out string errorMessage, ZExtractionWindowCollection zWindows = null, FovMode fovMode = FovMode.IsuInFovMode)";
        var leaves = SignatureParser.Parameters(signature);

        Assert.Equal(
            [
                new ParameterLeaf("ILogicLayer", "logicLayer"),
                new ParameterLeaf("string", "targetPath"),
                new ParameterLeaf("string", "errorMessage"),
                new ParameterLeaf("ZExtractionWindowCollection", "zWindows"),
                new ParameterLeaf("FovMode", "fovMode")
            ],
            leaves);
    }

    [Fact]
    public void KeepsGenericArgumentsTogether()
    {
        var leaves = SignatureParser.Parameters(
            "public static void MergeInto(XDocument doc, IEnumerable<ZExtractionFromXml> windows)");

        Assert.Equal("IEnumerable<ZExtractionFromXml>", leaves[1].Type);
        Assert.Equal("windows", leaves[1].Name);
    }

    [Fact]
    public void EmptyWhenThereIsNoParameterList()
    {
        Assert.Empty(SignatureParser.Parameters("public bool HasXmlZExtractionWindows => …"));
        Assert.Empty(SignatureParser.Parameters("public bool IsValid()"));
    }
}
