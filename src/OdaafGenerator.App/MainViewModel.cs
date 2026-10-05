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
        set => Set(ref _selectedCard, value);
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

            var (projects, rows) = DeclarationStore.Load(project.DatabasePath);
            _catalog = Catalog.Create(projects, rows);
            _layout = LayoutStore.Load(LayoutPath);
            Projects = _catalog.Projects;
            Cards = _catalog.BuildXmlContext(_layout);
            SelectedCard = Cards.FirstOrDefault();
            Status = $"{project.Name}。已加载 {projects.Count} 个项目，焦点声明 {rows.Count} 条。XML 上下文 {Cards.Count} 个函数。本体 {project.GraphVersion}。";
        }
        catch (Exception ex)
        {
            _catalog = null;
            _layout = null;
            Projects = [];
            Cards = [];
            SelectedCard = null;
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
        if (hit is null)
            return;
        if (hit.Level == SearchLevel.Function && hit.SyntaxPath is not null)
        {
            SelectSymbol(hit.SyntaxPath);
            return;
        }

        if (hit.Level == SearchLevel.Project && hit.Subtitle.StartsWith("辅助", StringComparison.Ordinal))
            Status = $"{hit.Title} 是辅助工程，目录只到项目。";
        else
            Status = $"{hit.Title}  {hit.Subtitle}";
    }

    public void SelectTree(object? item)
    {
        switch (item)
        {
            case ProjectEntry project when !project.IsFocus:
                Status = $"{project.Name} 是辅助工程，目录只到项目。";
                break;
            case CodeSymbol symbol when SymbolKinds.IsFunction(symbol.Kind):
                SelectSymbol(symbol.SyntaxPath);
                break;
        }
    }

    public void SaveLayout()
    {
        if (_layout is null)
            return;
        foreach (var card in Cards)
            _layout.Upsert(card);
        if (SelectedCard is { OnCanvas: false })
            _layout.Upsert(SelectedCard);
        LayoutStore.Save(LayoutPath, _layout);
    }

    private void SelectSymbol(string syntaxPath)
    {
        if (_catalog is null || _layout is null)
            return;
        var onCanvas = Cards.FirstOrDefault(card => card.SyntaxPath == syntaxPath);
        if (onCanvas is not null)
        {
            SelectedCard = onCanvas;
            Status = $"{onCanvas.StepLabel}  {onCanvas.Name}";
            return;
        }

        var symbol = _catalog.Find(syntaxPath);
        if (symbol is null || !SymbolKinds.IsFunction(symbol.Kind))
            return;
        SelectedCard = _catalog.BranchFor(symbol, _layout);
        Status = $"{symbol.SimpleName} 不在 XML 上下文本体图上。枝图仍可看。";
    }

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
