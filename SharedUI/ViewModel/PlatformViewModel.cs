// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Domain;

using Interface;

/// <summary>
/// ViewModel wrapper for a station platform with workflow assignment operations.
/// </summary>
public sealed partial class PlatformViewModel : ObservableObject, IViewModelWrapper<Platform>
{
    private readonly Platform _platform;
    private readonly Project _project;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlatformViewModel"/> class.
    /// </summary>
    public PlatformViewModel(Platform platform, Project project)
    {
        ArgumentNullException.ThrowIfNull(platform);
        ArgumentNullException.ThrowIfNull(project);
        _platform = platform;
        _project = project;
    }

    /// <summary>
    /// Gets the underlying platform model.
    /// </summary>
    public Platform Model => _platform;

    /// <summary>
    /// Gets or sets the optional platform display name.
    /// </summary>
    public string? Name
    {
        get => _platform.Name;
        set
        {
            if (SetProperty(_platform.Name, value, _platform, (m, v) => m.Name = v))
            {
                OnPropertyChanged(nameof(Summary));
            }
        }
    }

    /// <summary>
    /// Gets or sets the platform number.
    /// </summary>
    public int Number
    {
        get => (int)_platform.Number;
        set
        {
            if (SetProperty(_platform.Number, (uint)value, _platform, (m, v) => m.Number = v))
            {
                OnPropertyChanged(nameof(Summary));
            }
        }
    }

    /// <summary>
    /// Gets or sets the feedback input port used to detect this platform.
    /// </summary>
    public int InPort
    {
        get => (int)_platform.InPort;
        set => SetProperty(_platform.InPort, (uint)value, _platform, (m, v) => m.InPort = v);
    }

    /// <summary>
    /// Gets or sets the workflow assigned to this platform.
    /// </summary>
    public Guid? WorkflowId
    {
        get => _platform.WorkflowId;
        set
        {
            if (SetProperty(_platform.WorkflowId, value, _platform, (m, v) => m.WorkflowId = v))
            {
                OnPropertyChanged(nameof(WorkflowName));
            }
        }
    }

    /// <summary>
    /// Gets the platform summary.
    /// </summary>
    public string Summary => _platform.Summary;

    /// <summary>
    /// Gets the name of the assigned workflow, or a placeholder if none is assigned.
    /// </summary>
    public string WorkflowName
    {
        get
        {
            if (_platform.WorkflowId == null) return "(Drop workflow here)";
            var workflow = _project.Workflows.FirstOrDefault(w => w.Id == _platform.WorkflowId);
            return workflow?.Name ?? "(Unknown workflow)";
        }
    }

    [RelayCommand]
    private void AssignWorkflow(WorkflowViewModel? workflow)
    {
        if (workflow == null) return;
        WorkflowId = workflow.Model.Id;
    }
}
