// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Unit;

using Domain;

public class SolutionTest
{
    private string _testFile = string.Empty;

    [SetUp]
    public void Setup()
    {
        _testFile = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestFile\example-solution.json");

        Assert.That(File.Exists(_testFile), Is.True,
            $"Test file example-solution.json not found: {_testFile}");
    }

    [Test]
    public async Task LoadExampleSolutionJson_ShouldLoadSuccessfully()
    {
        // Act
        var solution = new Solution();
        await solution.LoadAsync(_testFile);

        // Assert
        Assert.That(solution, Is.Not.Null, "Solution sollte nicht null sein");
        Assert.That(solution!.Projects, Is.Not.Null, "Projects sollte nicht null sein");
        Assert.That(solution.Projects.Count, Is.EqualTo(1), "Es sollte genau ein Projekt vorhanden sein");

        var project = solution.Projects[0];

        // Settings are now in AppSettings (not in Solution)
        // This test is obsolete - Settings moved to Common.Configuration.AppSettings
        
        Assert.That(project.SpeakerEngines.Count, Is.EqualTo(1));
        Assert.That(project.SpeakerEngines[0].Name, Is.EqualTo("Microsoft.CognitiveServices.Speech"));

        Assert.That(project.Voices.Count, Is.EqualTo(5));
        Assert.That(project.Voices[0].Name, Is.EqualTo("ElkeNeural"));

        Assert.That(project.Trains.Count, Is.EqualTo(1));
        Assert.That(project.Trains[0].Name, Is.EqualTo("RE 78"));

        Assert.That(project.Workflows.Count, Is.EqualTo(2));
        Assert.That(project.Workflows[0].Name, Is.EqualTo("Platform Announcement"));

        Assert.That(project.Journeys.Count, Is.EqualTo(1));
        Assert.That(project.Journeys[0].Name, Is.EqualTo("RE 78 (Porta-Express)"));
    }
}