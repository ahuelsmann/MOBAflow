// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.SharedUI;

using Moba.Domain;
using Moba.SharedUI.ViewModel;
using Moba.SharedUI.ViewModel.WorkflowSteps;

/// <summary>Verifies named graph editing without coupling execution to list order.</summary>
[TestFixture]
public sealed class WorkflowEditorViewModelTests
{
    [Test]
    public void SelectNextStep_UsesIdentityAndPropagatesOnePersistedChange()
    {
        var start = new WorkflowDelayStep { Name = "Wait" };
        var first = new WorkflowTerminateStep { Name = "Done" };
        var second = new WorkflowTerminateStep { Name = "Done" };
        var workflow = new WorkflowViewModel(new Workflow { EntryStepId = start.Id, Steps = [start, first, second] });
        var changes = 0;
        workflow.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(workflow.Steps)) changes++; };

        workflow.Steps[0].NextStep = workflow.Steps[2];

        Assert.Multiple(() =>
        {
            Assert.That(start.NextStepId, Is.EqualTo(second.Id));
            Assert.That(workflow.Steps[0].NextStep, Is.SameAs(workflow.Steps[2]));
            Assert.That(workflow.Steps[0].ConnectionsSummary, Is.EqualTo("Next → Done (3)"));
            Assert.That(workflow.Steps[1].SelectionLabel, Is.EqualTo("Done (2)"));
            Assert.That(changes, Is.EqualTo(1));
        });
    }

    [Test]
    public void RenameTarget_RefreshesIncomingConnectionWithoutRewiring()
    {
        var target = new WorkflowTerminateStep { Name = "Done" };
        var start = new WorkflowDelayStep { Name = "Wait", NextStepId = target.Id };
        var workflow = new WorkflowViewModel(new Workflow { EntryStepId = start.Id, Steps = [start, target] });
        var summaries = 0;
        workflow.Steps[0].PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(WorkflowStepViewModel.ConnectionsSummary)) summaries++;
        };

        workflow.Steps[1].Name = "Completed";

        Assert.Multiple(() =>
        {
            Assert.That(workflow.Steps[0].ConnectionsSummary, Is.EqualTo("Next → Completed"));
            Assert.That(start.NextStepId, Is.EqualTo(target.Id));
            Assert.That(summaries, Is.EqualTo(1));
        });
    }

    [Test]
    public void MissingConnection_NullOrForeignSelectionDoesNotEraseStoredReference()
    {
        var missingId = Guid.NewGuid();
        var step = new WorkflowDelayStep { NextStepId = missingId };
        var workflow = new WorkflowViewModel(new Workflow { EntryStepId = step.Id, Steps = [step] });
        var editor = workflow.Steps.Single();

        editor.NextStep = null;
        editor.NextStep = new WorkflowDelayStepViewModel(new WorkflowDelayStep());

        Assert.Multiple(() =>
        {
            Assert.That(step.NextStepId, Is.EqualTo(missingId));
            Assert.That(editor.NextStep, Is.Null);
            Assert.That(editor.ConnectionsSummary, Is.EqualTo("Next → Missing step"));
        });
    }

    [Test]
    public void ConditionTargets_ShowBothPathsAndPersistTheirIdentifiers()
    {
        var condition = new WorkflowConditionStep { Name = "Check" };
        var yes = new WorkflowTerminateStep { Name = "Announced" };
        var no = new WorkflowTerminateStep { Name = "Skipped" };
        var workflow = new WorkflowViewModel(new Workflow { EntryStepId = condition.Id, Steps = [condition, yes, no] });
        var editor = (WorkflowConditionStepViewModel)workflow.Steps[0];

        editor.TrueStep = workflow.Steps[1];
        editor.FalseStep = workflow.Steps[2];

        Assert.Multiple(() =>
        {
            Assert.That(condition.TrueStepId, Is.EqualTo(yes.Id));
            Assert.That(condition.FalseStepId, Is.EqualTo(no.Id));
            Assert.That(editor.ConnectionsSummary, Is.EqualTo("True → Announced\nFalse → Skipped"));
        });
    }

    [Test]
    public void ParallelTargets_ShowBranchNamesAndJoin_AndRemoveDeletedBranchReferences()
    {
        var parallel = new WorkflowParallelStep { Name = "Together" };
        var branchStep = new WorkflowDelayStep { Name = "Announcement" };
        var join = new WorkflowTerminateStep { Name = "Done" };
        var workflow = new WorkflowViewModel(new Workflow { EntryStepId = parallel.Id, Steps = [parallel, branchStep, join] });
        var editor = (WorkflowParallelStepViewModel)workflow.Steps[0];

        editor.AddBranchCommand.Execute(null);
        editor.Branches[0].EntryStep = workflow.Steps[1];
        editor.JoinStep = workflow.Steps[2];

        Assert.Multiple(() =>
        {
            Assert.That(parallel.Branches[0].EntryStepId, Is.EqualTo(branchStep.Id));
            Assert.That(parallel.JoinStepId, Is.EqualTo(join.Id));
            Assert.That(editor.ConnectionsSummary, Is.EqualTo("In parallel:\nBranch 1 → Announcement\nJoin → Done"));
        });

        workflow.DeleteStepCommand.Execute(workflow.Steps[1]);

        Assert.Multiple(() =>
        {
            Assert.That(parallel.Branches, Is.Empty);
            Assert.That(editor.Branches, Is.Empty);
            Assert.That(editor.ConnectionsSummary, Does.Contain("No branches connected"));
        });
    }

    [Test]
    public void StartSelectionAndPresentation_DoNotChangeGraphOrPersistedListOrder()
    {
        var end = new WorkflowTerminateStep { Name = "End" };
        var start = new WorkflowDelayStep { Name = "Start", NextStepId = end.Id };
        var model = new Workflow { EntryStepId = end.Id, Steps = [end, start] };
        var workflow = new WorkflowViewModel(model);

        workflow.EntryStep = workflow.Steps[1];

        Assert.Multiple(() =>
        {
            Assert.That(model.EntryStepId, Is.EqualTo(start.Id));
            Assert.That(workflow.Steps[1].StepCaption, Is.EqualTo("Delay · Start"));
            Assert.That(workflow.Steps[0].StepCaption, Is.EqualTo("Terminate"));
            Assert.That(model.Steps, Is.EqualTo(new WorkflowStep[] { end, start }));
            Assert.That(start.NextStepId, Is.EqualTo(end.Id));
        });
    }

    [Test]
    public void DeleteTarget_UpdatesNamedSelectionAndConnectionSummary()
    {
        var target = new WorkflowTerminateStep { Name = "Done" };
        var start = new WorkflowDelayStep { Name = "Wait", NextStepId = target.Id };
        var workflow = new WorkflowViewModel(new Workflow { EntryStepId = start.Id, Steps = [start, target] });

        workflow.DeleteStepCommand.Execute(workflow.Steps[1]);

        Assert.Multiple(() =>
        {
            Assert.That(start.NextStepId, Is.Null);
            Assert.That(workflow.Steps[0].NextStep, Is.Null);
            Assert.That(workflow.Steps[0].ConnectionsSummary, Is.EqualTo("Next → Not connected"));
            Assert.That(workflow.Steps[0].AvailableSteps, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void DefaultFailureTarget_IsNamedAndShownOnAffectedSteps()
    {
        var end = new WorkflowTerminateStep { Name = "End" };
        var start = new WorkflowDelayStep { Name = "Wait", NextStepId = end.Id };
        var workflow = new WorkflowViewModel(new Workflow { EntryStepId = start.Id, Steps = [start, end] });

        workflow.DefaultFailureBehavior = WorkflowFailureBehavior.FailureBranch;
        workflow.DefaultFailureStep = workflow.Steps[1];

        Assert.Multiple(() =>
        {
            Assert.That(workflow.Model.DefaultErrorPolicy!.FailureStepId, Is.EqualTo(end.Id));
            Assert.That(workflow.Steps[0].ConnectionsSummary, Does.EndWith("On failure → End"));
        });
    }
}
