using System.Windows;
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

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    private void ShowIndex_Click(object sender, RoutedEventArgs e) => _model.ShowIndex();

    private void ShowOdaaf_Click(object sender, RoutedEventArgs e) => _model.ShowOdaaf();

    private void ViewProject_Click(object sender, RoutedEventArgs e) => _model.Navigate(ViewIds.ProjectIndex);

    private void ViewBrowse_Click(object sender, RoutedEventArgs e) => _model.Navigate(ViewIds.IndexBrowse);

    private void ViewMenu_Click(object sender, RoutedEventArgs e) => _model.Navigate(ViewIds.FileMenu);

    private void ViewZ_Click(object sender, RoutedEventArgs e) => _model.Navigate(ViewIds.ZExtract);

    private void ViewScatter_Click(object sender, RoutedEventArgs e) => _model.Navigate(ViewIds.ClassScatter);

    private void ViewLayer_Click(object sender, RoutedEventArgs e) => _model.Navigate(ViewIds.FunctionLayer);

    private void Cause_LostFocus(object sender, RoutedEventArgs e) => _model.SaveLayout();
}
