// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Domain;

using Enum;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Reads execution fields from typed <see cref="WorkflowAction"/> payloads (command / audio).
/// </summary>
public static class WorkflowActionParameterBinding
{
    /// <summary>
    /// Tries to read command bytes for a <see cref="ActionType.Command"/> action from <see cref="WorkflowAction.Command"/>.
    /// </summary>
    public static bool TryGetCommandBytes(WorkflowAction action, [NotNullWhen(true)] out byte[]? bytes)
    {
        bytes = null;
        var b64 = action.Command?.BytesBase64;
        if (string.IsNullOrWhiteSpace(b64))
            return false;

        try
        {
            bytes = Convert.FromBase64String(b64);
            return bytes.Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    /// <summary>
    /// Tries to read the audio file path for an <see cref="ActionType.Audio"/> action.
    /// </summary>
    public static bool TryGetAudioFilePath(WorkflowAction action, [NotNullWhen(true)] out string? filePath)
    {
        filePath = action.Audio?.FilePath;
        return !string.IsNullOrWhiteSpace(filePath);
    }

    /// <summary>
    /// Tries to read the configured display device ID for a <see cref="ActionType.TrainDestinationDisplay"/> action.
    /// </summary>
    public static bool TryGetDisplayDeviceId(WorkflowAction action, out Guid displayDeviceId)
    {
        displayDeviceId = action.TrainDestinationDisplay?.DisplayDeviceId ?? Guid.Empty;
        return displayDeviceId != Guid.Empty;
    }
}
