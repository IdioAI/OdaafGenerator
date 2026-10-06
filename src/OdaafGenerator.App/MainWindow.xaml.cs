using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using OdaafGenerator.Core;

namespace OdaafGenerator.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _model = new();
    private LayerSummaryWindow? _summary;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _model;
        NavFlow.CardChosen += (_, card) => _model.SelectCard(card);
        NavFlow.DragCompleted += (_, _) => _model.SaveLayout();
        _model.RevealRequested += path => NavFlow.Reveal(path);
        _model.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(MainViewModel.NavCards) or nameof(MainViewModel.NavEdges))
            {
                NavFlow.Cards = _model.NavCards;
                NavFlow.Edges = _model.NavEdges;
            }

            if (args.PropertyName == nameof(MainViewModel.SelectedCard) && _model.IsFileNavContext)
                NavFlow.SelectedSyntaxPath = _model.SelectedCard?.SyntaxPath;
        };
        _model.Load();
        NavFlow.Cards = _model.NavCards;
        NavFlow.Edges = _model.NavEdges;
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

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    private void ShowIndex_Click(object sender, RoutedEventArgs e) => _model.ShowIndex();

    private void ShowOdaaf_Click(object sender, RoutedEventArgs e) => _model.ShowOdaaf();

    private void ShowFileNav_Click(object sender, RoutedEventArgs e) => _model.ShowFileNav();

    private void ViewProject_Click(object sender, RoutedEventArgs e) => _model.Navigate(ViewIds.ProjectIndex);

    private void ViewBrowse_Click(object sender, RoutedEventArgs e) => _model.Navigate(ViewIds.IndexBrowse);

    private void ViewMenu_Click(object sender, RoutedEventArgs e) => _model.Navigate(ViewIds.FileMenu);

    private void ViewZ_Click(object sender, RoutedEventArgs e) => _model.Navigate(ViewIds.ZExtract);

    private void ViewScatter_Click(object sender, RoutedEventArgs e) => _model.Navigate(ViewIds.ClassScatter);

    private void ViewLayer_Click(object sender, RoutedEventArgs e) => _model.Navigate(ViewIds.FunctionLayer);

    private void Summary_Click(object sender, RoutedEventArgs e)
    {
        _summary ??= new LayerSummaryWindow(_model);
        _summary.Show();
        _summary.Activate();
    }

    private void Segments_Click(object sender, RoutedEventArgs e) => _model.ShowSegments();

    private void Boxes_Click(object sender, RoutedEventArgs e) => _model.ShowAllBoxes();

    private void Cause_LostFocus(object sender, RoutedEventArgs e) => _model.SaveLayout();

    private void Nav_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is NavNode node)
            _model.SelectNav(node);
    }

    private void Nav_Expanded(object sender, RoutedEventArgs e)
    {
        if (sender is TreeViewItem { DataContext: NavNode node })
            _model.ExpandNav(node);
        e.Handled = true;
    }
}
