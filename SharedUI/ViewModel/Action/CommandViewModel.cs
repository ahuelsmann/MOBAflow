// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel.Action;

using Domain;
using Domain.Enum;
using Helper;
using System.Diagnostics;

/// <summary>
/// ViewModel for Z21 Command actions (loco control).
/// Wraps WorkflowAction with typed properties for Address, Speed, Direction.
/// Provides decoding of raw Z21 DCC command bytes into human-readable format.
/// </summary>
public class CommandViewModel : WorkflowActionViewModel
{
    #region Fields
    private Z21DccCommandDecoder.DccCommand? _decodedCommand;
    #endregion

    public CommandViewModel(WorkflowAction action) : base(action, ActionType.Command) 
    {
        // Decode bytes on initialization
        _decodedCommand = DecodeBytes();
    }

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
            // Priority: decoded from bytes
            var decoded = DecodedCommand;
            if (decoded?.IsValid == true)
                return decoded.Address;
            
            // Fallback: stored parameter
            return GetParameter<int>("Address");
        }
        set
        {
            var currentValue = Address;
            if (currentValue == value)
                return;

            SetParameter("Address", value);
            // ReSharper disable once RedundantArgumentDefaultValue
            OnPropertyChanged(nameof(Address));  // Explicit notification for NumberBox
            UpdateBytesFromProperties();  // Auto-update bytes
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
            // Priority: decoded from bytes
            var decoded = DecodedCommand;
            if (decoded?.IsValid == true)
                return decoded.Speed;

            // Fallback: stored parameter
            return GetParameter<int>("Speed");
        }
        set
        {
            var currentValue = Speed;
            if (currentValue == value)
                return;

            SetParameter("Speed", value);
            // ReSharper disable once RedundantArgumentDefaultValue
            OnPropertyChanged(nameof(Speed));  // Explicit notification for NumberBox
            UpdateBytesFromProperties();  // Auto-update bytes
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
            // Priority: decoded from bytes
            var decoded = DecodedCommand;
            if (decoded?.IsValid == true)
                return decoded.Direction;

            // Fallback: stored parameter
            return GetParameter<string>("Direction") ?? "Forward";
        }
        set
        {
            var currentValue = Direction;
            if (currentValue == value)
                return;

            SetParameter("Direction", value);
            // ReSharper disable once RedundantArgumentDefaultValue
            OnPropertyChanged(nameof(Direction));  // Explicit notification for ComboBox
            UpdateBytesFromProperties();  // Auto-update bytes
            OnPropertyChanged(nameof(BytesHex));
        }
    }

    /// <summary>
    /// Raw command bytes (optional, for advanced users).
    /// </summary>
    public byte[]? Bytes
    {
        get => GetParameter<byte[]>("Bytes");
        set
        {
            SetParameter("Bytes", value);
            // Update decoded command when bytes change
            _decodedCommand = DecodeBytes();
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
            if (decoded?.IsValid == true)
                return Z21DccCommandDecoder.FormatDccCommand(decoded);
            
            return decoded?.ErrorMessage ?? "(No valid command)";
        }
    }

        /// <summary>
        /// Decodes the current Bytes property.
        /// </summary>
        private Z21DccCommandDecoder.DccCommand? DecodeBytes()
        {
            var bytes = Bytes;
            if (bytes == null || bytes.Length == 0)
                return null;

            return Z21DccCommandDecoder.DecodeLocoCommand(bytes);
        }

        /// <summary>
        /// Updates the Bytes property from current Address, Speed, and Direction values.
        /// Called automatically when any of these properties change.
        /// </summary>
        private void UpdateBytesFromProperties()
        {
            try
            {
                // Get current values (use stored parameters, not decoded values to avoid circular reference)
                int address = GetParameter<int>("Address");
                int speed = GetParameter<int>("Speed");
                string direction = GetParameter<string>("Direction") ?? "Forward";

                // Generate new bytes
                byte[] newBytes = Z21DccCommandDecoder.EncodeLocoCommand(address, speed, direction);

                // Update bytes (suppress property change notifications to avoid recursion)
                SetParameter("Bytes", newBytes);

                // Update decoded command
                _decodedCommand = DecodeBytes();
                OnPropertyChanged(nameof(DecodedCommand));
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                Debug.WriteLine($"Error encoding Z21 command: {ex.Message}");
            }
        }

        public override string ToString() => !string.IsNullOrEmpty(Name) ? $"{Name} (Command)" : $"Command - Addr:{Address} Speed:{Speed} Dir:{Direction}";
    }
