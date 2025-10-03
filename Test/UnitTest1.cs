namespace Test;

using Moba.Backend.Model;

public class Tests
{
    private string _exampleJsonFilePath = string.Empty;

    [SetUp]
    public void Setup()
    {
        _exampleJsonFilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "example-solution.json");
        
        Assert.That(File.Exists(_exampleJsonFilePath), Is.True, 
            $"Die example-solution.json Datei wurde nicht gefunden unter: {_exampleJsonFilePath}");
    }

    [Test]
    public async Task LoadExampleSolutionJson_ShouldLoadSuccessfully()
    {
        // Act
        var solution = await Solution.LoadAsync(_exampleJsonFilePath);

        // Assert
        Assert.That(solution, Is.Not.Null, "Solution sollte nicht null sein");
        Assert.That(solution!.Projects, Is.Not.Null, "Projects sollte nicht null sein");
        Assert.That(solution.Projects.Count, Is.EqualTo(1), "Es sollte genau ein Projekt vorhanden sein");

        var project = solution.Projects[0];
        
        Assert.That(project.Setting, Is.Not.Null, "Setting sollte nicht null sein");
        Assert.That(project.Setting.Name, Is.EqualTo("My Model Railway Project"));
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

        Assert.That(project.Journeys.Count, Is.EqualTo(2));
        Assert.That(project.Journeys[0].Name, Is.EqualTo("Journey 1 - Regional Express"));

        Assert.That(project.Ips.Count, Is.EqualTo(2));
        Assert.That(project.Ips[0].Address, Is.EqualTo("192.168.0.1"));
    }
}
