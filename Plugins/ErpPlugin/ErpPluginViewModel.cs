// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Moba.SharedUI.ViewModel;

using System.Collections.ObjectModel;

namespace Moba.Plugin.Erp;

/// <summary>
/// ViewModel for the MOBAerp Transaction Page.
/// Provides transaction-based navigation with command history and status tracking.
/// </summary>
public sealed partial class ErpPluginViewModel : ObservableObject
{
    private readonly MainWindowViewModel _mainViewModel;

    [ObservableProperty]
    private string _transactionCode = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "MOBAerp System Ready";

    public ObservableCollection<CommandHistoryItem> CommandHistory { get; } = [];

    public bool IsZ21Connected => _mainViewModel.IsConnected;

    public ErpPluginViewModel(MainWindowViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
    }

    [RelayCommand]
    private void ExecuteTransaction()
    {
        if (string.IsNullOrWhiteSpace(TransactionCode))
        {
            StatusMessage = "Please enter a transaction code";
            return;
        }

        var normalizedCommand = TransactionCode.Trim().ToUpperInvariant();
        var navigationTag = TransactionCodeMapper.MapToNavigationTag(normalizedCommand);

        if (navigationTag != null)
        {
            AddToHistory(TransactionCode, "OK");
            StatusMessage = $"Transaction {TransactionCode} executed successfully";

            // Request navigation via MainWindowViewModel event
            _mainViewModel.RequestNavigation(navigationTag);

            TransactionCode = string.Empty;
        }
        else
        {
            AddToHistory(TransactionCode, "Error");
            StatusMessage = $"Unknown transaction code: {TransactionCode}";
        }
    }

    [RelayCommand]
    private void ClearTransactionCode()
    {
        TransactionCode = string.Empty;
        StatusMessage = "Command cancelled";
    }

    [RelayCommand]
    private void SelectHistoryItem(CommandHistoryItem? item)
    {
        if (item != null)
        {
            TransactionCode = item.Command;
        }
    }

    [RelayCommand]
    private void ExecuteFromTreeView(string? content)
    {
        if (content == null || !content.Contains(" - "))
        {
            StatusMessage = $"Selected: {content}";
            return;
        }

        var code = content.Split(" - ")[0];
        TransactionCode = code;
        ExecuteTransaction();
    }

    private void AddToHistory(string command, string status)
    {
        CommandHistory.Insert(0, new CommandHistoryItem
        {
            Timestamp = DateTime.Now.ToString("HH:mm:ss"),
            Command = command,
            Status = status
        });

        if (CommandHistory.Count > 50)
        {
            CommandHistory.RemoveAt(CommandHistory.Count - 1);
        }
    }
}
