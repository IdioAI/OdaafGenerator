using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using OdaafGenerator.Core;

namespace OdaafGenerator.App;

public sealed class SearchLevelOption
{
    public SearchLevelOption(SearchLevel level, string label)
    {
        Level = level;
        Label = label;
    }

    public SearchLevel Level { get; }
    public string Label { get; }
    public override string ToString() => Label;
}

public sealed class MainViewModel : INotifyPropertyChanged
{
    private string _projectFilePath = "";
    private string _status = "尚未加载工程索引。";
    private string _contextLine = "未加载";
    private string _searchText = "";
    private SearchLevelOption _chosenLevel;
    private FunctionCard? _selectedCard;
    private Catalog? _catalog;
    private LayoutFile? _layout;
    private GeneratorProject? _project;
    private ClassScatterGraph? _scatter;
    private ProjectIndexInfo? _projectIndex;
    private string _contextId = OdaafFileContext.Id;
    private IReadOnlyList<ProjectEntry> _projects = [];
    private IReadOnlyList<SearchHit> _hits = [];
    private IReadOnlyList<FunctionCard> _menuCards = [];
    private IReadOnlyList<GraphEdge> _menuEdges = [];
    private IReadOnlyList<FunctionCard> _zCards = [];
    private IReadOnlyList<GraphEdge> _zEdges = [];
    private IReadOnlyList<WorkspaceView> _contextViews = [];
    private IReadOnlyList<NavNode> _navRoots = [];
    private IReadOnlyList<FunctionCard> _navCards = [];
    private IReadOnlyList<GraphEdge> _navEdges = [];
    private NavNode? _selectedNav;
    private WorkspaceDocument? _activeDocument;
    private WorkspaceView? _selectedView;

    public MainViewModel()
    {
        Levels =
        [
            new SearchLevelOption(SearchLevel.Project, "项目"),
            new SearchLevelOption(SearchLevel.Namespace, "命名空间"),
            new SearchLevelOption(SearchLevel.Class, "类"),
            new SearchLevelOption(SearchLevel.Function, "函数")
        ];
        _chosenLevel = Levels[3];
        LayoutPath = LayoutLocation.DefaultFile();
        _projectFilePath = GeneratorProject.FindDefaultFile() ?? "";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<SearchLevelOption> Levels { get; }
    public string LayoutPath { get; }
    public ObservableCollection<WorkspaceDocument> Documents { get; } = [];

    public string ProjectFilePath
    {
        get => _projectFilePath;
        set => Set(ref _projectFilePath, value);
    }

    public string SearchText
    {
        get => _searchText;
        set => Set(ref _searchText, value);
    }

    public SearchLevelOption ChosenLevel
    {
        get => _chosenLevel;
        set => Set(ref _chosenLevel, value);
    }

    public string Status
    {
        get => _status;
        set => Set(ref _status, value);
    }

    public string ContextLine
    {
        get => _contextLine;
        private set => Set(ref _contextLine, value);
    }

    public FunctionCard? SelectedCard
    {
        get => _selectedCard;
        set
        {
            if (!Set(ref _selectedCard, value))
                return;
            OnPropertyChanged(nameof(CanEnterZ));
            OnPropertyChanged(nameof(CanExpandLayer));
            OnPropertyChanged(nameof(CanOpenCluster));
        }
    }

    public IReadOnlyList<ProjectEntry> Projects
    {
        get => _projects;
        private set => Set(ref _projects, value);
    }

    public IReadOnlyList<SearchHit> Hits
    {
        get => _hits;
        private set => Set(ref _hits, value);
    }

    public IReadOnlyList<FunctionCard> MenuCards => _menuCards;
    public ProjectIndexInfo? ProjectIndex
    {
        get => _projectIndex;
        private set => Set(ref _projectIndex, value);
    }

    public string ContextId
    {
        get => _contextId;
        private set
        {
            if (!Set(ref _contextId, value))
                return;
            OnPropertyChanged(nameof(IsIndexContext));
            OnPropertyChanged(nameof(IsOdaafContext));
            OnPropertyChanged(nameof(IsFileNavContext));
            OnPropertyChanged(nameof(ContextName));
            RefreshContextViews();
            RefreshLine();
        }
    }

    public bool IsIndexContext => ContextId == IndexContext.Id;
    public bool IsOdaafContext => ContextId == OdaafFileContext.Id;
    public bool IsFileNavContext => ContextId == FileNavContext.Id;
    public string ContextName => ContextId switch
    {
        IndexContext.Id => "解析入库",
        FileNavContext.Id => FileNavContext.Name,
        _ => "ODAAF"
    };

    public IReadOnlyList<NavNode> NavRoots
    {
        get => _navRoots;
        private set => Set(ref _navRoots, value);
    }

    public IReadOnlyList<FunctionCard> NavCards
    {
        get => _navCards;
        private set => Set(ref _navCards, value);
    }

    public IReadOnlyList<GraphEdge> NavEdges
    {
        get => _navEdges;
        private set => Set(ref _navEdges, value);
    }

    public NavNode? SelectedNav
    {
        get => _selectedNav;
        private set => Set(ref _selectedNav, value);
    }

    public IReadOnlyList<WorkspaceView> ContextViews
    {
        get => _contextViews;
        private set => Set(ref _contextViews, value);
    }

    public WorkspaceView? SelectedView
    {
        get => _selectedView;
        set
        {
            if (!Set(ref _selectedView, value) || value is null)
                return;
            Navigate(value.Id);
        }
    }

    public WorkspaceDocument? ActiveDocument
    {
        get => _activeDocument;
        set
        {
            if (!Set(ref _activeDocument, value))
                return;
            if (value is not null && value.ContextId != ContextId)
                ContextId = value.ContextId;
            _selectedView = ContextViews.FirstOrDefault(view => view.Id == value?.ViewId);
            OnPropertyChanged(nameof(SelectedView));
            OnPropertyChanged(nameof(CanEnterZ));
            OnPropertyChanged(nameof(CanExpandLayer));
            OnPropertyChanged(nameof(CanOpenCluster));
            RefreshLine();
        }
    }

    public bool CanEnterZ =>
        IsOdaafContext
        && ActiveDocument?.ViewId == ViewIds.FileMenu
        && (SelectedCard?.OpensZExtract == true || _menuCards.Any(card => card.OpensZExtract));

    public bool CanExpandLayer =>
        IsOdaafContext
        && SelectedCard is not null
        && ActiveDocument?.ViewId is ViewIds.FileMenu or ViewIds.ZExtract or ViewIds.FunctionLayer;

    public bool CanOpenCluster =>
        ActiveDocument?.ViewId == ViewIds.ClassScatter && SelectedCard is not null;

    public void Load()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ProjectFilePath))
                throw new FileNotFoundException("找不到工程索引 rapidcad-zextract.ogproj。");
            var project = GeneratorProject.Load(ProjectFilePath);
            ProjectFilePath = project.FilePath;
            if (!File.Exists(project.SolutionPath))
                throw new FileNotFoundException("工程索引里的源码解决方案不存在。", project.SolutionPath);
            if (!File.Exists(project.OdaafPath))
                throw new FileNotFoundException("工程索引里的 odaaf 不存在。", project.OdaafPath);

            var (projects, rows, host) = DeclarationStore.Load(project.DatabasePath);
            _project = project;
            _catalog = Catalog.Create(projects, rows, host);
            _layout = LayoutStore.Load(LayoutPath);
            _scatter = ClassScatterGraph.Build(_catalog);
            Projects = _catalog.Projects;
            _zCards = _catalog.BuildXmlContext(_layout).ToList();
            _zEdges = XmlScene.Edges.Select(edge => new GraphEdge(edge.From, edge.To)).ToList();
            (_menuCards, _menuEdges) = FileMenu.Build(_catalog, _layout);
            OnPropertyChanged(nameof(MenuCards));
            ProjectIndex = ProjectIndexInfo.From(project, LayoutPath, projects.Count, rows.Count);
            NavRoots = MenuNavTree.Build(_catalog);
            Documents.Clear();
            ContextId = project.DefaultContextId switch
            {
                IndexContext.Id => IndexContext.Id,
                FileNavContext.Id => FileNavContext.Id,
                _ => OdaafFileContext.Id
            };
            OpenDefaults();
            Status = IsFileNavContext
                ? $"{project.Name}。左边从文件菜单展开处理函数，右边画这一层的流程图。"
                : $"{project.Name}。项目 {projects.Count} 个，焦点声明 {rows.Count} 条。";
        }
        catch (Exception ex)
        {
            _catalog = null;
            _layout = null;
            _project = null;
            _scatter = null;
            Projects = [];
            _menuCards = [];
            _zCards = [];
            ProjectIndex = null;
            NavRoots = [];
            NavCards = [];
            NavEdges = [];
            SelectedNav = null;
            Documents.Clear();
            SelectedCard = null;
            ActiveDocument = null;
            Status = ex is FileNotFoundException file && !string.IsNullOrEmpty(file.FileName)
                ? $"{ex.Message} {file.FileName}"
                : ex.Message;
            RefreshLine();
        }
    }

    public void ShowIndex() => SwitchContext(IndexContext.Id);

    public void ShowOdaaf() => SwitchContext(OdaafFileContext.Id);

    public void ShowFileNav() => SwitchContext(FileNavContext.Id);

    public void ExpandNav(NavNode? node)
    {
        if (node is null || _catalog is null || _project is null)
            return;
        MenuNavTree.Expand(node, _catalog, _project.SourceRoot);
    }

    public void SelectNav(NavNode? node)
    {
        if (node is null || _catalog is null || _layout is null || _project is null)
            return;
        if (node.Kind is NavNode.More or NavNode.Empty)
            return;
        SelectedNav = node;
        if (node.Kind == NavNode.Function && !node.MainFlow)
        {
            NavCards =
            [
                new FunctionCard
                {
                    SyntaxPath = node.SyntaxPath ?? node.Title,
                    Name = node.Title,
                    StepLabel = "修饰",
                    Project = node.Symbol?.Project ?? "",
                    Signature = node.Symbol?.Signature ?? "",
                    FilePath = node.Symbol?.FilePath ?? "",
                    StartLine = node.Symbol?.StartLine ?? 0,
                    EndLine = node.Symbol?.EndLine ?? 0,
                    Materials = SignatureParser.Parameters(node.Symbol?.Signature),
                    OnCanvas = true,
                    MainFlow = false,
                    X = 24,
                    Y = 36
                }
            ];
            NavEdges = [];
            SelectedCard = NavCards[0];
            Status = $"{node.Title} 是修饰，不进入主流程。";
            RefreshLine();
            return;
        }

        if (node.Kind == NavNode.Phase)
        {
            var functions = node.Children.Where(child => child.Kind == NavNode.Function).ToList();
            NavCards = functions.Select((item, index) => new FunctionCard
            {
                SyntaxPath = item.SyntaxPath ?? item.Title,
                Name = item.Title,
                StepLabel = node.Title,
                Project = item.Symbol?.Project ?? "",
                Signature = item.Symbol?.Signature ?? "",
                FilePath = item.Symbol?.FilePath ?? "",
                StartLine = item.Symbol?.StartLine ?? 0,
                EndLine = item.Symbol?.EndLine ?? 0,
                Materials = SignatureParser.Parameters(item.Symbol?.Signature),
                OnCanvas = true,
                MainFlow = node.MainFlow,
                X = 24,
                Y = 36 + index * 96
            }).ToList();
            NavEdges = [];
            SelectedCard = NavCards.FirstOrDefault();
            Status = node.MainFlow
                ? $"{node.Title}：主流程 {functions.Count} 个函数。"
                : $"{node.Title}：修饰 {functions.Count} 个函数，不进入主流程。";
            RefreshLine();
            return;
        }

        if (node.Kind == NavNode.Group || (node.Kind == NavNode.Menu && node.MethodName is null))
        {
            var items = node.Children.Where(child => child.Kind == NavNode.Menu && child.MethodName is not null).ToList();
            NavCards = items.Select((item, index) => new FunctionCard
            {
                SyntaxPath = item.Children.FirstOrDefault()?.SyntaxPath ?? item.Title,
                Name = item.Title,
                StepLabel = node.Title,
                Project = "",
                Signature = item.MethodName ?? "",
                FilePath = "",
                StartLine = 0,
                EndLine = 0,
                Materials = [],
                OnCanvas = true,
                MainFlow = false,
                X = 24,
                Y = 36 + index * 96
            }).ToList();
            NavEdges = [];
            SelectedCard = NavCards.FirstOrDefault();
            Status = $"{node.Title}：{items.Count} 个菜单项。点进去看处理函数。";
            RefreshLine();
            return;
        }

        var symbol = node.Symbol ?? (node.MethodName is null ? null : _catalog.FindMethod(node.MethodName));
        if (symbol is null)
        {
            NavCards = [];
            NavEdges = [];
            SelectedCard = null;
            Status = $"{node.Title} 没有对上处理函数。";
            RefreshLine();
            return;
        }

        var layer = CallLayerGraph.Expand(symbol, _project.SourceRoot, _catalog);
        var graph = layer.Cards(_layout);
        NavCards = graph.Cards;
        NavEdges = graph.Edges;
        SelectedCard = graph.Cards.FirstOrDefault();
        Status = layer.Menu
            ? $"{symbol.SimpleName} 模式A。主流程只画执行，空段不显示。"
            : $"{symbol.SimpleName} 模式B。主流程只画执行，空段不显示。";
        RefreshLine();
    }

    public void Navigate(string viewId, string? focusPath = null)
    {
        if (_catalog is null)
        {
            Status = "先打开工程索引。";
            return;
        }

        if (viewId == ViewIds.ZExtract)
            ContextId = OdaafFileContext.Id;
        if (viewId is ViewIds.IndexBrowse)
            ContextId = IndexContext.Id;
        if (viewId is ViewIds.FileMenu or ViewIds.ClassScatter or ViewIds.FunctionLayer or ViewIds.ClassCluster)
            ContextId = OdaafFileContext.Id;

        var existing = Documents.FirstOrDefault(document =>
            document.ContextId == ContextId
            && document.ViewId == viewId
            && document.FocusPath == focusPath);
        if (existing is not null)
        {
            ActiveDocument = existing;
            Status = $"{ContextName} · {existing.Title}";
            return;
        }

        var document = CreateDocument(viewId, focusPath);
        if (document is null)
            return;
        Documents.Add(document);
        ActiveDocument = document;
        Status = $"{ContextName} · {document.Title}";
    }

    public void EnterZExtract() => Navigate(ViewIds.ZExtract);

    public void OpenCluster()
    {
        if (SelectedCard is null || ActiveDocument?.ViewId != ViewIds.ClassScatter)
            return;
        Navigate(ViewIds.ClassCluster, SelectedCard.SyntaxPath);
    }

    public void ExpandLayer()
    {
        if (SelectedCard is null)
            return;
        Navigate(ViewIds.FunctionLayer, SelectedCard.SyntaxPath);
    }

    public void Search()
    {
        if (_catalog is null)
        {
            Hits = [];
            Status = "先加载工程索引。";
            return;
        }

        var result = _catalog.Search(ChosenLevel.Level, SearchText);
        Hits = result.Hits;
        Status = result.Truncated
            ? $"{ChosenLevel.Label} 命中超过 200 条，只显示前 200 条。"
            : $"{ChosenLevel.Label} 命中 {result.Hits.Count} 条。";
        if (ContextId != IndexContext.Id)
            SwitchContext(IndexContext.Id);
        Navigate(ViewIds.IndexBrowse);
    }

    public void SelectHit(SearchHit? hit)
    {
        if (hit is null)
            return;
        if (hit.Level == SearchLevel.Function && hit.SyntaxPath is not null && IsOdaafContext)
        {
            Navigate(ViewIds.FunctionLayer, hit.SyntaxPath);
            return;
        }

        Status = hit.Level == SearchLevel.Project && hit.Subtitle.StartsWith("辅助", StringComparison.Ordinal)
            ? $"{hit.Title} 是辅助工程，目录只到项目。"
            : $"{hit.Title}  {hit.Subtitle}";
    }

    public void SelectTree(object? item)
    {
        switch (item)
        {
            case ProjectEntry project when !project.IsFocus:
                Status = $"{project.Name} 是辅助工程，目录只到项目。";
                break;
            case CodeSymbol symbol when IsOdaafContext && SymbolKinds.IsFunction(symbol.Kind):
                Navigate(ViewIds.FunctionLayer, symbol.SyntaxPath);
                break;
            case CodeSymbol symbol:
                Status = $"{symbol.SimpleName}  {symbol.SyntaxPath}";
                break;
            case TypeEntry type:
                Status = $"{type.Name}  {type.SyntaxPath}";
                break;
            case NamespaceEntry space:
                Status = $"{space.Name}  {space.Project}";
                break;
        }
    }

    public void SelectMenu(FunctionCard? card)
    {
        if (card is null)
            return;
        Navigate(ViewIds.FileMenu);
        SelectedCard = _menuCards.FirstOrDefault(item => item.SyntaxPath == card.SyntaxPath) ?? card;
        Status = $"{SelectedCard.StepLabel}  {SelectedCard.Name}";
    }

    public void SelectCard(FunctionCard? card)
    {
        if (card is null)
            return;
        SelectedCard = card;
        if (ActiveDocument?.ViewId == ViewIds.ClassScatter)
            Status = $"簇 {card.Name}，{card.StepLabel}";
        else
            Status = $"{card.StepLabel}  {card.Name}";
    }

    public void SaveLayout()
    {
        if (_layout is null)
            return;
        foreach (var card in Documents.SelectMany(document => document.Cards))
            _layout.Upsert(card);
        if (SelectedCard is not null)
            _layout.Upsert(SelectedCard);
        LayoutStore.Save(LayoutPath, _layout);
    }

    private void SwitchContext(string contextId)
    {
        ContextId = contextId;
        if (IsFileNavContext)
        {
            var start = MenuNavTree.FindHandler(NavRoots, FileMenu.ImportXmlMethod)
                ?? NavRoots.SelectMany(group => group.Children).SelectMany(menu => menu.Children).FirstOrDefault();
            SelectNav(start);
            Status = "文件菜单。左边展开菜单到处理函数，右边是这一层的流程图。";
            return;
        }

        var current = Documents.LastOrDefault(document => document.ContextId == contextId);
        if (current is not null)
            ActiveDocument = current;
        else
            Navigate(ViewIds.ProjectIndex);
        Status = $"{ContextName}。左侧是这个上下文的视图。";
    }

    private void OpenDefaults()
    {
        RefreshContextViews();
        if (IsFileNavContext)
        {
            var start = MenuNavTree.FindHandler(NavRoots, FileMenu.ImportXmlMethod)
                ?? NavRoots.SelectMany(group => group.Children).SelectMany(menu => menu.Children).FirstOrDefault();
            SelectNav(start);
            return;
        }

        Navigate(ViewIds.ProjectIndex);
    }

    private WorkspaceDocument? CreateDocument(string viewId, string? focusPath)
    {
        if (_catalog is null || _layout is null)
            return null;
        var title = viewId switch
        {
            ViewIds.ClassCluster => "簇 · " + (_scatter?.Clusters.FirstOrDefault(cluster => cluster.Id == focusPath)?.Name ?? focusPath),
            ViewIds.FunctionLayer => "函数层 · " + SyntaxName.Last(focusPath ?? ""),
            _ => WorkspaceViews.Title(viewId)
        };
        var document = new WorkspaceDocument(
            viewId + (focusPath is null ? "" : ":" + focusPath),
            ContextId,
            viewId,
            title,
            focusPath);
        Fill(document);
        return document;
    }

    private void Fill(WorkspaceDocument document)
    {
        if (_catalog is null || _layout is null)
            return;
        switch (document.ViewId)
        {
            case ViewIds.FileMenu:
                document.Cards = _menuCards;
                document.Edges = _menuEdges;
                SelectedCard = _menuCards.FirstOrDefault();
                break;
            case ViewIds.ZExtract:
                document.Cards = _zCards;
                document.Edges = _zEdges;
                SelectedCard = _zCards.FirstOrDefault();
                break;
            case ViewIds.ClassScatter when _scatter is not null:
                document.Cards = _scatter.ClusterCards();
                document.Edges = [];
                SelectedCard = document.Cards.FirstOrDefault();
                break;
            case ViewIds.ClassCluster when _scatter is not null && document.FocusPath is not null:
                var members = _scatter.Members(document.FocusPath);
                document.Cards = members.Cards;
                document.Edges = members.Edges;
                SelectedCard = document.Cards.FirstOrDefault();
                break;
            case ViewIds.FunctionLayer:
                FillFunctionLayer(document);
                break;
            default:
                document.Cards = [];
                document.Edges = [];
                break;
        }
    }

    private void FillFunctionLayer(WorkspaceDocument document)
    {
        if (_catalog is null || _layout is null || _project is null)
            return;
        var path = document.FocusPath
            ?? SelectedCard?.SyntaxPath
            ?? _menuCards.FirstOrDefault(card => card.OpensZExtract)?.SyntaxPath
            ?? _menuCards.FirstOrDefault()?.SyntaxPath;
        if (path is null)
        {
            document.Cards = [];
            document.Edges = [];
            return;
        }

        var symbol = _catalog.Find(path) ?? _catalog.FindMethod(SyntaxName.Last(path));
        if (symbol is null)
        {
            document.Title = "函数层 · " + SyntaxName.Last(path);
            document.Cards = [];
            document.Edges = [];
            Status = "索引里没有这个函数，画不出上一层。";
            return;
        }

        var layer = CallLayerGraph.Expand(symbol, _project.SourceRoot, _catalog);
        var graph = layer.Cards(_layout);
        document.Title = "函数层 · " + symbol.SimpleName;
        document.Cards = graph.Cards;
        document.Edges = graph.Edges;
        SelectedCard = graph.Cards.FirstOrDefault();
        Status = $"{symbol.SimpleName} 的上一层有 {layer.Calls.Count} 个直接调用。不跨进被调函数。";
    }

    private void RefreshContextViews() =>
        ContextViews = WorkspaceViews.For(ContextId);

    private void RefreshLine()
    {
        var view = IsFileNavContext ? SelectedNav?.Title ?? "点左边的菜单" : ActiveDocument?.Title ?? "无文档";
        var name = _project?.Name ?? "未打开";
        ContextLine = $"{ContextName}  ·  {view}  ·  {name}";
    }

    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
}
