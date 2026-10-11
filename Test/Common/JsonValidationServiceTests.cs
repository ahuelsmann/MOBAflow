// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Common;

using Moba.Common.Validation;

/// <summary>
/// Unit tests for JsonValidationService and JsonValidationResult.
/// </summary>
[TestFixture]
[Category("Unit")]
internal class JsonValidationServiceTests
{
    [TestCase("[]")]
    [TestCase("null")]
    [TestCase("true")]
    [TestCase("false")]
    [TestCase("123")]
    [TestCase("\"not an object\"")]
    public void Validate_NonObjectRoot_ReturnsHelpfulFailure(string json)
    {
        var result = JsonValidationService.Validate(json);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("JSON root must be an object."));
        }
    }

    [TestCase("\"1\"")]
    [TestCase("null")]
    [TestCase("true")]
    [TestCase("{}")]
    [TestCase("[]")]
    [TestCase("1.5")]
    [TestCase("2147483648")]
    [TestCase("-2147483649")]
    public void Validate_NonIntegerSchemaVersion_ReturnsHelpfulFailure(string schemaVersion)
    {
        var json = "{\"name\":\"Test\",\"projects\":[],\"schemaVersion\":" + schemaVersion + "}";

        var result = JsonValidationService.Validate(json, 1);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("Schema version must be a number."));
        }
    }

    [Test]
    public void Validate_NamedProjects_ReturnsSuccess()
    {
        const string json = """{"name":"Test","schemaVersion":1,"projects":[{"name":"Main layout"},{"name":"Branch line"}]}""";

        var result = JsonValidationService.Validate(json, 1);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.ErrorMessage, Is.Null);
        }
    }

    [Test]
    public void Validate_ProjectMissingNameAfterValidProjects_IdentifiesZeroBasedIndex()
    {
        const string json = """{"name":"Test","projects":[{"name":"First"},{"name":"Second"},{"locomotives":[]}]}""";

        var result = JsonValidationService.Validate(json);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("Project at index 2 is missing 'name' property."));
        }
    }

    [Test]
    public void Validate_EmptyString_ReturnsFailure()
    {
        var result = JsonValidationService.Validate("");

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("empty"));
    }

    [Test]
    public void Validate_WhitespaceOnly_ReturnsFailure()
    {
        var result = JsonValidationService.Validate("   \t\n  ");

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Validate_InvalidJson_ReturnsFailure()
    {
        var result = JsonValidationService.Validate("{ invalid }");

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Invalid JSON"));
    }

    [Test]
    public void Validate_MissingName_ReturnsFailure()
    {
        var json = """{"projects":[]}""";

        var result = JsonValidationService.Validate(json);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("name"));
    }

    [Test]
    public void Validate_MissingProjects_ReturnsFailure()
    {
        var json = """{"name":"Test"}""";

        var result = JsonValidationService.Validate(json);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("projects"));
    }

    [Test]
    public void Validate_ProjectsNotArray_ReturnsFailure()
    {
        var json = """{"name":"Test","projects":123}""";

        var result = JsonValidationService.Validate(json);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("array"));
    }

    [Test]
    public void Validate_ValidMinimalSolution_ReturnsSuccess()
    {
        var json = """{"name":"My Solution","schemaVersion":1,"projects":[]}""";

        var result = JsonValidationService.Validate(json);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.ErrorMessage, Is.Null);
    }

    [Test]
    public void Validate_WithRequiredSchemaVersion_MissingVersion_ReturnsFailure()
    {
        var json = """{"name":"Test","projects":[]}""";

        var result = JsonValidationService.Validate(json, 1);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("schemaVersion"));
    }

    [Test]
    public void Validate_WithRequiredSchemaVersion_ZeroVersion_ReturnsFailure()
    {
        var json = """{"name":"Test","schemaVersion":0,"projects":[]}""";

        var result = JsonValidationService.Validate(json, 1);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Incompatible"));
    }

    [Test]
    public void Validate_WithRequiredSchemaVersion_WrongVersion_ReturnsFailure()
    {
        var json = """{"name":"Test","schemaVersion":2,"projects":[]}""";

        var result = JsonValidationService.Validate(json, 1);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Incompatible"));
    }

    [Test]
    public void Validate_WithRequiredSchemaVersion_MatchingVersion_ReturnsSuccess()
    {
        var json = """{"name":"Test","schemaVersion":1,"projects":[]}""";

        var result = JsonValidationService.Validate(json, 1);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_ProjectNotObject_ReturnsFailure()
    {
        var json = """{"name":"Test","projects":[123]}""";

        var result = JsonValidationService.Validate(json);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("index 0"));
    }

    [Test]
    public void Validate_ProjectMissingName_ReturnsFailure()
    {
        var json = """{"name":"Test","projects":[{"locomotives":[]}]}""";

        var result = JsonValidationService.Validate(json);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("'name'"));
    }

    [Test]
    public void JsonValidationResult_Success_IsValidTrue()
    {
        var result = JsonValidationResult.Success();

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.ErrorMessage, Is.Null);
    }

    [Test]
    public void JsonValidationResult_Failure_IsValidFalse()
    {
        var result = JsonValidationResult.Failure("Custom error");

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo("Custom error"));
    }
}
