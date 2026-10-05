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

    public NavNode(string title, string kind, string? methodName = null, string? syntaxPath = null, CodeSymbol? symbol = null)
    {
        Title = title;
        Kind = kind;
        MethodName = methodName;
        SyntaxPath = syntaxPath ?? symbol?.SyntaxPath;
        Symbol = symbol;
    }

    public string Title { get; }
    public string Kind { get; }
    public string? MethodName { get; }
    public string? SyntaxPath { get; }
    public CodeSymbol? Symbol { get; }
    public ObservableCollection<NavNode> Children { get; } = [];
    public bool CallsLoaded { get; private set; }

    public string Hint => Kind switch
    {
        Group => "分组",
        Menu => "菜单",
        Phase => "模式A",
        Function => Symbol is null ? "未对上" : "函数",
        Empty => "",
        _ => ""
    };

    public bool CanExpandCalls => Kind == Function && Symbol is not null && !CallsLoaded;

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
            symbol);
        if (symbol is not null)
            handler.Children.Add(NavNode.Placeholder());
        menu.Children.Add(handler);
        return menu;
    }

    public static void Expand(NavNode node, Catalog catalog, string sourceRoot)
    {
        if (!node.CanExpandCalls || node.Symbol is null)
            return;
        node.Children.Clear();
        var layer = CallLayerGraph.Expand(node.Symbol, sourceRoot, catalog);
        foreach (var phase in Enum.GetValues<PatternAPhase>())
        {
            var bucket = new NavNode(PatternA.Title(phase), NavNode.Phase);
            var calls = layer.Calls.Where(call => call.Phase == phase).ToList();
            if (calls.Count == 0)
            {
                bucket.Children.Add(new NavNode(
                    phase == PatternAPhase.Schedule ? "顺序执行，无排程器" : "（本步无调用）",
                    NavNode.Empty));
            }
            else
            {
                foreach (var call in calls)
                {
                    var symbol = call.SyntaxPath is null ? null : catalog.Find(call.SyntaxPath);
                    var child = new NavNode(call.Name, NavNode.Function, call.Name, call.SyntaxPath, symbol);
                    if (symbol is not null)
                        child.Children.Add(NavNode.Placeholder());
                    bucket.Children.Add(child);
                }
            }

            node.Children.Add(bucket);
        }

        node.MarkLoaded();
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
