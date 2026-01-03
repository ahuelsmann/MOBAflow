// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Service;

using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using System.Text;

/// <summary>
/// UDP Discovery Client for finding MOBAflow WebApp REST-API on local network.
/// Sends broadcast "MOBAFLOW_DISCOVER" and listens for server response.
/// Similar pattern to Z21 discovery.
/// </summary>
public class RestApiDiscoveryService
{
    private const int DISCOVERY_PORT = 21106;
    private const string DISCOVERY_REQUEST = "MOBAFLOW_DISCOVER";
    private const int RESPONSE_TIMEOUT_MS = 3000; // 3 seconds

    private readonly ILogger<RestApiDiscoveryService> _logger;

    public RestApiDiscoveryService(ILogger<RestApiDiscoveryService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Discovers MOBAflow REST-API server on local network.
    /// </summary>
    /// <returns>Server IP and Port, or null if not found</returns>
    public async Task<(string? ip, int? port)> DiscoverServerAsync()
    {
        UdpClient? udpClient = null;

        try
        {
            udpClient = new UdpClient();
            udpClient.EnableBroadcast = true;

            // Send broadcast discovery request
            var requestBytes = Encoding.UTF8.GetBytes(DISCOVERY_REQUEST);
            var broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, DISCOVERY_PORT);

            await udpClient.SendAsync(requestBytes, broadcastEndpoint);
            _logger.LogInformation("Sent UDP discovery broadcast to port {Port}", DISCOVERY_PORT);

            // Wait for response with timeout
            var receiveTask = udpClient.ReceiveAsync();
            var timeoutTask = Task.Delay(RESPONSE_TIMEOUT_MS);

            var completedTask = await Task.WhenAny(receiveTask, timeoutTask);

            if (completedTask == receiveTask)
            {
                var result = await receiveTask;
                var response = Encoding.UTF8.GetString(result.Buffer);

                _logger.LogInformation("Received discovery response: {Response} from {RemoteEndPoint}", 
                    response, result.RemoteEndPoint);

                // Parse response: "MOBAFLOW_REST_API|192.168.0.100|5000"
                var parts = response.Split('|');
                if (parts.Length == 3 && parts[0] == "MOBAFLOW_REST_API")
                {
                    var serverIp = parts[1];
                    if (int.TryParse(parts[2], out var serverPort))
                    {
                        _logger.LogInformation("Discovered MOBAflow server at {Ip}:{Port}", serverIp, serverPort);
                        return (serverIp, serverPort);
                    }
                }
            }
            else
            {
                _logger.LogWarning("Discovery timeout - no server found");
            }

            return (null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during REST-API discovery");
            return (null, null);
        }
        finally
        {
            udpClient?.Close();
            udpClient?.Dispose();
        }
    }
}
