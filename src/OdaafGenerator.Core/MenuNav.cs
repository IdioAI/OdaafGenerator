using System.Collections.ObjectModel;

namespace OdaafGenerator.Core;

public static class FileNavContext
{
    public const string Id = "file-nav";
    public const string Name = "文件菜单";
}

public sealed class NavNode
{
    public const string Group = "group";
    public const string Menu = "menu";
    public const string Function = "function";
    public const string More = "more";
    public const string Empty = "empty";
    public const string Phase = "phase";

    public NavNode(
        string title,
        string kind,
        string? methodName = null,
        string? syntaxPath = null,
        CodeSymbol? symbol = null,
        bool mainFlow = false,
        string? mark = null)
    {
        Title = title;
        Kind = kind;
        MethodName = methodName;
        SyntaxPath = syntaxPath ?? symbol?.SyntaxPath;
        Symbol = symbol;
        MainFlow = mainFlow;
        Mark = mark;
    }

    public string Title { get; }
    public string Kind { get; }
    public string? MethodName { get; }
    public string? SyntaxPath { get; }
    public CodeSymbol? Symbol { get; }
    public bool MainFlow { get; }
    public string? Mark { get; }
    public ObservableCollection<NavNode> Children { get; } = [];
    public bool CallsLoaded { get; private set; }
    public NavNode? Origin { get; private set; }
    public bool Expanded { get; set; }

    public NavNode AsView()
    {
        var copy = new NavNode(Title, Kind, MethodName, SyntaxPath, Symbol, MainFlow, Mark);
        copy.Origin = this;
        if (CallsLoaded)
            copy.MarkLoaded();
        return copy;
    }

    public NavNode CloneBranch()
    {
        var copy = AsView();
        foreach (var child in Children)
            copy.Children.Add(child.CloneBranch());
        return copy;
    }

    public string Hint => Mark ?? Kind switch
    {
        Group => "分组",
        Menu => "菜单",
        Phase when MainFlow => "主流程",
        Phase => "修饰",
        Function when !MainFlow => "修饰",
        Function => Symbol is null ? "未对上" : "函数",
        Empty => "",
        _ => ""
    };

    public bool CanExpandCalls => Kind == Function && MainFlow && Symbol is not null && !CallsLoaded;

    public void MarkLoaded() => CallsLoaded = true;

    public static NavNode Placeholder() => new("展开查看直接调用", More);
}

public static class MenuNavTree
{
    public static IReadOnlyList<NavNode> Build(Catalog catalog)
    {
        var roots = new List<NavNode>();
        foreach (var menuName in ShellMenus.TopMenus)
        {
            var root = new NavNode(menuName, NavNode.Group);
            NavNode? submenu = null;
            foreach (var entry in ShellMenus.Entries.Where(item => item.Menu == menuName))
            {
                var item = MenuItem(catalog, entry);
                if (entry.Submenu is null)
                {
                    submenu = null;
                    root.Children.Add(item);
                    continue;
                }

                if (submenu is null || submenu.Title != entry.Submenu)
                {
                    submenu = new NavNode(entry.Submenu, NavNode.Menu);
                    root.Children.Add(submenu);
                }

                submenu.Children.Add(item);
            }

            roots.Add(root);
        }

        return roots;
    }

    private static NavNode MenuItem(Catalog catalog, ShellMenu entry)
    {
        var menu = new NavNode(entry.Header, NavNode.Menu, entry.MethodName);
        var symbol = catalog.FindMethod(entry.MethodName);
        var handler = new NavNode(
            entry.MethodName,
            NavNode.Function,
            entry.MethodName,
            symbol?.SyntaxPath ?? "BHEditor.ViewModels.MainWindowViewModel." + entry.MethodName,
            symbol,
            mainFlow: true);
        if (symbol is not null)
            handler.Children.Add(NavNode.Placeholder());
        menu.Children.Add(handler);
        return menu;
    }

    public static IReadOnlyList<NavNode> Filter(IReadOnlyList<NavNode> roots, string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return roots;
        var text = query.Trim();
        var result = new List<NavNode>();
        foreach (var root in roots)
        {
            var copy = FilterNode(root, text);
            if (copy is not null)
                result.Add(copy);
        }

        return result;
    }

    public static bool Subsequence(string? text, string query)
    {
        if (string.IsNullOrEmpty(text))
            return false;
        var index = 0;
        foreach (var wanted in query)
        {
            var found = false;
            while (index < text.Length)
            {
                if (char.ToLowerInvariant(text[index]) == char.ToLowerInvariant(wanted))
                {
                    index++;
                    found = true;
                    break;
                }

                index++;
            }

            if (!found)
                return false;
        }

        return true;
    }

    private static NavNode? FilterNode(NavNode node, string query)
    {
        var matchedChildren = new List<NavNode>();
        foreach (var child in node.Children)
        {
            var copy = FilterNode(child, query);
            if (copy is not null)
                matchedChildren.Add(copy);
        }

        var self = Matches(node, query);
        if (!self && matchedChildren.Count == 0)
            return null;

        var view = node.AsView();
        if (self && matchedChildren.Count == 0)
        {
            foreach (var child in node.Children)
                view.Children.Add(child.CloneBranch());
        }
        else
        {
            foreach (var child in matchedChildren)
                view.Children.Add(child);
            view.Expanded = true;
        }

        return view;
    }

    private static bool Matches(NavNode node, string query)
    {
        if (Subsequence(node.Title, query) || Subsequence(node.MethodName, query))
            return true;
        var note = FlowNotes.Find(node.SyntaxPath, node.MethodName ?? node.Title);
        return note is not null && (Subsequence(note.Title, query) || Subsequence(note.Purpose, query));
    }

    public static void Expand(NavNode node, Catalog catalog, string sourceRoot)
    {
        var target = node.Origin ?? node;
        if (!target.CanExpandCalls || target.Symbol is null)
            return;
        target.Children.Clear();
        var layer = CallLayerGraph.Expand(target.Symbol, sourceRoot, catalog);
        var work = layer.Calls.Where(call => call.Kind != CallKind.System).ToList();
        foreach (var phase in Enum.GetValues<PatternAPhase>())
        {
            var calls = work.Where(call => call.Phase == phase).ToList();
            if (calls.Count == 0)
                continue;
            var main = phase == PatternAPhase.Execute;
            var bucket = new NavNode(PatternA.Title(phase, layer.Menu), NavNode.Phase, mainFlow: main);
            foreach (var call in calls)
                bucket.Children.Add(FunctionChild(catalog, call, main));
            target.Children.Add(bucket);
        }

        var systems = layer.Calls.Where(call => call.Kind == CallKind.System).ToList();
        if (systems.Count > 0)
        {
            var systemNode = new NavNode("系统", NavNode.Phase, mark: "不展开");
            foreach (var call in systems)
                systemNode.Children.Add(FunctionChild(catalog, call, main: false));
            target.Children.Add(systemNode);
        }

        target.MarkLoaded();
        if (ReferenceEquals(node, target))
            return;
        node.Children.Clear();
        foreach (var child in target.Children)
            node.Children.Add(child.CloneBranch());
        node.MarkLoaded();
    }

    private static NavNode FunctionChild(Catalog catalog, DirectCall call, bool main)
    {
        var symbol = call.Kind == CallKind.Domain && call.SyntaxPath is not null
            ? catalog.Find(call.SyntaxPath)
            : null;
        var mark = call.Kind switch
        {
            CallKind.System => "系统",
            CallKind.Unindexed => "未收入",
            _ => null
        };
        var child = new NavNode(
            call.Name,
            NavNode.Function,
            call.Name,
            call.SyntaxPath,
            symbol,
            mainFlow: main && call.Kind != CallKind.System,
            mark: mark);
        if (main && call.Kind == CallKind.Domain && symbol is not null)
            child.Children.Add(NavNode.Placeholder());
        return child;
    }

    public static NavNode? FindHandler(IEnumerable<NavNode> nodes, string methodName)
    {
        foreach (var node in nodes)
        {
            if (node.Kind == NavNode.Function && node.MethodName == methodName)
                return node;
            var nested = FindHandler(node.Children, methodName);
            if (nested is not null)
                return nested;
        }

        return null;
    }
}
