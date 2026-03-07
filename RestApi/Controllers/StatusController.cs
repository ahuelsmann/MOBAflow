// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.RestApi.Controllers;

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// REST API for server status and connected MAUI clients.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class StatusController : ControllerBase
{
    private const int ClientExpiryMinutes = 10;

    /// <summary>
    /// Returns REST API status and list of connected clients (e.g. MAUI app).
    /// </summary>
    [HttpGet]
    public IActionResult GetStatus([FromServices] IConfiguration configuration)
    {
        var port = GetPortFromConfig(configuration);

        ClientRegistry.PruneExpired(ClientExpiryMinutes);

        return Ok(new
        {
            status = "running",
            port,
            connectedClients = ClientRegistry.GetAll()
                .OrderBy(c => c.ConnectedAt)
                .Select(c => new { c.ClientId, c.DeviceName, c.ConnectedAt })
                .ToList()
        });
    }

    private static int GetPortFromConfig(IConfiguration configuration)
    {
        var url = configuration["Kestrel:Endpoints:Http:Url"];
        if (!string.IsNullOrEmpty(url) && url.Contains(':', StringComparison.Ordinal))
        {
            var part = url.Split(':').LastOrDefault()?.TrimEnd('/');
            if (part != null && int.TryParse(part, out var p))
                return p;
        }
        return 5001;
    }
}

/// <summary>
/// Register/unregister MAUI (or other) clients for the Overview page.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ClientsController : ControllerBase
{
    /// <summary>
    /// Registers a client (e.g. MAUI app). Call when the app connects to the REST API.
    /// </summary>
    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisterClientRequest? request)
    {
        if (string.IsNullOrWhiteSpace(request?.ClientId))
            return BadRequest(new { error = "ClientId is required" });

        var info = new ConnectedClientInfo
        {
            ClientId = request.ClientId.Trim(),
            DeviceName = request.DeviceName?.Trim() ?? "MOBAsmart",
            ConnectedAt = DateTime.UtcNow
        };
        ClientRegistry.Add(info);
        return Ok(new { registered = true, clientId = info.ClientId });
    }

    /// <summary>
    /// Unregisters a client. Call when the app disconnects or closes.
    /// </summary>
    [HttpPost("unregister")]
    public IActionResult Unregister([FromBody] UnregisterClientRequest? request)
    {
        if (string.IsNullOrWhiteSpace(request?.ClientId))
            return BadRequest(new { error = "ClientId is required" });
        ClientRegistry.Remove(request.ClientId.Trim());
        return Ok(new { unregistered = true });
    }
}

public record RegisterClientRequest(string ClientId, string? DeviceName);
public record UnregisterClientRequest(string ClientId);

/// <summary>
/// In-memory info for a connected client (MAUI app).
/// </summary>
internal class ConnectedClientInfo
{
    public string ClientId { get; set; } = "";
    public string DeviceName { get; set; } = "";
    public DateTime ConnectedAt { get; set; }
}

/// <summary>
/// Shared in-memory registry for connected clients.
/// </summary>
internal static class ClientRegistry
{
    private static readonly ConcurrentDictionary<string, ConnectedClientInfo> s_clients = new();

    public static void Add(ConnectedClientInfo info)
    {
        s_clients[info.ClientId] = info;
    }

    public static void Remove(string clientId)
    {
        s_clients.TryRemove(clientId, out _);
    }

    public static void PruneExpired(int expiryMinutes)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-expiryMinutes);
        foreach (var kv in s_clients.ToArray())
        {
            if (kv.Value.ConnectedAt < cutoff)
                s_clients.TryRemove(kv.Key, out _);
        }
    }

    public static IReadOnlyList<ConnectedClientInfo> GetAll()
    {
        return s_clients.Values.ToList();
    }
}
