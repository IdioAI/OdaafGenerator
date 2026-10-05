namespace OdaafGenerator.Core;

public sealed record ParameterLeaf(string Type, string Name);

public static class SignatureParser
{
    private static readonly string[] Modifiers = ["out", "ref", "in", "params", "this", "scoped", "readonly"];

    public static IReadOnlyList<ParameterLeaf> Parameters(string? signature)
    {
        if (string.IsNullOrWhiteSpace(signature))
            return [];

        var open = IndexOfParameterOpen(signature);
        if (open < 0)
            return [];
        var close = MatchingParen(signature, open);
        if (close < 0)
            return [];

        var inner = signature[(open + 1)..close];
        if (string.IsNullOrWhiteSpace(inner))
            return [];

        var leaves = new List<ParameterLeaf>();
        foreach (var part in SplitTopLevel(inner))
        {
            var leaf = ParseOne(part);
            if (leaf is not null)
                leaves.Add(leaf);
        }

        return leaves;
    }

    private static ParameterLeaf? ParseOne(string part)
    {
        var body = CutDefault(part.Trim());
        body = StripModifiers(body);
        if (body.Length == 0)
            return null;

        var nameStart = body.Length;
        while (nameStart > 0 && IsNameChar(body[nameStart - 1]))
            nameStart--;
        if (nameStart == body.Length || nameStart == 0)
            return null;
        if (nameStart > 0 && !char.IsWhiteSpace(body[nameStart - 1]))
            return null;

        var name = body[nameStart..].Trim();
        var type = body[..nameStart].Trim();
        if (name.Length == 0 || type.Length == 0)
            return null;
        return new ParameterLeaf(type, name);
    }

    private static string StripModifiers(string text)
    {
        var rest = text.Trim();
        while (rest.Length > 0)
        {
            var cut = rest.IndexOf(' ');
            var token = cut < 0 ? rest : rest[..cut];
            if (!Modifiers.Contains(token, StringComparer.Ordinal))
                break;
            if (cut < 0)
                return "";
            rest = rest[(cut + 1)..].TrimStart();
        }

        return rest;
    }

    private static string CutDefault(string text)
    {
        var paren = 0;
        var angle = 0;
        var bracket = 0;
        for (var i = 0; i < text.Length; i++)
        {
            switch (text[i])
            {
                case '(': paren++; break;
                case ')': if (paren > 0) paren--; break;
                case '<': angle++; break;
                case '>': if (angle > 0) angle--; break;
                case '[': bracket++; break;
                case ']': if (bracket > 0) bracket--; break;
                case '=' when paren == 0 && angle == 0 && bracket == 0
                    && (i == 0 || text[i - 1] is not ('=' or '!' or '<' or '>'))
                    && (i + 1 >= text.Length || text[i + 1] != '='):
                    return text[..i];
            }
        }

        return text;
    }

    private static List<string> SplitTopLevel(string text)
    {
        var parts = new List<string>();
        var paren = 0;
        var angle = 0;
        var bracket = 0;
        var start = 0;
        for (var i = 0; i < text.Length; i++)
        {
            switch (text[i])
            {
                case '(': paren++; break;
                case ')': if (paren > 0) paren--; break;
                case '<': angle++; break;
                case '>': if (angle > 0) angle--; break;
                case '[': bracket++; break;
                case ']': if (bracket > 0) bracket--; break;
                case ',' when paren == 0 && angle == 0 && bracket == 0:
                    parts.Add(text[start..i]);
                    start = i + 1;
                    break;
            }
        }

        parts.Add(text[start..]);
        return parts;
    }

    private static int IndexOfParameterOpen(string text)
    {
        var angle = 0;
        var bracket = 0;
        for (var i = 0; i < text.Length; i++)
        {
            switch (text[i])
            {
                case '<': angle++; break;
                case '>': if (angle > 0) angle--; break;
                case '[': bracket++; break;
                case ']': if (bracket > 0) bracket--; break;
                case '(' when angle == 0 && bracket == 0:
                    return i;
            }
        }

        return -1;
    }

    private static int MatchingParen(string text, int open)
    {
        var paren = 0;
        var angle = 0;
        var bracket = 0;
        for (var i = open; i < text.Length; i++)
        {
            switch (text[i])
            {
                case '(': paren++; break;
                case ')':
                    paren--;
                    if (paren == 0 && angle == 0 && bracket == 0)
                        return i;
                    break;
                case '<': angle++; break;
                case '>': if (angle > 0) angle--; break;
                case '[': bracket++; break;
                case ']': if (bracket > 0) bracket--; break;
            }
        }

        return -1;
    }

    private static bool IsNameChar(char value) =>
        char.IsLetterOrDigit(value) || value == '_';
}
