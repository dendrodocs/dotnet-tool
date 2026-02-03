namespace DendroDocs.Tool.Tests;

[TestClass]
public class OperationWalkerCoverageTests
{
    [TestMethod]
    public void OperationWalker_Should_HandleReturnStatements()
    {
        // Arrange
        var source = @"
        class Test
        {
            int GetNumber()
            {
                return 42;
            }
            
            void DoNothing()
            {
                return;
            }
        }
        ";

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        var method1 = types[0].Methods[0];
        method1.Statements.Count.ShouldBe(1);
        method1.Statements[0].ShouldBeOfType<ReturnDescription>();
        
        var method2 = types[0].Methods[1];
        method2.Statements.Count.ShouldBe(1);
        method2.Statements[0].ShouldBeOfType<ReturnDescription>();
    }

    [TestMethod]
    public void OperationWalker_Should_HandleAssignments()
    {
        // Arrange
        var source = @"
        class Test
        {
            void Method()
            {
                int x = 5;
                x = 10;
                x += 3;
            }
        }
        ";

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        var method = types[0].Methods[0];
        
        // Should detect assignments
        var assignments = method.Statements.OfType<AssignmentDescription>().ToList();
        assignments.Count.ShouldBeGreaterThan(0);
        
        method.Statements.Count.ShouldBeGreaterThan(0);
    }

    [TestMethod]
    public void OperationWalker_Should_HandleObjectCreations()
    {
        // Arrange
        var source = @"
        class Test
        {
            void Method()
            {
                object obj1 = new object();
                object obj2 = new();
                var sb = new System.Text.StringBuilder();
            }
        }
        ";

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        var method = types[0].Methods[0];
        
        // Should detect object creations
        var invocations = method.Statements.OfType<InvocationDescription>().ToList();
        invocations.Count.ShouldBe(3);
        
        method.Statements.Count.ShouldBe(3);
    }

    [TestMethod]
    public void OperationWalker_Should_HandleComplexScenario()
    {
        // Arrange
        var source = @"
        public class Person
        {
            public string Name { get; set; }
            public int Age { get; set; }
        }
        
        class Test
        {
            Person CreatePerson()
            {
                int age = 25;
                age += 5;
                
                var person = new Person 
                { 
                    Name = ""John"", 
                    Age = age 
                };
                
                return person;
            }
        }
        ";

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        var method = types[1].Methods[0]; // Test class method
        method.Statements.Count.ShouldBeGreaterThan(0);
        
        // Should contain different types of statements
        var assignments = method.Statements.OfType<AssignmentDescription>().ToList();
        var invocations = method.Statements.OfType<InvocationDescription>().ToList();
        var returns = method.Statements.OfType<ReturnDescription>().ToList();
        
        assignments.Count.ShouldBeGreaterThan(0);
        invocations.Count.ShouldBeGreaterThan(0);
        returns.Count.ShouldBe(1);
    }

    [TestMethod]
    public void OperationWalker_Should_HandleDifferentReturnTypes()
    {
        // Arrange
        var source = @"
        class Test
        {
            string GetString() => ""hello"";
            
            int Calculate()
            {
                return 42;
            }
        }
        ";

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        var method1 = types[0].Methods[0]; // Expression body
        var method2 = types[0].Methods[1]; // Block body
        
        // Both should have return statements
        method1.Statements.OfType<ReturnDescription>().Count().ShouldBe(1);
        method2.Statements.OfType<ReturnDescription>().Count().ShouldBe(1);
        
        // Methods should have statements
        method1.Statements.Count.ShouldBeGreaterThan(0);
        method2.Statements.Count.ShouldBeGreaterThan(0);
    }

    [TestMethod]
    public void OperationWalker_Should_HandleCollectionInitializers()
    {
        // Arrange
        var source = @"
        class Test
        {
            void Method()
            {
                var list = new System.Collections.Generic.List<int> { 1, 2, 3 };
            }
        }
        ";

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        var method = types[0].Methods[0];
        
        // Should detect object creation with initializer elements
        method.Statements.Count.ShouldBe(1);
        method.Statements[0].ShouldBeOfType<InvocationDescription>();
        
        var invocation = (InvocationDescription)method.Statements[0];
        invocation.Arguments.Count.ShouldBe(3); // Three initializer elements
    }
}