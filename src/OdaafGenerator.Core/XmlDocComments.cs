using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace OdaafGenerator.Core;

public static class XmlDocComments
{
    private static readonly Dictionary<string, (string Summary, Dictionary<string, string> Params)> Cache = new(StringComparer.OrdinalIgnoreCase);

    public readonly record struct Doc(string Summary, IReadOnlyDictionary<string, string> Params);

    public static Doc Read(string? file, int startLine, string methodName)
    {
        if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
            return new Doc("", new Dictionary<string, string>());
        var key = file + "#" + methodName + "#" + startLine;
        if (Cache.TryGetValue(key, out var hit))
            return new Doc(hit.Summary, hit.Params);

        var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file));
        var methods = tree.GetCompilationUnitRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(method => method.Identifier.Text == methodName)
            .ToList();
        var method = methods.FirstOrDefault(item =>
        {
            var line = tree.GetLineSpan(item.Span).StartLinePosition.Line + 1;
            return startLine <= 0 || Math.Abs(line - startLine) <= 3;
        }) ?? methods.FirstOrDefault();
        var doc = ReadMethod(method);
        Cache[key] = (doc.Summary, new Dictionary<string, string>(doc.Params));
        return doc;
    }

    private static Doc ReadMethod(MethodDeclarationSyntax? method)
    {
        if (method is null)
            return new Doc("", new Dictionary<string, string>());
        var trivia = method.GetLeadingTrivia()
            .Select(item => item.GetStructure())
            .OfType<DocumentationCommentTriviaSyntax>()
            .LastOrDefault();
        if (trivia is null)
            return new Doc("", new Dictionary<string, string>());

        var summary = "";
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var element in trivia.Content.OfType<XmlElementSyntax>())
        {
            var tag = element.StartTag.Name.ToString();
            var text = TextOf(element);
            if (tag == "summary" && summary.Length == 0)
                summary = text;
            else if (tag == "param")
            {
                var name = element.StartTag.Attributes.OfType<XmlNameAttributeSyntax>()
                    .Select(attribute => attribute.Identifier.Identifier.ValueText)
                    .FirstOrDefault();
                if (!string.IsNullOrEmpty(name))
                    parameters[name] = text;
            }
        }

        return new Doc(summary, parameters);
    }

    private static string TextOf(XmlElementSyntax element)
    {
        var text = string.Concat(element.Content.Select(node => node.ToString()));
        var lines = text.Split('\n').Select(line => line.Trim().TrimStart('/', ' ', '*')).Where(line => line.Length > 0);
        return string.Join(" ", lines);
    }
}
