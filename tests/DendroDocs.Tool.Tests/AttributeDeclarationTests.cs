namespace DendroDocs.Tool.Tests;

[TestClass]
public class AttributeDeclarationTests
{
    [TestMethod]
    public void ClassWithoutAttributes_Should_HaveEmptyAttributeCollection()
    {
        // Assign
        var source =
            """
            class Test
            {
            }
            """;

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        types[0].Attributes.Should().BeEmpty();
    }
    
    [TestMethod]
    public void ClassWithAttribute_Should_HaveAttributeInCollection()
    {
        // Assign
        var source =
            """
            [System.Obsolete]
            class Test
            {
            }
            """;

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        types[0].Attributes.Should().HaveCount(1);
        types[0].Attributes[0].Should().NotBeNull();
        types[0].Attributes[0].Name.Should().Be("System.Obsolete");
        types[0].Attributes[0].Arguments.Should().BeEmpty();
    }

    [TestMethod]
    public void ClassWithAttribute_Should_HaveAttributeWithPositionalArgumentInCollection()
    {
        // Assign
        var source =
            """
            [System.Obsolete("Message")]
            class Test
            {
            }
            """;

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        types[0].Attributes[0].Arguments.Should().HaveCount(1);
        types[0].Attributes[0].Arguments[0].Should().NotBeNull();
        types[0].Attributes[0].Arguments[0].Name.Should().Be("message");
        types[0].Attributes[0].Arguments[0].Value.Should().Be("Message");
    }

    [TestMethod]
    public void ClassWithAttribute_Should_HaveAttributeWithMultiplePositionalArgumentsInCollection()
    {
        // Assign
        var source =
            """
            [System.Obsolete("Message", true)]
            class Test
            {
            }
            """;

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        types[0].Attributes[0].Arguments.Should().HaveCount(2);
        types[0].Attributes[0].Arguments[0].Name.Should().Be("message");
        types[0].Attributes[0].Arguments[0].Type.Should().Be("string");
        types[0].Attributes[0].Arguments[0].Value.Should().Be("Message");
        types[0].Attributes[0].Arguments[1].Name.Should().Be("error");
        types[0].Attributes[0].Arguments[1].Type.Should().Be("bool");
        types[0].Attributes[0].Arguments[1].Value.Should().Be("true");
    }

    [TestMethod]
    public void ClassWithAttribute_Should_HaveAttributeWithNamedArgumentInCollection()
    {
        // Assign
        var source =
            """
            [System.Obsolete(DiagnosticId = "ID")]
            class Test
            {
            }
            """;

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        types[0].Attributes[0].Arguments.Should().HaveCount(1);
        types[0].Attributes[0].Arguments[0].Should().NotBeNull();
        types[0].Attributes[0].Arguments[0].Name.Should().Be("DiagnosticId");
        types[0].Attributes[0].Arguments[0].Value.Should().Be("ID");
    }

    [TestMethod]
    public void ClassWithAttribute_Should_HaveAttributeWithMixedArgumentInCollection()
    {
        // Assign
        var source =
            """
            [System.Obsolete("Message", DiagnosticId = "ID")]
            class Test
            {
            }
            """;

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        types[0].Attributes[0].Arguments.Should().HaveCount(2);
        types[0].Attributes[0].Arguments[0].Name.Should().Be("message");
        types[0].Attributes[0].Arguments[0].Value.Should().Be("Message");
        types[0].Attributes[0].Arguments[1].Name.Should().Be("DiagnosticId");
        types[0].Attributes[0].Arguments[1].Value.Should().Be("ID");
    }

    [TestMethod]
    public void EnumWithoutAttributes_Should_HaveEmptyAttributeCollection()
    {
        // Assign
        var source =
            """
            enum Test
            {
            }
            """;

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        types[0].Attributes.Should().BeEmpty();
    }

    [TestMethod]
    public void EnumWithAttribute_Should_HaveAttributeInCollection()
    {
        // Assign
        var source =
            """
            [System.Obsolete]
            enum Test
            {
            }
            """;

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        types[0].Attributes.Should().HaveCount(1);
        types[0].Attributes[0].Should().NotBeNull();
        types[0].Attributes[0].Name.Should().Be("System.Obsolete");
    }

    [TestMethod]
    public void InterfaceWithoutAttributes_Should_HaveEmptyAttributeCollection()
    {
        // Assign
        var source =
            """
            interface Test
            {
            }
            """;

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        types[0].Attributes.Should().BeEmpty();
    }

    [TestMethod]
    public void InterfaceWithAttribute_Should_HaveAttributeInCollection()
    {
        // Assign
        var source =
            """
            [System.Obsolete]
            interface Test
            {
            }
            """;

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        types[0].Attributes.Should().HaveCount(1);
        types[0].Attributes[0].Should().NotBeNull();
        types[0].Attributes[0].Name.Should().Be("System.Obsolete");
    }

    [TestMethod]
    public void StructWithoutAttributes_Should_HaveEmptyAttributeCollection()
    {
        // Assign
        var source =
            """
            struct Test
            {
            }
            """;

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        types[0].Attributes.Should().BeEmpty();
    }

    [TestMethod]
    public void StructWithAttribute_Should_HaveAttributeInCollection()
    {
        // Assign
        var source =
            """
            [System.Obsolete]
            struct Test
            {
            }
            """;

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        types[0].Attributes.Should().HaveCount(1);
        types[0].Attributes[0].Should().NotBeNull();
        types[0].Attributes[0].Name.Should().Be("System.Obsolete");
    }

    [TestMethod]
    public void MethodWithoutAttributes_Should_HaveEmptyAttributeCollection()
    {
        // Assign
        var source =
            """
            class Test
            {
                void Method() {}
            }
            """;

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        types[0].Methods[0].Attributes.Should().BeEmpty();
    }

    [TestMethod]
    public void MethodWithAttribute_Should_HaveAttributeInCollection()
    {
        // Assign
        var source =
            """
            class Test
            {
                [System.Obsolete]
                void Method() {}
            }
            """;

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        types[0].Methods[0].Attributes.Should().HaveCount(1);
        types[0].Methods[0].Attributes[0].Should().NotBeNull();
        types[0].Methods[0].Attributes[0].Name.Should().Be("System.Obsolete");
    }

    [TestMethod]
    public void ParameterWithoutAttributes_Should_HaveEmptyAttributeCollection()
    {
        // Assign
        var source =
            """
            class Test
            {
                void Method(string body) {}
            }
            """;

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        types[0].Methods[0].Parameters[0].Attributes.Should().BeEmpty();
    }

    [TestMethod]
    public void ParameterWithAttribute_Should_HaveAttributeInCollection()
    {
        // Assign
        var source =
            """
            class Test
            {
                void Method([System.ParamArray] string[] parameters) {}
            }
            """;

        // Act
        var types = TestHelper.VisitSyntaxTree(source, ignoreErrorCodes: "CS0674");

        // Assert
        types[0].Methods[0].Parameters[0].Attributes.Should().HaveCount(1);
        types[0].Methods[0].Parameters[0].Attributes[0].Should().NotBeNull();
        types[0].Methods[0].Parameters[0].Attributes[0].Name.Should().Be("System.ParamArray");
    }

    [TestMethod]
    public void ConstructorWithoutAttributes_Should_HaveEmptyAttributeCollection()
    {
        // Assign
        var source =
            """
            class Test
            {
                Test() {}
            }
            """;

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        types[0].Constructors[0].Attributes.Should().BeEmpty();
    }

    [TestMethod]
    public void ConstructorWithAttribute_Should_HaveAttributeInCollection()
    {
        // Assign
        var source =
            """
            class Test
            {
                [System.Obsolete]
                Test() {}
            }
            """;

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        types[0].Constructors[0].Attributes.Should().HaveCount(1);
        types[0].Constructors[0].Attributes[0].Should().NotBeNull();
        types[0].Constructors[0].Attributes[0].Name.Should().Be("System.Obsolete");
    }

    [TestMethod]
    public void FieldWithoutAttributes_Should_HaveEmptyAttributeCollection()
    {
        // Assign
        var source =
            """
            public class Test
            {
                public int field;
            }
            """;

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        types[0].Fields[0].Attributes.Should().BeEmpty();
    }

    [TestMethod]
    public void FieldWithAttribute_Should_HaveAttributeInCollection()
    {
        // Assign
        var source =
            """
            public class Test
            {
                [System.Obsolete]
                public int field;
            }
            """;

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        types[0].Fields[0].Attributes.Should().HaveCount(1);
        types[0].Fields[0].Attributes[0].Should().NotBeNull();
        types[0].Fields[0].Attributes[0].Name.Should().Be("System.Obsolete");
    }

    [TestMethod]
    public void EventWithoutAttributes_Should_HaveEmptyAttributeCollection()
    {
        // Assign
        var source =
            """
            class Test
            {
                event System.Action @event;

                Test() { @event(); }
            }
            """;

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        types[0].Events[0].Attributes.Should().BeEmpty();
    }

    [TestMethod]
    public void EventWithAttribute_Should_HaveAttributeInCollection()
    {
        // Assign
        var source =
            """
            class Test
            {
                [System.Obsolete]
                event System.Action @event;

                Test() { @event(); }
            }
            """;

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        types[0].Events[0].Attributes.Should().HaveCount(1);
        types[0].Events[0].Attributes[0].Should().NotBeNull();
        types[0].Events[0].Attributes[0].Name.Should().Be("System.Obsolete");
    }

    [TestMethod]
    public void PropertyWithoutAttributes_Should_HaveEmptyAttributeCollection()
    {
        // Assign
        var source =
            """
            class Test
            {
                int Property { get; set; }
            }
            """;

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        types[0].Properties[0].Attributes.Should().BeEmpty();
    }

    [TestMethod]
    public void PropertyWithAttribute_Should_HaveAttributeInCollection()
    {
        // Assign
        var source =
            """
            class Test
            {
                [System.Obsolete]
                int Property { get; set; }
            }
            """;

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        types[0].Properties[0].Attributes.Should().HaveCount(1);
        types[0].Properties[0].Attributes[0].Should().NotBeNull();
        types[0].Properties[0].Attributes[0].Name.Should().Be("System.Obsolete");
    }
}
