namespace OdaafGenerator.Core;

public sealed record RawDeclaration(
    string FilePath,
    int StartByte,
    int? ParentStartByte,
    string Kind,
    string SyntaxPath,
    string Signature,
    int StartLine,
    int EndLine,
    string ProjectName);

public sealed record CodeSymbol(
    string Project,
    string FilePath,
    int StartByte,
    string Kind,
    string SyntaxPath,
    string Signature,
    int StartLine,
    int EndLine,
    string Namespace,
    string TypeName,
    string TypeSyntaxPath)
{
    public string SimpleName => SyntaxName.Last(SyntaxPath);
}

public static class SyntaxName
{
    public static string Last(string syntaxPath)
    {
        var dot = syntaxPath.LastIndexOf('.');
        return dot < 0 ? syntaxPath : syntaxPath[(dot + 1)..];
    }
}

public static class SymbolKinds
{
    public static bool IsType(string kind) =>
        kind is "class" or "struct" or "record" or "interface";

    public static bool IsFunction(string kind) =>
        kind is "method" or "property";
}
