// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Controllers;

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// REST API for server status and connected MAUI clients (in-process WinUI).
/// </summary>
[ApiController]
[Route("api/[controller]")]
internal sealed class StatusController : ControllerBase
{
    private const int ClientExpiryMinutes = 10;
    private readonly int _port;

    public StatusController(int port)
    {
        _port = port;
    }

    /// <summary>
    /// Returns REST API status and list of connected clients (e.g. MAUI app).
    /// </summary>
    [HttpGet]
    public IActionResult GetStatus()
    {
        ClientRegistry.PruneExpired(ClientExpiryMinutes);

        return Ok(new
        {
            status = "running",
            port = _port,
            connectedClients = ClientRegistry.GetAll()
                .OrderBy(c => c.ConnectedAt)
                .Select(c => new { c.ClientId, c.DeviceName, c.ConnectedAt })
                .ToList()
        });
    }
}

/// <summary>
/// Register/unregister MAUI (or other) clients for the Overview page.
/// </summary>
[ApiController]
[Route("api/[controller]")]
internal sealed class ClientsController : ControllerBase
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

internal sealed record RegisterClientRequest(string ClientId, string? DeviceName);
internal sealed record UnregisterClientRequest(string ClientId);

internal sealed class ConnectedClientInfo
{
    public string ClientId { get; set; } = "";
    public string DeviceName { get; set; } = "";
    public DateTime ConnectedAt { get; set; }
}

/// <summary>
/// In-memory registry for connected clients (MAUI) in WinUI in-process REST API.
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
