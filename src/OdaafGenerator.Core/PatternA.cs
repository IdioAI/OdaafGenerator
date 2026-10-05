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
    public static string Title(PatternAPhase phase, bool menu)
    {
        if (!menu)
        {
            return phase switch
            {
                PatternAPhase.Check => "输入参数检查",
                PatternAPhase.Result => "结果检查",
                _ => "执行"
            };
        }

        return phase switch
        {
            PatternAPhase.Dispatch => "消分派与跳转",
            PatternAPhase.Check => "条件检查",
            PatternAPhase.Schedule => "排程",
            PatternAPhase.Result => "结果检查与显示",
            _ => "执行"
        };
    }

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
