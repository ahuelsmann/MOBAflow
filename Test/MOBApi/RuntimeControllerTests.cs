// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.MOBApi;

using Microsoft.AspNetCore.Mvc;

using Moba.Contracts.Runtime;
using Moba.MOBApi.Controllers;
using Moba.MOBApi.Service;

using System.Text.Json;

/// <summary>
/// Verifies the REST fallback for runtime snapshots: each project reads only its own snapshot, and a snapshot without
/// a body, valid JSON or a project is rejected before it reaches the cache.
/// </summary>
[TestFixture]
internal sealed class RuntimeControllerTests
{
    private static readonly Guid ProjectA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ProjectB = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private RuntimeSnapshotCache _cache = null!;
    private RuntimeController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _cache = new RuntimeSnapshotCache();
        _controller = new RuntimeController(_cache);
    }

    [Test]
    public void GetMetaAndSnapshot_ForAProjectWithoutSnapshot_AreNotFound()
    {
        _cache.Set(Snapshot(ProjectA, isConnected: true));

        using (Assert.EnterMultipleScope())
        {
            AssertError<NotFoundObjectResult>(_controller.GetMeta(ProjectB), "No runtime snapshot available yet.");
            AssertError<NotFoundObjectResult>(_controller.GetSnapshot(ProjectB), "No runtime snapshot available yet.");
        }
    }

    [Test]
    public void PutSnapshot_StoresItForItsProjectOnly()
    {
        var result = _controller.PutSnapshot(Body(Snapshot(ProjectA, isConnected: true)));

        var stored = Value(result);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(stored.GetProperty("projectId").GetGuid(), Is.EqualTo(ProjectA));
            Assert.That(stored.GetProperty("isConnected").GetBoolean(), Is.True);
            Assert.That(_cache.TryGet(ProjectA, out _), Is.True);
            Assert.That(_cache.TryGet(ProjectB, out _), Is.False);
        }
    }

    [Test]
    public void GetMeta_ReportsTheStoredConnectionStateAndTime()
    {
        var entry = _cache.Set(Snapshot(ProjectA, isConnected: false));

        var meta = Value(_controller.GetMeta(ProjectA));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(meta.GetProperty("isConnected").GetBoolean(), Is.False);
            Assert.That(meta.GetProperty("updatedAt").GetDateTimeOffset(), Is.EqualTo(entry.UpdatedAt));
        }
    }

    [Test]
    public void GetSnapshot_ReturnsTheStoredJsonOfThatProject()
    {
        _cache.Set(Snapshot(ProjectA, isConnected: true));
        var entryB = _cache.Set(Snapshot(ProjectB, isConnected: false));

        var result = _controller.GetSnapshot(ProjectB);

        Assert.That(result, Is.InstanceOf<ContentResult>());
        var content = (ContentResult)result;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(content.Content, Is.EqualTo(entryB.Json));
            Assert.That(content.ContentType, Is.EqualTo("application/json"));
        }
    }

    [Test]
    public void PutSnapshot_WithoutBody_IsRejected()
    {
        AssertError<BadRequestObjectResult>(_controller.PutSnapshot(null), "Snapshot body is required.");
        Assert.That(_cache.GetAll(), Is.Empty);
    }

    [Test]
    public void PutSnapshot_WithJsonNull_IsRejected()
    {
        AssertError<BadRequestObjectResult>(_controller.PutSnapshot(Body("null")), "Invalid runtime snapshot JSON.");
        Assert.That(_cache.GetAll(), Is.Empty);
    }

    [Test]
    public void PutSnapshot_WithoutProject_IsRejected()
    {
        AssertError<BadRequestObjectResult>(
            _controller.PutSnapshot(Body(Snapshot(Guid.Empty, isConnected: true))),
            "The runtime snapshot names no project.");
        Assert.That(_cache.GetAll(), Is.Empty);
    }

    private static string Snapshot(Guid projectId, bool isConnected) =>
        RuntimeJsonSerializer.Serialize(new MobaRuntimeSnapshot { ProjectId = projectId, IsConnected = isConnected });

    private static JsonElement Body(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static JsonElement Value(IActionResult result)
    {
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        return JsonSerializer.SerializeToElement(((OkObjectResult)result).Value);
    }

    private static void AssertError<TResult>(IActionResult result, string error)
        where TResult : ObjectResult
    {
        Assert.That(Error(result as TResult), Is.EqualTo(error));
    }

    private static string? Error(ObjectResult? result) =>
        result is null ? null : JsonSerializer.SerializeToElement(result.Value).GetProperty("error").GetString();
}
