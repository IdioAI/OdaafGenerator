using OdaafGenerator.Core;

namespace OdaafGenerator.Core.Tests;

public class LayoutStoreTests
{
    [Fact]
    public void RoundTrip_KeepsPositionAndBlankCauses()
    {
        var path = Path.Combine(Path.GetTempPath(), $"odaaf-layout-{Guid.NewGuid():N}.json");
        try
        {
            var card = FunctionCard.FromScene(XmlScene.Slots[0], symbol: null, interfaceSymbol: null, placed: null);
            card.X = 40;
            card.Y = 80;
            card.Formal = "窗集合";
            card.Final = "";
            var layout = new LayoutFile();
            layout.Upsert(card);
            LayoutStore.Save(path, layout);

            var loaded = LayoutStore.Load(path);
            var node = loaded.Find(card.SyntaxPath);
            Assert.NotNull(node);
            Assert.Equal(40, node.X);
            Assert.Equal(80, node.Y);
            Assert.Equal("窗集合", node.Formal);
            Assert.Equal("", node.Final);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
