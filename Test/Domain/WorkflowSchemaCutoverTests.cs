// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Domain;

using Moba.Backend.Service;
using Moba.Domain;

using System.Text.Json;

[TestFixture]
internal sealed class WorkflowSchemaCutoverTests
{
    [Test]
    public async Task CurrentSolutionFixture_UsesValidWorkflowGraphsWithoutLegacyActionLists()
    {
        // Arrange
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestFile", "solution.json");
        await using var stream = File.OpenRead(path);

        // Act
        var solution = await JsonSerializer.DeserializeAsync<Solution>(stream, JsonOptions.Default);

        // Assert
        Assert.That(solution, Is.Not.Null);
        var workflows = solution!.Projects.SelectMany(project => project.Workflows).ToArray();
        Assert.That(workflows, Is.Not.Empty);
        Assert.Multiple(() =>
        {
            Assert.That(workflows.All(workflow => workflow.EntryStepId.HasValue), Is.True);
            Assert.That(workflows.All(workflow => workflow.Steps is { Count: > 0 }), Is.True);
            Assert.That(workflows.All(workflow => workflow.Actions.Count == 0), Is.True);
            Assert.That(workflows.SelectMany(workflow => workflow.Steps!).Any(step => step is WorkflowActionStep), Is.True);
            Assert.That(workflows.SelectMany(workflow => workflow.Steps!).Any(step => step is WorkflowTerminateStep), Is.True);
        });

        foreach (var project in solution.Projects)
            Assert.That(new WorkflowValidator().Validate(project).IsValid, Is.True);
    }
}
