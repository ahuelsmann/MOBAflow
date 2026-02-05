// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Common;

using Moba.Common.Validation;
using Moba.Domain;

/// <summary>
/// Tests for JSON validation service.
/// Ensures invalid JSON files are rejected before deserialization.
/// </summary>
[TestFixture]
public class JsonValidationTests
{
    [Test]
    public void Validate_EmptyString_ShouldFail()
    {
        // Act
        var result = JsonValidationService.Validate(string.Empty);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("empty"));
    }

    [Test]
    public void Validate_WhitespaceOnly_ShouldFail()
    {
        // Act
        var result = JsonValidationService.Validate("   \n\t  ");

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("empty"));
    }

    [Test]
    public void Validate_InvalidJson_ShouldFail()
    {
        // Arrange
        var invalidJson = "{ this is not valid json }";

        // Act
        var result = JsonValidationService.Validate(invalidJson);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Invalid JSON"));
    }

    [Test]
    public void Validate_JsonArray_ShouldFail()
    {
        // Arrange
        var jsonArray = "[1, 2, 3]";

        // Act
        var result = JsonValidationService.Validate(jsonArray);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("must be an object"));
    }

    [Test]
    public void Validate_MissingNameProperty_ShouldFail()
    {
        // Arrange
        var json = @"{
            ""projects"": []
        }";

        // Act
        var result = JsonValidationService.Validate(json);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("name"));
    }

    [Test]
    public void Validate_MissingProjectsProperty_ShouldFail()
    {
        // Arrange
        var json = @"{
            ""name"": ""Test Solution""
        }";

        // Act
        var result = JsonValidationService.Validate(json);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("projects"));
    }

    [Test]
    public void Validate_ProjectsNotArray_ShouldFail()
    {
        // Arrange
        var json = @"{
            ""name"": ""Test Solution"",
            ""projects"": ""not an array""
        }";

        // Act
        var result = JsonValidationService.Validate(json);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("must be an array"));
    }

    [Test]
    public void Validate_ProjectMissingName_ShouldFail()
    {
        // Arrange
        var json = @"{
            ""name"": ""Test Solution"",
            ""projects"": [
                {
                    ""workflows"": []
                }
            ]
        }";

        // Act
        var result = JsonValidationService.Validate(json);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("missing 'name'"));
    }

    [Test]
    public void Validate_ProjectNotObject_ShouldFail()
    {
        // Arrange
        var json = @"{
            ""name"": ""Test Solution"",
            ""projects"": [
                ""not an object""
            ]
        }";

        // Act
        var result = JsonValidationService.Validate(json);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("not an object"));
    }

    [Test]
    public void Validate_ValidMinimalJson_ShouldSucceed()
    {
        // Arrange
        var json = @"{
            ""name"": ""Test Solution"",
            ""projects"": []
        }";

        // Act
        var result = JsonValidationService.Validate(json);

        // Assert
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.ErrorMessage, Is.Null);
    }

    [Test]
    public void Validate_ValidJsonWithProjects_ShouldSucceed()
    {
        // Arrange
        var json = @"{
            ""name"": ""Test Solution"",
            ""projects"": [
                {
                    ""name"": ""Project 1"",
                    ""workflows"": [],
                    ""trains"": []
                }
            ]
        }";

        // Act
        var result = JsonValidationService.Validate(json);

        // Assert
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.ErrorMessage, Is.Null);
    }

    [Test]
    public void Validate_MissingSchemaVersion_WithRequiredVersion_ShouldFail()
    {
        // Arrange
        var json = @"{
            ""name"": ""Test Solution"",
            ""projects"": []
        }";

        // Act
        var result = JsonValidationService.Validate(json, requiredSchemaVersion: 1);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Missing schema version"));
    }

    [Test]
    public void Validate_WrongSchemaVersion_ShouldFail()
    {
        // Arrange
        var json = @"{
            ""name"": ""Test Solution"",
            ""projects"": [],
            ""schemaVersion"": 999
        }";

        // Act
        var result = JsonValidationService.Validate(json, requiredSchemaVersion: Solution.CurrentSchemaVersion);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Incompatible schema version"));
        Assert.That(result.ErrorMessage, Does.Contain("999"));
    }

    [Test]
    public void Validate_InvalidSchemaVersionType_ShouldFail()
    {
        // Arrange
        var json = @"{
            ""name"": ""Test Solution"",
            ""projects"": [],
            ""schemaVersion"": ""not a number""
        }";

        // Act
        var result = JsonValidationService.Validate(json, requiredSchemaVersion: 1);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("must be a number"));
    }

    [Test]
    public void Validate_CorrectSchemaVersion_ShouldSucceed()
    {
        // Arrange
        var json = @"{
            ""name"": ""Test Solution"",
            ""projects"": [],
            ""schemaVersion"": 1
        }";

        // Act
        var result = JsonValidationService.Validate(json, requiredSchemaVersion: Solution.CurrentSchemaVersion);

        // Assert
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.ErrorMessage, Is.Null);
    }

    [Test]
    public void Validate_NoSchemaVersionRequired_ShouldSucceed()
    {
        // Arrange
        var json = @"{
            ""name"": ""Test Solution"",
            ""projects"": []
        }";

        // Act
        var result = JsonValidationService.Validate(json, requiredSchemaVersion: null);

        // Assert
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.ErrorMessage, Is.Null);
    }
}
