namespace OdaafGenerator.Core;

public static class FrameworkCalls
{
    public static bool Exclude(string name, string? receiver, CodeSymbol? symbol)
    {
        if (AlwaysBcl(name) || name == "SemiAutoWorkflowLog")
            return true;
        if (IsFrameworkReceiver(receiver))
            return true;
        if (IsLogApi(name) && (IsLoggerReceiver(receiver) || symbol is null || IsLoggerSymbol(symbol)))
            return true;
        if (name is "Show" or "Start" or "Add" or "Clear" or "Remove" or "Run" or "Wait"
            && (symbol is null || IsFrameworkReceiver(receiver)))
            return true;
        return false;
    }

    public static bool IsMenuHandler(string? simpleName) =>
        simpleName is not null && ShellMenus.Entries.Any(entry => entry.MethodName == simpleName);

    private static bool AlwaysBcl(string name) =>
        name is "ToList" or "OfType" or "ToArray" or "Select" or "Where" or "Any" or "All"
            or "Count" or "Contains" or "First" or "FirstOrDefault" or "Single" or "SingleOrDefault"
            or "Distinct" or "OrderBy" or "Cast" or "Append"
            or "IsNullOrEmpty" or "IsNullOrWhiteSpace"
            or "GetFileName" or "GetFileNameWithoutExtension" or "GetDirectoryName" or "GetExtension"
            or "Combine" or "GetFullPath" or "Exists"
            or "Invoke" or "BeginInvoke" or "Yield" or "Delay" or "ConfigureAwait" or "GetAwaiter" or "GetResult"
            or "GetValue" or "TryGetValue" or "Equals" or "ReferenceEquals" or "GetHashCode" or "ToString"
            or "Format" or "Concat" or "Join" or "Split" or "Trim" or "Substring" or "StartsWith" or "EndsWith"
            or "Write" or "WriteLine";

    private static bool IsLogApi(string name) =>
        name is "Info" or "Warn" or "Warning" or "Error" or "Debug" or "Trace" or "Fatal";

    private static bool IsLoggerReceiver(string? receiver) =>
        receiver is not null
        && (receiver.Equals("Logger", StringComparison.OrdinalIgnoreCase)
            || receiver.Contains("logger", StringComparison.OrdinalIgnoreCase)
            || receiver.Equals("Log", StringComparison.OrdinalIgnoreCase));

    private static bool IsLoggerSymbol(CodeSymbol symbol) =>
        symbol.TypeName.Contains("Logger", StringComparison.OrdinalIgnoreCase)
        || symbol.SyntaxPath.Contains(".Logger.", StringComparison.Ordinal);

    private static bool IsFrameworkReceiver(string? receiver) =>
        receiver is "Logger" or "MessageBox" or "string" or "String" or "Path" or "File" or "Directory"
            or "Console" or "Debug" or "Trace" or "Task" or "Enumerable" or "Dispatcher";
}
