// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Demo;

/// <summary>
/// Builds the MOBAdemo host: it serves the static files of the web root and a health response, nothing else.
/// </summary>
internal static class MobaDemoHost
{
    /// <summary>Creates the host. <paramref name="webRootPath"/> overrides the web root (used by tests).</summary>
    /// <param name="args">Command-line arguments.</param>
    /// <param name="webRootPath">Optional web root; the default is <c>wwwroot</c> next to the application.</param>
    /// <param name="limits">Per-client limits; the production values when <see langword="null"/>.</param>
    /// <param name="configure">Test hook that may add endpoints after the host pipeline is set up.</param>
    internal static WebApplication Build(
        string[] args,
        string? webRootPath = null,
        Action<WebApplication>? configure = null,
        HostLimits? limits = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            // On App Service the working directory differs from the application directory.
            ContentRootPath = AppContext.BaseDirectory,
            WebRootPath = webRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot"),
        });

        builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

        var app = builder.Build();

        // A neutral answer in every environment: no exception message, stack trace or path leaves the host.
        app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync("Internal Server Error", context.RequestAborted).ConfigureAwait(false);
        }));
        app.UseResponsePolicy();
        app.UseDefaultFiles();
        app.UsePrecompressedFiles();
        app.UseStaticFiles(HostHardening.CreateStaticFileOptions());
        app.MapMethods("/health", ["GET", "HEAD"], () => "ok");
        // "$" (end of path) is a Google and Bing extension that RFC 9309 does not define. A plain RFC rule cannot
        // allow "/" alone (every prefix rule also covers the sub-paths), so the extension is kept: crawlers that
        // ignore it only see "Disallow: /" and index nothing, which is the safe failure.
        app.MapMethods(
            "/robots.txt",
            ["GET", "HEAD"],
            () => Results.Text("User-agent: *\nAllow: /$\nDisallow: /\n", "text/plain"));
        configure?.Invoke(app);
        return app;
    }
}
