// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Common.Navigation;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using SharedUI.ViewModel;

using System.IO;

/// <summary>
/// Solution page displaying projects list with properties panel.
/// </summary>
[NavigationItem(
    Tag = "solution",
    Title = "Solution",
    Icon = "\uE8B7",
    Category = NavigationCategory.Solution,
    Order = 10,
    FeatureToggleKey = "IsSolutionPageAvailable",
    BadgeLabelKey = "SolutionPageLabel")]
internal sealed partial class SolutionPage
{
    public MainWindowViewModel ViewModel { get; }

    public SolutionPage(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    private async void DeleteProjectButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedProject == null)
            return;

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Projekt löschen",
            Content = "Wollen Sie das Projekt wirklich löschen?",
            PrimaryButtonText = "Ja",
            SecondaryButtonText = "Nein",
            DefaultButton = ContentDialogButton.Secondary
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
            return;

        // Backup der aktuellen Solution-Datei erstellen (vor dem Löschen)
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
                // Backup fehlgeschlagen – Löschen trotzdem ausführen (Benutzer hat bestätigt)
            }
        }

        if (ViewModel.DeleteProjectCommand.CanExecute(null))
            ViewModel.DeleteProjectCommand.Execute(null);
    }
}