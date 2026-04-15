// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Common.Extension;
using SharedUI.ViewModel;

/// <summary>
/// Solution page displaying projects list with properties panel.
/// </summary>
internal sealed partial class SolutionPage
{
    public MainWindowViewModel ViewModel { get; }
    private readonly ILogger<SolutionPage>? _logger;

    public SolutionPage(MainWindowViewModel viewModel, ILogger<SolutionPage>? logger = null)
    {
        ViewModel = viewModel;
        _logger = logger;
        InitializeComponent();
    }

    private void DeleteProjectButton_Click(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        HandleDeleteProjectButtonClickAsync().Observe(ex => _logger?.LogWarning(ex, "Delete project failed"));
    }

    private async Task HandleDeleteProjectButtonClickAsync()
    {
        try
        {
            if (ViewModel.SelectedProject == null)
                return;

            var dialog = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = "Delete Project",
                Content = "Do you really want to delete the project?",
                PrimaryButtonText = "Yes",
                SecondaryButtonText = "No",
                DefaultButton = ContentDialogButton.Secondary
            };

            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                return;

            // Create backup of current solution file (before deletion)
            var solutionPath = ViewModel.CurrentSolutionPath;
            if (!string.IsNullOrEmpty(solutionPath) && File.Exists(solutionPath))
            {
                try
                {
                    var dir = Path.GetDirectoryName(solutionPath);
                    var fileName = Path.GetFileNameWithoutExtension(solutionPath);
                    var ext = Path.GetExtension(solutionPath);
                    var backupPath = Path.Combine(dir ?? string.Empty, $"{fileName}.backup{ext}");
                    File.Copy(solutionPath, backupPath, overwrite: true);
                }
                catch
                {
                    // Backup failed – still perform deletion (user has confirmed)
                }
            }

            if (ViewModel.DeleteProjectCommand.CanExecute(null))
                ViewModel.DeleteProjectCommand.Execute(null);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Delete project failed");
        }
    }
}