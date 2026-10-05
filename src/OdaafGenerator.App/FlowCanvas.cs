using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using OdaafGenerator.Core;

namespace OdaafGenerator.App;

public sealed class FlowCanvas : Canvas
{
    private const double NodeWidth = 210;
    private const double NodeHeight = 78;

    private readonly Dictionary<string, Border> _borders = new(StringComparer.Ordinal);
    private readonly List<(string From, string To, Line Line)> _lines = [];
    private readonly List<FunctionCard> _watched = [];
    private Border? _dragBorder;
    private FunctionCard? _dragCard;
    private Point _dragOrigin;
    private double _cardOriginX;
    private double _cardOriginY;
    private bool _moved;

    public static readonly DependencyProperty CardsProperty = DependencyProperty.Register(
        nameof(Cards),
        typeof(IReadOnlyList<FunctionCard>),
        typeof(FlowCanvas),
        new PropertyMetadata(null, (d, _) => ((FlowCanvas)d).Rebuild()));

    public static readonly DependencyProperty SelectedSyntaxPathProperty = DependencyProperty.Register(
        nameof(SelectedSyntaxPath),
        typeof(string),
        typeof(FlowCanvas),
        new PropertyMetadata(null, (d, _) => ((FlowCanvas)d).Restyle()));

    public event EventHandler<FunctionCard>? CardChosen;
    public event EventHandler? DragCompleted;

    public IReadOnlyList<FunctionCard>? Cards
    {
        get => (IReadOnlyList<FunctionCard>?)GetValue(CardsProperty);
        set => SetValue(CardsProperty, value);
    }

    public string? SelectedSyntaxPath
    {
        get => (string?)GetValue(SelectedSyntaxPathProperty);
        set => SetValue(SelectedSyntaxPathProperty, value);
    }

    public void Reveal(string syntaxPath)
    {
        if (_borders.TryGetValue(syntaxPath, out var border))
            border.BringIntoView();
    }

    private void Rebuild()
    {
        foreach (var card in _watched)
            card.PropertyChanged -= OnCardMoved;
        _watched.Clear();
        _borders.Clear();
        _lines.Clear();
        Children.Clear();

        var cards = Cards;
        if (cards is null)
            return;

        foreach (var (from, to) in XmlScene.Edges)
        {
            var line = new Line
            {
                Stroke = new SolidColorBrush(Color.FromRgb(0x98, 0xA2, 0xB3)),
                StrokeThickness = 1.6
            };
            _lines.Add((from, to, line));
            Children.Add(line);
        }

        foreach (var card in cards)
        {
            var border = BuildNode(card);
            _borders[card.SyntaxPath] = border;
            Children.Add(border);
            SetLeft(border, card.X);
            SetTop(border, card.Y);
            card.PropertyChanged += OnCardMoved;
            _watched.Add(card);
        }

        MoveLines();
        Restyle();
        Fit();
    }

    private Border BuildNode(FunctionCard card)
    {
        var step = new TextBlock
        {
            Text = card.StepLabel,
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x70, 0x85))
        };
        var name = new TextBlock
        {
            Text = card.Name,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        var project = new TextBlock
        {
            Text = card.Project,
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x70, 0x85)),
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        var stack = new StackPanel();
        stack.Children.Add(step);
        stack.Children.Add(name);
        stack.Children.Add(project);
        var border = new Border
        {
            Width = NodeWidth,
            Height = NodeHeight,
            Background = Brushes.White,
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 6, 10, 6),
            BorderThickness = new Thickness(1.4),
            Cursor = Cursors.Hand,
            Child = stack,
            Tag = card,
            ToolTip = card.SyntaxPath
        };
        border.MouseLeftButtonDown += OnDown;
        border.MouseMove += OnMove;
        border.MouseLeftButtonUp += OnUp;
        return border;
    }

    private void OnDown(object sender, MouseButtonEventArgs e)
    {
        _dragBorder = (Border)sender;
        _dragCard = (FunctionCard)_dragBorder.Tag;
        _dragOrigin = e.GetPosition(this);
        _cardOriginX = _dragCard.X;
        _cardOriginY = _dragCard.Y;
        _moved = false;
        _dragBorder.CaptureMouse();
        e.Handled = true;
    }

    private void OnMove(object sender, MouseEventArgs e)
    {
        if (_dragBorder is null || _dragCard is null || e.LeftButton != MouseButtonState.Pressed)
            return;
        var point = e.GetPosition(this);
        var dx = point.X - _dragOrigin.X;
        var dy = point.Y - _dragOrigin.Y;
        if (Math.Abs(dx) + Math.Abs(dy) < 3)
            return;
        _moved = true;
        _dragCard.X = Math.Max(0, _cardOriginX + dx);
        _dragCard.Y = Math.Max(0, _cardOriginY + dy);
    }

    private void OnUp(object sender, MouseButtonEventArgs e)
    {
        if (_dragBorder is null || _dragCard is null)
            return;
        _dragBorder.ReleaseMouseCapture();
        var card = _dragCard;
        var moved = _moved;
        _dragBorder = null;
        _dragCard = null;
        if (moved)
            DragCompleted?.Invoke(this, EventArgs.Empty);
        else
            CardChosen?.Invoke(this, card);
        e.Handled = true;
    }

    private void OnCardMoved(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is not FunctionCard card)
            return;
        if (e.PropertyName is not (nameof(FunctionCard.X) or nameof(FunctionCard.Y)))
            return;
        if (_borders.TryGetValue(card.SyntaxPath, out var border))
        {
            SetLeft(border, card.X);
            SetTop(border, card.Y);
        }

        MoveLines();
        Fit();
    }

    private void MoveLines()
    {
        var cards = Cards;
        if (cards is null)
            return;
        foreach (var (from, to, line) in _lines)
        {
            var source = cards.FirstOrDefault(card => card.SyntaxPath == from);
            var target = cards.FirstOrDefault(card => card.SyntaxPath == to);
            if (source is null || target is null)
                continue;
            line.X1 = source.X + NodeWidth / 2;
            line.Y1 = source.Y + NodeHeight / 2;
            line.X2 = target.X + NodeWidth / 2;
            line.Y2 = target.Y + NodeHeight / 2;
        }
    }

    private void Restyle()
    {
        var selected = SelectedSyntaxPath;
        foreach (var (path, border) in _borders)
        {
            var on = path == selected;
            border.BorderBrush = new SolidColorBrush(on
                ? Color.FromRgb(0x1D, 0x4E, 0x89)
                : Color.FromRgb(0xD0, 0xD5, 0xDD));
            border.BorderThickness = new Thickness(on ? 2.2 : 1.4);
        }
    }

    private void Fit()
    {
        var cards = Cards;
        if (cards is null || cards.Count == 0)
        {
            Width = 900;
            Height = 420;
            return;
        }

        Width = Math.Max(900, cards.Max(card => card.X) + NodeWidth + 48);
        Height = Math.Max(420, cards.Max(card => card.Y) + NodeHeight + 48);
    }
}
