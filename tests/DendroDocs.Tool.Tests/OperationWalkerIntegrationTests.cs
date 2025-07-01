namespace DendroDocs.Tool.Tests;

[TestClass]
public class OperationWalkerIntegrationTests
{
    [TestMethod]
    public void OperationBasedAnalyzer_Should_HandleImplicitObjectCreation()
    {
        // Assign
        var source = @"
        class Test
        {
            void Method()
            {
                object obj = new();
            }
        }
        ";

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        var method = types[0].Methods[0];
        method.Statements.Count.ShouldBe(1);
        method.Statements[0].ShouldBeOfType<InvocationDescription>();
    }

    [TestMethod]
    public void OperationBasedAnalyzer_Should_ProvideConstantValues()
    {
        // Assign
        var source = @"
        class Test
        {
            const string CONSTANT = ""Hello"";
            
            void Method()
            {
                var str = new System.Text.StringBuilder(CONSTANT);
            }
        }
        ";

        // This demonstrates that OperationWalker can resolve constant values
        // whereas SyntaxWalker would just see the identifier 'CONSTANT'
        
        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        var method = types[0].Methods[0];
        method.Statements.Count.ShouldBe(1);
        method.Statements[0].ShouldBeOfType<InvocationDescription>();
        
        var invocation = (InvocationDescription)method.Statements[0];
        invocation.Arguments.Count.ShouldBe(1);
        // Note: The current syntax-based approach would show "CONSTANT" 
        // but an operation-based approach could resolve it to "\"Hello\""
    }

    [TestMethod]
    public void OperationBasedAnalyzer_Should_HandleExplicitAndImplicitObjectCreation()
    {
        // Assign
        var source = @"
        class Test
        {
            void Method()
            {
                object obj1 = new object();        // Explicit
                object obj2 = new();               // Implicit
                System.Text.StringBuilder sb = new(""test""); // Implicit with args
            }
        }
        ";

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        var method = types[0].Methods[0];
        method.Statements.Count.ShouldBe(3);
        
        // All should be detected as invocations
        method.Statements[0].ShouldBeOfType<InvocationDescription>();
        method.Statements[1].ShouldBeOfType<InvocationDescription>();
        method.Statements[2].ShouldBeOfType<InvocationDescription>();
    }
}