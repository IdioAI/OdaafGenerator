namespace OdaafGenerator.Core;

public enum CallKind
{
    Domain,
    System,
    Unindexed
}

public static class SystemCalls
{
    public const string Unlisted = "系统调用，表中无说明。";

    private static readonly Dictionary<string, string> Listed = new(StringComparer.Ordinal)
    {
        ["Min"] = "取两个数里较小的一个。",
        ["Max"] = "取两个数里较大的一个。",
        ["Parse"] = "把文本转成数。",
        ["Show"] = "弹出提示。人只看结果，不在这里改数据。",
        ["ShowDialog"] = "弹出参数窗。人在这里确认或取消。",
        ["IsNullOrEmpty"] = "检查字符串是否没有内容。",
        ["IsNullOrWhiteSpace"] = "检查字符串是否没有内容。",
        ["Invoke"] = "切回界面线程再执行。"
    };

    public static bool TryDescribe(string name, string? receiver, CodeSymbol? symbol, string? callerType, out string purpose)
    {
        purpose = "";
        if (!Listed.TryGetValue(name, out var text))
            return false;
        if (receiver is null && symbol is not null && symbol.TypeSyntaxPath == callerType)
            return false;
        purpose = text;
        return true;
    }
}
