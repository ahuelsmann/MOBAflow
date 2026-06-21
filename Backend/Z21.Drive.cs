// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend;

using Microsoft.Extensions.Logging;

using Model;

using Protocol;

public partial class Z21
{
    #region Locomotive Drive Commands
    /// <summary>
    /// Event raised when locomotive info is received from Z21.
    /// </summary>
    public event Action<LocoInfo>? OnLocoInfoChanged;

    /// <summary>
    /// Sets locomotive speed and direction using 128 speed steps.
    /// LAN_X_SET_LOCO_DRIVE: 0xE4 0x13 Adr_MSB Adr_LSB RVVVVVVV XOR
    /// </summary>
    /// <param name="address">DCC locomotive address (1-9999)</param>
    /// <param name="speed">Speed value (0-126, where 0=stop)</param>
    /// <param name="forward">True = forward direction, False = backward</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task SetLocoDriveAsync(int address, int speed, bool forward, CancellationToken cancellationToken = default)
    {
        if (address < 1 || address > 9999)
            throw new ArgumentOutOfRangeException(nameof(address), "DCC address must be 1-9999");
        if (speed < 0 ||
            speed > 126)
            throw new ArgumentOutOfRangeException(nameof(speed), "Speed must be between 0 and 126 (0 = stop)");

        var packet = Z21Command.BuildSetLocoDrive(address, speed, forward);

        _logger?.LogInformation(
            "SetLocoDrive: Addr={Address}, Speed={Speed}, Forward={Forward}, Packet={Packet}",
            address,
            speed,
            forward,
            Z21Protocol.ToHex(packet));

        await SendAsync(packet, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets a locomotive function on/off.
    /// LAN_X_SET_LOCO_FUNCTION: 0xE4 0xF8 Adr_MSB Adr_LSB TTNNNNNN XOR
    /// </summary>
    /// <param name="address">DCC locomotive address (1-9999)</param>
    /// <param name="functionIndex">Function index (0=F0/light, 1=F1/sound, ... up to 31=F31)</param>
    /// <param name="on">True = function on, False = function off</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task SetLocoFunctionAsync(int address, int functionIndex, bool on, CancellationToken cancellationToken = default)
    {
        if (address < 1 || address > 9999)
            throw new ArgumentOutOfRangeException(nameof(address), "DCC address must be 1-9999");
        // The LAN_X_SET_LOCO_FUNCTION packet encodes the function index in the lower 6 bits
        // (NNNNNN, see Z21Command.BuildSetLocoFunction), so F0-F31 are all supported.
        if (functionIndex < 0 || functionIndex > 31)
            throw new ArgumentOutOfRangeException(nameof(functionIndex), "Function index must be between 0 and 31");

        var command = Z21Command.BuildSetLocoFunction(address, functionIndex, on);
        await SendAsync(command, cancellationToken).ConfigureAwait(false);
        _logger?.LogDebug("SetLocoFunction: Address={Address}, Function={Function}, On={On}",
            address, functionIndex, on);
    }

    /// <summary>
    /// Turns off all locomotive functions F0-F31 for the given address.
    /// Sends an explicit OFF command (TT=00) per function - never a toggle - so the
    /// resulting state is deterministic regardless of the decoder's previous state.
    /// </summary>
    /// <param name="address">DCC locomotive address (1-9999)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task SetAllLocoFunctionsOffAsync(int address, CancellationToken cancellationToken = default)
    {
        if (address < 1 || address > 9999)
            throw new ArgumentOutOfRangeException(nameof(address), "DCC address must be 1-9999");

        for (int functionIndex = 0; functionIndex <= 31; functionIndex++)
        {
            var command = Z21Command.BuildSetLocoFunction(address, functionIndex, on: false);
            await SendAsync(command, cancellationToken).ConfigureAwait(false);
        }

        _logger?.LogDebug("SetAllLocoFunctionsOff: Address={Address} (F0-F31 OFF)", address);
    }

    /// <summary>
    /// Requests locomotive information and subscribes to updates for this address.
    /// LAN_X_GET_LOCO_INFO: 0xE3 0xF0 Adr_MSB Adr_LSB XOR
    /// Max 16 loco addresses can be subscribed per client (FIFO).
    /// </summary>
    /// <param name="address">DCC locomotive address (1-9999)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task GetLocoInfoAsync(int address, CancellationToken cancellationToken = default)
    {
        if (address < 1 || address > 9999)
            throw new ArgumentOutOfRangeException(nameof(address), "DCC address must be 1-9999");

        var command = Z21Command.BuildGetLocoInfo(address);
        await SendAsync(command, cancellationToken).ConfigureAwait(false);
        _logger?.LogDebug("GetLocoInfo: Address={Address}", address);
    }
    #endregion

    #region Switching Commands
    /// <summary>
    /// Sets a turnout or 2-output signal decoder position.
    /// </summary>
    public async Task SetTurnoutAsync(int decoderAddress, int output, bool activate, bool queue = false, CancellationToken cancellationToken = default)
    {
        if (decoderAddress is < 1 or > 2044)
        {
            throw new ArgumentOutOfRangeException(nameof(decoderAddress), "Accessory decoder address must be between 1 and 2044");
        }

        if (output is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(output), "Output must be 0 or 1");
        }

        var command = Z21Command.BuildSetTurnout(decoderAddress, output, activate, queue);
        await SendAsync(command, cancellationToken).ConfigureAwait(false);
        _logger?.LogInformation("SetTurnout: Address={Address}, Output={Output}, Activate={Activate}, Queue={Queue}", decoderAddress, output, activate, queue);
    }

    /// <summary>
    /// Sets an extended accessory decoder value (multiplex signal decoder).
    /// </summary>
    public async Task SetExtAccessoryAsync(int extAccessoryAddress, int commandValue, CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("SetExtAccessory start: Address={Address}, Value={Value}", extAccessoryAddress, commandValue);

        if (extAccessoryAddress is < 0 or > 255)
        {
            _logger?.LogWarning("SetExtAccessory invalid address: {Address}", extAccessoryAddress);
            throw new ArgumentOutOfRangeException(nameof(extAccessoryAddress), "Extended accessory address must be between 0 and 255");
        }

        if (commandValue is < 0 or > 255)
        {
            _logger?.LogWarning("SetExtAccessory invalid command value: {CommandValue}", commandValue);
            throw new ArgumentOutOfRangeException(nameof(commandValue), "Command value must be between 0 and 255");
        }

        var command = Z21Command.BuildSetExtAccessory(extAccessoryAddress, commandValue);
        _logger?.LogTrace("SetExtAccessory command built: {CommandBytes}", string.Join(" ", command.Select(b => b.ToString("X2"))));

        await SendAsync(command, cancellationToken).ConfigureAwait(false);

        _logger?.LogDebug("SetExtAccessory command sent successfully");
        _logger?.LogInformation("SetExtAccessory: Address={Address}, Value={Value}", extAccessoryAddress, commandValue);
    }

    /// <summary>
    /// Requests turnout or signal decoder status information.
    /// </summary>
    public async Task GetTurnoutInfoAsync(int decoderAddress, CancellationToken cancellationToken = default)
    {
        if (decoderAddress is < 1 or > 2044)
        {
            throw new ArgumentOutOfRangeException(nameof(decoderAddress), "Accessory decoder address must be between 1 and 2044");
        }

        var command = Z21Command.BuildGetTurnoutInfo(decoderAddress);
        await SendAsync(command, cancellationToken).ConfigureAwait(false);
        _logger?.LogDebug("GetTurnoutInfo: Address={Address}", decoderAddress);
    }
    #endregion
}