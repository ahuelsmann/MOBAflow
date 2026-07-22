// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Backend;

using Moba.Backend.Service;
using Moba.Domain;

using System.Text.Json;

[TestFixture]
internal sealed class WorkflowSchemaCutoverValidationTests
{
    [Test]
    public async Task CurrentSolutionFixture_ContainsOnlyValidWorkflowGraphs()
    {
        // Arrange
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestFile", "solution.json");
        await using var stream = File.OpenRead(path);
        var solution = await JsonSerializer.DeserializeAsync<Solution>(stream, JsonOptions.Default);

        Assert.That(solution, Is.Not.Null);

        // Act
        var validationResults = solution.Projects
            .Select(project => new WorkflowValidator().Validate(project))
            .ToArray();

        // Assert
        Assert.That(validationResults, Is.All.Matches<WorkflowValidationResult>(result => result.IsValid));
    }
}
