// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

public partial class MainWindowViewModel
{
    public Task SendTurnoutCommandAsync(int decoderAddress, int output, bool activate, bool queue = false, CancellationToken cancellationToken = default)
        => _mobaRuntime.SendTurnoutCommandAsync(decoderAddress, output, activate, queue, cancellationToken);
}