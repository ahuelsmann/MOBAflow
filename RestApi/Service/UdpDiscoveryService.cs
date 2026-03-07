// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.RestApi.Service;

using System.Net;
using System.Net.Sockets;
using System.Text;

/// <summary>
/// UDP Discovery Service for MOBAflow REST API.
/// Listens for UDP Multicast from MAUI clients and responds with server IP + Port.
/// </summary>
internal class UdpDiscoveryService : BackgroundService
{
    private const int DiscoveryPort = 21106;
    private const string DiscoveryRequest = "MOBAFLOW_DISCOVER";
    private const string DiscoveryResponsePrefix = "MOBAFLOW_REST_API";
    private const string MulticastAddress = "239.255.42.99";

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
        _logger.LogInformation("UDP Discovery starting on Multicast {MulticastAddress}:{Port}", MulticastAddress, DiscoveryPort);

        try
        {
            _udpListener = new UdpClient { ExclusiveAddressUse = false };

            var localEndPoint = new IPEndPoint(IPAddress.Any, DiscoveryPort);
            _udpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpListener.Client.Bind(localEndPoint);

            var multicastAddress = IPAddress.Parse(MulticastAddress);
            _udpListener.JoinMulticastGroup(multicastAddress);

            _logger.LogInformation("Joined Multicast group {MulticastAddress}:{Port}", MulticastAddress, DiscoveryPort);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = await _udpListener.ReceiveAsync(stoppingToken);
                    var message = Encoding.UTF8.GetString(result.Buffer);

                    if (message.Trim() == DiscoveryRequest)
                    {
                        _logger.LogInformation("Discovery request from {RemoteEndPoint}", result.RemoteEndPoint);

                        var localIp = GetLocalIpAddress();
                        var kestrelUrl = _configuration["Kestrel:Endpoints:Http:Url"] ?? "http://localhost:5001";
                        var restPort = 5001;
                        if (Uri.TryCreate(kestrelUrl, UriKind.Absolute, out var uri))
                            restPort = uri.Port;

                        var response = $"{DiscoveryResponsePrefix}|{localIp}|{restPort}";
                        var responseBytes = Encoding.UTF8.GetBytes(response);
                        await _udpListener.SendAsync(responseBytes, result.RemoteEndPoint, stoppingToken);

                        _logger.LogInformation("Discovery response sent: {Response}", response);
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing UDP discovery request");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UDP Discovery failed to start");
        }
        finally
        {
            _udpListener?.Close();
            _logger.LogInformation("UDP Discovery stopped");
        }
    }

    private static string GetLocalIpAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var privateIp = host.AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork
                    && ip.ToString().StartsWith("192.168."));
            if (privateIp != null) return privateIp.ToString();

            var anyIp = host.AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
            return anyIp?.ToString() ?? "127.0.0.1";
        }
        catch
        {
            return "127.0.0.1";
        }
    }

    public override void Dispose()
    {
        _udpListener?.Close();
        _udpListener?.Dispose();
        base.Dispose();
    }
}
