// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Backend.Model;

using CommunityToolkit.Mvvm.ComponentModel;

using System.Collections.ObjectModel;

public partial class SolutionViewModel : ObservableObject
{
    public SolutionViewModel(Solution model)
    {
        Model = model;
        Projects = new ObservableCollection<Project>(model.Projects);
    }

    public string Name
    {
        get => Model.Name;
        set => SetProperty(Model.Name, value, Model, (m, v) => m.Name = v);
    }

    [ObservableProperty]
    private Solution model;

    public ObservableCollection<Project> Projects { get; }
}