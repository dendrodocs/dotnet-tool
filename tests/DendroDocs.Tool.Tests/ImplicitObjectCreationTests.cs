namespace DendroDocs.Tool.Tests;

[TestClass]
public class ImplicitObjectCreationTests
{
    [TestMethod]
    public void ImplicitObjectCreation_Should_BeDetected()
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
    public void ExplicitObjectCreation_Should_StillWork()
    {
        // Assign
        var source = @"
        class Test
        {
            void Method()
            {
                object obj = new object();
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
    public void ImplicitObjectCreation_WithArguments_Should_BeDetected()
    {
        // Assign
        var source = @"
        class Test
        {
            void Method()
            {
                System.Text.StringBuilder sb = new(""Hello"");
            }
        }
        ";

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        var method = types[0].Methods[0];
        method.Statements.Count.ShouldBe(1);
        method.Statements[0].ShouldBeOfType<InvocationDescription>();
        
        var invocation = (InvocationDescription)method.Statements[0];
        invocation.Arguments.Count.ShouldBe(1);
    }
}