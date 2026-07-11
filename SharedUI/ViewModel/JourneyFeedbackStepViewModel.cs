// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using Domain;

/// <summary>Editable wrapper for one ordered journey feedback sequence entry.</summary>
public sealed class JourneyFeedbackStepViewModel(JourneyFeedbackStep model, Project project) : ObservableObject
{
    public JourneyFeedbackStep Model { get; } = model;

    public uint InPort
    {
        get => Model.InPort;
        set => SetProperty(Model.InPort, value, Model, (step, port) => step.InPort = port);
    }

    public Guid? WorkflowId
    {
        get => Model.WorkflowId;
        set => SetProperty(Model.WorkflowId, value, Model, (step, workflowId) => step.WorkflowId = workflowId);
    }

    public IEnumerable<Workflow> AvailableWorkflows => project.Workflows;
}
