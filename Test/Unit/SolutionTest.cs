// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Unit;

[TestFixture]
internal class SolutionTest
{
    private string _testFile = string.Empty;

    [SetUp]
    public void Setup()
    {
        _testFile = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestFile\solution.json");

        Assert.That(File.Exists(_testFile), Is.True,
            $"Test file solution.json not found: {_testFile}");
    }

    [Test]
    public async Task LoadExampleSolutionJson_ShouldLoadSuccessfully()
    {
        // Act
        var solution = new Solution();
        await solution.LoadAsync(_testFile);

        // Assert
        Assert.That(solution, Is.Not.Null, "Solution sollte nicht null sein");
        Assert.That(solution.Projects, Is.Not.Null, "Projects sollte nicht null sein");
        Assert.That(solution.Projects, Has.Count.EqualTo(1), "Es sollte genau ein Projekt vorhanden sein");

        var project = solution.Projects[0];

        Assert.That(project.Trains, Has.Count.EqualTo(1));
        Assert.That(project.Trains[0].Name, Is.EqualTo("RE 78"));

        Assert.That(project.Workflows, Has.Count.EqualTo(2));
        Assert.That(project.Workflows[0].Name, Is.EqualTo("Platform Announcement"));

        Assert.That(project.Journeys, Has.Count.EqualTo(1));
        Assert.That(project.Journeys[0].Name, Is.EqualTo("RE 78 (Porta-Express)"));
    }

    [Test]
    public void LoadAsync_WithNullPath_ThrowsArgumentException()
    {
        var solution = new Solution();
        var ex = Assert.ThrowsAsync<ArgumentException>(async () => await solution.LoadAsync(null!));
        Assert.That(ex!.ParamName, Is.EqualTo("filePath"));
    }

    [Test]
    public void LoadAsync_WithEmptyPath_ThrowsArgumentException()
    {
        var solution = new Solution();
        var ex = Assert.ThrowsAsync<ArgumentException>(async () => await solution.LoadAsync(""));
        Assert.That(ex!.ParamName, Is.EqualTo("filePath"));
    }

    [Test]
    public void LoadAsync_WithWhitespacePath_ThrowsArgumentException()
    {
        var solution = new Solution();
        Assert.ThrowsAsync<ArgumentException>(async () => await solution.LoadAsync("   "));
    }

    [Test]
    public void LoadAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        var solution = new Solution();
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "nonexistent.json");
        var ex = Assert.ThrowsAsync<FileNotFoundException>(async () => await solution.LoadAsync(path));
        Assert.That(ex!.FileName, Is.EqualTo(path));
    }

    [Test]
    public void UpdateFrom_WithNull_ThrowsArgumentNullException()
    {
        var solution = new Solution();
        Assert.Throws<ArgumentNullException>(() => solution.UpdateFrom(null!));
    }

    [Test]
    public void Constructor_InitializesWithDefaults()
    {
        var solution = new Solution();

        Assert.That(solution.Name, Is.EqualTo("New Solution"));
        Assert.That(solution.SchemaVersion, Is.EqualTo(Solution.CurrentSchemaVersion));
        Assert.That(solution.Projects, Is.Not.Null);
        Assert.That(solution.Projects, Is.Empty);
    }
}
