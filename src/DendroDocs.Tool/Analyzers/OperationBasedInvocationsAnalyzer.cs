using Microsoft.CodeAnalysis.Operations;

namespace DendroDocs.Tool;

/// <summary>
/// OperationWalker-based analyzer for method invocations, providing better support for VB.NET,
/// constant values, and implicit object creation expressions.
/// 
/// Benefits over CSharpSyntaxWalker:
/// - Language-agnostic: Works with both C# and VB.NET
/// - Better type information: Access to resolved types and constant values
/// - Unified object creation: Handles both explicit and implicit object creation seamlessly
/// - Enhanced semantic analysis: Works at the operation level rather than syntax level
/// 
/// Example usage for VB.NET support:
/// Instead of needing separate VB-specific syntax walkers, this analyzer can process
/// VB.NET code through IOperation, making it language-neutral.
/// </summary>
internal class OperationBasedInvocationsAnalyzer(SemanticModel semanticModel, List<Statement> statements) : OperationWalker
{
    // Keep semantic model available for future enhancements
    private readonly SemanticModel _semanticModel = semanticModel;
    public override void VisitObjectCreation(IObjectCreationOperation operation)
    {
        string containingType = operation.Type?.ToDisplayString() ?? string.Empty;
        string typeName = operation.Type?.Name ?? string.Empty;

        var invocation = new InvocationDescription(containingType, typeName);
        statements.Add(invocation);

        foreach (var argument in operation.Arguments)
        {
            var value = GetConstantValueOrDefault(argument.Value);
            var argumentDescription = new ArgumentDescription(argument.Value.Type?.ToDisplayString() ?? string.Empty, value);
            invocation.Arguments.Add(argumentDescription);
        }

        if (operation.Initializer != null)
        {
            foreach (var initializer in operation.Initializer.Initializers)
            {
                var value = initializer switch
                {
                    IAssignmentOperation assignment => assignment.Value.Syntax.ToString(),
                    _ => initializer.Syntax.ToString()
                };
                
                var argumentDescription = new ArgumentDescription(initializer.Type?.ToDisplayString() ?? string.Empty, value);
                invocation.Arguments.Add(argumentDescription);
            }
        }

        base.VisitObjectCreation(operation);
    }

    public override void VisitInvocation(IInvocationOperation operation)
    {
        // Check for nameof expression
        if (operation.TargetMethod.Name == "nameof" && operation.Arguments.Length == 1)
        {
            // nameof is compiler sugar, and is actually a method we are not interested in
            return;
        }

        var containingType = operation.TargetMethod.ContainingType?.ToDisplayString() ?? string.Empty;
        var methodName = operation.TargetMethod.Name;

        var invocation = new InvocationDescription(containingType, methodName);
        statements.Add(invocation);

        foreach (var argument in operation.Arguments)
        {
            var value = GetConstantValueOrDefault(argument.Value);
            var argumentDescription = new ArgumentDescription(argument.Value.Type?.ToDisplayString() ?? string.Empty, value);
            invocation.Arguments.Add(argumentDescription);
        }

        base.VisitInvocation(operation);
    }

    public override void VisitReturn(IReturnOperation operation)
    {
        var value = operation.ReturnedValue != null ? GetConstantValueOrDefault(operation.ReturnedValue) : string.Empty;
        var returnDescription = new ReturnDescription(value);
        statements.Add(returnDescription);

        base.VisitReturn(operation);
    }

    public override void VisitSimpleAssignment(ISimpleAssignmentOperation operation)
    {
        var target = operation.Target.Syntax.ToString();
        var value = operation.Value.Syntax.ToString();
        
        var assignmentDescription = new AssignmentDescription(target, "=", value);
        statements.Add(assignmentDescription);

        base.VisitSimpleAssignment(operation);
    }

    public override void VisitCompoundAssignment(ICompoundAssignmentOperation operation)
    {
        var target = operation.Target.Syntax.ToString();
        var value = operation.Value.Syntax.ToString();
        var operatorToken = operation.OperatorKind switch
        {
            BinaryOperatorKind.Add => "+=",
            BinaryOperatorKind.Subtract => "-=",
            BinaryOperatorKind.Multiply => "*=",
            BinaryOperatorKind.Divide => "/=",
            BinaryOperatorKind.Remainder => "%=",
            BinaryOperatorKind.And => "&=",
            BinaryOperatorKind.Or => "|=",
            BinaryOperatorKind.ExclusiveOr => "^=",
            BinaryOperatorKind.LeftShift => "<<=",
            BinaryOperatorKind.RightShift => ">>=",
            _ => "="
        };
        
        var assignmentDescription = new AssignmentDescription(target, operatorToken, value);
        statements.Add(assignmentDescription);

        base.VisitCompoundAssignment(operation);
    }

    private static string GetConstantValueOrDefault(IOperation operation)
    {
        return operation switch
        {
            ILiteralOperation literal => literal.ConstantValue.Value?.ToString() ?? string.Empty,
            IFieldReferenceOperation field when field.Field.IsConst => field.Field.ConstantValue?.ToString() ?? string.Empty,
            _ => operation.Syntax.ToString()
        };
    }
}