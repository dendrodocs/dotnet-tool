namespace DendroDocs.Tool.Tests;

[TestClass]
public class PropertyDeclarationTests
{
    [DataRow("string Property { get; set; }", Modifier.Private, DisplayName = "A property description without a modifier should contain the `private` modifier")]
    [DataRow("private string Property { get; set; }", Modifier.Private, DisplayName = "A property description with a `private` modifier should contain the `private` modifier")]
    [DataRow("public string Property { get; set; }", Modifier.Public, DisplayName = "A property description with a `public` modifier should contain the `public` modifier")]
    [DataRow("protected string Property { get; set; }", Modifier.Protected, DisplayName = "A property description with a `protected` modifier should contain the `protected` modifier")]
    [DataRow("internal string Property { get; set; }", Modifier.Internal, DisplayName = "A property description with an `internal` modifier should contain the `internal` modifier")]
    [DataRow("protected internal string Property { get; set; }", Modifier.Protected | Modifier.Internal, DisplayName = "A property description with a `protected internal` modifier should contain the `protected` and `internal` modifiers")]
    [DataRow("private protected string Property { get; set; }", Modifier.Private | Modifier.Protected, DisplayName = "A property description with a `private protected` modifier should contain the `private` and `protected` modifiers")]
    [TestMethod]
    public void PropertiesShouldHaveTheCorrectAccessModifiers(string property, Modifier modifier)
    {
        // Assign
        var source =
            $$"""
            public class Test
            {
                {{property}}
            }
            """;

        // Act
        var types = TestHelper.VisitSyntaxTree(source, "CS0169");

        // Assert
        types[0].Properties[0].Modifiers.ShouldBe(modifier);
    }

    [TestMethod]
    [DataRow("public static string Property { get; set; }", Modifier.Static, DisplayName = "A property description with a `static` modifier should contain the `static` modifier")]
    [DataRow("public unsafe string Property { get; set; }", Modifier.Unsafe, DisplayName = "A property description with an `unsafe` modifier should contain the `unsafe` modifier")]
    public void PropertiesOnClassesShouldHaveTheCorrectModifiers(string property, Modifier modifier)
    {
        // Assign
        var source =
            $$"""
            public class Test
            {
                {{property}}
            }
            """;

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        types[0].Properties[0].Modifiers.ShouldHaveFlag(modifier);
    }

    [TestMethod]
    [DataRow("public static string Property { get; set; }", Modifier.Static, DisplayName = "A property description with a `static` modifier should contain the `static` modifier")]
    [DataRow("public unsafe string Property { get; set; }", Modifier.Unsafe, DisplayName = "A property description with an `unsafe` modifier should contain the `unsafe` modifier")]
    public void PropertiesOnRecordsShouldHaveTheCorrectModifiers(string property, Modifier modifier)
    {
        // Assign
        var source =
            $$"""
            public record Test
            {
                {{property}}
            }
            """;

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        types[0].Properties[0].Modifiers.ShouldHaveFlag(modifier);
    }

    [TestMethod]
    public void PropertyDeclarationWithAnInitializerShouldBeRecognizedAndParsed()
    {
        // Assign
        var source =
            """
            public class Test
            {
                public string Property { get; set; } = "value";
            }
            """;

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        types[0].Properties[0].HasInitializer.ShouldBeTrue();
        types[0].Properties[0].Initializer.ShouldBe("value");
    }

    [TestMethod]
    public void PropertyInitializedWithAConstantShouldBeResolvedToTheActualConstValue()
    {
        // Assign
        var source =
            """
            public class Test
            {
                private const string CONST = "value";
                public string Property { get; set; } = CONST;
            }
            """;

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        types[0].Properties[0].Initializer.ShouldBe("value");
    }

    [TestMethod]
    public void MixedPropertyDeclarationsShouldBeRecognizedCorrectly()
    {
        // Assign
        var source = 
            """
            public class Test
            {
                public string AutoProperty { get; set; }

                private string _field;
                public string ManualProperty
                {
                    get { return _field; }
                    set { _field = value; }
                }
            }
            """;

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        types[0].Properties.Count.ShouldBe(2);
        types[0].Properties[0].Name.ShouldBe("AutoProperty");
        types[0].Properties[1].Name.ShouldBe("ManualProperty");
    }

    [TestMethod]
    public void ReadWriteOnlyAutoPropertiesShouldBeRecognizedCorrectly()
    {
        // Assign
        var source =
            """
            #nullable enable
            public class Test
            {
                public string ReadOnlyProperty { get; } = "";
                public string? WriteOnlyProperty { get; private set; }
            }
            """;

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        types[0].Properties.Count.ShouldBe(2);
        types[0].Properties[0].Name.ShouldBe("ReadOnlyProperty");
        types[0].Properties[1].Name.ShouldBe("WriteOnlyProperty");
    }

    [TestMethod]
    public void ReadOnlyPropertyShouldBeRecognizedCorrectly()
    {
        // Assign
        var source =
            """
            public class Test
            {
                private string _field = "";
                public string ReadOnlyProperty
                {
                    get { return _field; }
                }
            }
            """;

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        types[0].Properties.Count.ShouldBe(1);
        types[0].Properties[0].Name.ShouldBe("ReadOnlyProperty");
    }
}
