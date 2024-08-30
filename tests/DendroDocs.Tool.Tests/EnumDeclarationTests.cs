namespace DendroDocs.Tool.Tests;

[TestClass]
public class EnumDeclarationTests
{
    [TestMethod]
    public void EnumWithoutModifier_Should_HaveDefaultInternalModifier()
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
        types[0].Modifiers.Should().Be(Modifier.Internal);
    }

    [TestMethod]
    public void PublicEnum_Should_HavePublicModifier()
    {
        // Assign
        var source =
            """
            public enum Test
            {
            }
            """;

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        types[0].Modifiers.Should().Be(Modifier.Public);
    }

    [TestMethod]
    public void EnumMembers_Should_HavePublicModifier()
    {
        // Assign
        var source =
            """
            public enum Test
            {
                Value
            }
            """;

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        types[0].EnumMembers[0].Modifiers.Should().Be(Modifier.Public);
    }

    [TestMethod]
    public void EnumMembers_Should_HaveCorrectName()
    {
        // Assign
        var source =
            """
            public enum Test
            {
                Value
            }
            """;

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        types[0].EnumMembers[0].Name.Should().Be("Value");
    }

    [TestMethod]
    public void EnumMembers_Should_HaveCorrectValue()
    {
        // Assign
        var source =
            """
            public enum Test
            {
                Value = 42
            }
            """;

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        types[0].EnumMembers[0].Value.Should().Be("42");
    }
}
