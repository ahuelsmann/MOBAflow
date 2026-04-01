namespace Moba.SharedUI.ViewModel;

public partial class MainWindowViewModel
{
    public Task SendTurnoutCommandAsync(int decoderAddress, int output, bool activate, bool queue = false, CancellationToken cancellationToken = default)
        => _mobaClient.SendTurnoutCommandAsync(decoderAddress, output, activate, queue, cancellationToken);
}
