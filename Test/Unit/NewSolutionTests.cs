namespace Moba.Test.Unit;

/// <summary>
/// Tests for the "New Solution" functionality.
/// Verifies that creating a new solution properly updates the singleton instance.
/// </summary>
[TestFixture]
[Category("Unit")]
public class NewSolutionTests
{
    [Test]
    public void NewSolution_ShouldCreateEmptyProject()
    {
        // Arrange
        var newSolution = new Solution
        {
            Name = "New Solution"
        };
        
        newSolution.Projects.Add(new Project
        {
            Name = "New Project",
            Journeys = new List<Journey>(),
            Workflows = new List<Workflow>(),
            Trains = new List<Train>()
        });
        
        // Assert
        Assert.That(newSolution.Name, Is.EqualTo("New Solution"));
        Assert.That(newSolution.Projects.Count, Is.EqualTo(1));
        Assert.That(newSolution.Projects[0].Name, Is.EqualTo("New Project"));
        Assert.That(newSolution.Projects[0].Journeys, Is.Empty);
        Assert.That(newSolution.Projects[0].Workflows, Is.Empty);
        Assert.That(newSolution.Projects[0].Trains, Is.Empty);
    }
    
    [Test]
    public void NewSolution_ShouldReplaceExistingData()
    {
        // Arrange
        var existingSolution = new Solution { Name = "Old Solution" };
        existingSolution.Projects.Add(new Project { Name = "Old Project 1" });
        existingSolution.Projects.Add(new Project { Name = "Old Project 2" });
        
        var newSolution = new Solution { Name = "New Solution" };
        newSolution.Projects.Add(new Project
        {
            Name = "New Project",
            Journeys = new List<Journey>(),
            Workflows = new List<Workflow>(),
            Trains = new List<Train>()
        });
        
        // Act
        existingSolution.UpdateFrom(newSolution);
        
        // Assert
        Assert.That(existingSolution.Name, Is.EqualTo("New Solution"));
        Assert.That(existingSolution.Projects.Count, Is.EqualTo(1));
        Assert.That(existingSolution.Projects[0].Name, Is.EqualTo("New Project"));
    }
    
    [Test]
    public void NewSolution_WithSingleton_ShouldKeepReference()
    {
        // Arrange
        var singletonSolution = new Solution { Name = "Singleton" };
        singletonSolution.Projects.Add(new Project { Name = "Existing Project" });
        
        var originalReference = singletonSolution;
        
        var newSolution = new Solution { Name = "New Solution" };
        newSolution.Projects.Add(new Project { Name = "New Project" });
        
        // Act
        singletonSolution.UpdateFrom(newSolution);
        
        // Assert
        Assert.That(singletonSolution, Is.SameAs(originalReference), 
            "UpdateFrom should keep the same instance reference");
        Assert.That(singletonSolution.Name, Is.EqualTo("New Solution"), 
            "Name should be updated");
        Assert.That(singletonSolution.Projects.Count, Is.EqualTo(1), 
            "Should have exactly one project");
        Assert.That(singletonSolution.Projects[0].Name, Is.EqualTo("New Project"), 
            "Project should be the new one");
    }
    
    [Test]
    public void NewSolution_ShouldClearOldProjects()
    {
        // Arrange
        var solution = new Solution();
        solution.Projects.Add(new Project { Name = "Project 1" });
        solution.Projects.Add(new Project { Name = "Project 2" });
        solution.Projects.Add(new Project { Name = "Project 3" });
        
        var newSolution = new Solution();
        newSolution.Projects.Add(new Project { Name = "New Project" });
        
        // Act
        solution.UpdateFrom(newSolution);
        
        // Assert
        Assert.That(solution.Projects.Count, Is.EqualTo(1), 
            "All old projects should be removed");
        Assert.That(solution.Projects[0].Name, Is.EqualTo("New Project"), 
            "Should have only the new project");
    }
    
    [Test]
    public void NewSolution_WithDefaultProject_ShouldHaveEmptyCollections()
    {
        // Arrange & Act
        var solution = new Solution { Name = "Test" };
        solution.Projects.Add(new Project
        {
            Name = "Default Project",
            Journeys = new List<Journey>(),
            Workflows = new List<Workflow>(),
            Trains = new List<Train>()
        });
        
        // Assert
        var project = solution.Projects[0];
        Assert.That(project.Journeys, Is.Not.Null);
        Assert.That(project.Journeys, Is.Empty);
        Assert.That(project.Workflows, Is.Not.Null);
        Assert.That(project.Workflows, Is.Empty);
        Assert.That(project.Trains, Is.Not.Null);
        Assert.That(project.Trains, Is.Empty);
    }
}
