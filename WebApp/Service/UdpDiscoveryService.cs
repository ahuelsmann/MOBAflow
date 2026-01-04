// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WebApp.Service;

using System.Net;
using System.Net.Sockets;
using System.Text;

/// <summary>
/// UDP Discovery Service for MOBAflow REST-API.
/// Listens for UDP Multicast from MAUI clients and responds with server IP + Port.
/// Uses Multicast (239.255.42.99) instead of Broadcast for better WiFi router compatibility.
/// Similar pattern to Z21 discovery but for REST-API service discovery.
/// </summary>
public class UdpDiscoveryService : BackgroundService
{
    private const int DISCOVERY_PORT = 21106; // Different from Z21 (21105) to avoid conflicts
    private const string DISCOVERY_REQUEST = "MOBAFLOW_DISCOVER";
    private const string DISCOVERY_RESPONSE_PREFIX = "MOBAFLOW_REST_API";
    private const string MULTICAST_ADDRESS = "239.255.42.99"; // Local network multicast

    private readonly ILogger<UdpDiscoveryService> _logger;
    private readonly IConfiguration _configuration;
    private UdpClient? _udpListener;

    public UdpDiscoveryService(ILogger<UdpDiscoveryService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("UDP Discovery Service starting on Multicast {MulticastAddress}:{Port}", MULTICAST_ADDRESS, DISCOVERY_PORT);

        try
        {
            _udpListener = new UdpClient();
            _udpListener.ExclusiveAddressUse = false;
            
            var localEndPoint = new IPEndPoint(IPAddress.Any, DISCOVERY_PORT);
            _udpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpListener.Client.Bind(localEndPoint);
            
            // Join multicast group
            var multicastAddress = IPAddress.Parse(MULTICAST_ADDRESS);
            _udpListener.JoinMulticastGroup(multicastAddress);
            
            _logger.LogInformation("✅ Joined Multicast group {MulticastAddress} on port {Port}", MULTICAST_ADDRESS, DISCOVERY_PORT);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = await _udpListener.ReceiveAsync(stoppingToken);
                    var message = Encoding.UTF8.GetString(result.Buffer);

                    if (message.Trim() == DISCOVERY_REQUEST)
                    {
                        _logger.LogInformation("Received discovery request from {RemoteEndPoint}", result.RemoteEndPoint);

                        // Get local IP and REST API port
                        var localIp = GetLocalIPAddress();
                        
                        // ✅ Parse port from Kestrel URL (e.g., "http://localhost:5001")
                        // Default to 5001 if not configured
                        var kestrelUrl = _configuration["Kestrel:Endpoints:Http:Url"] ?? "http://localhost:5001";
                        var restPort = 5001; // Default
                        
                        if (Uri.TryCreate(kestrelUrl, UriKind.Absolute, out var uri))
                        {
                            restPort = uri.Port;
                        }

                        // Response format: "MOBAFLOW_REST_API|192.168.0.100|5000"
                        var response = $"{DISCOVERY_RESPONSE_PREFIX}|{localIp}|{restPort}";
                        var responseBytes = Encoding.UTF8.GetBytes(response);

                        // Send response back to client
                        await _udpListener.SendAsync(responseBytes, result.RemoteEndPoint, stoppingToken);

                        _logger.LogInformation("Sent discovery response to {RemoteEndPoint}: {Response}", 
                            result.RemoteEndPoint, response);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when stopping
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing UDP discovery request");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UDP Discovery Service failed to start");
        }
        finally
        {
            _udpListener?.Close();
            _logger.LogInformation("UDP Discovery Service stopped");
        }
    }

    /// <summary>
    /// Gets the local IP address of the server (prefers 192.168.x.x range for local networks).
    /// </summary>
    private static string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        
        // Prefer 192.168.x.x addresses (common private network range)
        var privateIp = host.AddressList
            .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork 
                               && ip.ToString().StartsWith("192.168."));

        if (privateIp != null)
            return privateIp.ToString();

        // Fallback: Any IPv4 address
        var anyIp = host.AddressList
            .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);

        return anyIp?.ToString() ?? "127.0.0.1";
    }

    public override void Dispose()
    {
        _udpListener?.Close();
        _udpListener?.Dispose();
        base.Dispose();
    }
}
