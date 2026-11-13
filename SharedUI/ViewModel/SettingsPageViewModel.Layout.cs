// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public sealed partial class SettingsPageViewModel
{
    [ObservableProperty]
    public partial string LayoutResetTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LayoutResetMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsLayoutResetSuccessful { get; set; }

    [RelayCommand]
    private async Task ResetLayoutsAsync()
    {
        try
        {
            await _layout.ResetAsync().ConfigureAwait(true);
            IsLayoutResetSuccessful = true;
            LayoutResetTitle = "Layouts reset";
            LayoutResetMessage = "Saved layouts were reset. Reopen pages to apply their default layout.";
        }
        catch (Exception ex)
        {
            IsLayoutResetSuccessful = false;
            LayoutResetTitle = "Layout reset failed";
            LayoutResetMessage = ex.Message;
        }
    }
}
