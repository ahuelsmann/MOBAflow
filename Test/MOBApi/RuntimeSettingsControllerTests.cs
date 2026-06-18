// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moba.MOBApi.Controllers;
using Moba.MOBApi.Models;
using Moba.MOBApi.Service;
using System.Net;

namespace Moba.Test.MOBApi;

[TestFixture]
internal sealed class RuntimeSettingsControllerTests
{
    private RuntimeSettingsCache _cache = null!;

    [SetUp]
    public void SetUp()
    {
        _cache = new RuntimeSettingsCache();
    }

    [Test]
    public void GetRuntimeSettings_ReturnsNotFound_WhenCacheEmpty()
    {
        var controller = CreateController(_cache, IPAddress.Loopback);

        var result = controller.GetRuntimeSettings();

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public void PutRuntimeSettings_ReturnsForbid_WhenRemoteIsNotLocalhost()
    {
        var controller = CreateController(_cache, IPAddress.Parse("192.168.1.10"));
        var request = new RuntimeSettingsRequest { Z21IpAddress = "192.168.0.111", Z21Port = 21105 };

        var result = controller.PutRuntimeSettings(request);

        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public void PutRuntimeSettings_StoresEndpoint_WhenCalledFromLocalhost()
    {
        var controller = CreateController(_cache, IPAddress.Loopback);
        var request = new RuntimeSettingsRequest { Z21IpAddress = "192.168.0.111", Z21Port = 21105 };

        var result = controller.PutRuntimeSettings(request);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        Assert.That(_cache.TryGetZ21Endpoint(out var ip, out var port), Is.True);
        Assert.That(ip, Is.EqualTo("192.168.0.111"));
        Assert.That(port, Is.EqualTo(21105));
    }

    [Test]
    public void GetRuntimeSettings_ReturnsStoredEndpoint_AfterPut()
    {
        _cache.SetZ21Endpoint("192.168.0.120", 21105);
        var controller = CreateController(_cache, IPAddress.Loopback);

        var result = controller.GetRuntimeSettings();

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var value = ((OkObjectResult)result).Value;
        Assert.That(value, Is.Not.Null);
    }

    private static RuntimeSettingsController CreateController(IRuntimeSettingsCache cache, IPAddress remoteIp)
    {
        return new RuntimeSettingsController(cache)
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
    }
}
