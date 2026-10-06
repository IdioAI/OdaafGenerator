using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using OdaafGenerator.Core;

namespace OdaafGenerator.App;

public partial class LayerSummaryWindow : Window
{
    private readonly MainViewModel _model;

    public LayerSummaryWindow(MainViewModel model)
    {
        _model = model;
        InitializeComponent();
        DataContext = model;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }

    private void Export_Click(object sender, RoutedEventArgs e) => _model.ExportBrief();

    private async void Propose_Click(object sender, RoutedEventArgs e) => await _model.ProposeNamesAsync();

    private void Accept_Click(object sender, RoutedEventArgs e) => _model.AcceptProposals();

    private void Row_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.Controls.ListBox { SelectedItem: ExecuteRow row })
            _model.FocusExecuteRow(row);
    }
}
