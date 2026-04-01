namespace Moba.WinUI.View;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Moba.SharedUI.ViewModel;

internal sealed partial class SignalMultiplexTestPage
{
    public MainWindowViewModel ViewModel { get; }

    public SignalMultiplexTestPage(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    private async void OnTrackPowerOnClick(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        await ViewModel.SetTrackPowerCommand.ExecuteAsync(true);
        CommandStatusText.Text = "Track Power EIN angefordert.";
    }

    private async void OnTrackPowerOffClick(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        await ViewModel.SetTrackPowerCommand.ExecuteAsync(false);
        CommandStatusText.Text = "Track Power AUS angefordert.";
    }

    private async void OnSendRawClick(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;

        var decoderAddress = (int)DecoderAddressBox.Value;
        var output = GetSelectedOutput();
        var activate = ActivateToggle.IsOn;
        var queue = QueueToggle.IsOn;

        await SendTurnoutCommandAsync("Rohbefehl", decoderAddress, output, activate, queue);
    }

    private async void OnSendPresetClick(object sender, RoutedEventArgs e)
    {
        _ = e;

        if (sender is not Button { Tag: string tag })
            return;

        var parts = tag.Split('|');
        if (parts.Length != 4)
            return;

        if (!int.TryParse(parts[1], out var decoderAddress))
            return;

        if (!int.TryParse(parts[2], out var output))
            return;

        if (!bool.TryParse(parts[3], out var activate))
            return;

        DecoderAddressBox.Value = decoderAddress;
        OutputComboBox.SelectedIndex = output;
        ActivateToggle.IsOn = activate;

        await SendTurnoutCommandAsync(parts[0], decoderAddress, output, activate, QueueToggle.IsOn);
    }

    private void OnClearTrafficClick(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        ViewModel.ClearTrafficMonitorCommand.Execute(null);
        CommandStatusText.Text = "Traffic-Monitor geleert.";
    }

    private int GetSelectedOutput()
    {
        if (OutputComboBox.SelectedItem is ComboBoxItem { Tag: int output })
            return output;

        if (OutputComboBox.SelectedItem is ComboBoxItem { Tag: string outputText } && int.TryParse(outputText, out output))
            return output;

        return OutputComboBox.SelectedIndex <= 0 ? 0 : 1;
    }

    private async Task SendTurnoutCommandAsync(string label, int decoderAddress, int output, bool activate, bool queue)
    {
        try
        {
            await ViewModel.SendTurnoutCommandAsync(decoderAddress, output, activate, queue);
            CommandStatusText.Text = $"{label}: DCC-Adresse {decoderAddress}, Ausgang {output}, Activate={activate}, Queue={queue}";
        }
        catch (Exception ex)
        {
            CommandStatusText.Text = $"{label}: Fehler beim Senden - {ex.Message}";
        }
    }
}
