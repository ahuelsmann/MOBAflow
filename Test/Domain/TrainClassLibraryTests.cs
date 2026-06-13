// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Domain;

using Moba.Domain;

/// <summary>
/// Tests for <see cref="TrainClassLibrary"/> lookup and filtering against master locomotive data.
/// Uses a dedicated temp JSON fixture so matching rules stay deterministic without touching UI code.
/// </summary>
[TestFixture]
internal sealed class TrainClassLibraryTests
{
    private string _jsonPath = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _jsonPath = Path.Combine(Path.GetTempPath(), $"moba-train-classes-{Guid.NewGuid():N}.json");
        File.WriteAllText(_jsonPath,
            """
            {
              "locomotives": [
                {
                  "Series": [
                    {
                      "Name": "E 10 / BR 110",
                      "Vmax": 160,
                      "Type": "Elektrolok",
                      "Epoch": "IV",
                      "Description": "Universal electric locomotive"
                    },
                    {
                      "Name": "BR 103.1",
                      "Vmax": 200,
                      "Type": "Elektrolok",
                      "Epoch": "IV",
                      "Description": "InterCity locomotive"
                    },
                    {
                      "Name": "BR 218",
                      "Vmax": 140,
                      "Type": "Diesellok",
                      "Epoch": "IV",
                      "Description": "Diesel locomotive"
                    }
                  ]
                }
              ]
            }
            """);

        TrainClassLibrary.Initialize(_jsonPath);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        if (File.Exists(_jsonPath))
            File.Delete(_jsonPath);
    }

    [Test]
    public void TryGetByClassNumber_ExactNumericInput_ReturnsMatchingSeries()
    {
        var series = TrainClassLibrary.TryGetByClassNumber("110");

        Assert.Multiple(() =>
        {
            Assert.That(series, Is.Not.Null);
            Assert.That(series!.Name, Does.Contain("110"));
            Assert.That(series.Type, Is.EqualTo("Elektrolok"));
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

        Assert.That(all, Has.Count.EqualTo(3));
    }

    [Test]
    public void GetByType_FiltersByLocomotiveCategory()
    {
        var electric = TrainClassLibrary.GetByType("Elektrolok");
        var diesel = TrainClassLibrary.GetByType("Diesellok");

        Assert.Multiple(() =>
        {
            Assert.That(electric, Has.Count.EqualTo(2));
            Assert.That(diesel, Has.Count.EqualTo(1));
            Assert.That(diesel.First().Name, Is.EqualTo("BR 218"));
        });
    }
}