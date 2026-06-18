// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.ViewModel;

using Common.Extension;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Controls.Matrix;

using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

using Moba.SharedUI.ViewModel;

using System.Collections.ObjectModel;
using System.ComponentModel;

using Windows.UI;

public sealed partial class MatrixPageViewModel : ObservableObject, IDisposable
{
    private readonly MainWindowViewModel? mainWindowViewModel;
    private readonly ILogger<MatrixPageViewModel>? logger;
    private bool isLoading;
    private bool isDisposed;

    [ObservableProperty]
    private Color selectedColor = Colors.Red;

    [ObservableProperty]
    private SolidColorBrush selectedColorBrush;

    public ObservableCollection<ViewModel5x5> Matrices { get; } = [];

    public MatrixPageViewModel()
    {
        SelectedColorBrush = new SolidColorBrush(SelectedColor);
    }

    public MatrixPageViewModel(MainWindowViewModel mainWindowViewModel, ILogger<MatrixPageViewModel> logger)
        : this()
    {
        this.mainWindowViewModel = mainWindowViewModel;
        this.logger = logger;
        this.mainWindowViewModel.SolutionLoaded += OnSolutionLoaded;
        this.mainWindowViewModel.PropertyChanged += OnMainWindowViewModelPropertyChanged;
        LoadFromSelectedProject();
    }

    partial void OnSelectedColorChanged(Color value)
    {
        SelectedColorBrush = new SolidColorBrush(value);
        foreach (var matrix in Matrices)
        {
            matrix.SelectedColorBrush = SelectedColorBrush;
        }
    }

    [RelayCommand]
    private void AddMatrix()
    {
        var matrix = CreateMatrixViewModel("New Matrix");
        Matrices.Add(matrix);
        SaveMatricesToProject();
        QueueSaveSolution();
    }

    [RelayCommand]
    private void DeleteMatrix(ViewModel5x5? matrix)
    {
        if (matrix == null)
        {
            return;
        }

        DetachMatrix(matrix);
        Matrices.Remove(matrix);

        SaveMatricesToProject();
        QueueSaveSolution();
    }

    private void OnSolutionLoaded(object? sender, EventArgs e)
    {
        LoadFromSelectedProject();
    }

    private void OnMainWindowViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.SelectedProject))
        {
            LoadFromSelectedProject();
        }
    }

    private void LoadFromSelectedProject()
    {
        isLoading = true;
        try
        {
            foreach (var matrix in Matrices)
            {
                DetachMatrix(matrix);
            }

            Matrices.Clear();

            var project = mainWindowViewModel?.SelectedProject?.Model;
            if (project != null)
            {
                project.Matrices ??= [];
                foreach (var model in project.Matrices)
                {
                    var matrix = ViewModel5x5.FromModel(model);
                    matrix.SelectedColorBrush = SelectedColorBrush;
                    AttachMatrix(matrix);
                    Matrices.Add(matrix);
                }
            }

        }
        finally
        {
            isLoading = false;
        }
    }

    private void SaveMatricesToProject()
    {
        var project = mainWindowViewModel?.SelectedProject?.Model;
        if (project == null)
        {
            return;
        }

        project.Matrices = Matrices.Select(matrix => matrix.ToModel()).ToList();
    }

    private ViewModel5x5 CreateMatrixViewModel(string name)
    {
        var matrix = new ViewModel5x5
        {
            Name = name,
            SelectedColorBrush = SelectedColorBrush
        };

        AttachMatrix(matrix);
        return matrix;
    }

    private void AttachMatrix(ViewModel5x5 matrix)
    {
        matrix.DeleteCommand = DeleteMatrixCommand;
        matrix.PropertyChanged += OnMatrixPropertyChanged;
    }

    private void DetachMatrix(ViewModel5x5 matrix)
    {
        matrix.PropertyChanged -= OnMatrixPropertyChanged;
        matrix.DeleteCommand = null;
    }

    private void OnMatrixPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (isDisposed || isLoading || e.PropertyName == nameof(ViewModel5x5.SelectedColorBrush))
        {
            return;
        }

        SaveMatricesToProject();
        QueueSaveSolution();
    }

    private void QueueSaveSolution()
    {
        mainWindowViewModel?.SaveSolutionInternalAsync().Observe(ex => logger?.LogWarning(ex, "Auto-save matrix changes failed"));
    }

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        isDisposed = true;

        if (mainWindowViewModel != null)
        {
            mainWindowViewModel.SolutionLoaded -= OnSolutionLoaded;
            mainWindowViewModel.PropertyChanged -= OnMainWindowViewModelPropertyChanged;
        }

        foreach (var matrix in Matrices)
        {
            DetachMatrix(matrix);
        }
    }
}