namespace DendroDocs.Tool.Tests;

[TestClass]
public class JsonValidationTests
{
    [TestMethod]
    public void ValidJsonShouldValidateSuccessfully()
    {
        // Arrange
        string content =
            """
            [
                { "FullName": "ValidJson" }
            ]
            """;

        // Act
        var isValid = JsonValidator.ValidateJson(ref content, out IList<string> validationErrors);

        // Assert
        isValid.ShouldBeTrue();
        validationErrors.ShouldBeEmpty();
    }

    [TestMethod]
    public void MissingFullNameShouldFailValidation()
    {
        // Arrange
        string content =
            """
            [
                { "BaseTypes": ["ValidJson"] }
            ]
            """;

        // Act
        var isValid = JsonValidator.ValidateJson(ref content, out IList<string> validationErrors);

        // Assert
        isValid.ShouldBeFalse();
        validationErrors.ShouldNotBeEmpty();
    }

    [TestMethod]
    public void MismatchedTypeShouldFailValidation()
    {
        // Arrange
        string content =
            """
            [
                { "FullName": 123 }
            ]
            """;

        // Act
        var isValid = JsonValidator.ValidateJson(ref content, out IList<string> validationErrors);

        // Assert
        isValid.ShouldBeFalse();
        validationErrors.ShouldNotBeEmpty();
    }

    [TestMethod]
    public void MalformedJsonShouldFailValidation()
    {
        // Arrange
        string content =
            """
            [
                { "FullName": "ValidJson" }
            """;

        // Act
        var isValid = JsonValidator.ValidateJson(ref content, out IList<string> validationErrors);

        // Assert
        isValid.ShouldBeFalse();
        validationErrors.ShouldSatisfyAllConditions(
            () => validationErrors.Count.ShouldBe(1),
            () => validationErrors[0].ShouldStartWith("Exception during validation")
        );
    }

    [TestMethod]
    public void UnexpectedElementsShouldFailValidation()
    {
        // Arrange
        string content = 
            """
            [
                {
                    "FullName": "ValidJson",
                    "Unexpected": true
                }
            ]
            """;

        // Act
        var isValid = JsonValidator.ValidateJson(ref content, out IList<string> validationErrors);

        // Assert
        isValid.ShouldBeFalse();
        validationErrors.ShouldSatisfyAllConditions(
            () => validationErrors.Count.ShouldBe(1),
            () => validationErrors[0].ShouldContain("Unexpected")
        );
    }
}
