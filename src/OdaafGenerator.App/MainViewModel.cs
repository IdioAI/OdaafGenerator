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
    private string _databasePath = "";
    private string _anchorLine = "";
    private string _searchText = "";
    private string _status = "尚未加载索引。";
    private SearchLevelOption _chosenLevel;
    private FunctionCard? _selectedCard;
    private Catalog? _catalog;
    private LayoutFile? _layout;
    private IReadOnlyList<ProjectEntry> _projects = [];
    private IReadOnlyList<FunctionCard> _cards = [];
    private IReadOnlyList<SearchHit> _hits = [];
    private string _contextId = OdaafFileContext.Id;
    private bool _showingZExtract;
    private string _indexSummary = "";
    private string _canvasTitle = "文件菜单";
    private IReadOnlyList<GraphEdge> _canvasEdges = [];
    private IReadOnlyList<FunctionCard> _menuCards = [];
    private IReadOnlyList<GraphEdge> _menuEdges = [];
    private IReadOnlyList<FunctionCard> _zCards = [];
    private IReadOnlyList<GraphEdge> _zEdges = [];
    private FunctionCard? _importCard;

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

    public string ProjectFilePath
    {
        get => _projectFilePath;
        set => Set(ref _projectFilePath, value);
    }

    public string DatabasePath
    {
        get => _databasePath;
        private set => Set(ref _databasePath, value);
    }

    public string AnchorLine
    {
        get => _anchorLine;
        private set => Set(ref _anchorLine, value);
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

    public FunctionCard? SelectedCard
    {
        get => _selectedCard;
        set
        {
            if (!Set(ref _selectedCard, value))
                return;
            OnPropertyChanged(nameof(CanEnterZ));
        }
    }

    public IReadOnlyList<ProjectEntry> Projects
    {
        get => _projects;
        private set => Set(ref _projects, value);
    }

    public IReadOnlyList<FunctionCard> Cards
    {
        get => _cards;
        private set => Set(ref _cards, value);
    }

    public IReadOnlyList<SearchHit> Hits
    {
        get => _hits;
        private set => Set(ref _hits, value);
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
            OnPropertyChanged(nameof(CanEnterZ));
        }
    }

    public bool IsIndexContext => ContextId == IndexContext.Id;
    public bool IsOdaafContext => ContextId == OdaafFileContext.Id;

    public bool ShowingZExtract
    {
        get => _showingZExtract;
        private set
        {
            if (!Set(ref _showingZExtract, value))
                return;
            OnPropertyChanged(nameof(CanEnterZ));
        }
    }

    public bool CanEnterZ => IsOdaafContext && !ShowingZExtract && SelectedCard?.OpensZExtract == true;

    public string CanvasTitle
    {
        get => _canvasTitle;
        private set => Set(ref _canvasTitle, value);
    }

    public string IndexSummary
    {
        get => _indexSummary;
        private set => Set(ref _indexSummary, value);
    }

    public IReadOnlyList<GraphEdge> CanvasEdges
    {
        get => _canvasEdges;
        private set => Set(ref _canvasEdges, value);
    }

    public IReadOnlyList<FunctionCard> MenuCards => _menuCards;

    public void Load()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ProjectFilePath))
                throw new FileNotFoundException("找不到工程索引 rapidcad-zextract.ogproj。");
            var project = GeneratorProject.Load(ProjectFilePath);
            ProjectFilePath = project.FilePath;
            DatabasePath = project.DatabasePath;
            AnchorLine = $"数据库  {project.DatabasePath}    源码  {project.SourceRoot}    本体  {project.OdaafPath}";
            if (!File.Exists(project.SolutionPath))
                throw new FileNotFoundException("工程索引里的源码解决方案不存在。", project.SolutionPath);
            if (!File.Exists(project.OdaafPath))
                throw new FileNotFoundException("工程索引里的 odaaf 不存在。", project.OdaafPath);

            var (projects, rows, host) = DeclarationStore.Load(project.DatabasePath);
            _catalog = Catalog.Create(projects, rows, host);
            _layout = LayoutStore.Load(LayoutPath);
            Projects = _catalog.Projects;
            _zCards = _catalog.BuildXmlContext(_layout).ToList();
            _zEdges = XmlScene.Edges.Select(edge => new GraphEdge(edge.From, edge.To)).ToList();
            (_menuCards, _menuEdges) = FileMenu.Build(_catalog, _layout);
            _importCard = _menuCards.FirstOrDefault(card => card.OpensZExtract);
            OnPropertyChanged(nameof(MenuCards));
            var focusNames = Projects.Where(item => item.IsFocus).Select(item => item.Name);
            IndexSummary =
                $"数据库\n{project.DatabasePath}\n\n源码\n{project.SourceRoot}\n\n声明 {rows.Count} 条，项目 {projects.Count} 个。\n焦点项目：{string.Join("、", focusNames)}。\n\n这里只看解析入库的结果，不打开函数枝图。";
            ShowingZExtract = false;
            ContextId = project.DefaultContextId == IndexContext.Id ? IndexContext.Id : OdaafFileContext.Id;
            if (IsOdaafContext)
                ShowMenuCanvas();
            else
                ShowIndexCanvas();
            SelectedCard = IsOdaafContext ? _menuCards.FirstOrDefault() : null;
            Status = $"{project.Name}。已加载 {projects.Count} 个项目，焦点声明 {rows.Count} 条。当前上下文：{(IsOdaafContext ? "ODAAF" : "解析入库")}。";
        }
        catch (Exception ex)
        {
            _catalog = null;
            _layout = null;
            Projects = [];
            _menuCards = [];
            _zCards = [];
            OnPropertyChanged(nameof(MenuCards));
            IndexSummary = "";
            SelectedCard = null;
            ShowIndexCanvas();
            Status = ex is FileNotFoundException file && !string.IsNullOrEmpty(file.FileName)
                ? $"{ex.Message} {file.FileName}"
                : ex.Message;
        }
    }

    public void Search()
    {
        if (_catalog is null)
        {
            Hits = [];
            Status = "先加载索引。";
            return;
        }

        var result = _catalog.Search(ChosenLevel.Level, SearchText);
        Hits = result.Hits;
        Status = result.Truncated
            ? $"{ChosenLevel.Label} 命中超过 200 条，只显示前 200 条。"
            : $"{ChosenLevel.Label} 命中 {result.Hits.Count} 条。";
    }

    public void SelectHit(SearchHit? hit)
    {
        if (hit is null || !IsIndexContext)
            return;
        if (hit.Level == SearchLevel.Project && hit.Subtitle.StartsWith("辅助", StringComparison.Ordinal))
            Status = $"{hit.Title} 是辅助工程，目录只到项目。";
        else
            Status = $"{hit.Title}  {hit.Subtitle}";
    }

    public void SelectTree(object? item)
    {
        if (!IsIndexContext)
            return;
        switch (item)
        {
            case ProjectEntry project when !project.IsFocus:
                Status = $"{project.Name} 是辅助工程，目录只到项目。";
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
        ShowingZExtract = false;
        ShowMenuCanvas();
        SelectedCard = _menuCards.FirstOrDefault(item => item.SyntaxPath == card.SyntaxPath) ?? card;
        Status = $"{SelectedCard.StepLabel}  {SelectedCard.Name}";
    }

    public void ShowIndex()
    {
        ContextId = IndexContext.Id;
        SelectedCard = null;
        ShowIndexCanvas();
        Status = "解析入库。目录可查项目、命名空间、类、函数。";
    }

    public void ShowOdaaf()
    {
        ContextId = OdaafFileContext.Id;
        if (ShowingZExtract)
            ShowZCanvas();
        else
            ShowMenuCanvas();
        Status = ShowingZExtract ? "ODAAF · Z 抽出" : "ODAAF · 文件菜单";
    }

    public void EnterZExtract()
    {
        if (!CanEnterZ)
            return;
        _importCard = SelectedCard;
        ShowingZExtract = true;
        ShowZCanvas();
        Status = "ODAAF · 导入XML 下的 Z 抽出";
    }

    public void BackToFileMenu()
    {
        ShowingZExtract = false;
        ShowMenuCanvas();
        SelectedCard = _importCard ?? _menuCards.FirstOrDefault(card => card.OpensZExtract);
        Status = "ODAAF · 文件菜单";
    }

    public void SaveLayout()
    {
        if (_layout is null)
            return;
        foreach (var card in _menuCards.Concat(_zCards))
            _layout.Upsert(card);
        if (SelectedCard is { OnCanvas: false })
            _layout.Upsert(SelectedCard);
        LayoutStore.Save(LayoutPath, _layout);
    }

    private void ShowMenuCanvas()
    {
        Cards = _menuCards;
        CanvasEdges = _menuEdges;
        CanvasTitle = "文件菜单";
    }

    private void ShowZCanvas()
    {
        Cards = _zCards;
        CanvasEdges = _zEdges;
        CanvasTitle = "导入XML · Z 抽出";
    }

    private void ShowIndexCanvas()
    {
        Cards = [];
        CanvasEdges = [];
        CanvasTitle = "解析入库";
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
