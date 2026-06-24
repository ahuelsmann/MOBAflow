// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using System.Net;

using Microsoft.AspNetCore.Http;
using Moba.Common.Security;
using Moba.MOBApi.Auth;

namespace Moba.Test.MOBApi;

[TestFixture]
internal sealed class MobaApiKeyAuthMiddlewareTests
{
    [Test]
    public async Task InvokeAsync_AllowsPublicHealthEndpoint_WithoutApiKey()
    {
        var context = CreateContext("/api/photos/health", IPAddress.Parse("192.168.0.10"));
        var invoked = false;

        var middleware = new MobaApiKeyAuthMiddleware(_ =>
        {
            invoked = true;
            return Task.CompletedTask;
        });

        Environment.SetEnvironmentVariable(MobaApiAuth.ApiKeyEnvironmentVariable, "secret");
        try
        {
            await middleware.InvokeAsync(context);
        }
        finally
        {
            Environment.SetEnvironmentVariable(MobaApiAuth.ApiKeyEnvironmentVariable, null);
        }

        Assert.That(invoked, Is.True);
        Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
    }

    [Test]
    public async Task InvokeAsync_RejectsRemoteRequest_WithoutApiKey()
    {
        var context = CreateContext("/api/solution", IPAddress.Parse("192.168.0.10"));
        var invoked = false;

        var middleware = new MobaApiKeyAuthMiddleware(_ =>
        {
            invoked = true;
            return Task.CompletedTask;
        });

        Environment.SetEnvironmentVariable(MobaApiAuth.ApiKeyEnvironmentVariable, "secret");
        try
        {
            await middleware.InvokeAsync(context);
        }
        finally
        {
            Environment.SetEnvironmentVariable(MobaApiAuth.ApiKeyEnvironmentVariable, null);
        }

        Assert.That(invoked, Is.False);
        Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
    }

    [Test]
    public async Task InvokeAsync_AllowsRemoteRequest_WithValidApiKey()
    {
        var context = CreateContext("/api/solution", IPAddress.Parse("192.168.0.10"));
        context.Request.Headers[MobaApiAuth.ApiKeyHeaderName] = "secret";
        var invoked = false;

        var middleware = new MobaApiKeyAuthMiddleware(_ =>
        {
            invoked = true;
            return Task.CompletedTask;
        });

        Environment.SetEnvironmentVariable(MobaApiAuth.ApiKeyEnvironmentVariable, "secret");
        try
        {
            await middleware.InvokeAsync(context);
        }
        finally
        {
            Environment.SetEnvironmentVariable(MobaApiAuth.ApiKeyEnvironmentVariable, null);
        }

        Assert.That(invoked, Is.True);
        Assert.That(context.Items[MobaApiAuth.AuthenticatedItemKey], Is.True);
    }

    private static DefaultHttpContext CreateContext(string path, IPAddress remoteIp)
    {
        var context = new DefaultHttpContext
        {
            Request =
            {
                Path = path
            },
            Connection =
            {
                RemoteIpAddress = remoteIp,
                LocalIpAddress = IPAddress.Parse("192.168.0.5")
            },
            Response =
            {
                Body = new MemoryStream()
            }
        };

        return context;
    }
}
