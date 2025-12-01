using Microsoft.Extensions.DependencyInjection;
using Moba.Domain;

namespace Moba.Test.Unit;

/// <summary>
/// Verifies that Solution instance management is correct across the application.
/// These tests ensure there is only ONE Solution instance shared by all ViewModels.
/// </summary>
[TestFixture]
[Category("Unit")]
public class SolutionInstanceTests
{
    [Test]
    public void SolutionSingleton_ShouldReturnSameInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<Solution>(sp => new Solution());
        
        var serviceProvider = services.BuildServiceProvider();
        
        // Act
        var instance1 = serviceProvider.GetRequiredService<Solution>();
        var instance2 = serviceProvider.GetRequiredService<Solution>();
        
        // Assert
        Assert.That(instance1, Is.SameAs(instance2), 
            "Solution should be registered as Singleton and return the same instance");
    }
    
    [Test]
    public void UpdateFrom_ShouldKeepSameReference()
    {
        // Arrange
        var originalSolution = new Solution();
        var originalReference = originalSolution;
        
        var loadedSolution = new Solution
        {
            Name = "Loaded Solution"
        };
        loadedSolution.Projects.Add(new Project { Name = "Test Project" });
        
        // Act
        // TODO: Use SolutionService.UpdateFrom(loadedSolution, originalSolution)  // Was: originalSolution.UpdateFrom(loadedSolution);
        
        // Assert
        Assert.That(originalSolution, Is.SameAs(originalReference), 
            "UpdateFrom should keep the same instance reference");
        Assert.That(originalSolution.Name, Is.EqualTo("Loaded Solution"), 
            "UpdateFrom should copy Name");
        Assert.That(originalSolution.Projects.Count, Is.EqualTo(1), 
            "UpdateFrom should copy Projects");
        Assert.That(originalSolution.Projects[0].Name, Is.EqualTo("Test Project"), 
            "UpdateFrom should copy Project data");
    }
    
    [Test]
    public void UpdateFrom_ShouldClearExistingProjects()
    {
        // Arrange
        var existingSolution = new Solution();
        existingSolution.Projects.Add(new Project { Name = "Old Project 1" });
        existingSolution.Projects.Add(new Project { Name = "Old Project 2" });
        
        var newSolution = new Solution();
        newSolution.Projects.Add(new Project { Name = "New Project" });
        
        // Act
        // TODO: Use SolutionService.UpdateFrom(newSolution, existingSolution)  // Was: existingSolution.UpdateFrom(newSolution);
        
        // Assert
        Assert.That(existingSolution.Projects.Count, Is.EqualTo(1), 
            "UpdateFrom should replace all projects");
        Assert.That(existingSolution.Projects[0].Name, Is.EqualTo("New Project"), 
            "UpdateFrom should have the new project");
    }
    
    [Test]
    public void UpdateFrom_ShouldCopySettings()
    {
        // Arrange
        var originalSolution = new Solution();
        var newSolution = new Solution
        {
            Settings = new Settings
            {
                CurrentIpAddress = "192.168.0.111",
                DefaultPort = "21105"
            }
        };
        
        // Act
        // TODO: Use SolutionService.UpdateFrom(newSolution, originalSolution)  // Was: originalSolution.UpdateFrom(newSolution);
        
        // Assert
        Assert.That(originalSolution.Settings, Is.Not.Null, 
            "UpdateFrom should copy Settings");
        Assert.That(originalSolution.Settings?.CurrentIpAddress, 
            Is.EqualTo("192.168.0.111"), 
            "UpdateFrom should copy Settings properties");
    }
    
    [Test]
    public void MultipleViewModels_ShouldShareSameSolutionInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<Solution>(sp => new Solution());
        
        var serviceProvider = services.BuildServiceProvider();
        
        // Act
        var solutionForViewModel1 = serviceProvider.GetRequiredService<Solution>();
        var solutionForViewModel2 = serviceProvider.GetRequiredService<Solution>();
        var solutionForViewModel3 = serviceProvider.GetRequiredService<Solution>();
        
        // Simulate UpdateFrom (as happens when loading a file)
        var loadedSolution = new Solution();
        loadedSolution.Projects.Add(new Project { Name = "Shared Project" });
        // TODO: Use SolutionService.UpdateFrom(loadedSolution, solutionForViewModel1)  // Was: solutionForViewModel1.UpdateFrom(loadedSolution);
        
        // Assert
        Assert.That(solutionForViewModel1, Is.SameAs(solutionForViewModel2), 
            "ViewModel 1 and 2 should share the same Solution");
        Assert.That(solutionForViewModel2, Is.SameAs(solutionForViewModel3), 
            "ViewModel 2 and 3 should share the same Solution");
        
        // All ViewModels should see the updated data
        Assert.That(solutionForViewModel1.Projects.Count, Is.EqualTo(1));
        Assert.That(solutionForViewModel2.Projects.Count, Is.EqualTo(1));
        Assert.That(solutionForViewModel3.Projects.Count, Is.EqualTo(1));
        
        Assert.That(solutionForViewModel1.Projects[0].Name, Is.EqualTo("Shared Project"));
        Assert.That(solutionForViewModel2.Projects[0].Name, Is.EqualTo("Shared Project"));
        Assert.That(solutionForViewModel3.Projects[0].Name, Is.EqualTo("Shared Project"));
    }
}
