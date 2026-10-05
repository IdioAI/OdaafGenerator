using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OdaafGenerator.Core;

public sealed class FunctionCard : INotifyPropertyChanged
{
    private double _x;
    private double _y;
    private string _formal = "";
    private string _final = "";

    public event PropertyChangedEventHandler? PropertyChanged;

    public required string SyntaxPath { get; init; }
    public required string Name { get; init; }
    public required string StepLabel { get; init; }
    public required string Project { get; init; }
    public required string Signature { get; init; }
    public required string FilePath { get; init; }
    public required int StartLine { get; init; }
    public required int EndLine { get; init; }
    public required IReadOnlyList<ParameterLeaf> Materials { get; init; }
    public string? Note { get; init; }
    public bool OnCanvas { get; init; }
    public bool OpensZExtract { get; init; }

    public double X
    {
        get => _x;
        set => Set(ref _x, value);
    }

    public double Y
    {
        get => _y;
        set => Set(ref _y, value);
    }

    public string Formal
    {
        get => _formal;
        set => Set(ref _formal, value);
    }

    public string Final
    {
        get => _final;
        set => Set(ref _final, value);
    }

    public string Location =>
        string.IsNullOrEmpty(FilePath) ? "" : $"{FilePath}:{StartLine}";

    public static FunctionCard FromScene(XmlSlot slot, CodeSymbol? symbol, CodeSymbol? interfaceSymbol, LayoutNode? placed)
    {
        var card = new FunctionCard
        {
            SyntaxPath = slot.SyntaxPath,
            Name = symbol is null ? SyntaxName.Last(slot.SyntaxPath) : symbol.SimpleName,
            StepLabel = slot.StepLabel,
            Project = symbol?.Project ?? "",
            Signature = symbol?.Signature ?? "",
            FilePath = symbol?.FilePath ?? "",
            StartLine = symbol?.StartLine ?? 0,
            EndLine = symbol?.EndLine ?? 0,
            Materials = SignatureParser.Parameters(symbol?.Signature),
            Note = NoteFor(slot, interfaceSymbol),
            OnCanvas = true,
            X = placed?.X ?? slot.X,
            Y = placed?.Y ?? slot.Y,
            Formal = placed?.Formal ?? "",
            Final = placed?.Final ?? ""
        };
        return card;
    }

    public static FunctionCard OffCanvas(CodeSymbol symbol, LayoutNode? placed, bool onCanvas = false) =>
        new()
        {
            SyntaxPath = symbol.SyntaxPath,
            Name = symbol.SimpleName,
            StepLabel = symbol.Kind == "property" ? "属性" : "函数",
            Project = symbol.Project,
            Signature = symbol.Signature,
            FilePath = symbol.FilePath,
            StartLine = symbol.StartLine,
            EndLine = symbol.EndLine,
            Materials = SignatureParser.Parameters(symbol.Signature),
            Note = null,
            OnCanvas = onCanvas,
            X = placed?.X ?? 0,
            Y = placed?.Y ?? 0,
            Formal = placed?.Formal ?? "",
            Final = placed?.Final ?? ""
        };

    private static string? NoteFor(XmlSlot slot, CodeSymbol? interfaceSymbol)
    {
        if (slot.InterfaceSyntaxPath is null)
            return null;
        if (interfaceSymbol is null)
            return $"接口声明 {slot.InterfaceSyntaxPath}。画布上的节点是 LogicLayer 的实现。";
        return $"接口声明 {interfaceSymbol.SyntaxPath}（{interfaceSymbol.FilePath}:{interfaceSymbol.StartLine}）。画布上的节点是 LogicLayer 的实现。";
    }

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
