using Microsoft.CodeAnalysis.Operations;

namespace DendroDocs.Tool;

internal class BranchingAnalyzer(SemanticModel semanticModel, List<Statement> statements) : CSharpSyntaxWalker
{
    public override void VisitIfStatement(IfStatementSyntax node)
    {
        var ifStatement = new If();
        statements.Add(ifStatement);

        var ifSection = new IfElseSection();
        ifStatement.Sections.Add(ifSection);

        ifSection.Condition = node.Condition.ToString();

        var ifInvocationAnalyzer = new InvocationsAnalyzer(semanticModel, ifSection.Statements);
        var statementOperation = semanticModel.GetOperation(node.Statement);
        if (statementOperation != null)
        {
            ifInvocationAnalyzer.Visit(statementOperation);
        }

        var elseNode = node.Else;
        while (elseNode != null)
        {
            var section = new IfElseSection();
            ifStatement.Sections.Add(section);

            var elseInvocationAnalyzer = new InvocationsAnalyzer(semanticModel, section.Statements);
            var elseStatementOperation = semanticModel.GetOperation(elseNode.Statement);
            if (elseStatementOperation != null)
            {
                elseInvocationAnalyzer.Visit(elseStatementOperation);
            }

            if (elseNode.Statement.IsKind(SyntaxKind.IfStatement))
            {
                var elseIfNode = (IfStatementSyntax)elseNode.Statement;
                section.Condition = elseIfNode.Condition.ToString();

                elseNode = elseIfNode.Else;
            }
            else
            {
                elseNode = null;
            }
        }
    }

    public override void VisitSwitchStatement(SwitchStatementSyntax node)
    {
        var switchStatement = new Switch();
        statements.Add(switchStatement);

        switchStatement.Expression = node.Expression.ToString();

        foreach (var section in node.Sections)
        {
            var switchSection = new SwitchSection();
            switchStatement.Sections.Add(switchSection);

            switchSection.Labels.AddRange(section.Labels.Select(l => Label(l)));

            var invocationAnalyzer = new InvocationsAnalyzer(semanticModel, switchSection.Statements);
            var sectionOperation = semanticModel.GetOperation(section);
            if (sectionOperation != null)
            {
                invocationAnalyzer.Visit(sectionOperation);
            }
        }
    }

    public override void VisitSwitchExpression(SwitchExpressionSyntax node)
    {
        var switchStatement = new Switch();
        statements.Add(switchStatement);

        switchStatement.Expression = node.GoverningExpression.ToString();

        foreach (var arm in node.Arms)
        {
            var switchSection = new SwitchSection();
            switchStatement.Sections.Add(switchSection);

            var label = $"{arm.Pattern}{(arm.WhenClause?.Condition is not null ? $" when {arm.WhenClause.Condition}" : string.Empty)}";
            switchSection.Labels.Add(label);

            var invocationAnalyzer = new InvocationsAnalyzer(semanticModel, switchSection.Statements);
            var expressionOperation = semanticModel.GetOperation(arm.Expression);
            if (expressionOperation != null)
            {
                invocationAnalyzer.Visit(expressionOperation);
            }
        }
    }

    private static string Label(SwitchLabelSyntax label)
    {
        return label switch
        {
            CasePatternSwitchLabelSyntax casePattern when casePattern.WhenClause?.Condition is not null => $"{casePattern.Pattern} when {casePattern.WhenClause.Condition}",
            CasePatternSwitchLabelSyntax casePattern => casePattern.Pattern.ToString(),
            CaseSwitchLabelSyntax @case => @case.Value.ToString(),
            DefaultSwitchLabelSyntax @default => @default.Keyword.ToString(),
            _ => throw new ArgumentOutOfRangeException(nameof(label)),
        };
    }

    // Operation-based methods for the new OperationWalker approach
    public void VisitSwitchOperation(ISwitchOperation operation)
    {
        var switchStatement = new Switch();
        statements.Add(switchStatement);

        switchStatement.Expression = operation.Value.Syntax.ToString();

        foreach (var @case in operation.Cases)
        {
            var switchSection = new SwitchSection();
            switchStatement.Sections.Add(switchSection);

            // Add labels for this case
            foreach (var clause in @case.Clauses)
            {
                var label = clause switch
                {
                    ISingleValueCaseClauseOperation singleValue => singleValue.Value.Syntax.ToString(),
                    IDefaultCaseClauseOperation => "default",
                    IPatternCaseClauseOperation pattern => pattern.Pattern.Syntax.ToString(),
                    _ => clause.Syntax.ToString()
                };
                switchSection.Labels.Add(label);
            }

            // Analyze the body
            var invocationAnalyzer = new InvocationsAnalyzer(semanticModel, switchSection.Statements);
            foreach (var bodyOperation in @case.Body)
            {
                invocationAnalyzer.Visit(bodyOperation);
            }
        }
    }

    public void VisitConditionalOperation(IConditionalOperation operation)
    {
        var ifStatement = new If();
        statements.Add(ifStatement);

        var ifSection = new IfElseSection();
        ifStatement.Sections.Add(ifSection);

        ifSection.Condition = operation.Condition.Syntax.ToString();

        var ifInvocationAnalyzer = new InvocationsAnalyzer(semanticModel, ifSection.Statements);
        ifInvocationAnalyzer.Visit(operation.WhenTrue);

        if (operation.WhenFalse != null)
        {
            var elseSection = new IfElseSection();
            ifStatement.Sections.Add(elseSection);

            var elseInvocationAnalyzer = new InvocationsAnalyzer(semanticModel, elseSection.Statements);
            elseInvocationAnalyzer.Visit(operation.WhenFalse);
        }
    }
}
