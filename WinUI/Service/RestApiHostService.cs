// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Common.Configuration;
using Controllers;
using Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharedUI.Service;
using System.Reflection;
using System.Text.Json;

/// <summary>
/// Application part that exposes only the WinUI API controller types so MVC discovers them
/// when Kestrel is hosted in-process (avoids "No action descriptors found").
/// </summary>
internal sealed class WinUiControllersApplicationPart : ApplicationPart, IApplicationPartTypeProvider
{
    public WinUiControllersApplicationPart()
    {
        Types = new[]
        {
            typeof(StatusController).GetTypeInfo(),
            typeof(ClientsController).GetTypeInfo(),
            typeof(PhotoUploadController).GetTypeInfo()
        };
    }

    public override string Name => "Moba.WinUI.Controllers";

    public IEnumerable<TypeInfo> Types { get; }
}

/// <summary>
/// Hosts the REST API (Kestrel) in-process when "Auto-start REST API with MOBAflow" is enabled.
/// Serves /api/status, /api/clients, /api/photos for MOBAsmart and Overview page status.
/// </summary>
internal sealed class RestApiHostService : IAsyncDisposable
{
    private readonly AppSettings _appSettings;
    private readonly IServiceProvider _winUiServices;
    private readonly ILogger<RestApiHostService> _logger;
    private WebApplication? _app;
    private UdpDiscoveryResponder? _udpResponder;

    /// <summary>
    /// True when the in-process REST API server has been started and not yet stopped.
    /// </summary>
    public bool IsRunning { get; private set; }

    public RestApiHostService(
        AppSettings appSettings,
        IServiceProvider winUiServices,
        ILogger<RestApiHostService> logger)
    {
        _appSettings = appSettings;
        _winUiServices = winUiServices;
        _logger = logger;
    }

    /// <summary>
    /// Starts the in-process REST API server and UDP discovery responder.
    /// Idempotent: if already running, returns immediately.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsRunning)
            return;
        var port = _appSettings.RestApi?.Port > 0 ? _appSettings.RestApi.Port : 5001;

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = AppContext.BaseDirectory,
            Args = Array.Empty<string>()
        });

        builder.WebHost.UseKestrel(options =>
        {
            options.Listen(System.Net.IPAddress.Any, port);
        });

        // Ensure the host uses the WinUI assembly for MVC discovery (avoids "No action descriptors found").
        var controllerAssembly = typeof(StatusController).Assembly;
        builder.WebHost.UseSetting(WebHostDefaults.ApplicationKey, controllerAssembly.GetName().Name);

        builder.Logging.ClearProviders();
        builder.Logging.AddDebug();

        // Controllers and SignalR. Add an explicit application part that exposes only our controller types
        // so MVC discovers them when Kestrel runs in-process (avoids "No action descriptors found").
        builder.Services.AddControllers()
            .ConfigureApplicationPartManager(manager =>
            {
                manager.ApplicationParts.Add(new WinUiControllersApplicationPart());
            });
        builder.Services.AddSignalR();

        // StatusController needs port (register via factory; int cannot be AddSingleton directly)
        builder.Services.AddSingleton<StatusController>(_ => new StatusController(port));

        // PhotoUploadController needs PhotoStorageService from WinUI DI; IHubContext and ILogger come from the web host
        builder.Services.AddSingleton(_ => _winUiServices.GetRequiredService<PhotoStorageService>());

        var app = builder.Build();

        // Fallback minimal API routes so /api/status and /api/photos/health always respond even when
        // MVC controller discovery fails (e.g. in-process host with different assembly load context).
        const int clientExpiryMinutes = 10;
        app.MapGet("/api/status", () =>
        {
            ClientRegistry.PruneExpired(clientExpiryMinutes);
            return Results.Json(new
            {
                status = "running",
                port,
                connectedClients = ClientRegistry.GetAll()
                    .OrderBy(c => c.ConnectedAt)
                    .Select(c => new { c.ClientId, c.DeviceName, c.ConnectedAt })
                    .ToList()
            }, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        });

        app.MapGet("/api/photos/health", () =>
            Results.Json(new
            {
                service = "MOBAflow Photo API (WinUI)",
                status = "healthy",
                version = "1.0.0"
            }, new JsonSerializerOptions(JsonSerializerDefaults.Web)));

        app.MapControllers();
        app.MapHub<PhotoHub>("/hubs/photos");

        await app.StartAsync(cancellationToken).ConfigureAwait(false);
        _app = app;
        IsRunning = true;

        _logger.LogInformation("REST API started in-process on port {Port}", port);

        // Ensure firewall allows UDP discovery (21106) and REST API (TCP port) so MAUI can discover and connect
        FirewallHelper.EnsureFirewallRulesExist(port);

        // Start UDP discovery so MAUI can find this server
        try
        {
            _udpResponder = new UdpDiscoveryResponder(
                _winUiServices.GetRequiredService<ILogger<UdpDiscoveryResponder>>(),
                port);
            _udpResponder.Start();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "UDP Discovery responder could not start");
        }
    }

    /// <summary>
    /// Stops the REST API server and UDP discovery.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _udpResponder?.Stop();
        _udpResponder?.Dispose();
        _udpResponder = null;

        if (_app != null)
        {
            await _app.StopAsync(cancellationToken).ConfigureAwait(false);
            await _app.DisposeAsync().ConfigureAwait(false);
            _app = null;
        }
        IsRunning = false;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }
}
