// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.SharedUI.Interface;

using System.ComponentModel;

using ViewModel;

/// <summary>
/// Narrow selection context for journey map and related views.
/// </summary>
public interface IJourneySelectionContext : INotifyPropertyChanged
{
    ProjectViewModel? SelectedProject { get; set; }

    JourneyViewModel? SelectedJourney { get; set; }
}