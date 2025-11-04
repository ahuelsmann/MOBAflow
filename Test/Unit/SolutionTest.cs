namespace Moba.Test.Unit;

using Backend.Model;

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
        solution = await solution.LoadAsync(_testFile);

        // Assert
        Assert.That(solution, Is.Not.Null, "Solution sollte nicht null sein");
        Assert.That(solution!.Projects, Is.Not.Null, "Projects sollte nicht null sein");
        Assert.That(solution.Projects.Count, Is.EqualTo(1), "Es sollte genau ein Projekt vorhanden sein");

        var project = solution.Projects[0];

        Assert.That(project.Setting, Is.Not.Null, "Setting sollte nicht null sein");
        Assert.That(project.Setting.SpeechSynthesizerVolume, Is.EqualTo(90));

        Assert.That(project.SpeakerEngines.Count, Is.EqualTo(1));
        Assert.That(project.SpeakerEngines[0].Name, Is.EqualTo("Microsoft.CognitiveServices.Speech"));

        Assert.That(project.Voices.Count, Is.EqualTo(5));
        Assert.That(project.Voices[0].Name, Is.EqualTo("ElkeNeural"));

        Assert.That(project.Locomotives.Count, Is.EqualTo(2));
        Assert.That(project.Locomotives[0].Name, Is.EqualTo("BR 101 - DB Regio"));
        Assert.That(project.Locomotives[0].DigitalAddress, Is.EqualTo(3));

        Assert.That(project.PassengerWagons.Count, Is.EqualTo(3));
        Assert.That(project.PassengerWagons[0].Name, Is.EqualTo("Compartment Coach 2nd Class"));

        Assert.That(project.GoodsWagons.Count, Is.EqualTo(3));
        Assert.That(project.GoodsWagons[0].Name, Is.EqualTo("Container Wagon"));

        Assert.That(project.Trains.Count, Is.EqualTo(3));
        Assert.That(project.Trains[0].Name, Is.EqualTo("RE 6 Cologne - Düsseldorf"));

        Assert.That(project.Workflows.Count, Is.EqualTo(2));
        Assert.That(project.Workflows[0].Name, Is.EqualTo("Platform Announcement"));

        Assert.That(project.Journeys.Count, Is.EqualTo(1));
        Assert.That(project.Journeys[0].Name, Is.EqualTo("RE 78 (Porta-Express)"));

        Assert.That(project.IpAddresses.Count, Is.EqualTo(2));
        Assert.That(project.IpAddresses[0], Is.EqualTo("127.0.0.1"));
    }
}