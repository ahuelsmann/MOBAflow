// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.MOBApi;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

using Moba.Contracts.Runtime;
using Moba.MOBApi.Controllers;
using Moba.MOBApi.Hubs;
using Moba.MOBApi.Service;

using Moq;

using System.Text.Json;

/// <summary>
/// Verifies the solution metadata that MOBAsmart polls before it downloads a solution: name, schema version, the
/// project to show first and the source of the cached solution.
/// </summary>
[TestFixture]
internal sealed class SolutionControllerMetaTests
{
    private SolutionCache _cache = null!;
    private SolutionController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _cache = new SolutionCache();
        var hubContext = new Mock<IHubContext<RuntimeHub, IRuntimeHubCallbacks>>();
        _controller = new SolutionController(_cache, new RuntimeSnapshotCache(), hubContext.Object);
    }

    [Test]
    public void GetMeta_WithoutSolution_ReportsThatNoneIsAvailable()
    {
        AssertNotFound(_controller.GetMeta());
    }

    [Test]
    public void GetSolution_WithoutSolution_ReportsThatNoneIsAvailable()
    {
        AssertNotFound(_controller.GetSolution());
    }

    [Test]
    public void GetMeta_ReportsTheSolutionIdentityAndItsSource()
    {
        _cache.Set(
            """{"name":"Layout","schemaVersion":4,"projects":[{"name":"Station"},{"name":"Yard"}]}""",
            sourcePath: "C:/layouts/layout.json");
        _cache.TryGet(out var entry);

        var meta = Meta(_controller.GetMeta());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(meta.GetProperty("name").GetString(), Is.EqualTo("Layout"));
            Assert.That(meta.GetProperty("schemaVersion").GetInt32(), Is.EqualTo(4));
            Assert.That(meta.GetProperty("firstProjectName").GetString(), Is.EqualTo("Station"));
            Assert.That(meta.GetProperty("sourcePath").GetString(), Is.EqualTo("C:/layouts/layout.json"));
            Assert.That(meta.GetProperty("activeProjectName").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(meta.GetProperty("updatedAt").GetDateTimeOffset(), Is.EqualTo(entry.UpdatedAt));
        }
    }

    [Test]
    public void GetMeta_FirstProjectName_SkipsProjectsWithoutAName()
    {
        _cache.Set("""{"projects":[{"id":"x"},{"name":7},{"name":"Yard"},{"name":"Depot"}]}""");

        var meta = Meta(_controller.GetMeta());

        Assert.That(meta.GetProperty("firstProjectName").GetString(), Is.EqualTo("Yard"));
    }

    [Test]
    public void GetMeta_ActiveProject_IsTheProjectToShowFirst()
    {
        _cache.Set("""{"projects":[{"name":"Station"},{"name":"Yard"}]}""", activeProjectName: "Yard");

        var meta = Meta(_controller.GetMeta());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(meta.GetProperty("activeProjectName").GetString(), Is.EqualTo("Yard"));
            Assert.That(meta.GetProperty("firstProjectName").GetString(), Is.EqualTo("Yard"));
        }
    }

    [Test]
    public void GetMeta_IgnoresValuesOfTheWrongJsonKind()
    {
        _cache.Set("""{"name":5,"schemaVersion":"4","projects":{"name":"Station"}}""");

        var meta = Meta(_controller.GetMeta());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(meta.GetProperty("name").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(meta.GetProperty("schemaVersion").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(meta.GetProperty("firstProjectName").ValueKind, Is.EqualTo(JsonValueKind.Null));
        }
    }

    [Test]
    public void GetMeta_WithASchemaVersionOutsideInt32_ReportsNoVersion()
    {
        _cache.Set("""{"name":"Layout","schemaVersion":4.5}""");

        var meta = Meta(_controller.GetMeta());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(meta.GetProperty("name").GetString(), Is.EqualTo("Layout"));
            Assert.That(meta.GetProperty("schemaVersion").ValueKind, Is.EqualTo(JsonValueKind.Null));
        }
    }

    private static JsonElement Meta(IActionResult result)
    {
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        return JsonSerializer.SerializeToElement(((OkObjectResult)result).Value);
    }

    private static void AssertNotFound(IActionResult result)
    {
        Assert.That(Error(result as NotFoundObjectResult), Is.EqualTo("No solution available yet."));
    }

    private static string? Error(ObjectResult? result) =>
        result is null ? null : JsonSerializer.SerializeToElement(result.Value).GetProperty("error").GetString();
}
