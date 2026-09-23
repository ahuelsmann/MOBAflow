// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Domain;

using Moba.Domain;

using System.Text.Json;

[TestFixture]
internal sealed class WorkflowSchemaCutoverTests
{
    [Test]
    public async Task CurrentSolutionFixture_ContainsPersistedActionSequences()
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
        Assert.That(workflows.All(workflow => workflow.Actions is { Count: > 0 }), Is.True);
    }
}
