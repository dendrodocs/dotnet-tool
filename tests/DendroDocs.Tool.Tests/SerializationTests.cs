using DendroDocs.Json;
using System.Text.Json;

namespace DendroDocs.Tool.Tests
{
    [TestClass]
    public class SerializationTests
    {
        [TestMethod]
        public void NoTypes_Should_GiveEmptyArray()
        {
            // Assign
            var source = @"namespace Test {}";

            // Act
            var types = TestHelper.VisitSyntaxTree(source);

            var result = JsonSerializer.Serialize(types, JsonDefaults.SerializerOptions());

            // Assert
            result.ShouldBe("[]");
        }

        [TestMethod]
        public void InternalClass_Should_GiveOnlyFullName()
        {
            // Assign
            var source = @"class Test {}";

            // Act
            var types = TestHelper.VisitSyntaxTree(source);

            var result = JsonSerializer.Serialize(types, JsonDefaults.SerializerOptions());

            // Assert
            result.ShouldBe(@"[{""FullName"":""Test""}]");
        }

        [TestMethod]
        public void PublicClass_Should_GiveOnlyNonDefaultModifier()
        {
            // Assign
            var source = @"public class Test {}";

            // Act
            var types = TestHelper.VisitSyntaxTree(source);

            var result = JsonSerializer.Serialize(types, JsonDefaults.SerializerOptions());

            // Assert
            result.ShouldBe(@"[{""FullName"":""Test"",""Modifiers"":2}]");
        }

        [TestMethod]
        public void PrivateVoidMethod_Should_GiveOnlyName()
        {
            // Assign
            var source = @"class Test {
                void Method() {}
            }";

            // Act
            var types = TestHelper.VisitSyntaxTree(source);

            var result = JsonSerializer.Serialize(types, JsonDefaults.SerializerOptions());

            // Assert
            result.ShouldBe(@"[{""FullName"":""Test"",""Methods"":[{""Name"":""Method""}]}]");
        }

        [TestMethod]
        public void PrivateNonVoidMethod_Should_GiveNameAndReturnType()
        {
            // Assign
            var source = @"class Test {
                int Method() { return 0; }
            }";

            // Act
            var types = TestHelper.VisitSyntaxTree(source);

            var result = JsonSerializer.Serialize(types, JsonDefaults.SerializerOptions());

            // Assert
            result.ShouldMatch(@"[{""FullName"":""Test"",""Methods"":[{""Name"":""Method"",""ReturnType"":""int"",*}]}]");
        }

        [TestMethod]
        public void Attributes_Should_GiveNameAndType()
        {
            // Assign
            var source = @"
            [System.Obsolete]
            class Test {
            }";

            // Act
            var types = TestHelper.VisitSyntaxTree(source);

            var result = JsonSerializer.Serialize(types, JsonDefaults.SerializerOptions());

            // Assert
            result.ShouldMatch(@"[{""FullName"":""Test"",""Attributes"":[{""Type"":""System.ObsoleteAttribute"",""Name"":""System.Obsolete""}]}]");
        }

        [TestMethod]
        public void AttributeArguments_Should_GiveName_TypeAndValue()
        {
            // Assign
            var source = @"
            [System.Obsolete(""Reason"")]
            class Test {
            }";

            // Act
            var types = TestHelper.VisitSyntaxTree(source);

            var result = JsonSerializer.Serialize(types, JsonDefaults.SerializerOptions());

            // Assert
            result.ShouldMatch(@"[{""FullName"":""Test"",""Attributes"":[{""Type"":""System.ObsoleteAttribute"",""Name"":""System.Obsolete"",""Arguments"":[{""Name"":""message"",""Type"":""string"",""Value"":""Reason""}]}]}]");
        }
    }
}
