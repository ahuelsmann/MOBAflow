// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.SignalR;
using Moba.Contracts.Runtime;
using Moba.MOBApi.Controllers;
using Moba.MOBApi.Hubs;
using Moba.MOBApi.ModelBinding;
using Moba.MOBApi.Service;
using Moq;
using System.Net;
using System.Text;

namespace Moba.Test.MOBApi;

[TestFixture]
internal sealed class SolutionControllerTests
{
    private const string ValidSolutionJson =
        """
        {
          "name": "Test Solution",
          "schemaVersion": 4,
          "projects": [
            {
              "name": "Project A"
            }
          ]
        }
        """;

    private SolutionCache _cache = null!;
    private RuntimeSnapshotCache _snapshots = null!;

    [SetUp]
    public void SetUp()
    {
        _cache = new SolutionCache();
        _snapshots = new RuntimeSnapshotCache();
    }

    [TestCase("")]
    [TestCase(" \t ")]
    [TestCase("first, second")]
    public async Task HeaderBinding_PreservesTheFrameworksFirstValue(string firstValue)
    {
        var context = new DefaultModelBindingContext
        {
            ActionContext = new ActionContext { HttpContext = new DefaultHttpContext() },
            ModelState = new ModelStateDictionary(),
            ModelName = "X-MOBAflow-Solution-Path"
        };
        context.HttpContext.Request.Headers[context.ModelName] = new[] { firstValue, "later" };

        await new FirstHeaderValueModelBinder().BindModelAsync(context).ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Result.IsModelSet, Is.True);
            Assert.That(context.Result.Model, Is.EqualTo(firstValue));
            Assert.That(context.ModelState.IsValid, Is.True);
        }
    }

    [Test]
    public async Task PutSolution_InvalidJsonDoesNotMutateCachesOrBroadcast()
    {
        var existingProject = Guid.NewGuid();
        _cache.Set(ValidSolutionJson, "original path", "original project");
        _cache.TryGet(out var originalEntry);
        _snapshots.Set(RuntimeJsonSerializer.Serialize(new MobaRuntimeSnapshot { ProjectId = existingProject }));
        var remotes = new Mock<IRuntimeHubCallbacks>();
        var controller = CreateController(_cache, IPAddress.Loopback, _snapshots, remotes);
        SetRequestBody(controller, "not json");

        var result = await controller.PutSolution(CancellationToken.None, "rejected path", "rejected project").ConfigureAwait(false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            Assert.That(_cache.TryGet(out var entry), Is.True);
            Assert.That(entry, Is.SameAs(originalEntry));
            Assert.That(_snapshots.GetAll().Select(snapshot => snapshot.ProjectId), Is.EqualTo(new[] { existingProject }));
        }
        remotes.VerifyNoOtherCalls();
    }

    [Test]
    public async Task PutSolution_ForgetsSnapshotsOfProjectsThatLeftTheSolution()
    {
        var kept = Guid.NewGuid();
        var removed = Guid.NewGuid();
        _snapshots.Set(RuntimeJsonSerializer.Serialize(new MobaRuntimeSnapshot { ProjectId = kept, IsConnected = true }));
        _snapshots.Set(RuntimeJsonSerializer.Serialize(new MobaRuntimeSnapshot { ProjectId = removed, IsConnected = true }));
        var controller = CreateController(_cache, IPAddress.Loopback, _snapshots);
        SetRequestBody(controller, $$"""{"name":"Layout","schemaVersion":4,"projects":[{"id":"{{kept}}","name":"Station"}]}""");

        await controller.PutSolution(CancellationToken.None).ConfigureAwait(false);

        Assert.That(_snapshots.GetAll().Select(entry => entry.ProjectId), Is.EquivalentTo(new[] { kept }));
    }

    [Test]
    public async Task PutSolution_NotifiesAllRemotesOfTheNewSolutionTimestamp()
    {
        var remotes = new Mock<IRuntimeHubCallbacks>();
        var controller = CreateController(_cache, IPAddress.Loopback, remotes: remotes);
        SetRequestBody(controller, ValidSolutionJson);
        using var request = new CancellationTokenSource();

        await controller.PutSolution(request.Token).ConfigureAwait(false);

        Assert.That(_cache.TryGet(out var entry), Is.True);
        // The broadcast is cancelled together with the request.
        remotes.Verify(remote => remote.SolutionUpdated(entry.UpdatedAt.ToString("O"), request.Token), Times.Once);
    }

    [Test]
    public void GetSolution_ReturnsNotFound_WhenCacheEmpty()
    {
        var controller = CreateController(_cache, IPAddress.Loopback);

        var result = controller.GetSolution();

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task PutSolution_StoresSolution_WhenCalledFromAnotherDevice()
    {
        var controller = CreateController(_cache, IPAddress.Parse("192.168.1.10"));
        SetRequestBody(controller, ValidSolutionJson);

        var result = await controller.PutSolution(CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            Assert.That(_cache.TryGet(out var entry), Is.True);
            Assert.That(entry.Json, Is.EqualTo(ValidSolutionJson));
        }
    }

    [Test]
    public async Task PutSolution_StoresSolution_WhenCalledFromLocalhost()
    {
        var controller = CreateController(_cache, IPAddress.Loopback);
        SetRequestBody(controller, ValidSolutionJson);

        var result = await controller.PutSolution(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        Assert.That(_cache.TryGet(out var entry), Is.True);
        Assert.That(entry.Json, Is.EqualTo(ValidSolutionJson));
    }

    [Test]
    public async Task GetSolution_ReturnsCachedJson_AfterPut()
    {
        var controller = CreateController(_cache, IPAddress.Loopback);
        SetRequestBody(controller, ValidSolutionJson);
        await controller.PutSolution(CancellationToken.None);

        var result = controller.GetSolution();

        Assert.That(result, Is.InstanceOf<ContentResult>());
        var content = (ContentResult)result;
        Assert.That(content.Content, Is.EqualTo(ValidSolutionJson));
        Assert.That(content.ContentType, Is.EqualTo("application/json"));
    }

    [Test]
    public void GetMeta_ReturnsUpdatedAt_AfterPut()
    {
        _cache.Set(ValidSolutionJson, "C:\\temp\\solution.json");
        var controller = CreateController(_cache, IPAddress.Loopback);

        var result = controller.GetMeta();

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var value = ((OkObjectResult)result).Value;
        Assert.That(value, Is.Not.Null);
    }

    private static SolutionController CreateController(
        ISolutionCache cache,
        IPAddress remoteIp,
        IRuntimeSnapshotCache? snapshots = null,
        Mock<IRuntimeHubCallbacks>? remotes = null)
    {
        // Only the group of all remotes is set up; a broadcast to any other group would fail.
        var hubClients = new Mock<IHubClients<IRuntimeHubCallbacks>>();
        hubClients
            .Setup(clients => clients.Group(RuntimeHub.RuntimeRemoteGroup))
            .Returns((remotes ?? new Mock<IRuntimeHubCallbacks>()).Object);

        var hubContext = new Mock<IHubContext<RuntimeHub, IRuntimeHubCallbacks>>();
        hubContext.Setup(context => context.Clients).Returns(hubClients.Object);

        var controller = new SolutionController(cache, snapshots ?? new RuntimeSnapshotCache(), hubContext.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    Connection =
                    {
                        RemoteIpAddress = remoteIp,
                        LocalIpAddress = IPAddress.Loopback
                    }
                }
            }
        };

        return controller;
    }

    private static void SetRequestBody(ControllerBase controller, string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        controller.ControllerContext.HttpContext.Request.Body = new MemoryStream(bytes);
        controller.ControllerContext.HttpContext.Request.ContentLength = bytes.Length;
    }
}
