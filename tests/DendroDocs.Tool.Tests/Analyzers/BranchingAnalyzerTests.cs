namespace DendroDocs.Tool.Tests.Analyzers;

[TestClass]
public class BranchingAnalyzerTests
{
    [DynamicData(nameof(GetIfStatements), DynamicDataSourceType.Method)]
    [TestMethod]
    public void ShouldParseIfBlocksCorrectly(string code, int sections, string[] conditions)
    {
        // Assign
        var source = @$"
        public class Test
        {{
            public int Something;
            void Method()
            {{
                {code}
            }}
        }}
        ";

        // Act
        var types = TestHelper.VisitSyntaxTree(source);

        // Assert
        types[0].Methods[0].Statements[0].Should().BeOfType<If>();

        var @if = (If)types[0].Methods[0].Statements[0];
        @if.Sections.Should().HaveCount(sections);
        @if.Sections.Select(s => s.Condition).Should().Equal(conditions);
    }

    private static IEnumerable<object[]> GetIfStatements()
    {
        yield return new object[] { "if (Something == 1) { }", 1, new[] { "Something == 1" } };
        yield return new object[] { "if (Something == 1) { } else { }", 2, new[] { "Something == 1", null } };
        yield return new object[] { "if (Something == 1) { } else if (Something == 2) { }", 2, new[] { "Something == 1", "Something == 2" } };
        yield return new object[] { "if (Something == 1) { } else if (Something == 2) { } else { }", 3, new[] { "Something == 1", "Something == 2", null } };
    }

    [DynamicData(nameof(GetSwitchStatements), DynamicDataSourceType.Method)]
    [TestMethod]
    public void ShouldParseSwitchBlocksCorrectly(string code, int sections, string[][] conditions)
    {
        // Assign
        var source = @$"
        public class Test
        {{
            public int Something;
            void Method()
            {{
                {code}
            }}
        }}
        ";

        // Act
        var types = TestHelper.VisitSyntaxTree(source, "CS1522");

        // Assert
        types[0].Methods[0].Statements[0].Should().BeOfType<Switch>();

        var @switch = (Switch)types[0].Methods[0].Statements[0];
        @switch.Sections.Should().HaveCount(sections);

        for (var i = 0; i < @switch.Sections.Count; i++)
        {
            @switch.Sections[i].Labels.Should().Equal(conditions[i]);
        }
    }

    private static IEnumerable<object[]> GetSwitchStatements()
    {
        yield return new object[] { "switch (Something) { }", 0, new[] { Array.Empty<string>() } };
        yield return new object[] { "switch (Something) { case 1: break; }", 1, new[] { new[] { "1" } } };
        yield return new object[] { "switch (Something) { case 1: case 2: break; }", 1, new[] { new[] { "1", "2" } } };
        yield return new object[] { "switch (Something) { case 1: break; case 2: break; }", 2, new[] { new[] { "1" }, new[] { "2" } } };
        yield return new object[] { "switch (Something) { default: break; }", 1, new[] { new[] { "default" } } };
        yield return new object[] { "switch (Something) { case 1 when true: break; }", 1, new[] { new[] { "1 when true" } } };
        yield return new object[] { "switch (Something) { case int value: break; }", 1, new[] { new[] { "int value" } } };
    }

    [DynamicData(nameof(GetSwitchExpressions), DynamicDataSourceType.Method)]
    [TestMethod]
    public void ShouldParseSwitchExpressionsCorrectly(string code, int sections, string[][] conditions)
    {
        // Assign
        var source = @$"
        public class Test
        {{
            public int Something;
            object Method()
            {{
                {code}
            }}
        }}
        ";

        // Act
        var types = TestHelper.VisitSyntaxTree(source, "CS8509");

        // Assert
        types[0].Methods[0].Statements[0].Should().BeOfType<ReturnDescription>();
        var @switch = types[0].Methods[0].Statements[1].Should().BeOfType<Switch>().Subject;
        @switch.Sections.Should().HaveCount(sections);

        for (var i = 0; i < @switch.Sections.Count; i++)
        {
            @switch.Sections[i].Labels.Should().Equal(conditions[i]);
        }
    }

    private static IEnumerable<object[]> GetSwitchExpressions()
    {
        yield return new object[] { "return Something switch { _ => false };", 1, new[] { new[] { "_" } } };
        yield return new object[] { "return Something switch { 1 => true };", 1, new[] { new[] { "1" } } };
        yield return new object[] { "return Something switch { 1 or 2 => true };", 1, new[] { new[] { "1 or 2" } } };
        yield return new object[] { "return Something switch { 1 => true, 2 => true };", 2, new[] { new[] { "1" }, new[] { "2" } } };
        yield return new object[] { "return Something switch { 1 when true => true, _ => false };", 2, new[] { new[] { "1 when true" }, new[] { "_" } } };
        yield return new object[] { "return Something switch { int value => false };", 1, new[] { new[] { "int value" } } };
    }
}
