using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace OdaafGenerator.Core;

public static class PatternB
{
    public static PatternAPhase Region(InvocationExpressionSyntax invocation, BlockSyntax? body)
    {
        if (body is null)
            return PatternAPhase.Execute;
        var statement = invocation.Ancestors().OfType<StatementSyntax>()
            .FirstOrDefault(item => item.Parent == body);
        if (statement is null)
            return PatternAPhase.Execute;
        var index = body.Statements.IndexOf(statement);
        if (index < 0)
            return PatternAPhase.Execute;

        var firstWork = 0;
        while (firstWork < body.Statements.Count && IsGuard(body.Statements[firstWork]))
            firstWork++;
        if (index < firstWork)
            return PatternAPhase.Check;

        var lastWork = body.Statements.Count - 1;
        while (lastWork > firstWork && IsTail(body.Statements[lastWork]))
            lastWork--;
        return index > lastWork ? PatternAPhase.Result : PatternAPhase.Execute;
    }

    private static bool IsGuard(StatementSyntax statement) =>
        statement is IfStatementSyntax check
        && check.DescendantNodes().Any(node => node is ReturnStatementSyntax or ThrowStatementSyntax);

    private static bool IsTail(StatementSyntax statement) =>
        statement is ReturnStatementSyntax || IsGuard(statement);
}
