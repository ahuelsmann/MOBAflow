using Moba.Backend.Model;
using Moba.Test.TestBase;

namespace Moba.Test.SharedUI;

public class TreeViewBuilderTests : ViewModelTestBase
{
    [Test]
    public void BuildTreeView_WithSolution_CreatesSolutionNode()
    {
        // Arrange
        var solution = new Solution();
        var project = new Project { Name = "P1" };
        solution.Projects.Add(project);

        // Act - TreeViewBuilder from base class
        var tree = TreeViewBuilder.BuildTreeView(solution);

        // Assert
        Assert.That(tree, Is.Not.Null);
        Assert.That(tree.Count, Is.EqualTo(1));
        var solutionNode = tree[0];
        Assert.That(solutionNode.DisplayName, Is.EqualTo("Solution"));
        Assert.That(solutionNode.Children.Count, Is.GreaterThan(0));
    }
}
