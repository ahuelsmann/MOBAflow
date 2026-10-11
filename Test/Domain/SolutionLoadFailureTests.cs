// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Domain;

using System.Text.Json;

[TestFixture]
internal sealed class SolutionLoadFailureTests
{
    [TestCase("null", typeof(InvalidOperationException))]
    [TestCase("{", typeof(JsonException))]
    [TestCase("{\"schemaVersion\":999,\"name\":\"Rejected\",\"projects\":[]}", typeof(InvalidOperationException))]
    public async Task LoadAsync_InvalidDocument_DoesNotReplaceExistingSolution(string json, Type exceptionType)
    {
        var project = new Project { Name = "Existing project" };
        var solution = new Solution { Name = "Existing solution", Projects = [project] };
        var projects = solution.Projects;
        var directory = Directory.CreateTempSubdirectory("mobaflow-solution-tests-");
        var path = Path.Combine(directory.FullName, "solution.json");

        try
        {
            await File.WriteAllTextAsync(path, json).ConfigureAwait(false);

            Assert.ThrowsAsync(exceptionType, async () => await solution.LoadAsync(path).ConfigureAwait(false));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(solution.Name, Is.EqualTo("Existing solution"));
                Assert.That(solution.Projects, Is.SameAs(projects));
                Assert.That(solution.Projects.Single(), Is.SameAs(project));
                Assert.That(solution.SchemaVersion, Is.EqualTo(Solution.CurrentSchemaVersion));
            }
        }
        finally
        {
            directory.Delete(true);
        }
    }
}
