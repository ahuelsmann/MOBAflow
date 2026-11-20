// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Web;

/// <summary>
/// Builds the MOBAdemo host: it serves the static files of the web root and a health response, nothing else.
/// </summary>
public static class MobaWebHost
{
    /// <summary>Creates the host. <paramref name="webRootPath"/> overrides the web root (used by tests).</summary>
    public static WebApplication Build(string[] args, string? webRootPath = null)
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
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.MapGet("/health", () => "ok");
        return app;
    }
}
