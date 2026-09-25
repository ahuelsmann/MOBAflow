// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using System.Diagnostics;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text.Json;

namespace Moba.Common.Security;

/// <summary>
/// Defines the one-launch named-pipe protocol used to enroll the local MOBAflow host.
/// </summary>
public static class HostBootstrapProtocol
{
    public const string RequestPipeEnvironmentVariable = "MOBAFLOW_HOST_BOOTSTRAP_REQUEST_PIPE";
    public const string ResponsePipeEnvironmentVariable = "MOBAFLOW_HOST_BOOTSTRAP_RESPONSE_PIPE";
    public const string ParentProcessEnvironmentVariable = "MOBAFLOW_HOST_PARENT_PROCESS_ID";

    public static string CreateSecret()
    {
        var encoded = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        return encoded.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}

public sealed record HostBootstrapPipeRequest(string Secret, int ParentProcessId);

public sealed record HostBootstrapPipeResponse(string PublicKeyFingerprint, string ServerInstanceId);

public sealed record HostBootstrapRequest(string Secret);

public sealed record HostRenewalRequest(string CredentialId, string RenewalToken);

public sealed record HostTokenResponse(
    string CredentialId,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RenewalToken);

/// <summary>
/// Owns the parent side of the one-launch bootstrap channel.
/// </summary>
public sealed class HostBootstrapParentChannel : IAsyncDisposable
{
    private readonly string _requestPipeName = CreatePipeName("request");
    private readonly string _responsePipeName = CreatePipeName("response");
    private readonly NamedPipeServerStream _requestPipe;
    private readonly NamedPipeServerStream _responsePipe;
    private string? _secret = HostBootstrapProtocol.CreateSecret();
    private bool _childProcessStarted;

    public HostBootstrapParentChannel()
    {
        _requestPipe = CreateServerPipe(_requestPipeName, PipeDirection.Out);
        _responsePipe = CreateServerPipe(_responsePipeName, PipeDirection.In);
    }

    public void Configure(ProcessStartInfo startInfo)
    {
        ArgumentNullException.ThrowIfNull(startInfo);
        startInfo.Environment[HostBootstrapProtocol.RequestPipeEnvironmentVariable] = _requestPipeName;
        startInfo.Environment[HostBootstrapProtocol.ResponsePipeEnvironmentVariable] = _responsePipeName;
        startInfo.Environment[HostBootstrapProtocol.ParentProcessEnvironmentVariable] = Environment.ProcessId.ToString(
            System.Globalization.CultureInfo.InvariantCulture);
    }

    public void CompleteProcessStart()
    {
        _childProcessStarted = true;
    }

    public async Task<(string Secret, HostBootstrapPipeResponse Response)> ExchangeAsync(
        CancellationToken cancellationToken)
    {
        if (!_childProcessStarted || string.IsNullOrEmpty(_secret))
            throw new InvalidOperationException("The bootstrap channel has not been transferred to the child process.");

        await _requestPipe.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
        var request = new HostBootstrapPipeRequest(_secret, Environment.ProcessId);
        await JsonSerializer.SerializeAsync(_requestPipe, request, cancellationToken: cancellationToken).ConfigureAwait(false);
        await _requestPipe.FlushAsync(cancellationToken).ConfigureAwait(false);
        await _requestPipe.DisposeAsync().ConfigureAwait(false);

        await _responsePipe.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
        var response = await JsonSerializer.DeserializeAsync<HostBootstrapPipeResponse>(
            _responsePipe,
            cancellationToken: cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidDataException("MOBApi returned an empty host bootstrap response.");
        return (_secret, response);
    }

    public ValueTask DisposeAsync()
    {
        _secret = null;
        _requestPipe.Dispose();
        _responsePipe.Dispose();
        return ValueTask.CompletedTask;
    }

    private static string CreatePipeName(string direction)
        => $"mobaflow-host-bootstrap-{direction}-{Guid.NewGuid():N}";

    private static NamedPipeServerStream CreateServerPipe(string name, PipeDirection direction)
        => new(
            name,
            direction,
            maxNumberOfServerInstances: 1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
}

/// <summary>
/// Owns the child side of the named bootstrap channel.
/// </summary>
public sealed class HostBootstrapChildChannel : IAsyncDisposable
{
    private readonly NamedPipeClientStream _requestPipe;
    private readonly NamedPipeClientStream _responsePipe;

    private HostBootstrapChildChannel(string requestPipeName, string responsePipeName)
    {
        _requestPipe = CreateClientPipe(requestPipeName, PipeDirection.In);
        _responsePipe = CreateClientPipe(responsePipeName, PipeDirection.Out);
    }

    public static HostBootstrapChildChannel? TryOpenFromEnvironment()
    {
        var requestHandle = Environment.GetEnvironmentVariable(HostBootstrapProtocol.RequestPipeEnvironmentVariable);
        var responseHandle = Environment.GetEnvironmentVariable(HostBootstrapProtocol.ResponsePipeEnvironmentVariable);
        Environment.SetEnvironmentVariable(HostBootstrapProtocol.RequestPipeEnvironmentVariable, null);
        Environment.SetEnvironmentVariable(HostBootstrapProtocol.ResponsePipeEnvironmentVariable, null);
        Environment.SetEnvironmentVariable(HostBootstrapProtocol.ParentProcessEnvironmentVariable, null);

        return string.IsNullOrWhiteSpace(requestHandle) || string.IsNullOrWhiteSpace(responseHandle)
            ? null
            : new HostBootstrapChildChannel(requestHandle, responseHandle);
    }

    public async Task<HostBootstrapPipeRequest> ReadRequestAsync(CancellationToken cancellationToken)
    {
        await _requestPipe.ConnectAsync(cancellationToken).ConfigureAwait(false);
        var request = await JsonSerializer.DeserializeAsync<HostBootstrapPipeRequest>(
            _requestPipe,
            cancellationToken: cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidDataException("MOBAflow supplied an empty host bootstrap request.");
        if (request.Secret.Length != 43 || request.ParentProcessId <= 0)
            throw new InvalidDataException("MOBAflow supplied an invalid host bootstrap request.");

        return request;
    }

    public async Task WriteResponseAsync(HostBootstrapPipeResponse response, CancellationToken cancellationToken)
    {
        await _responsePipe.ConnectAsync(cancellationToken).ConfigureAwait(false);
        await JsonSerializer.SerializeAsync(_responsePipe, response, cancellationToken: cancellationToken).ConfigureAwait(false);
        await _responsePipe.FlushAsync(cancellationToken).ConfigureAwait(false);
        await _responsePipe.DisposeAsync().ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        await _requestPipe.DisposeAsync().ConfigureAwait(false);
        await _responsePipe.DisposeAsync().ConfigureAwait(false);
    }

    private static NamedPipeClientStream CreateClientPipe(string name, PipeDirection direction)
        => new(".", name, direction, PipeOptions.Asynchronous);
}