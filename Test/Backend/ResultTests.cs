// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

/// <summary>
/// Unit tests for Result and Result&lt;T&gt; classes.
/// Tests functional error handling patterns.
/// </summary>
[TestFixture]
public class ResultTests
{
    #region Result<T> Tests
    [Test]
    public void Success_CreatesSuccessResult()
    {
        // Act
        var result = Result<int>.Success(42);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.IsFailure, Is.False);
        Assert.That(result.Value, Is.EqualTo(42));
        Assert.That(result.Error, Is.Null);
    }

    [Test]
    public void Failure_CreatesFailureResult()
    {
        // Act
        var result = Result<int>.Failure("Something went wrong");

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Value, Is.EqualTo(default(int)));
        Assert.That(result.Error, Is.EqualTo("Something went wrong"));
    }

    [Test]
    public void Success_WithReferenceType()
    {
        // Arrange
        var testObject = new TestClass { Value = "test" };

        // Act
        var result = Result<TestClass>.Success(testObject);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.SameAs(testObject));
    }

    [Test]
    public void Success_WithNull_StillSucceeds()
    {
        // Act
        var result = Result<string?>.Success(null);

        // Assert
        Assert.That(result.IsSuccess, Is.True, "Result with null value should still be success");
        Assert.That(result.Value, Is.Null);
    }

    [Test]
    public void Map_TransformsValue_WhenSuccess()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var mapped = result.Map(x => x.ToString());

        // Assert
        Assert.That(mapped.IsSuccess, Is.True);
        Assert.That(mapped.Value, Is.EqualTo("42"));
    }

    [Test]
    public void Map_PropagatesError_WhenFailure()
    {
        // Arrange
        var result = Result<int>.Failure("Original error");

        // Act
        var mapped = result.Map(x => x.ToString());

        // Assert
        Assert.That(mapped.IsSuccess, Is.False);
        Assert.That(mapped.Error, Is.EqualTo("Original error"));
    }

    [Test]
    public void Map_CanChangeType()
    {
        // Arrange
        var result = Result<string>.Success("123");

        // Act
        var mapped = result.Map(int.Parse);

        // Assert
        Assert.That(mapped.IsSuccess, Is.True);
        Assert.That(mapped.Value, Is.EqualTo(123));
    }

    [Test]
    public void Map_ChainMultipleTransformations()
    {
        // Arrange
        var result = Result<int>.Success(10);

        // Act
        var final = result
            .Map(x => x * 2)      // 20
            .Map(x => x + 5)      // 25
            .Map(x => x.ToString()); // "25"

        // Assert
        Assert.That(final.IsSuccess, Is.True);
        Assert.That(final.Value, Is.EqualTo("25"));
    }

    [Test]
    public void OnSuccess_ExecutesAction_WhenSuccess()
    {
        // Arrange
        var result = Result<int>.Success(42);
        int? capturedValue = null;

        // Act
        var returned = result.OnSuccess(v => capturedValue = v);

        // Assert
        Assert.That(capturedValue, Is.EqualTo(42));
        Assert.That(returned, Is.SameAs(result), "Should return same instance for chaining");
    }

    [Test]
    public void OnSuccess_DoesNotExecute_WhenFailure()
    {
        // Arrange
        var result = Result<int>.Failure("Error");
        var actionExecuted = false;

        // Act
        result.OnSuccess(_ => actionExecuted = true);

        // Assert
        Assert.That(actionExecuted, Is.False);
    }

    [Test]
    public void OnFailure_ExecutesAction_WhenFailure()
    {
        // Arrange
        var result = Result<int>.Failure("Test error");
        string? capturedError = null;

        // Act
        var returned = result.OnFailure(e => capturedError = e);

        // Assert
        Assert.That(capturedError, Is.EqualTo("Test error"));
        Assert.That(returned, Is.SameAs(result), "Should return same instance for chaining");
    }

    [Test]
    public void OnFailure_DoesNotExecute_WhenSuccess()
    {
        // Arrange
        var result = Result<int>.Success(42);
        var actionExecuted = false;

        // Act
        result.OnFailure(_ => actionExecuted = true);

        // Assert
        Assert.That(actionExecuted, Is.False);
    }

    [Test]
    public void OnSuccess_OnFailure_CanBeChained()
    {
        // Arrange
        var successResult = Result<int>.Success(42);
        var failureResult = Result<int>.Failure("Error");

        int? successValue = null;
        string? failureError = null;

        // Act
        successResult
            .OnSuccess(v => successValue = v)
            .OnFailure(e => failureError = e);

        failureResult
            .OnSuccess(v => successValue = v)
            .OnFailure(e => failureError = e);

        // Assert
        Assert.That(successValue, Is.EqualTo(42));
        Assert.That(failureError, Is.EqualTo("Error"));
    }

    [Test]
    public void Record_Equality_ByValue()
    {
        // Arrange
        var result1 = Result<int>.Success(42);
        var result2 = Result<int>.Success(42);
        var result3 = Result<int>.Success(43);

        // Assert
        Assert.That(result1, Is.EqualTo(result2), "Records with same values should be equal");
        Assert.That(result1, Is.Not.EqualTo(result3), "Records with different values should not be equal");
    }

    [Test]
    public void Failure_Record_Equality()
    {
        // Arrange
        var result1 = Result<int>.Failure("Error");
        var result2 = Result<int>.Failure("Error");
        var result3 = Result<int>.Failure("Different Error");

        // Assert
        Assert.That(result1, Is.EqualTo(result2));
        Assert.That(result1, Is.Not.EqualTo(result3));
    }
    #endregion

    #region Result (non-generic) Tests
    [Test]
    public void Result_Success_CreatesSuccessResult()
    {
        // Act
        var result = Result.Success();

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.IsFailure, Is.False);
        Assert.That(result.Error, Is.Null);
    }

    [Test]
    public void Result_Failure_CreatesFailureResult()
    {
        // Act
        var result = Result.Failure("Operation failed");

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.EqualTo("Operation failed"));
    }

    [Test]
    public void Result_Record_Equality()
    {
        // Arrange
        var result1 = Result.Success();
        var result2 = Result.Success();
        var result3 = Result.Failure("Error");
        var result4 = Result.Failure("Error");

        // Assert
        Assert.That(result1, Is.EqualTo(result2));
        Assert.That(result3, Is.EqualTo(result4));
        Assert.That(result1, Is.Not.EqualTo(result3));
    }
    #endregion

    #region Edge Cases
    [Test]
    public void Map_WithNullValue_HandlesGracefully()
    {
        // Arrange
        var result = Result<string?>.Success(null);

        // Act
        var mapped = result.Map(x => x?.Length ?? 0);

        // Assert
        Assert.That(mapped.IsSuccess, Is.True);
        Assert.That(mapped.Value, Is.EqualTo(0));
    }

    [Test]
    public void OnSuccess_WithNullValue_StillExecutes()
    {
        // Arrange
        var result = Result<string?>.Success(null);
        var actionExecuted = false;

        // Act
        result.OnSuccess(_ => actionExecuted = true);

        // Assert
        Assert.That(actionExecuted, Is.True, "OnSuccess should execute even with null value");
    }

    [Test]
    public void Map_UsesDefaultError_WhenErrorIsNull()
    {
        // Arrange - Create result with null error manually (edge case)
        var result = new Result<int> { Error = null, Value = default };

        // Act
        var mapped = result.Map(x => x.ToString());

        // Assert
        Assert.That(mapped.IsSuccess, Is.True); // null error means success
    }

    [Test]
    public void Failure_WithEmptyString()
    {
        // Act
        var result = Result<int>.Failure(string.Empty);

        // Assert
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.EqualTo(string.Empty));
    }
    #endregion

    #region Real-World Usage Examples
    [Test]
    public void RealWorld_DatabaseQuery_Success()
    {
        // Simulate database query
        Result<User> QueryUser(int id)
        {
            if (id > 0)
                return Result<User>.Success(new User { Id = id, Name = "John" });
            return Result<User>.Failure("Invalid ID");
        }

        // Act
        var result = QueryUser(1);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value?.Name, Is.EqualTo("John"));
    }

    [Test]
    public void RealWorld_Validation_Pipeline()
    {
        // Arrange
        Result<string> ValidateEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return Result<string>.Failure("Email is required");
            if (!email.Contains("@"))
                return Result<string>.Failure("Invalid email format");
            return Result<string>.Success(email);
        }

        // Act
        var valid = ValidateEmail("test@example.com");
        var invalid1 = ValidateEmail("");
        var invalid2 = ValidateEmail("notanemail");

        // Assert
        Assert.That(valid.IsSuccess, Is.True);
        Assert.That(invalid1.Error, Is.EqualTo("Email is required"));
        Assert.That(invalid2.Error, Is.EqualTo("Invalid email format"));
    }

    [Test]
    public void RealWorld_ErrorHandling_WithLogging()
    {
        // Arrange
        var errorLogs = new List<string>();

        Result<int> DivideNumbers(int a, int b)
        {
            if (b == 0)
                return Result<int>.Failure("Division by zero");
            return Result<int>.Success(a / b);
        }

        // Act
        var result = DivideNumbers(10, 0)
            .OnFailure(error => errorLogs.Add($"Error occurred: {error}"));

        // Assert
        Assert.That(result.IsFailure, Is.True);
        Assert.That(errorLogs, Has.Count.EqualTo(1));
        Assert.That(errorLogs[0], Does.Contain("Division by zero"));
    }
    #endregion
}

// Test helper classes
file class TestClass
{
    public string? Value { get; set; }
}

file class User
{
    public int Id { get; set; }
    public string? Name { get; set; }
}