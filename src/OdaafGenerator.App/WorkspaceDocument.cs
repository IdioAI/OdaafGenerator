using System.ComponentModel;
using System.Runtime.CompilerServices;
using OdaafGenerator.Core;

namespace OdaafGenerator.App;

public sealed class WorkspaceDocument : INotifyPropertyChanged
{
    private string _title;
    private IReadOnlyList<FunctionCard> _cards = [];
    private IReadOnlyList<GraphEdge> _edges = [];

    public WorkspaceDocument(string id, string contextId, string viewId, string title, string? focusPath = null)
    {
        Id = id;
        ContextId = contextId;
        ViewId = viewId;
        _title = title;
        FocusPath = focusPath;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Id { get; }
    public string ContextId { get; }
    public string ViewId { get; }
    public string? FocusPath { get; }

    public string Title
    {
        get => _title;
        set => Set(ref _title, value);
    }

    public IReadOnlyList<FunctionCard> Cards
    {
        get => _cards;
        set => Set(ref _cards, value);
    }

    public IReadOnlyList<GraphEdge> Edges
    {
        get => _edges;
        set => Set(ref _edges, value);
    }

    public bool IsProjectIndex => ViewId == ViewIds.ProjectIndex;
    public bool IsIndexBrowse => ViewId == ViewIds.IndexBrowse;
    public bool IsGraph => ViewId is ViewIds.FileMenu or ViewIds.ZExtract or ViewIds.FunctionLayer or ViewIds.ClassCluster;
    public bool IsClassScatter => ViewId == ViewIds.ClassScatter;
    public bool IsFileMenu => ViewId == ViewIds.FileMenu;
    public bool IsZExtract => ViewId == ViewIds.ZExtract;
    public bool IsFunctionLayer => ViewId == ViewIds.FunctionLayer;

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
}
