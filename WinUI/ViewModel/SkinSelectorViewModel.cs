// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.ViewModel;

using Common.Configuration;

using CommunityToolkit.Mvvm.Input;

using Microsoft.Extensions.Logging;

using Service;

using SharedUI.Interface;

/// <summary>
/// ViewModel for selecting application skins across WinUI pages.
/// </summary>
public sealed class SkinSelectorViewModel
{
    private readonly ISkinProvider _skinProvider;
    private readonly AppSettings _settings;
    private readonly ISettingsService? _settingsService;
    private readonly ILogger<SkinSelectorViewModel>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SkinSelectorViewModel"/> class.
    /// </summary>
    public SkinSelectorViewModel(
        ISkinProvider skinProvider,
        AppSettings settings,
        ISettingsService? settingsService = null,
        ILogger<SkinSelectorViewModel>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(skinProvider);
        ArgumentNullException.ThrowIfNull(settings);

        _skinProvider = skinProvider;
        _settings = settings;
        _settingsService = settingsService;
        _logger = logger;

        SelectSystemCommand = new AsyncRelayCommand(() => SetSkinAsync(AppSkin.System));
        SelectBlueCommand = new AsyncRelayCommand(() => SetSkinAsync(AppSkin.Blue));
        SelectGreenCommand = new AsyncRelayCommand(() => SetSkinAsync(AppSkin.Green));
        SelectOrangeCommand = new AsyncRelayCommand(() => SetSkinAsync(AppSkin.Orange));
        SelectRedCommand = new AsyncRelayCommand(() => SetSkinAsync(AppSkin.Red));
    }

    /// <summary>
    /// Command to select the system skin.
    /// </summary>
    public IAsyncRelayCommand SelectSystemCommand { get; }

    /// <summary>
    /// Command to select the blue skin.
    /// </summary>
    public IAsyncRelayCommand SelectBlueCommand { get; }

    /// <summary>
    /// Command to select the green skin.
    /// </summary>
    public IAsyncRelayCommand SelectGreenCommand { get; }

    /// <summary>
    /// Command to select the orange skin.
    /// </summary>
    public IAsyncRelayCommand SelectOrangeCommand { get; }

    /// <summary>
    /// Command to select the red skin.
    /// </summary>
    public IAsyncRelayCommand SelectRedCommand { get; }

    private async Task SetSkinAsync(AppSkin skin)
    {
        _skinProvider.SetSkin(skin);

        _settings.Application.SelectedSkin = skin.ToString();

        if (_settingsService != null)
        {
            await _settingsService.SaveSettingsAsync(_settings).ConfigureAwait(false);
        }
        else
        {
            _logger?.LogDebug("Settings service unavailable; skin selection was not persisted.");
        }
    }
}
