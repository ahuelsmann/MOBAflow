// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.Runtime;

using Domain;

/// <summary>
/// Checks remote runtime commands against the value ranges the Z21 runtime accepts,
/// so invalid values are rejected before they reach the host.
/// </summary>
public static class RuntimeCommandValidator
{
    /// <summary>Lowest DCC locomotive address.</summary>
    public const int MinLocomotiveAddress = 1;

    /// <summary>Highest DCC locomotive address.</summary>
    public const int MaxLocomotiveAddress = 9999;

    /// <summary>Highest speed step for 128 speed steps (0 = stop).</summary>
    public const int MaxSpeed = 126;

    /// <summary>Highest locomotive function index (F31).</summary>
    public const int MaxFunctionIndex = 31;

    /// <summary>
    /// Validates the fields that belong to the command type.
    /// </summary>
    /// <param name="command">The remote command to check.</param>
    /// <param name="error">An English reason when the command is invalid; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the command may be executed.</returns>
    public static bool TryValidate(RuntimeCommandEnvelope command, out string? error)
    {
        ArgumentNullException.ThrowIfNull(command);

        error = ValidateProject(command.ProjectId) ?? command.Type switch
        {
            RuntimeCommandType.SetLocomotiveDrive => ValidateAddress(command.LocomotiveAddress)
                ?? ValidateRange(command.Speed, 0, MaxSpeed, "Speed")
                ?? (command.Forward is null ? "Forward is required." : null),
            RuntimeCommandType.SetLocomotiveFunction => ValidateAddress(command.LocomotiveAddress)
                ?? ValidateRange(command.FunctionIndex, 0, MaxFunctionIndex, "FunctionIndex")
                ?? (command.FunctionIsOn is null ? "FunctionIsOn is required." : null),
            RuntimeCommandType.SetSignalAspect => ValidateIdentifier(command.SignalId, "SignalId")
                ?? ValidateSignalAspect(command.SignalAspect),
            RuntimeCommandType.ResetJourney => ValidateIdentifier(command.JourneyId, "JourneyId"),
            _ => "Unknown command type."
        };

        return error is null;
    }

    private static string? ValidateAddress(int? address) =>
        ValidateRange(address, MinLocomotiveAddress, MaxLocomotiveAddress, "Address");

    private static string? ValidateRange(int? value, int minimum, int maximum, string name) => value switch
    {
        null => $"{name} is required.",
        _ when value < minimum || value > maximum => $"{name} must be between {minimum} and {maximum}.",
        _ => null
    };

    private static string? ValidateProject(Guid projectId) =>
        projectId == Guid.Empty ? "ProjectId is required." : null;

    private static string? ValidateIdentifier(Guid? identifier, string name) =>
        identifier is null || identifier == Guid.Empty ? $"{name} is required." : null;

    private static string? ValidateSignalAspect(SignalAspect? aspect) => aspect switch
    {
        null => "SignalAspect is required.",
        _ when !Enum.IsDefined(aspect.Value) => "SignalAspect is not a known aspect.",
        _ => null
    };
}
