// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Selector;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Moba.SharedUI.ViewModel;

/// <summary>
/// Selects the appropriate DataTemplate based on the type of entity.
/// Used by ContentControl to display type-specific property editors.
/// </summary>
public class EntityTemplateSelector : DataTemplateSelector
{
    /// <summary>
    /// DataTemplate for JourneyViewModel
    /// </summary>
    public DataTemplate? JourneyTemplate { get; set; }

    /// <summary>
    /// DataTemplate for StationViewModel
    /// </summary>
    public DataTemplate? StationTemplate { get; set; }

    /// <summary>
    /// DataTemplate for WorkflowViewModel
    /// </summary>
    public DataTemplate? WorkflowTemplate { get; set; }

    /// <summary>
    /// DataTemplate for TrainViewModel
    /// </summary>
    public DataTemplate? TrainTemplate { get; set; }

    /// <summary>
    /// DataTemplate for ProjectViewModel
    /// </summary>
    public DataTemplate? ProjectTemplate { get; set; }

    /// <summary>
    /// DataTemplate for LocomotiveViewModel
    /// </summary>
    public DataTemplate? LocomotiveTemplate { get; set; }

    /// <summary>
    /// DataTemplate for WagonViewModel (PassengerWagon/GoodsWagon)
    /// </summary>
    public DataTemplate? WagonTemplate { get; set; }

    /// <summary>
    /// DataTemplate for Action objects (WorkflowAction, etc.)
    /// </summary>
    public DataTemplate? ActionTemplate { get; set; }

    /// <summary>
    /// Fallback DataTemplate for unknown types
    /// </summary>
    public DataTemplate? DefaultTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
    {
        return item switch
        {
            StationViewModel => StationTemplate,
            JourneyViewModel => JourneyTemplate,
            WorkflowViewModel => WorkflowTemplate,
            TrainViewModel => TrainTemplate,
            LocomotiveViewModel => LocomotiveTemplate,
            PassengerWagonViewModel => WagonTemplate,
            GoodsWagonViewModel => WagonTemplate,
            ProjectViewModel => ProjectTemplate,
            Domain.WorkflowAction => ActionTemplate,
            _ => DefaultTemplate
        };
    }
}