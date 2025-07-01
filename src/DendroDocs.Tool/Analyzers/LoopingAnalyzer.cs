using Microsoft.CodeAnalysis.Operations;
using System.Linq;

namespace DendroDocs.Tool;

internal class LoopingAnalyzer(SemanticModel semanticModel, List<Statement> statements) : CSharpSyntaxWalker
{
    public override void VisitForEachStatement(ForEachStatementSyntax node)
    {
        var forEachStatement = new ForEach();
        statements.Add(forEachStatement);

        forEachStatement.Expression = $"{node.Identifier} in {node.Expression}";

        var invocationAnalyzer = new InvocationsAnalyzer(semanticModel, forEachStatement.Statements);
        var statementOperation = semanticModel.GetOperation(node.Statement);
        if (statementOperation != null)
        {
            invocationAnalyzer.Visit(statementOperation);
        }
    }

    // Operation-based method for the new OperationWalker approach
    public void VisitForEachLoopOperation(IForEachLoopOperation operation)
    {
        var forEachStatement = new ForEach();
        statements.Add(forEachStatement);

        // Try to build the expression from the operation  
        var variableName = operation.Syntax.ToString().Split(' ').FirstOrDefault() ?? "item";
        var collectionExpression = operation.Collection.Syntax.ToString();
        forEachStatement.Expression = $"{variableName} in {collectionExpression}";

        var invocationAnalyzer = new InvocationsAnalyzer(semanticModel, forEachStatement.Statements);
        invocationAnalyzer.Visit(operation.Body);
    }
}
