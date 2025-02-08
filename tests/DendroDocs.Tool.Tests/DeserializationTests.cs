using DendroDocs.Json;
using System.Text.Json;

namespace DendroDocs.Tool.Tests;

[TestClass]
public class DeserializationTests
{
    [TestMethod]
    public void NoTypes_Should_GiveEmptyArray()
    {
        // Assign
        var json = @"[]";

        // Act
        var types = JsonSerializer.Deserialize<List<TypeDescription>>(json, JsonDefaults.DeserializerOptions())!;

        // Assert
        types.ShouldBeEmpty();
    }

    [TestMethod]
    public void AClassWithoutAModifierShouldBeInternalByDefault()
    {
        // Assign
        var json = @"[{""FullName"":""Test""}]";

        // Act
        var types = JsonSerializer.Deserialize<List<TypeDescription>>(json, JsonDefaults.DeserializerOptions())!;

        // Assert
        types.Count.ShouldBe(1);
        types[0].ShouldNotBeNull();
        types[0].Type.ShouldBe(TypeType.Class);
        types[0].FullName.ShouldBe("Test");
        types[0].Modifiers.ShouldBe(Modifier.Internal);
    }

    [TestMethod]
    public void Collections_Should_NotBeNull()
    {
        // Assign
        var json = @"[{""FullName"":""Test""}]";

        // Act
        var types = JsonSerializer.Deserialize<List<TypeDescription>>(json, JsonDefaults.DeserializerOptions())!;

        // Assert
        types[0].Fields.ShouldBeEmpty();
        types[0].Constructors.ShouldBeEmpty();
        types[0].Properties.ShouldBeEmpty();
        types[0].Methods.ShouldBeEmpty();
        types[0].EnumMembers.ShouldBeEmpty();
        types[0].Events.ShouldBeEmpty();
    }

    [DataRow(00_001, Modifier.Internal, DisplayName = "A serialized value of `1` should be parsed as an `internal` modifier")]
    [DataRow(00_002, Modifier.Public, DisplayName = "A serialized value of `2` should be parsed as a `public` modifier")]
    [DataRow(00_004, Modifier.Private, DisplayName = "A serialized value of `4` should be parsed as a `private` modifier")]
    [DataRow(00_008, Modifier.Protected, DisplayName = "A serialized value of `8` should be parsed as a `protected` modifier")]
    [DataRow(00_012, Modifier.Private | Modifier.Protected, DisplayName = "A serialized value of `12` should be parsed as a `private` and `protected` modifier")]
    [DataRow(00_016, Modifier.Static, DisplayName = "A serialized value of `16` should be parsed as a `static` modifier")]
    [DataRow(00_032, Modifier.Abstract, DisplayName = "A serialized value of `32` should be parsed as an `abstract` modifier")]
    [DataRow(00_064, Modifier.Override, DisplayName = "A serialized value of `64` should be parsed as an `override` modifier")]
    [DataRow(00_128, Modifier.Readonly, DisplayName = "A serialized value of `128` should be parsed as a `readonly` modifier")]
    [DataRow(00_256, Modifier.Async, DisplayName = "A serialized value of `256` should be parsed as an `async` modifier")]
    [DataRow(00_512, Modifier.Const, DisplayName = "A serialized value of `512` should be parsed as a `const` modifier")]
    [DataRow(01_024, Modifier.Sealed, DisplayName = "A serialized value of `1024` should be parsed as a `sealed` modifier")]
    [DataRow(02_048, Modifier.Virtual, DisplayName = "A serialized value of `2048` should be parsed as a `virtual` modifier")]
    [DataRow(04_096, Modifier.Extern, DisplayName = "A serialized value of `4096` should be parsed as an `extern` modifier")]
    [DataRow(08_192, Modifier.New, DisplayName = "A serialized value of `8192` should be parsed as a `new` modifier")]
    [DataRow(16_384, Modifier.Unsafe, DisplayName = "A serialized value of `16384` should be parsed as an `unsafe` modifier")]
    [DataRow(32_768, Modifier.Partial, DisplayName = "A serialized value of `32768` should be parsed as a `partial` modifier")]
    [TestMethod]
    public void ModifiersShouldBeDeserializedCorrectly(int value, Modifier modifier)
    {
        // Assign
        var json = @$"[{{""Modifiers"":{value},""FullName"":""Test""}}]";

        // Act
        var types = JsonSerializer.Deserialize<List<TypeDescription>>(json, JsonDefaults.DeserializerOptions())!;

        // Assert
        types[0].Modifiers.ShouldBe(modifier);
    }

    [TestMethod]
    public void MembersOfAClassWithoutAModifierShouldBePrivateByDefault()
    {
        // Assign
        var json = @"[{""FullName"":""Test"",""Methods"":[{""Name"":""Method""}]}]";

        // Act
        var types = JsonSerializer.Deserialize<List<TypeDescription>>(json, JsonDefaults.DeserializerOptions())!;

        // Assert
        types[0].Methods.Count.ShouldBe(1);
        types[0].Methods[0].ShouldNotBeNull();
        types[0].Methods[0].Name.ShouldBe("Method");
        types[0].Methods[0].Modifiers.ShouldBe(Modifier.Private);
    }

    [TestMethod]
    public void AttributeCollection_Should_GiveAttributeWithNameAndType()
    {
        // Assign
        var json = @"[{""FullName"":""Test"",""Attributes"":[{""Type"":""System.ObsoleteAttribute"",""Name"":""System.Obsolete""}]}]";

        // Act
        var types = JsonSerializer.Deserialize<List<TypeDescription>>(json, JsonDefaults.DeserializerOptions())!;

        // Assert
        types[0].Attributes.Count.ShouldBe(1);
        types[0].Attributes[0].ShouldNotBeNull();
        types[0].Attributes[0].Type.ShouldBe("System.ObsoleteAttribute");
        types[0].Attributes[0].Name.ShouldBe("System.Obsolete");
    }

    [TestMethod]
    public void AttributeArgumentCollection_Should_GiveAttributeArgumentWithName_TypeAndValue()
    {
        // Assign
        var json = @"[{""FullName"":""Test"",""Attributes"":[{""Type"":""System.ObsoleteAttribute"",""Name"":""System.Obsolete"",""Arguments"":[{""Name"":""\""Reason\"""",""Type"":""string"",""Value"":""Reason""}]}]}]";

        // Act
        var types = JsonSerializer.Deserialize<List<TypeDescription>>(json, JsonDefaults.DeserializerOptions())!;

        // Assert
        types[0].Attributes[0].Arguments.Count.ShouldBe(1);
        types[0].Attributes[0].Arguments[0].ShouldNotBeNull();
        types[0].Attributes[0].Arguments[0].Type.ShouldBe("string");
        types[0].Attributes[0].Arguments[0].Name.ShouldBe(@"""Reason""");
        types[0].Attributes[0].Arguments[0].Value.ShouldBe(@"Reason");
    }

    [TestMethod]
    public void AStatementInAMethodBodyShouldHaveTheMethodAsParent()
    {
        // Assign
        var json = @"[{
            ""FullName"":""Test"",
            ""Methods"":[{
                ""Name"":""Method"",
                ""Statements"":[{
                    ""$type"":""DendroDocs.InvocationDescription, DendroDocs.Shared"",
                    ""ContainingType"": ""Test"",
                    ""Name"": ""TestMethod""
                }]
            }]
        }]";

        // Act
        var types = JsonSerializer.Deserialize<List<TypeDescription>>(json, JsonDefaults.DeserializerOptions())!;

        // Assert
        types[0].Methods[0].Statements[0].Parent.ShouldBe(types[0].Methods[0]);
    }

    [TestMethod]
    public void AStatementInAConstructorBodyShouldHaveTheConstructorAsParent()
    {
        // Assign
        var json = @"[{
            ""FullName"":""Test"",
            ""Constructors"":[{
                ""Name"":""Constructor"",
                ""Statements"":[{
                    ""$type"":""DendroDocs.InvocationDescription, DendroDocs.Shared"",
                    ""ContainingType"": ""Test"",
                    ""Name"": ""TestMethod""
                }]
            }]
        }]";

        // Act
        var types = JsonSerializer.Deserialize<List<TypeDescription>>(json, JsonDefaults.DeserializerOptions())!;

        // Assert
        types[0].Constructors[0].Statements[0].Parent.ShouldBe(types[0].Constructors[0]);
    }

    [TestMethod]
    public void AnIfElseSectionShouldHaveTheIfAsParent()
    {
        // Assign
        var json = @"[{
            ""FullName"":""Test"",
            ""Methods"":[{
                ""Name"":""Method"",
                ""Statements"":[{
                    ""$type"": ""DendroDocs.If, DendroDocs.Shared"",
                    ""Sections"":[{}]
                }]
            }]
        }]";

        // Act
        var types = JsonSerializer.Deserialize<List<TypeDescription>>(json, JsonDefaults.DeserializerOptions())!;

        // Assert
        types[0].Methods[0].Statements[0].ShouldBeOfType<If>();

        var @if = (If)types[0].Methods[0].Statements[0];
        @if.Sections[0].Parent.ShouldBe(@if);
    }

    [TestMethod]
    public void AnIfElseConditionShouldBeParsedCorrectly()
    {
        // Assign
        var json = @"[{
            ""FullName"":""Test"",
            ""Methods"":[{
                ""Name"":""Method"",
                ""Statements"":[{
                    ""$type"": ""DendroDocs.If, DendroDocs.Shared"",
                    ""Sections"":[{""Condition"": ""true""}]
                }]
            }]
        }]";

        // Act
        var types = JsonSerializer.Deserialize<List<TypeDescription>>(json, JsonDefaults.DeserializerOptions())!;

        // Assert
        types[0].Methods[0].Statements[0].ShouldBeOfType<If>();

        var @if = (If)types[0].Methods[0].Statements[0];
        @if.Sections[0].Condition.ShouldBe("true");
    }
    
    [TestMethod]
    public void AStatementInAnIfElseSectionShouldHaveTheIfElseSectionAsParent()
    {
        // Assign
        var json = @"[{
            ""FullName"":""Test"",
            ""Methods"":[{
                ""Name"":""Method"",
                ""Statements"":[{
                    ""$type"": ""DendroDocs.If, DendroDocs.Shared"",
                    ""Sections"":[{
                        ""Statements"":[{
                            ""$type"":""DendroDocs.InvocationDescription, DendroDocs.Shared"",
                            ""ContainingType"": ""Test"",
                            ""Name"": ""TestMethod""
                        }]
                    }]
                }]
            }]
        }]";

        // Act
        var types = JsonSerializer.Deserialize<List<TypeDescription>>(json, JsonDefaults.DeserializerOptions())!;

        // Assert
        types[0].Methods[0].Statements[0].ShouldBeOfType<If>();

        var @if = (If)types[0].Methods[0].Statements[0];
        @if.Sections[0].Statements[0].Parent.ShouldBe(@if.Sections[0]);
    }

    [TestMethod]
    public void ASwitchSectionShouldHaveTheSwitchAsParent()
    {
        // Assign
        var json = @"[{
            ""FullName"":""Test"",
            ""Methods"":[{
                ""Name"":""Method"",
                ""Statements"":[{
                    ""$type"": ""DendroDocs.Switch, DendroDocs.Shared"",
                    ""Sections"":[{}]
                }]
            }]
        }]";

        // Act
        var types = JsonSerializer.Deserialize<List<TypeDescription>>(json, JsonDefaults.DeserializerOptions())!;

        // Assert
        types[0].Methods[0].Statements[0].ShouldBeOfType<Switch>();

        var @switch = (Switch)types[0].Methods[0].Statements[0];
        @switch.Sections[0].Parent.ShouldBe(@switch);
    }

    [TestMethod]
    public void ASwitchExpressionShouldBeParsedCorrectly()
    {
        // Assign
        var json = @"[{
            ""FullName"":""Test"",
            ""Methods"":[{
                ""Name"":""Method"",
                ""Statements"":[{
                    ""$type"": ""DendroDocs.Switch, DendroDocs.Shared"",
                    ""Expression"":""type""
                }]
            }]
        }]";

        // Act
        var types = JsonSerializer.Deserialize<List<TypeDescription>>(json, JsonDefaults.DeserializerOptions())!;

        // Assert
        types[0].Methods[0].Statements[0].ShouldBeOfType<Switch>();

        var @switch = (Switch)types[0].Methods[0].Statements[0];
        @switch.Expression.ShouldBe("type");
    }

    [TestMethod]
    public void SwitchLabelsShouldBeParsedCorrectly()
    {
        // Assign
        var json = @"[{
            ""FullName"":""Test"",
            ""Methods"":[{
                ""Name"":""Method"",
                ""Statements"":[{
                    ""$type"": ""DendroDocs.Switch, DendroDocs.Shared"",
                    ""Sections"":[{
                        ""Labels"": [""System.String""]
                    }]
                }]
            }]
        }]";

        // Act
        var types = JsonSerializer.Deserialize<List<TypeDescription>>(json, JsonDefaults.DeserializerOptions())!;

        // Assert
        types[0].Methods[0].Statements[0].ShouldBeOfType<Switch>();

        var @switch = (Switch)types[0].Methods[0].Statements[0];
        @switch.Sections[0].Labels.Count.ShouldBe(1);
        @switch.Sections[0].Labels.ShouldContain("System.String");
    }

    [TestMethod]
    public void AStatementInASwitchSectionShouldHaveTheSwitchSectionAsParent()
    {
        // Assign
        var json = @"[{
            ""FullName"":""Test"",
            ""Methods"":[{
                ""Name"":""Method"",
                ""Statements"":[{
                    ""$type"": ""DendroDocs.Switch, DendroDocs.Shared"",
                    ""Sections"":[{
                        ""Statements"":[{
                            ""$type"":""DendroDocs.InvocationDescription, DendroDocs.Shared"",
                            ""ContainingType"": ""Test"",
                            ""Name"": ""TestMethod""
                        }]
                    }]
                }]
            }]
        }]";

        // Act
        var types = JsonSerializer.Deserialize<List<TypeDescription>>(json, JsonDefaults.DeserializerOptions())!;

        // Assert
        types[0].Methods[0].Statements[0].ShouldBeOfType<Switch>();

        var @switch = (Switch)types[0].Methods[0].Statements[0];
        @switch.Sections[0].Statements[0].Parent.ShouldBe(@switch.Sections[0]);
    }
}
