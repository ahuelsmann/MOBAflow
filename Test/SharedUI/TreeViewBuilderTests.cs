using Moba.SharedUI.Service;
using Moba.Backend.Model;

namespace Moba.Test.SharedUI;

public class TreeViewBuilderTests
{
    [Test]
    public void BuildTreeView_WithSolution_CreatesSolutionNode()
    {
        var solution = new Solution();
        var project = new Project { Name = "P1" };
        solution.Projects.Add(project);

        var tree = TreeViewBuilder.BuildTreeView(solution);

        Assert.That(tree, Is.Not.Null);
        Assert.That(tree.Count, Is.EqualTo(1));
        var solutionNode = tree[0];
        Assert.That(solutionNode.DisplayName, Is.EqualTo("Solution"));
        Assert.That(solutionNode.Children.Count, Is.GreaterThan(0));
    }
}
