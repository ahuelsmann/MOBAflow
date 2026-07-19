// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Moba.MOBApi.Controllers;
using Moba.MOBApi.Hubs;
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
          "schemaVersion": 3,
          "projects": [
            {
              "name": "Project A"
            }
          ]
        }
        """;

    private SolutionCache _cache = null!;

    [SetUp]
    public void SetUp()
    {
        _cache = new SolutionCache();
    }

    [Test]
    public void GetSolution_ReturnsNotFound_WhenCacheEmpty()
    {
        var controller = CreateController(_cache, IPAddress.Loopback);

        var result = controller.GetSolution();

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task PutSolution_ReturnsForbid_WhenRemoteIsNotLocalhost()
    {
        var controller = CreateController(_cache, IPAddress.Parse("192.168.1.10"));
        SetRequestBody(controller, ValidSolutionJson);

        var result = await controller.PutSolution(CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ForbidResult>());
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

    private static SolutionController CreateController(ISolutionCache cache, IPAddress remoteIp)
    {
        var clientProxy = new Mock<IClientProxy>();
        clientProxy
            .Setup(proxy => proxy.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var hubClients = new Mock<IHubClients>();
        hubClients.Setup(clients => clients.Group("runtime-remote")).Returns(clientProxy.Object);

        var hubContext = new Mock<IHubContext<RuntimeHub>>();
        hubContext.Setup(context => context.Clients).Returns(hubClients.Object);

        var controller = new SolutionController(cache, hubContext.Object)
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
