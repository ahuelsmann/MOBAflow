// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Interface;

public interface ILocomotiveFunctionCommandGateway
{
    bool IsConnected { get; }

    Task SetFunctionAsync(int address, int functionIndex, bool isOn, CancellationToken cancellationToken = default);
}

public sealed class MobaRuntimeLocomotiveFunctionCommandGateway(IMobaRuntime runtime) : ILocomotiveFunctionCommandGateway
{
    private readonly IMobaRuntime _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));

    public bool IsConnected => _runtime.Current.IsConnected;

    public Task SetFunctionAsync(int address, int functionIndex, bool isOn, CancellationToken cancellationToken = default)
        => _runtime.SetLocomotiveFunctionAsync(address, functionIndex, isOn, cancellationToken);
}
