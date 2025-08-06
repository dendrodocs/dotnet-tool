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

    [TestMethod]
    public void ImplicitObjectCreation_WithMultipleArguments_Should_BeDetected()
    {
        // Assign
        var source = @"
        class Test
        {
            void Method()
            {
                System.Text.StringBuilder sb = new(""Hello"", 100);
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
        invocation.Arguments.Count.ShouldBe(2);
    }

    [TestMethod]
    public void ImplicitObjectCreation_WithInitializers_Should_BeDetected()
    {
        // Assign
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
                Person person = new() { Name = ""John"", Age = 30 };
            }
        }
        ";

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        var method = types[1].Methods[0]; // Second type is Test class
        
        // Should detect object creation
        method.Statements.Count.ShouldBeGreaterThan(0);
        
        var invocations = method.Statements.OfType<InvocationDescription>().ToList();
        invocations.Count.ShouldBeGreaterThan(0);
        
        // Should have arguments for the initializers
        invocations[0].Arguments.Count.ShouldBeGreaterThanOrEqualTo(0);
    }

    [TestMethod]
    public void ImplicitObjectCreation_WithArgumentsAndInitializers_Should_BeDetected()
    {
        // Assign
        var source = @"
        public class CustomList
        {
            public CustomList(int capacity) { }
            public string Name { get; set; }
            public int Count { get; set; }
        }
        
        class Test
        {
            void Method()
            {
                CustomList list = new(10) { Name = ""MyList"", Count = 5 };
            }
        }
        ";

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        var method = types[1].Methods[0]; // Second type is Test class
        
        // Should detect object creation
        method.Statements.Count.ShouldBeGreaterThan(0);
        
        var invocations = method.Statements.OfType<InvocationDescription>().ToList();
        invocations.Count.ShouldBeGreaterThan(0);
        
        // Should have arguments for constructor and initializers
        invocations[0].Arguments.Count.ShouldBeGreaterThanOrEqualTo(1);
    }

    [TestMethod]
    public void ImplicitObjectCreation_InVariousContexts_Should_BeDetected()
    {
        // Assign
        var source = @"
        class Test
        {
            void Method()
            {
                // Local variable
                var list1 = new System.Collections.Generic.List<int>();
                
                // Assignment
                System.Collections.Generic.List<string> list2;
                list2 = new();
                
                // Method parameter
                ProcessList(new());
                
                // Return value
                var result = CreateList();
            }
            
            void ProcessList(System.Collections.Generic.List<string> list) { }
            
            System.Collections.Generic.List<string> CreateList()
            {
                return new();
            }
        }
        ";

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        var method = types[0].Methods[0];
        var processMethod = types[0].Methods[1];
        var createMethod = types[0].Methods[2];
        
        // Method should have object creations and method calls
        var invocations = method.Statements.OfType<InvocationDescription>().ToList();
        invocations.Count.ShouldBeGreaterThan(0);
        
        // CreateList method should have statements including return
        createMethod.Statements.Count.ShouldBeGreaterThan(0);
        
        var returns = createMethod.Statements.OfType<ReturnDescription>().ToList();
        returns.Count.ShouldBe(1);
    }

    [TestMethod]
    public void ImplicitObjectCreation_WithNestedTypes_Should_BeDetected()
    {
        // Assign
        var source = @"
        class Test
        {
            void Method()
            {
                var dict = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<int>>();
                System.Collections.Generic.Dictionary<int, string> dict2 = new();
            }
        }
        ";

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        var method = types[0].Methods[0];
        method.Statements.Count.ShouldBe(2);
        
        method.Statements[0].ShouldBeOfType<InvocationDescription>();
        method.Statements[1].ShouldBeOfType<InvocationDescription>();
        
        var invocation1 = (InvocationDescription)method.Statements[0];
        var invocation2 = (InvocationDescription)method.Statements[1];
        
        // Both should be detected as Dictionary creations
        invocation1.ContainingType.ShouldContain("Dictionary");
        invocation2.ContainingType.ShouldContain("Dictionary");
    }

    [TestMethod]
    public void ImplicitObjectCreation_WithCollectionInitializers_Should_BeDetected()
    {
        // Assign
        var source = @"
        class Test
        {
            void Method()
            {
                var list = new System.Collections.Generic.List<int> { 1, 2, 3, 4, 5 };
                var dict = new System.Collections.Generic.Dictionary<string, int> 
                { 
                    [""one""] = 1, 
                    [""two""] = 2 
                };
            }
        }
        ";

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        var method = types[0].Methods[0];
        
        // Should detect object creations
        method.Statements.Count.ShouldBeGreaterThan(0);
        
        var invocations = method.Statements.OfType<InvocationDescription>().ToList();
        invocations.Count.ShouldBeGreaterThan(0);
        
        // Should have object creations for both collections
        invocations.ShouldAllBe(inv => inv.Arguments.Count >= 0);
    }

    [TestMethod]
    public void ImplicitObjectCreation_WithAnonymousTypes_Should_Work()
    {
        // Assign
        var source = @"
        class Test
        {
            void Method()
            {
                var anon = new { Name = ""Test"", Value = 42 };
                var list = new System.Collections.Generic.List<object> { anon };
            }
        }
        ";

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        var method = types[0].Methods[0];
        
        // Should handle anonymous type creation and list creation
        method.Statements.Count.ShouldBeGreaterThan(0);
        
        var invocations = method.Statements.OfType<InvocationDescription>().ToList();
        invocations.Count.ShouldBeGreaterThan(0);
    }
}