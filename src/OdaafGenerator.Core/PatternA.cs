namespace OdaafGenerator.Core;

public enum PatternAPhase
{
    Dispatch,
    Check,
    Schedule,
    Execute,
    Result
}

public static class PatternA
{
    public static string Title(PatternAPhase phase) => phase switch
    {
        PatternAPhase.Dispatch => "1 消分派与跳转",
        PatternAPhase.Check => "2 条件检查",
        PatternAPhase.Schedule => "3 排程",
        PatternAPhase.Result => "5 结果检查与显示",
        _ => "4 执行"
    };

    public static bool IsSyntaxNoise(string name) =>
        name is "ToList" or "OfType" or "GetFileName" or "GetDirectoryName"
            or "GetValue" or "TryGetValue" or "Equals" or "Start" or "Add";

    public static PatternAPhase Classify(string name, int index, int dialogIndex)
    {
        if (name is "BeginSemiAutoWorkflow" or "SemiAutoWorkflowEnter" or "SemiAutoWorkflowLeave" or "SemiAutoWorkflowLog" or "Yield")
            return PatternAPhase.Schedule;
        if (name is "EndSemiAutoWorkflow" or "NotifyNewSpecCreatorToRefresh" or "SemiExportPcbFile")
            return PatternAPhase.Result;
        if (name is "ShowDialog" or "Invoke"
            or "GetFusionContextForSemiAuto" or "GetFusionContextForActive" or "GetActiveContext"
            or "GetOrCreateContextByFilePath")
            return PatternAPhase.Dispatch;
        if (name is "IsNullOrEmpty" or "IsNullOrWhiteSpace")
            return PatternAPhase.Check;
        if (name is "Show" or "Warn" or "Error" or "Info")
            return dialogIndex < 0 || index < dialogIndex ? PatternAPhase.Check : PatternAPhase.Result;
        return PatternAPhase.Execute;
    }
}
