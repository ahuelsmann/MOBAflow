// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using Moba.Backend.Interface;
using Moba.Backend.Service;
using Moba.Domain;

/// <summary>
/// Unit tests for ITrainClassParser interface implementation.
/// Tests locomotive class number parsing and resolution logic using germany-locomotives.json data.
/// </summary>
[TestFixture]
public class TrainClassParserTests
{
    private ITrainClassParser _parser = null!;

    [SetUp]
    public void SetUp()
    {
        // Initialize library with test JSON file
        // Navigate from test binary directory (Test/bin/Debug/net10.0) to WinUI folder
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        
        // Remove 'Test/bin/Debug/net10.0/' parts to get solution root
        var solutionRoot = Path.GetFullPath(Path.Combine(baseDir, "..\\..\\..\\.."));
        var jsonPath = Path.Combine(solutionRoot, "WinUI", "germany-locomotives.json");
        
        if (File.Exists(jsonPath))
        {
            TrainClassLibrary.Initialize(jsonPath);
        }
        else
        {
            throw new FileNotFoundException($"germany-locomotives.json not found at: {jsonPath}");
        }

        _parser = new TrainClassParser();
    }

    #region Parse Tests - BR 110 (Bügelfalte)

    [Test]
    public void Parse_WithNumericInput_110_ShouldReturnBR110()
    {
        // Arrange
        const string input = "110";

        // Act
        var result = _parser.Parse(input);

        // Assert
        Assert.That(result, Is.Not.Null, "Result should not be null");
        Assert.That(result?.Name, Does.Contain("110"));
        Assert.That(result?.Vmax, Is.GreaterThan(0));
        Assert.That(result?.Type, Is.EqualTo("Elektrolok"));
    }

    [Test]
    public void Parse_WithLowercaseBRPrefix_br110_ShouldReturnBR110()
    {
        // Arrange
        const string input = "br110";

        // Act
        var result = _parser.Parse(input);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result?.Name, Does.Contain("110"));
    }

    [Test]
    public void Parse_WithUppercaseBRPrefix_BR110_ShouldReturnBR110()
    {
        // Arrange
        const string input = "BR110";

        // Act
        var result = _parser.Parse(input);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result?.Name, Does.Contain("110"));
    }

    [Test]
    public void Parse_WithBRPrefixAndSpace_BR_110_ShouldReturnBR110()
    {
        // Arrange
        const string input = "BR 110";

        // Act
        var result = _parser.Parse(input);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result?.Name, Does.Contain("110"));
    }

    [Test]
    public void Parse_WithEPrefix_E10_ShouldReturnBR110()
    {
        // Arrange
        const string input = "E10";

        // Act
        var result = _parser.Parse(input);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result?.Name, Does.Match(@"(E 10|110)"));
    }

    [Test]
    public void Parse_WithMixedCaseBRPrefix_Br_110_ShouldReturnBR110()
    {
        // Arrange
        const string input = "Br 110";

        // Act
        var result = _parser.Parse(input);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result?.Name, Does.Contain("110"));
    }

    #endregion

    #region Parse Tests - Other Classes

    [Test]
    public void Parse_WithInput_103_ShouldReturnBR103()
    {
        // Arrange
        const string input = "103";

        // Act
        var result = _parser.Parse(input);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result?.Name, Does.Contain("103"));
        Assert.That(result?.Vmax, Is.EqualTo(200));
    }

    [Test]
    public void Parse_WithInput_38_ShouldReturnBR38SteamLoco()
    {
        // Arrange
        const string input = "38";

        // Act
        var result = _parser.Parse(input);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result?.Name, Does.Contain("38"));
        Assert.That(result?.Type, Is.EqualTo("Dampflok"));
    }

    [Test]
    public void Parse_WithInput_412_ShouldReturnICE4()
    {
        // Arrange
        const string input = "412";

        // Act
        var result = _parser.Parse(input);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result?.Name, Does.Contain("412"));
        Assert.That(result?.Type, Is.EqualTo("Triebzug"));
        Assert.That(result?.Vmax, Is.GreaterThan(0));
    }

    [Test]
    public void Parse_WithInput_BR01_ShouldReturnSteamLoco()
    {
        // Arrange
        const string input = "01";

        // Act
        var result = _parser.Parse(input);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result?.Name, Does.Contain("01"));
        Assert.That(result?.Type, Is.EqualTo("Dampflok"));
    }

    [Test]
    public void Parse_WithInput_218_ShouldReturnDieselLoco()
    {
        // Arrange
        const string input = "218";

        // Act
        var result = _parser.Parse(input);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result?.Name, Does.Contain("218"));
        Assert.That(result?.Type, Is.EqualTo("Diesellok"));
    }

    #endregion

    #region Parse Tests - Invalid/Not Found

    [Test]
    public void Parse_WithUnknownInput_999_ShouldReturnNull()
    {
        // Arrange
        const string input = "999";

        // Act
        var result = _parser.Parse(input);

        // Assert
        Assert.That(result, Is.Null, "Unknown class should return null");
    }

    [Test]
    public void Parse_WithEmptyString_ShouldReturnNull()
    {
        // Arrange
        const string input = "";

        // Act
        var result = _parser.Parse(input);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void Parse_WithWhitespaceOnly_ShouldReturnNull()
    {
        // Arrange
        const string input = "   ";

        // Act
        var result = _parser.Parse(input);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void Parse_WithCompletelyInvalidFormat_XYZ_ShouldReturnNull()
    {
        // Arrange
        const string input = "xyz";

        // Act
        var result = _parser.Parse(input);

        // Assert
        Assert.That(result, Is.Null);
    }

    #endregion

    #region ParseAsync Tests

    [Test]
    public async Task ParseAsync_WithInput_110_ShouldReturnBR110()
    {
        // Arrange
        const string input = "110";

        // Act
        var result = await _parser.ParseAsync(input);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result?.Name, Does.Contain("110"));
    }

    [Test]
    public async Task ParseAsync_WithUnknownInput_999_ShouldReturnNull()
    {
        // Arrange
        const string input = "999";

        // Act
        var result = await _parser.ParseAsync(input);

        // Assert
        Assert.That(result, Is.Null);
    }

    #endregion

    #region Edge Cases

    [Test]
    public void Parse_WithLeadingAndTrailingSpaces_Should_Normalize()
    {
        // Arrange
        const string input = "  BR 110  ";

        // Act
        var result = _parser.Parse(input);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result?.Name, Does.Contain("110"));
    }

    [Test]
    public void Parse_WithMultipleSpaces_BRSpace_110_Should_Normalize()
    {
        // Arrange
        const string input = "BR   110";  // Multiple spaces

        // Act
        var result = _parser.Parse(input);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result?.Name, Does.Contain("110"));
    }

    [Test]
    public void Parse_WithDecimal_110_3_ShouldMatch()
    {
        // Arrange
        const string input = "110.3";

        // Act
        var result = _parser.Parse(input);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result?.Name, Does.Contain("110"));
    }

    #endregion

    #region Data Integrity Tests

    [Test]
    public void Parse_ResultsShould_HaveValidDataFields()
    {
        // Arrange
        const string input = "110";

        // Act
        var result = _parser.Parse(input);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result?.Name, Is.Not.Null.And.Not.Empty);
        Assert.That(result?.Type, Is.Not.Null.And.Not.Empty);
        Assert.That(result?.Epoch, Is.Not.Null.And.Not.Empty);
        Assert.That(result?.Description, Is.Not.Null.And.Not.Empty);
        Assert.That(result?.Vmax, Is.GreaterThan(0));
    }

    [Test]
    public void GetAllClasses_ShouldReturn_ManyClocomives()
    {
        // Act
        var all = TrainClassLibrary.GetAllClasses();

        // Assert
        Assert.That(all.Count, Is.GreaterThan(50), "Should have 100+ locomotives from JSON");
    }

    [Test]
    public void GetByType_WithElektrolok_ShouldReturn_OnlyElektroloks()
    {
        // Act
        var elektroloks = TrainClassLibrary.GetByType("Elektrolok");

        // Assert
        Assert.That(elektroloks.Count, Is.GreaterThan(10));
        Assert.That(elektroloks, Has.All.Property("Type").EqualTo("Elektrolok"));
    }

    #endregion
}
