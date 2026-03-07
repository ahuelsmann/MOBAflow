// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.ReactApp.Controllers;

using Microsoft.AspNetCore.Mvc;
using SharedUI.ViewModel;

/// <summary>
/// REST API controller for Z21 digital command station operations.
/// Provides endpoints for connecting, controlling track power, and emergency stop.
/// Mirrors WebAppViewModel properties for React frontend.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class Z21Controller : ControllerBase
{
    private readonly WebAppViewModel _viewModel;
    private readonly ILogger<Z21Controller> _logger;

    public Z21Controller(WebAppViewModel viewModel, ILogger<Z21Controller> logger)
    {
        _viewModel = viewModel;
        _logger = logger;
    }

    /// <summary>
    /// Get current connection and system status.
    /// Returns all properties needed by React Dashboard (matches WebAppViewModel).
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            // Connection
            isConnected = _viewModel.IsConnected,
            ipAddress = _viewModel.IpAddress,
            statusText = _viewModel.StatusText,
            availableIpAddresses = _viewModel.AvailableIpAddresses.ToList(),

            // System State (matches SystemState in Blazor)
            isTrackPowerOn = _viewModel.IsTrackPowerOn,
            mainCurrent = _viewModel.MainCurrent,
            temperature = _viewModel.Temperature,
            supplyVoltage = _viewModel.SupplyVoltage,
            vccVoltage = _viewModel.VccVoltage,

            // Version Info
            serialNumber = _viewModel.SerialNumber,
            firmwareVersion = _viewModel.FirmwareVersion,
            hardwareType = _viewModel.HardwareType
        });
    }

    /// <summary>
    /// Connect to Z21 command station.
    /// Waits up to 8 seconds for the Z21 to respond before returning; connection is confirmed
    /// asynchronously by the device (UDP handshake + response).
    /// </summary>
    [HttpPost("connect")]
    public async Task<IActionResult> ConnectAsync([FromBody] ConnectRequest request)
    {
        const int waitTimeoutMs = 8000;
        const int pollIntervalMs = 200;

        try
        {
            if (string.IsNullOrWhiteSpace(request?.IpAddress))
            {
                return Ok(new { success = false, error = "IP-Adresse fehlt." });
            }

            _logger.LogInformation("Connecting to Z21 at {IpAddress}", request.IpAddress);

            _viewModel.IpAddress = request.IpAddress;

            if (!_viewModel.ConnectCommand.CanExecute(null))
            {
                _logger.LogWarning("ConnectCommand.CanExecute returned false - connect not executed");
                return Ok(new { success = false, error = "Verbindung kann derzeit nicht ausgeführt werden.", statusText = _viewModel.StatusText });
            }

            await _viewModel.ConnectCommand.ExecuteAsync(null).ConfigureAwait(false);

            // Z21 confirms connection asynchronously when it responds to our packets.
            // Wait for IsConnected to become true (or timeout).
            var deadline = DateTime.UtcNow.AddMilliseconds(waitTimeoutMs);
            while (!_viewModel.IsConnected && DateTime.UtcNow < deadline)
            {
                await Task.Delay(pollIntervalMs).ConfigureAwait(false);
            }

            if (_viewModel.IsConnected)
            {
                _logger.LogInformation("Z21 connection confirmed at {IpAddress}", request.IpAddress);
                return Ok(new { success = true, statusText = _viewModel.StatusText });
            }

            _logger.LogWarning("Z21 connection timeout at {IpAddress} after {Ms} ms", request.IpAddress, waitTimeoutMs);
            return Ok(new
            {
                success = false,
                error = "Verbindungszeitüberschreitung. Ist die Z21 unter " + request.IpAddress + " erreichbar? "
                    + "Wenn die WinUI-App sich verbinden kann: WinUI vorher beenden (nur ein Client pro Z21-Slot), Backend-Logs prüfen (Konsole beim Start der ReactApp).",
                statusText = _viewModel.StatusText
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection failed");
            return Ok(new { success = false, error = ex.Message, statusText = _viewModel.StatusText });
        }
    }

    /// <summary>
    /// Disconnect from Z21 command station.
    /// </summary>
    [HttpPost("disconnect")]
    public async Task<IActionResult> DisconnectAsync()
    {
        try
        {
            if (_viewModel.DisconnectCommand.CanExecute(null))
            {
                await _viewModel.DisconnectCommand.ExecuteAsync(null).ConfigureAwait(false);
            }
            return Ok(new { success = true, statusText = _viewModel.StatusText });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Disconnect failed");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Set track power on or off.
    /// </summary>
    [HttpPost("track-power")]
    public async Task<IActionResult> SetTrackPowerAsync([FromBody] TrackPowerRequest request)
    {
        try
        {
            if (_viewModel.SetTrackPowerCommand.CanExecute(request.On))
            {
                await _viewModel.SetTrackPowerCommand.ExecuteAsync(request.On).ConfigureAwait(false);
            }
            return Ok(new { success = true, isTrackPowerOn = _viewModel.IsTrackPowerOn });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Track power command failed");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Emergency stop - stops all trains immediately by turning off track power.
    /// </summary>
    [HttpPost("emergency-stop")]
    public async Task<IActionResult> EmergencyStopAsync()
    {
        try
        {
            _logger.LogWarning("Emergency stop triggered via REST API");

            // Emergency stop = turn off track power
            if (_viewModel.SetTrackPowerCommand.CanExecute(false))
            {
                await _viewModel.SetTrackPowerCommand.ExecuteAsync(false).ConfigureAwait(false);
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Emergency stop failed");
            return BadRequest(new { error = ex.Message });
        }
    }
}

public record ConnectRequest(string IpAddress);
public record TrackPowerRequest(bool On);
