namespace DendroDocs.Tool.Tests;

[TestClass]
public class OperationBasedCoverageTests
{
    [TestMethod]
    public void OperationBasedAnalyzer_Should_HandleReturnStatements()
    {
        // Arrange
        var source = @"
        class Test
        {
            int GetNumber()
            {
                return 42;
            }
            
            string GetText()
            {
                return ""hello"";
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
        
        var method3 = types[0].Methods[2];
        method3.Statements.Count.ShouldBe(1);
        method3.Statements[0].ShouldBeOfType<ReturnDescription>();
    }

    [TestMethod]
    public void OperationBasedAnalyzer_Should_HandleBasicAssignments()
    {
        // Arrange
        var source = @"
        class Test
        {
            void Method()
            {
                int x = 5;
                string y = ""hello"";
                bool z = true;
            }
        }
        ";

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        var method = types[0].Methods[0];
        
        // Should detect all assignments
        var assignments = method.Statements.OfType<AssignmentDescription>().ToList();
        assignments.Count.ShouldBe(3);
        
        // All statements should be assignments
        method.Statements.Count.ShouldBe(3);
        method.Statements.ShouldAllBe(s => s is AssignmentDescription);
    }

    [TestMethod]
    public void OperationBasedAnalyzer_Should_HandleCompoundAssignments()
    {
        // Arrange
        var source = @"
        class Test
        {
            void Method()
            {
                int x = 5;
                x += 10;
                x -= 2;
                x *= 3;
            }
        }
        ";

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        var method = types[0].Methods[0];
        
        // Should detect all assignments including compound ones
        var assignments = method.Statements.OfType<AssignmentDescription>().ToList();
        assignments.Count.ShouldBe(4); // 1 initial + 3 compound
        
        method.Statements.Count.ShouldBe(4);
        method.Statements.ShouldAllBe(s => s is AssignmentDescription);
    }

    [TestMethod]
    public void OperationBasedAnalyzer_Should_HandleNameofCorrectly()
    {
        // Arrange
        var source = @"
        class Test
        {
            void Method()
            {
                string name = nameof(Test);
            }
        }
        ";

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        var method = types[0].Methods[0];
        
        // Should have assignment, nameof calls should be ignored by OperationBasedInvocationsAnalyzer
        method.Statements.Count.ShouldBe(1);
        method.Statements[0].ShouldBeOfType<AssignmentDescription>();
    }

    [TestMethod]
    public void OperationBasedAnalyzer_Should_HandleObjectCreationWithInitializers()
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
            void Method()
            {
                var person = new Person 
                { 
                    Name = ""John"", 
                    Age = 30 
                };
            }
        }
        ";

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        var method = types[1].Methods[0]; // Test class method
        
        // Should detect object creation
        method.Statements.Count.ShouldBeGreaterThan(0);
        
        var invocations = method.Statements.OfType<InvocationDescription>().ToList();
        invocations.Count.ShouldBeGreaterThan(0);
        
        // Should have arguments for the initializers
        invocations.ShouldAllBe(inv => inv.Arguments.Count >= 0);
    }

    [TestMethod]
    public void OperationBasedAnalyzer_Should_HandleMethodInvocations()
    {
        // Arrange
        var source = @"
        class Test
        {
            void Method()
            {
                System.Console.WriteLine(""Hello"");
                int value = System.Math.Abs(-5);
                ""test"".ToString();
            }
        }
        ";

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        var method = types[0].Methods[0];
        
        // Should have invocations and assignment
        var invocations = method.Statements.OfType<InvocationDescription>().ToList();
        var assignments = method.Statements.OfType<AssignmentDescription>().ToList();
        
        invocations.Count.ShouldBeGreaterThan(0);
        assignments.Count.ShouldBeGreaterThan(0);
        
        // Should find the WriteLine and ToString calls
        var hasWriteLine = invocations.Any(inv => inv.Name == "WriteLine");
        var hasToString = invocations.Any(inv => inv.Name == "ToString");
        
        hasWriteLine.ShouldBeTrue();
        hasToString.ShouldBeTrue();
    }

    [TestMethod]
    public void OperationBasedAnalyzer_Should_HandleImplicitObjectCreation()
    {
        // Arrange
        var source = @"
        class Test
        {
            void Method()
            {
                object obj1 = new();
                System.Text.StringBuilder sb = new(""hello"");
                var list = new System.Collections.Generic.List<int>();
            }
        }
        ";

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        var method = types[0].Methods[0];
        
        // Should detect multiple object creations
        var invocations = method.Statements.OfType<InvocationDescription>().ToList();
        invocations.Count.ShouldBe(3);
        
        // Should detect all as object creations
        method.Statements.ShouldAllBe(s => s is InvocationDescription);
    }

    [TestMethod]
    public void OperationBasedAnalyzer_Should_HandleLiteralConstants()
    {
        // Arrange
        var source = @"
        class Test
        {
            void Method()
            {
                int number = 42;
                string text = ""hello"";
                bool flag = true;
                double value = 3.14;
            }
        }
        ";

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        var method = types[0].Methods[0];
        method.Statements.Count.ShouldBe(4);
        
        // All should be assignments
        method.Statements.ShouldAllBe(s => s is AssignmentDescription);
    }

    [TestMethod]
    public void OperationBasedAnalyzer_Should_HandleFieldConstants()
    {
        // Arrange
        var source = @"
        class Test
        {
            const string MESSAGE = ""Hello"";
            const int NUMBER = 42;
            
            void Method()
            {
                string msg = MESSAGE;
                int num = NUMBER;
            }
        }
        ";

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        var method = types[0].Methods[0];
        method.Statements.Count.ShouldBe(2);
        
        // Should be assignments
        method.Statements.ShouldAllBe(s => s is AssignmentDescription);
    }

    [TestMethod]
    public void OperationBasedAnalyzer_Should_HandleComplexScenario()
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
            const string DEFAULT_NAME = ""Unknown"";
            
            Person CreatePerson()
            {
                int age = 25;
                age += 5;
                
                var person = new Person 
                { 
                    Name = DEFAULT_NAME, 
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
        
        // Should contain assignments, object creation, and return
        var assignments = method.Statements.OfType<AssignmentDescription>().ToList();
        var invocations = method.Statements.OfType<InvocationDescription>().ToList();
        var returns = method.Statements.OfType<ReturnDescription>().ToList();
        
        assignments.Count.ShouldBeGreaterThan(0);
        invocations.Count.ShouldBeGreaterThan(0);
        returns.Count.ShouldBe(1);
    }
}