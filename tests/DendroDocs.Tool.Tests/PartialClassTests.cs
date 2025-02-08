namespace DendroDocs.Tool.Tests;

[TestClass]
public class PartialClassTests
{
    [TestMethod]
    public void PartialClassesShouldBecomeASingleType()
    {
        // Assign
        var source = @"
        partial class Test
        {
            public string Property1 { get; }
        }

        partial class Test
        {
            public string Property2 { get; }
        }
        ";

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        types.Count.ShouldBe(1);
    }

    [TestMethod]
    public void MembersOfPartialClassesShouldBeCombined()
    {
        // Assign
        var source = @"
        partial class Test
        {
            public string Property1 { get; }
        }

        partial class Test
        {
            public string Property2 { get; }
        }
        ";

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        types[0].Properties.Count.ShouldBe(2);
    }
}
