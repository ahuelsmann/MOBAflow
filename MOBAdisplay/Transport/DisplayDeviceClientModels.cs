// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Display.Transport;

using Moba.Display.Protocol;

/// <summary>
/// Provides capability-aware operations for one explicitly configured display endpoint.
/// </summary>
public interface IDisplayDeviceClient : IDisposable
{
    /// <summary>Opens the endpoint and negotiates a new live protocol session.</summary>
    /// <param name="endpoint">Validated display endpoint.</param>
    /// <param name="cancellationToken">Stops the connection attempt.</param>
    /// <returns>The negotiated capabilities or a structured failure.</returns>
    Task<DisplayDeviceNegotiationResult> ConnectAsync(
        DisplayEndpoint endpoint,
        CancellationToken cancellationToken = default);

    /// <summary>Queries health through the current negotiated session.</summary>
    /// <param name="cancellationToken">Stops the health request.</param>
    /// <returns>The current health payload or a structured failure.</returns>
    Task<DisplayDeviceHealthResult> QueryHealthAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Sends the host-rendered protocol conformance pattern.</summary>
    /// <param name="cancellationToken">Stops the frame transfer.</param>
    /// <returns>The presentation result.</returns>
    Task<DisplayDeviceOperationResult> SendStandardTestPatternAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Requests the optional device-rendered conformance pattern.</summary>
    /// <param name="cancellationToken">Stops the request.</param>
    /// <returns>The command result, including unsupported capability.</returns>
    Task<DisplayDeviceOperationResult> RenderBuiltInTestPatternAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Applies an optional device brightness percentage.</summary>
    /// <param name="percentage">Brightness from zero through 100.</param>
    /// <param name="cancellationToken">Stops the request.</param>
    /// <returns>The command result, including unsupported capability.</returns>
    Task<DisplayDeviceOperationResult> SetBrightnessAsync(
        byte percentage,
        CancellationToken cancellationToken = default);

    /// <summary>Discards the current transport, session, and live capabilities.</summary>
    void Disconnect();
}

/// <summary>
/// Reports a capability negotiation outcome without exposing packet data.
/// </summary>
/// <param name="Capabilities">Validated live capabilities when negotiation succeeded.</param>
/// <param name="RequestFailure">Host request failure, if any.</param>
/// <param name="ResultCode">Structured device result, if any.</param>
/// <param name="Diagnostic">Safe diagnostic text without packet or credential data.</param>
public sealed record DisplayDeviceNegotiationResult(
    CapabilitiesResponsePayload? Capabilities,
    DisplayRequestFailure RequestFailure,
    DisplayResultCode? ResultCode,
    string? Diagnostic)
{
    /// <summary>Gets whether negotiation produced validated live capabilities.</summary>
    public bool IsSuccessful =>
        Capabilities is not null
        && RequestFailure == DisplayRequestFailure.None
        && ResultCode is null;

    /// <summary>Creates a successful negotiation result.</summary>
    /// <param name="capabilities">Validated live capabilities.</param>
    /// <returns>A successful result.</returns>
    public static DisplayDeviceNegotiationResult Succeeded(CapabilitiesResponsePayload capabilities)
    {
        ArgumentNullException.ThrowIfNull(capabilities);
        return new(capabilities, DisplayRequestFailure.None, null, null);
    }

    /// <summary>Creates a failed negotiation result.</summary>
    /// <param name="requestFailure">Host request failure.</param>
    /// <param name="diagnostic">Safe diagnostic text.</param>
    /// <param name="resultCode">Structured device result, if available.</param>
    /// <returns>A failed result.</returns>
    public static DisplayDeviceNegotiationResult Failed(
        DisplayRequestFailure requestFailure,
        string? diagnostic,
        DisplayResultCode? resultCode = null) =>
        new(null, requestFailure, resultCode, diagnostic);
}

/// <summary>
/// Reports a health query outcome without exposing packet data.
/// </summary>
/// <param name="Health">Health payload when the query succeeded.</param>
/// <param name="RequestFailure">Host request failure, if any.</param>
/// <param name="ResultCode">Structured device result, if any.</param>
/// <param name="Diagnostic">Safe diagnostic text without packet or credential data.</param>
public sealed record DisplayDeviceHealthResult(
    HealthResponsePayload? Health,
    DisplayRequestFailure RequestFailure,
    DisplayResultCode? ResultCode,
    string? Diagnostic)
{
    /// <summary>Gets whether a valid health payload was received.</summary>
    public bool IsSuccessful =>
        Health.HasValue
        && RequestFailure == DisplayRequestFailure.None
        && ResultCode is null;

    /// <summary>Creates a successful health result.</summary>
    /// <param name="health">Validated health payload.</param>
    /// <returns>A successful result.</returns>
    public static DisplayDeviceHealthResult Succeeded(HealthResponsePayload health) =>
        new(health, DisplayRequestFailure.None, null, null);

    /// <summary>Creates a failed health result.</summary>
    /// <param name="requestFailure">Host request failure.</param>
    /// <param name="diagnostic">Safe diagnostic text.</param>
    /// <param name="resultCode">Structured device result, if available.</param>
    /// <returns>A failed result.</returns>
    public static DisplayDeviceHealthResult Failed(
        DisplayRequestFailure requestFailure,
        string? diagnostic,
        DisplayResultCode? resultCode = null) =>
        new(null, requestFailure, resultCode, diagnostic);
}

/// <summary>
/// Reports one optional command or test-frame outcome.
/// </summary>
/// <param name="RequestFailure">Host request failure, if any.</param>
/// <param name="ResultCode">Structured device result, if any.</param>
/// <param name="Diagnostic">Safe diagnostic text without packet or credential data.</param>
public sealed record DisplayDeviceOperationResult(
    DisplayRequestFailure RequestFailure,
    DisplayResultCode? ResultCode,
    string? Diagnostic)
{
    /// <summary>Gets whether the device confirmed successful completion.</summary>
    public bool IsSuccessful =>
        RequestFailure == DisplayRequestFailure.None
        && ResultCode == DisplayResultCode.Ok;

    /// <summary>Creates a successful operation result.</summary>
    /// <returns>A successful result.</returns>
    public static DisplayDeviceOperationResult Succeeded() =>
        new(DisplayRequestFailure.None, DisplayResultCode.Ok, null);

    /// <summary>Creates a failed operation result.</summary>
    /// <param name="requestFailure">Host request failure.</param>
    /// <param name="diagnostic">Safe diagnostic text.</param>
    /// <param name="resultCode">Structured device result, if available.</param>
    /// <returns>A failed result.</returns>
    public static DisplayDeviceOperationResult Failed(
        DisplayRequestFailure requestFailure,
        string? diagnostic,
        DisplayResultCode? resultCode = null) =>
        new(requestFailure, resultCode, diagnostic);

    /// <summary>Creates a result for a capability the device did not advertise.</summary>
    /// <param name="diagnostic">Actionable unsupported-capability explanation.</param>
    /// <returns>An unsupported result.</returns>
    public static DisplayDeviceOperationResult Unsupported(string diagnostic) =>
        new(DisplayRequestFailure.None, DisplayResultCode.Unsupported, diagnostic);
}
