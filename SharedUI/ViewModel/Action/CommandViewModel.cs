// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel.Action;

using Domain;
using Domain.Enum;

using Helper;

using Microsoft.Extensions.Logging;

/// <summary>
/// ViewModel for Z21 Command actions (loco control).
/// Wraps WorkflowAction with typed properties for Address, Speed, Direction.
/// Provides decoding of raw Z21 DCC command bytes into human-readable format.
/// </summary>
public sealed class CommandViewModel : WorkflowActionViewModel
{
    #region Fields
    private Z21DccCommandDecoder.DccCommand? _decodedCommand;
    private readonly ILogger<CommandViewModel>? _logger;
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandViewModel"/> class for the given workflow action.
    /// </summary>
    /// <param name="action">The underlying workflow action that defines this Z21 locomotive command.</param>
    /// <param name="logger">Optional logger for encoding failures.</param>
    public CommandViewModel(WorkflowAction action, ILogger<CommandViewModel>? logger = null) : base(action, ActionType.Command)
    {
        _logger = logger;
        action.Command ??= new CommandActionPayload();
        _decodedCommand = DecodeBytes();
    }

    private CommandActionPayload Cmd => UnderlyingAction.Command ??= new CommandActionPayload();

    /// <summary>
    /// Available direction values for ComboBox binding.
    /// </summary>
    public static string[] DirectionValues { get; } = ["Forward", "Backward"];

    /// <summary>
    /// Locomotive address (DCC address).
    /// Extracted from raw Z21 bytes if available, otherwise returns stored Address value.
    /// Setting this value updates the raw bytes automatically.
    /// </summary>
    public int Address
    {
        get
        {
            var decoded = DecodedCommand;
            if (decoded?.IsValid == true)
                return decoded.Address;

            return Cmd.Address ?? 0;
        }
        set
        {
            var currentValue = Address;
            if (currentValue == value)
                return;

            Cmd.Address = value;
            OnPropertyChanged(nameof(Address));
            UpdateBytesFromProperties();
            OnPropertyChanged(nameof(BytesHex));
        }
    }

    /// <summary>
    /// Speed (0-127 for DCC).
    /// Extracted from raw Z21 bytes if available, otherwise returns stored Speed value.
    /// Setting this value updates the raw bytes automatically.
    /// </summary>
    public int Speed
    {
        get
        {
            var decoded = DecodedCommand;
            if (decoded?.IsValid == true)
                return decoded.Speed;

            return Cmd.Speed ?? 0;
        }
        set
        {
            var currentValue = Speed;
            if (currentValue == value)
                return;

            Cmd.Speed = value;
            OnPropertyChanged(nameof(Speed));
            UpdateBytesFromProperties();
            OnPropertyChanged(nameof(BytesHex));
        }
    }

    /// <summary>
    /// Direction: "Forward" or "Backward".
    /// Extracted from raw Z21 bytes if available, otherwise returns stored Direction value.
    /// Setting this value updates the raw bytes automatically.
    /// </summary>
    public string Direction
    {
        get
        {
            var decoded = DecodedCommand;
            if (decoded?.IsValid == true)
                return decoded.Direction;

            return Cmd.Direction ?? "Forward";
        }
        set
        {
            var currentValue = Direction;
            if (currentValue == value)
                return;

            Cmd.Direction = value;
            OnPropertyChanged(nameof(Direction));
            UpdateBytesFromProperties();
            OnPropertyChanged(nameof(BytesHex));
        }
    }

    /// <summary>
    /// Raw command bytes (optional, for advanced users).
    /// </summary>
    public byte[]? Bytes
    {
        get
        {
            var b64 = Cmd.BytesBase64;
            if (string.IsNullOrEmpty(b64))
                return null;
            try
            {
                return Convert.FromBase64String(b64);
            }
            catch (FormatException)
            {
                return null;
            }
        }
        set
        {
            string? newB64 = value is { Length: > 0 } ? Convert.ToBase64String(value) : null;
            if (Cmd.BytesBase64 == newB64)
                return;

            Cmd.BytesBase64 = newB64;
            _decodedCommand = DecodeBytes();
            OnPropertyChanged(nameof(Bytes));
            OnPropertyChanged(nameof(DecodedCommand));
            OnPropertyChanged(nameof(Address));
            OnPropertyChanged(nameof(Speed));
            OnPropertyChanged(nameof(Direction));
            OnPropertyChanged(nameof(BytesHex));
        }
    }

    /// <summary>
    /// Decoded DCC command information from raw Z21 bytes.
    /// Returns null if bytes cannot be decoded or are empty.
    /// </summary>
    public Z21DccCommandDecoder.DccCommand? DecodedCommand
    {
        get => _decodedCommand;
    }

    /// <summary>
    /// Hexadecimal representation of raw bytes for UI display.
    /// Example: "0A 00 80 00 E4 03 E5 80 12"
    /// </summary>
    public string BytesHex
    {
        get => Z21DccCommandDecoder.FormatBytesAsHex(Bytes);
    }

    /// <summary>
    /// Human-readable summary of the decoded DCC command.
    /// Example: "Addr: 101, Speed: 127, Direction: Forward"
    /// </summary>
    public string CommandSummary
    {
        get
        {
            var decoded = DecodedCommand;
            return decoded?.IsValid == true ? Z21DccCommandDecoder.FormatDccCommand(decoded) : decoded?.ErrorMessage ?? "(No valid command)";
        }
    }

    /// <summary>
    /// Decodes the current <see cref="Bytes"/> value into a <see cref="Z21DccCommandDecoder.DccCommand"/> instance.
    /// </summary>
    private Z21DccCommandDecoder.DccCommand? DecodeBytes()
    {
        var bytes = Bytes;
        return bytes == null || bytes.Length == 0 ? null : Z21DccCommandDecoder.DecodeLocoCommand(bytes);
    }

    /// <summary>
    /// Updates the <see cref="Bytes"/> property from the current <see cref="Address"/>, <see cref="Speed"/>,
    /// and <see cref="Direction"/> values. Called automatically when any of these properties change.
    /// </summary>
    private void UpdateBytesFromProperties()
    {
        try
        {
            int address = Cmd.Address ?? 0;
            int speed = Cmd.Speed ?? 0;
            string direction = Cmd.Direction ?? "Forward";

            byte[] newBytes = Z21DccCommandDecoder.EncodeLocoCommand(address, speed, direction);

            Cmd.BytesBase64 = Convert.ToBase64String(newBytes);

            _decodedCommand = DecodeBytes();
            OnPropertyChanged(nameof(DecodedCommand));
            OnPropertyChanged(nameof(Bytes));
            OnPropertyChanged(nameof(BytesHex));
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error encoding Z21 command from property values");
        }
    }

    /// <summary>
    /// Returns a human-readable description of the Z21 command for debugging and UI display.
    /// </summary>
    /// <returns>A string describing the command.</returns>
    public override string ToString() => !string.IsNullOrEmpty(Name) ? $"{Name} (Command)" : $"Command - Addr:{Address} Speed:{Speed} Dir:{Direction}";
}
