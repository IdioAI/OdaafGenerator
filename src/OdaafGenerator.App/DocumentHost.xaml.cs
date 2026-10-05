using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OdaafGenerator.Core;

namespace OdaafGenerator.App;

public partial class DocumentHost : UserControl
{
    private WorkspaceDocument? _bound;

    public DocumentHost()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => BindCanvas();
        GraphFlow.CardChosen += (_, card) => Model?.SelectCard(card);
        GraphFlow.DragCompleted += (_, _) => Model?.SaveLayout();
        ScatterFlow.CardChosen += (_, card) => Model?.SelectCard(card);
        ScatterFlow.DragCompleted += (_, _) => Model?.SaveLayout();
        Loaded += (_, _) => BindCanvas();
    }

    private MainViewModel? Model => Window.GetWindow(this)?.DataContext as MainViewModel;

    private WorkspaceDocument? Document => DataContext as WorkspaceDocument;

    private void BindCanvas()
    {
        var document = Document;
        if (document is null)
            return;
        if (!ReferenceEquals(_bound, document))
        {
            if (_bound is not null)
                _bound.PropertyChanged -= OnDocumentChanged;
            _bound = document;
            document.PropertyChanged += OnDocumentChanged;
        }

        PushCards(document);
        if (Model is not null)
        {
            GraphFlow.SelectedSyntaxPath = Model.SelectedCard?.SyntaxPath;
            ScatterFlow.SelectedSyntaxPath = Model.SelectedCard?.SyntaxPath;
        }
    }

    private void OnDocumentChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
    {
        if (sender is WorkspaceDocument document
            && args.PropertyName is nameof(WorkspaceDocument.Cards) or nameof(WorkspaceDocument.Edges))
            PushCards(document);
    }

    private void PushCards(WorkspaceDocument document)
    {
        GraphFlow.Cards = document.Cards;
        GraphFlow.Edges = document.Edges;
        ScatterFlow.Cards = document.Cards;
        ScatterFlow.Edges = document.Edges;
    }

    private void Search_Click(object sender, RoutedEventArgs e) => Model?.Search();

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            Model?.Search();
    }

    private void Hits_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is SearchHit hit)
            Model?.SelectHit(hit);
    }

    private void Tree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) =>
        Model?.SelectTree(e.NewValue);

    private void EnterZ_Click(object sender, RoutedEventArgs e) => Model?.EnterZExtract();

    private void Expand_Click(object sender, RoutedEventArgs e) => Model?.ExpandLayer();

    private void Cluster_Click(object sender, RoutedEventArgs e) => Model?.OpenCluster();

    private void Layer_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is FunctionCard card)
            Model?.SelectCard(card);
    }
}
