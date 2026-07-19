// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Domain;

using Moba.Domain;

/// <summary>
/// Tests for <see cref="TrainClassLibrary"/> lookup and filtering against master locomotive data.
/// Uses a dedicated temp JSON fixture so matching rules stay deterministic without touching UI code.
/// </summary>
[TestFixture]
[NonParallelizable]
[Order(0)]
internal sealed class TrainClassLibraryUninitializedTests
{
    [Test]
    public void PublicQueries_ThrowInvalidOperationException_BeforeInitialization()
    {
        const string expectedMessage =
            "TrainClassLibrary not initialized. Call Initialize(jsonPath) during app startup.";

        var lookupException = Assert.Throws<InvalidOperationException>(() =>
            TrainClassLibrary.TryGetByClassNumber("110"));
        var allClassesException = Assert.Throws<InvalidOperationException>(() =>
            TrainClassLibrary.GetAllClasses());
        var typeException = Assert.Throws<InvalidOperationException>(() =>
            TrainClassLibrary.GetByType("Electric loco"));

        Assert.Multiple(() =>
        {
            Assert.That(lookupException!.Message, Is.EqualTo(expectedMessage));
            Assert.That(allClassesException!.Message, Is.EqualTo(expectedMessage));
            Assert.That(typeException!.Message, Is.EqualTo(expectedMessage));
        });
    }
}

/// <summary>
/// Tests catalog loading, lookup and filtering after initialization.
/// </summary>
[TestFixture]
[NonParallelizable]
[Order(1)]
internal sealed class TrainClassLibraryTests
{
    private string _tempDirectory = null!;
    private string _jsonPath = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"moba-train-classes-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);
        _jsonPath = WriteJson(
            """
            {
              "locomotives": [
                {
                  "Series": [
                    {
                      "Name": "E 10 / BR 110",
                      "Vmax": 160,
                      "Type": "Electric loco",
                      "Epoch": "IV",
                      "Description": "Universal electric locomotive"
                    },
                    {
                      "Name": "BR 103.1",
                      "Vmax": 200,
                      "Type": "Electric loco",
                      "Epoch": "IV",
                      "Description": "InterCity locomotive"
                    },
                    {
                      "Name": "BR 218",
                      "Vmax": 140,
                      "Type": "Diesel loco",
                      "Epoch": "IV",
                      "Description": "Diesel locomotive"
                    },
                    {
                      "Name": "ICE 3",
                      "Vmax": 330,
                      "Type": "Railcar",
                      "Epoch": "V-VI",
                      "Description": "High-speed train"
                    }
                  ]
                }
              ]
            }
            """);

        TrainClassLibrary.Initialize(_jsonPath);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDirectory))
            Directory.Delete(_tempDirectory, recursive: true);
    }

    [Test]
    public void TryGetByClassNumber_ExactNumericInput_ReturnsMatchingSeries()
    {
        var series = TrainClassLibrary.TryGetByClassNumber("110");

        Assert.Multiple(() =>
        {
            Assert.That(series, Is.Not.Null);
            Assert.That(series!.Name, Does.Contain("110"));
            Assert.That(series.Type, Is.EqualTo("Electric loco"));
        });
    }

    [TestCase("BR 110")]
    [TestCase("br110")]
    [TestCase("E 10")]
    public void TryGetByClassNumber_FlexiblePrefixes_ResolveToKnownSeries(string input)
    {
        var series = TrainClassLibrary.TryGetByClassNumber(input);

        Assert.That(series, Is.Not.Null);
        Assert.That(series!.Name, Does.Contain("110").Or.Contain("10"));
    }

    [Test]
    public void TryGetByClassNumber_SubSeriesInput_Matches103()
    {
        var series = TrainClassLibrary.TryGetByClassNumber("103.1");

        Assert.That(series?.Name, Is.EqualTo("BR 103.1"));
    }

    [Test]
    public void TryGetByClassNumber_ExactMatchTakesPriorityOverEarlierPrefixMatch()
    {
        // Arrange
        var orderedPath = WriteJson(
            """
            {
              "locomotives": [
                {
                  "Series": [
                    { "Name": "BR 110.1" },
                    { "Name": "E 10 / BR 110" }
                  ]
                }
              ]
            }
            """,
            "exact-priority.json");
        TrainClassLibrary.Initialize(orderedPath);

        // Act
        var series = TrainClassLibrary.TryGetByClassNumber("110");

        // Assert
        Assert.That(series?.Name, Is.EqualTo("E 10 / BR 110"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void TryGetByClassNumber_EmptyInput_ReturnsNull(string? input)
    {
        Assert.That(TrainClassLibrary.TryGetByClassNumber(input!), Is.Null);
    }

    [Test]
    public void GetAllClasses_ReturnsInitializedCatalog()
    {
        var all = TrainClassLibrary.GetAllClasses();

        Assert.That(all, Has.Count.EqualTo(4));
    }

    [Test]
    public void GetByType_FiltersByLocomotiveCategory()
    {
        var electric = TrainClassLibrary.GetByType("Electric loco");
        var diesel = TrainClassLibrary.GetByType("Diesel loco");

        Assert.Multiple(() =>
        {
            Assert.That(electric, Has.Count.EqualTo(2));
            Assert.That(diesel, Has.Count.EqualTo(1));
            Assert.That(diesel.First().Name, Is.EqualTo("BR 218"));
        });
    }

    [Test]
    public void Initialize_UppercaseRootProperty_ReplacesExistingCatalog()
    {
        // Arrange
        var replacementPath = WriteJson(
            """
            {
              "Locomotives": [
                {
                  "Series": [
                    {
                      "Name": "BR 151",
                      "Vmax": 120,
                      "Type": "Electric loco",
                      "Epoch": "IV-VI",
                      "Description": "Freight locomotive"
                    }
                  ]
                }
              ]
            }
            """,
            "replacement.json");

        // Act
        TrainClassLibrary.Initialize(replacementPath);

        // Assert
        Assert.That(TrainClassLibrary.GetAllClasses().Select(series => series.Name),
            Is.EqualTo(new[] { "BR 151" }));

        var series = TrainClassLibrary.GetAllClasses().Single();
        Assert.Multiple(() =>
        {
            Assert.That(series.Vmax, Is.EqualTo(120));
            Assert.That(series.Type, Is.EqualTo("Electric loco"));
            Assert.That(series.Epoch, Is.EqualTo("IV-VI"));
            Assert.That(series.Description, Is.EqualTo("Freight locomotive"));
        });
    }

    [Test]
    public void Initialize_MissingSeriesAndProperties_SkipsCategoryAndUsesDefaults()
    {
        // Arrange
        var sparsePath = WriteJson(
            """
            {
              "locomotives": [
                {},
                {
                  "Series": [
                    {
                      "Name": null,
                      "Type": null,
                      "Epoch": null,
                      "Description": null
                    },
                    {}
                  ]
                }
              ]
            }
            """,
            "sparse.json");

        // Act
        TrainClassLibrary.Initialize(sparsePath);
        var all = TrainClassLibrary.GetAllClasses();

        // Assert
        Assert.That(all, Has.Count.EqualTo(2));
        Assert.That(all, Has.All.Matches<LocomotiveSeries>(series =>
            series.Name == string.Empty &&
            series.Vmax == 0 &&
            series.Type == string.Empty &&
            series.Epoch == string.Empty &&
            series.Description == string.Empty));
    }

    [Test]
    public void Initialize_MissingLocomotivesProperty_PreservesExistingCatalog()
    {
        // Arrange
        var invalidRootPath = WriteJson("{ \"cities\": [] }", "missing-locomotives.json");

        // Act
        TrainClassLibrary.Initialize(invalidRootPath);

        // Assert
        Assert.That(TrainClassLibrary.GetAllClasses(), Has.Count.EqualTo(4));
    }

    [Test]
    public void Initialize_NonArrayLocomotivesProperty_PreservesExistingCatalog()
    {
        // Arrange
        var invalidRootPath = WriteJson("{ \"locomotives\": {} }", "invalid-locomotives.json");

        // Act
        TrainClassLibrary.Initialize(invalidRootPath);

        // Assert
        Assert.That(TrainClassLibrary.GetAllClasses(), Has.Count.EqualTo(4));
    }

    [Test]
    public void Initialize_MissingFile_WrapsFileNotFoundExceptionAndPreservesCatalog()
    {
        // Arrange
        var missingPath = Path.Combine(_tempDirectory, "missing.json");

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() =>
            TrainClassLibrary.Initialize(missingPath));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Does.Contain(missingPath));
            Assert.That(exception.InnerException, Is.TypeOf<FileNotFoundException>());
            Assert.That(TrainClassLibrary.GetAllClasses(), Has.Count.EqualTo(4));
        });
    }

    [Test]
    public void Initialize_InvalidJson_WrapsJsonExceptionAndPreservesCatalog()
    {
        // Arrange
        var invalidJsonPath = WriteJson("not-json", "invalid.json");

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() =>
            TrainClassLibrary.Initialize(invalidJsonPath));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Does.Contain(invalidJsonPath));
            Assert.That(exception.InnerException, Is.InstanceOf<System.Text.Json.JsonException>());
            Assert.That(TrainClassLibrary.GetAllClasses(), Has.Count.EqualTo(4));
        });
    }

    [Test]
    public void TryGetByClassNumber_PrefixInput_DoesNotMatchNumberContainingPrefixInMiddle()
    {
        // Arrange
        var orderedPath = WriteJson(
            """
            {
              "locomotives": [
                {
                  "Series": [
                    { "Name": "BR 211" },
                    { "Name": "BR 110" }
                  ]
                }
              ]
            }
            """,
            "prefix.json");
        TrainClassLibrary.Initialize(orderedPath);

        // Act
        var series = TrainClassLibrary.TryGetByClassNumber("BR 11");

        // Assert
        Assert.That(series?.Name, Is.EqualTo("BR 110"));
    }

    [Test]
    public void TryGetByClassNumber_PrefixMatchAcceptsOneOfSeveralDesignations()
    {
        // Arrange
        var prefixPath = WriteJson(
            """
            {
              "locomotives": [
                {
                  "Series": [
                    { "Name": "E 10 / BR 110" },
                    { "Name": "BR 120" }
                  ]
                }
              ]
            }
            """,
            "multiple-designations.json");
        TrainClassLibrary.Initialize(prefixPath);

        // Act
        var series = TrainClassLibrary.TryGetByClassNumber("11");

        // Assert
        Assert.That(series?.Name, Is.EqualTo("E 10 / BR 110"));
    }

    [Test]
    public void TryGetByClassNumber_UnknownNumericInput_ReturnsNull()
    {
        var series = TrainClassLibrary.TryGetByClassNumber("999");

        Assert.That(series, Is.Null);
    }

    [Test]
    public void TryGetByClassNumber_SeparatedNumericInput_StopsAfterFirstNumber()
    {
        // Arrange
        var numericPath = WriteJson(
            """
            {
              "locomotives": [
                {
                  "Series": [
                    { "Name": "BR 110" },
                    { "Name": "BR 11" }
                  ]
                }
              ]
            }
            """,
            "separated-number.json");
        TrainClassLibrary.Initialize(numericPath);

        // Act
        var series = TrainClassLibrary.TryGetByClassNumber("BR 11 X 0");

        // Assert
        Assert.That(series?.Name, Is.EqualTo("BR 11"));
    }

    [Test]
    public void TryGetByClassNumber_CombinedDesignationsAreNotTreatedAsOneNumber()
    {
        // Act
        var series = TrainClassLibrary.TryGetByClassNumber("10110");

        // Assert
        Assert.That(series, Is.Null);
    }

    [Test]
    public void TryGetByClassNumber_NumberAtEndOfName_IsMatchedExactly()
    {
        // Arrange
        var numericPath = WriteJson(
            """
            {
              "locomotives": [
                {
                  "Series": [
                    { "Name": "Freight locomotive BR 151" }
                  ]
                }
              ]
            }
            """,
            "number-at-end.json");
        TrainClassLibrary.Initialize(numericPath);

        // Act
        var series = TrainClassLibrary.TryGetByClassNumber("151");

        // Assert
        Assert.That(series?.Name, Is.EqualTo("Freight locomotive BR 151"));
    }

    [Test]
    public void TryGetByClassNumber_NumberBeforeSeparator_IsMatchedExactly()
    {
        // Arrange
        var numericPath = WriteJson(
            """
            {
              "locomotives": [
                {
                  "Series": [
                    { "Name": "E 10 / BR 999" }
                  ]
                }
              ]
            }
            """,
            "number-before-separator.json");
        TrainClassLibrary.Initialize(numericPath);

        // Act
        var series = TrainClassLibrary.TryGetByClassNumber("10");

        // Assert
        Assert.That(series?.Name, Is.EqualTo("E 10 / BR 999"));
    }

    [Test]
    public void TryGetByClassNumber_TextInput_PrefersExactSubstringDuringFuzzyMatching()
    {
        // Arrange
        var fuzzyPath = WriteJson(
            """
            {
              "locomotives": [
                {
                  "Series": [
                    { "Name": "InterCity Express" },
                    { "Name": "ICE 3" }
                  ]
                }
              ]
            }
            """,
            "fuzzy.json");
        TrainClassLibrary.Initialize(fuzzyPath);

        // Act
        var series = TrainClassLibrary.TryGetByClassNumber("i c e");

        // Assert
        Assert.That(series?.Name, Is.EqualTo("ICE 3"));
    }

    [Test]
    public void TryGetByClassNumber_FuzzyInputRequiresAtLeastHalfTheCharacters()
    {
        // Arrange
        var fuzzyPath = WriteJson(
            """
            {
              "locomotives": [
                {
                  "Series": [
                    { "Name": "Express" }
                  ]
                }
              ]
            }
            """,
            "fuzzy-threshold.json");
        TrainClassLibrary.Initialize(fuzzyPath);

        // Act
        var halfMatch = TrainClassLibrary.TryGetByClassNumber("XZ");
        var lessThanHalfMatch = TrainClassLibrary.TryGetByClassNumber("XYZ");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(halfMatch?.Name, Is.EqualTo("Express"));
            Assert.That(lessThanHalfMatch, Is.Null);
        });
    }

    [Test]
    public void TryGetByClassNumber_EquivalentFuzzyMatches_PrefersClosestLength()
    {
        // Arrange
        var fuzzyPath = WriteJson(
            """
            {
              "locomotives": [
                {
                  "Series": [
                    { "Name": "Long XZ designation" },
                    { "Name": "XZ" }
                  ]
                }
              ]
            }
            """,
            "fuzzy-score.json");
        TrainClassLibrary.Initialize(fuzzyPath);

        // Act
        var series = TrainClassLibrary.TryGetByClassNumber("XZ");

        // Assert
        Assert.That(series?.Name, Is.EqualTo("XZ"));
    }

    [Test]
    public void TryGetByClassNumber_FuzzyRankingRewardsEachMatchingCharacter()
    {
        // Arrange
        var fuzzyPath = WriteJson(
            """
            {
              "locomotives": [
                {
                  "Series": [
                    { "Name": "AB" },
                    { "Name": "A-B-C" }
                  ]
                }
              ]
            }
            """,
            "fuzzy-character-score.json");
        TrainClassLibrary.Initialize(fuzzyPath);

        // Act
        var series = TrainClassLibrary.TryGetByClassNumber("ABC");

        // Assert
        Assert.That(series?.Name, Is.EqualTo("A-B-C"));
    }

    [Test]
    public void TryGetByClassNumber_FuzzyRankingBalancesMatchesAgainstLengthDifference()
    {
        // Arrange
        var fuzzyPath = WriteJson(
            """
            {
              "locomotives": [
                {
                  "Series": [
                    { "Name": "ABC" },
                    { "Name": "A---B---C---D" }
                  ]
                }
              ]
            }
            """,
            "fuzzy-length-score.json");
        TrainClassLibrary.Initialize(fuzzyPath);

        // Act
        var series = TrainClassLibrary.TryGetByClassNumber("ABCDE");

        // Assert
        Assert.That(series?.Name, Is.EqualTo("A---B---C---D"));
    }

    [Test]
    public void GetAllClasses_ReturnsReadOnlyCollection()
    {
        // Arrange
        var all = TrainClassLibrary.GetAllClasses();
        var collection = (ICollection<LocomotiveSeries>)all;

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => collection.Add(new LocomotiveSeries()));
    }

    [Test]
    public void GetByType_MatchesCaseInsensitivelyAndReturnsEmptyForUnknownType()
    {
        // Act
        var electric = TrainClassLibrary.GetByType("electric LOCO");
        var unknown = TrainClassLibrary.GetByType("Steam loco");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(electric, Has.Count.EqualTo(2));
            Assert.That(unknown, Is.Empty);
        });
    }

    private string WriteJson(string json, string fileName = "data.json")
    {
        var path = Path.Combine(_tempDirectory, fileName);
        File.WriteAllText(path, json);
        return path;
    }
}
