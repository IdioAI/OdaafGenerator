using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using OdaafGenerator.Core;

namespace OdaafGenerator.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _model = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _model;
        Flow.CardChosen += (_, card) => _model.SelectedCard = card;
        Flow.DragCompleted += (_, _) => _model.SaveLayout();
        _model.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MainViewModel.Cards))
                Flow.Cards = _model.Cards;
            if (args.PropertyName == nameof(MainViewModel.CanvasEdges))
                Flow.Edges = _model.CanvasEdges;
            if (args.PropertyName == nameof(MainViewModel.SelectedCard))
            {
                Flow.SelectedSyntaxPath = _model.SelectedCard is { OnCanvas: true } card ? card.SyntaxPath : null;
                if (_model.SelectedCard is { OnCanvas: true } selected)
                    Flow.Reveal(selected.SyntaxPath);
            }
        };
        _model.Load();
    }

    private void Load_Click(object sender, RoutedEventArgs e) => _model.Load();

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "工程索引 (*.ogproj)|*.ogproj|所有文件|*.*",
            FileName = _model.ProjectFilePath
        };
        if (dialog.ShowDialog(this) == true)
        {
            _model.ProjectFilePath = dialog.FileName;
            _model.Load();
        }
    }

    private void Search_Click(object sender, RoutedEventArgs e) => _model.Search();

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            _model.Search();
    }

    private void Hits_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is SearchHit hit)
            _model.SelectHit(hit);
    }

    private void Tree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) =>
        _model.SelectTree(e.NewValue);

    private void Cause_LostFocus(object sender, RoutedEventArgs e) => _model.SaveLayout();

    private void ShowIndex_Click(object sender, RoutedEventArgs e) => _model.ShowIndex();

    private void ShowOdaaf_Click(object sender, RoutedEventArgs e) => _model.ShowOdaaf();

    private void EnterZ_Click(object sender, RoutedEventArgs e) => _model.EnterZExtract();

    private void BackToMenu_Click(object sender, RoutedEventArgs e) => _model.BackToFileMenu();

    private void Menu_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is FunctionCard card)
            _model.SelectMenu(card);
    }
}
