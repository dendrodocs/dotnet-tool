using Shouldly.ShouldlyExtensionMethods;

namespace DendroDocs.Tool.Tests;

[TestClass]
public class FieldDeclarationTests
{
    [DataRow("string field;", Modifier.Private, DisplayName = "A field description about a field without a modifier should contain the `private` modifier")]
    [DataRow("private string field;", Modifier.Private, DisplayName = "A field description about a `private` field should contain the `private` modifier")]
    [DataRow("public string field;", Modifier.Public, DisplayName = "A field description about a `public` field should contain the `public` modifier")]
    [DataRow("protected string field;", Modifier.Protected, DisplayName = "A field description about a `protected` field should contain the `protected` modifier")]
    [DataRow("internal string field = default;", Modifier.Internal, DisplayName = "A field description about a `internal` field should contain the `internal` modifier")]
    [DataRow("protected internal string field;", Modifier.Protected | Modifier.Internal, DisplayName = "A field description about a `protected internal` field should contain the `protected` and `internal` modifiers")]
    [DataRow("private protected string field;", Modifier.Private | Modifier.Protected, DisplayName = "A field description about a `private protected` field should contain the `private` and `protected` modifiers")]
    [TestMethod]
    public void FieldsShouldHaveTheCorrectAccessModifiers(string field, Modifier modifier)
    {
        // Assign
        var source = @$"
        public class Test
        {{
            {field}
        }}
        ";

        // Act
        var types = TestHelper.VisitSyntaxTree(source, "CS0169");

        // Assert
        types[0].Fields[0].Modifiers.ShouldBe(modifier);
    }

    [TestMethod]
    [DataRow("public static string field;", Modifier.Static, DisplayName = "A field description about a `static` field should contain the `static` modifier")]
    [DataRow("public unsafe string field;", Modifier.Unsafe, DisplayName = "A field description about an `unsafe` field should contain the `unsafe` modifier")]
    public void FieldsShouldHaveTheCorrectModifiers(string method, Modifier modifier)
    {
        // Assign
        var source = @$"
        public class Test
        {{
            {method}
        }}
        ";

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        types[0].Fields[0].Modifiers.ShouldHaveFlag(modifier);
    }

    [TestMethod]
    public void MultipleFieldDeclarationsShouldCreateAFieldDescriptionPerField()
    {
        // Assign
        var source = @"
        public class Test
        {
            public string field1, field2;
        }
        ";

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        types[0].Fields.Count.ShouldBe(2);
    }

    [TestMethod]
    public void AFieldDeclarationWithAnInitializerShouldBeRecognizedAndParsed()
    {
        // Assign
        var source = @"
        public class Test
        {
            public string field1 = ""value"";
        }
        ";

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        types[0].Fields[0].HasInitializer.ShouldBeTrue();
        types[0].Fields[0].Initializer.ShouldBe("value");
    }

    [TestMethod]
    public void AFieldInitializedWithAConstantShouldBeResolvedToTheActualConstValue()
    {
        // Assign
        var source = @"
        public class Test
        {
            private const string CONST = ""value"";
            public string field1 = CONST;
        }
        ";

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        types[0].Fields[0].Initializer.ShouldBe("value");
    }
}
