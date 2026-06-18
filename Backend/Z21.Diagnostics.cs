// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend;

using Common.Events;

using Microsoft.Extensions.Logging;

using Protocol;

public partial class Z21
{
    #region Testing & Simulation
    /// <summary>
    /// Simulates a feedback event for testing purposes without requiring actual Z21 hardware.
    /// This triggers the same Received event as a real Z21 feedback message would.
    /// Only for testing in WinUI - not used in MOBAsmart.
    /// </summary>
    /// <param name="inPort">The InPort number (0-255) to simulate feedback for.</param>
    public void SimulateFeedback(int inPort)
    {
        if (inPort < 0 || inPort > 512)
        {
            throw new ArgumentOutOfRangeException(nameof(inPort), "InPort must be between 0 and 512");
        }

        // Convert InPort (1-based) to group/byte/bit representation per Z21 protocol
        var portIndex = Math.Max(inPort - 1, 0); // clamp 0 to first slot
        var groupNumber = portIndex / 64;        // 0-based module
        var byteIndex = portIndex % 64 / 8;    // which data byte (0-7)
        var bitPosition = portIndex % 8;         // which bit inside the byte (0-7)

        var simulatedContent = new byte[15];
        simulatedContent[0] = 0x0F; // Length LSB (15 bytes)
        simulatedContent[1] = 0x00; // Length MSB
        simulatedContent[2] = 0x80; // Header LSB (LAN_RMBUS_DATACHANGED)
        simulatedContent[3] = 0x00; // Header MSB
        simulatedContent[4] = (byte)groupNumber; // Group number (module id)
        simulatedContent[5 + byteIndex] = (byte)(1 << bitPosition); // Feedback bit pattern
        // Remaining bytes stay 0; checksum ignored in parser/tests

        _logger?.LogInformation("SimulateFeedback: InPort={InPort}, Group={Group}, Byte={ByteIndex}, Bit={BitPosition}, Subscribers={Count}",
            inPort, groupNumber, byteIndex, bitPosition, Received?.GetInvocationList().Length ?? 0);

        Received?.Invoke(new FeedbackResult(simulatedContent));
        PublishEventAsync(new FeedbackReceivedEvent(inPort));

        _logger?.LogDebug("SimulateFeedback event invoked for InPort={InPort}", inPort);
    }
    #endregion

    #region Log & Debugging
    /// <summary>
    /// Forces the Z21 to send a status update immediately.
    /// This is useful for debugging to check the current state of the Z21.
    /// LAN_X_GET_STATUS (X-Header: 0x21, DB0: 0x24)
    /// </summary>
    public async Task DebugForceStatusUpdateAsync(CancellationToken cancellationToken = default)
    {
        await SendCommandAsync(Z21Command.BuildGetStatus(), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a custom debug command to the Z21.
    /// This can be used to test raw command sequences without modifying the firmware.
    /// </summary>
    /// <param name="command">The byte sequence containing the command for the Z21.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task DebugSendCommandAsync(byte[] command, CancellationToken cancellationToken = default)
    {
        await SendAsync(command, cancellationToken).ConfigureAwait(false);
    }
    #endregion

    #region RailCom Support (Prepared for Future Enhancement)
    /// <summary>
    /// Requests RailCom data for a specific locomotive.
    /// NOT YET IMPLEMENTED - infrastructure prepared but inactive.
    /// 
    /// Future implementation will:
    /// - Send LAN_RAILCOM_GETDATA command (0x89)
    /// - Parse decoder-reported current consumption
    /// - Parse decoder temperature
    /// - Track RailCom quality metrics
    /// </summary>
    public Task GetRailComDataAsync(int address, CancellationToken cancellationToken = default)
    {
        _logger?.LogWarning("GetRailComDataAsync not yet implemented. RailCom support prepared but inactive.");
        return Task.CompletedTask;
    }
    #endregion
}