// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;

namespace Moba.Demo;

/// <summary>Per-client request budgets of the demo host (FR-056).</summary>
/// <param name="NonStaticPerMinute">Requests per minute that are not static files.</param>
/// <param name="StaticPerHour">
/// Static-file requests per hour: ten first-visit equivalents (FR-056), counted as requests. Measured against the
/// Train Control landing page build output (~38-40 static requests per cold first visit within FR-057), so 400 covers
/// ten first-visit equivalents per hour.
/// </param>
internal sealed record HostLimits(int NonStaticPerMinute = 60, int StaticPerHour = 400);

/// <summary>
/// Client address handling and request limits of the demo host (FR-056, FR-078): the address comes from the
/// forwarded headers of the App Service front end and keys one global limiter with a budget for static files and
/// one for everything else.
/// </summary>
internal static class ClientLimits
{
    private const string EntryDocument = "index.html";
    private const string UnknownAddress = "unknown";
    private const int StaticSegments = 6;
    private const int StaticFallbackRetryAfterSeconds = 3600;
    private const int NonStaticFallbackRetryAfterSeconds = 60;

    private static readonly HostHardening.OriginalTypeProvider ContentTypes = new();

    /// <summary>
    /// Registers the forwarded-headers options and the limiter. Forwarding runs once, from the explicit
    /// <c>UseForwardedHeaders</c> call: the platform's own startup filter (<c>ASPNETCORE_FORWARDEDHEADERS_ENABLED</c>)
    /// is removed, because a second pass with <c>ForwardLimit = 1</c> would consume the next entry a caller put in front of the
    /// address the front end appended.
    /// </summary>
    internal static void AddClientLimits(this WebApplicationBuilder builder, HostLimits limits)
    {
        foreach (var filter in builder.Services
            .Where(d => d.ServiceType == typeof(IStartupFilter)
                && d.ImplementationType?.Name == "ForwardedHeadersStartupFilter")
            .ToList())
        {
            builder.Services.Remove(filter);
        }

        // The front end has no fixed address range, so every immediate peer is trusted and only the right-most entry
        // (the one the front end appends) is used. X-Forwarded-Host is not honoured: the Host header stays as sent.
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
            options.ForwardLimit = 1;
        });

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context => CreatePartition(context, limits));
            options.OnRejected = static (context, _) =>
            {
                // The body comes from the status-code pages of the response policy.
                var seconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                    ? Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds))
                    : FallbackRetryAfterSeconds(context.HttpContext);
                context.HttpContext.Response.Headers.RetryAfter = seconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
                return ValueTask.CompletedTask;
            };
        });
    }

    private static RateLimitPartition<string> CreatePartition(HttpContext context, HostLimits limits)
    {
        var address = Normalize(context.Connection.RemoteIpAddress);
        if (IsStaticRequest(context))
        {
            return RateLimitPartition.GetSlidingWindowLimiter("static:" + address, _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = limits.StaticPerHour,
                Window = TimeSpan.FromHours(1),
                SegmentsPerWindow = StaticSegments,
                QueueLimit = 0,
                AutoReplenishment = true,
            });
        }

        return RateLimitPartition.GetFixedWindowLimiter("other:" + address, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = limits.NonStaticPerMinute,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true,
        });
    }

    private static int FallbackRetryAfterSeconds(HttpContext context)
    {
        if (IsStaticRequest(context))
        {
            return StaticFallbackRetryAfterSeconds;
        }

        return NonStaticFallbackRetryAfterSeconds;
    }

    private static string Normalize(IPAddress? address)
    {
        if (address is null)
        {
            return UnknownAddress;
        }

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        return address.ToString();
    }

    // Static means: the file provider that serves the request has the file, or, for a directory path such as "/",
    // the entry document that UseDefaultFiles serves in its place, and the static-file middleware would serve it
    // (known content type). Everything else, including 404s and rejected methods, is not static.
    private static bool IsStaticRequest(HttpContext context)
    {
        if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method))
        {
            return false;
        }

        var files = context.RequestServices.GetRequiredService<IWebHostEnvironment>().WebRootFileProvider;
        var path = context.Request.Path.Value ?? "/";
        if (HostHardening.IsCompressedVariantPath(path))
        {
            // UsePrecompressedFiles answers 404 for a variant requested by its own name.
            return false;
        }

        var file = files.GetFileInfo(path);
        if (file.Exists && !file.IsDirectory)
        {
            // The static-file middleware answers 404 for an extension without a known content type.
            return ContentTypes.TryGetContentType(path, out _);
        }

        if (path.EndsWith('/'))
        {
            return files.GetFileInfo(path.TrimEnd('/') + "/" + EntryDocument).Exists;
        }

        return false;
    }
}
