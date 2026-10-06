namespace OdaafGenerator.Core;

public static class FunctionBrief
{
    public static void Apply(FunctionCard card)
    {
        var note = FlowNotes.Find(card.SyntaxPath, card.Name);
        if (string.IsNullOrWhiteSpace(card.FriendlyName))
            card.FriendlyName = note?.Title ?? "";
        if (card.Project == "系统" && string.IsNullOrWhiteSpace(card.FriendlyName))
            card.FriendlyName = SystemCalls.TitleOf(card.Name);
        if (string.IsNullOrWhiteSpace(card.Purpose))
            card.Purpose = note?.Purpose ?? "";
        if (string.IsNullOrWhiteSpace(card.Documents))
            card.Documents = note?.Documents ?? "";
        card.Materials = FlowNotes.WithMeanings(card.SyntaxPath, card.Name, card.Materials);

        var missingMeaning = card.Materials.Any(leaf => string.IsNullOrWhiteSpace(leaf.Meaning));
        if (!string.IsNullOrWhiteSpace(card.Purpose) && !missingMeaning)
            return;

        var xml = XmlDocComments.Read(card.FilePath, card.StartLine, card.Name);
        if (string.IsNullOrWhiteSpace(card.Purpose))
            card.Purpose = xml.Summary;
        if (missingMeaning)
        {
            card.Materials = card.Materials.Select(leaf =>
                string.IsNullOrWhiteSpace(leaf.Meaning) && xml.Params.TryGetValue(leaf.Name, out var meaning)
                    ? leaf with { Meaning = meaning }
                    : leaf).ToList();
        }
    }
}
