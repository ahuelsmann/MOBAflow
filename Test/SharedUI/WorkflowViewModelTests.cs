// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.Domain;
using Moba.Domain.Enum;
using Moba.Backend.Service;
using Moba.SharedUI.ViewModel;
using Moba.SharedUI.ViewModel.Action;

/// <summary>
/// Tests for WorkflowViewModel - ViewModel wrapper for Workflow domain model.
/// </summary>
[TestFixture]
internal class WorkflowViewModelTests
{
    [TestCase("null")]
    [TestCase("{\"type\":999,\"name\":\"Unknown\"}")]
    [TestCase("{\"type\":\"ChangeJourneyStop\"}")]
    public void InvalidPersistedAction_CanBeLoadedReorderedAndRemovedWithoutRepairingIt(string actionJson)
    {
        var workflow = System.Text.Json.JsonSerializer.Deserialize<Workflow>(
            "{\"actions\":[" + actionJson + "]}", JsonOptions.Default)
            ?? throw new InvalidOperationException("Test workflow could not be loaded.");
        var original = workflow.Actions[0];
        var project = new Project { Workflows = [workflow] };
        var editor = new ProjectViewModel(project).Workflows.Single();
        var invalid = editor.Actions.Single();

        editor.AddActionCommand.Execute(ActionType.ChangeJourneyStop);
        editor.MoveAction(invalid, 1);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(invalid, Is.TypeOf<InvalidWorkflowActionViewModel>());
            Assert.That(workflow.Actions[1], Is.SameAs(original));
            Assert.That(new WorkflowValidator().Validate(project).Issues, Is.Not.Empty);
            Assert.That(System.Text.Json.JsonSerializer.Serialize(workflow.Actions[1]),
                Is.EqualTo(System.Text.Json.JsonSerializer.Serialize(original)));
        }

        editor.DeleteActionCommand.Execute(invalid);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(new WorkflowValidator().Validate(project).Issues, Is.Empty);
            Assert.That(editor.Actions.Single(), Is.TypeOf<ChangeJourneyStopViewModel>());
        }
    }

    [Test]
    public void NewlyAddedScriptWithEmptySettings_StillHasEditableTypedPayload()
    {
        var editor = new WorkflowViewModel(new Workflow());
        editor.AddActionCommand.Execute(ActionType.ExecuteScript);
        Assert.That(editor.Actions.Single(), Is.TypeOf<PowerShellActionViewModel>());
    }

    private Workflow _workflow = null!;
    private WorkflowViewModel _viewModel = null!;

    [SetUp]
    public void SetUp()
    {
        _workflow = new Workflow
        {
            Id = Guid.NewGuid(),
            Name = "Test Workflow",
            Description = "Test Description",
        };
        _viewModel = new WorkflowViewModel(_workflow);
    }

    [Test]
    public void Constructor_InitializesFromModel()
    {
        Assert.That(_viewModel.Id, Is.EqualTo(_workflow.Id));
        Assert.That(_viewModel.Name, Is.EqualTo("Test Workflow"));
        Assert.That(_viewModel.Description, Is.EqualTo("Test Description"));
    }

    [Test]
    public void Model_ReturnsUnderlyingWorkflow()
    {
        Assert.That(_viewModel.Model, Is.SameAs(_workflow));
    }

    [Test]
    public void Name_SetValue_UpdatesModel()
    {
        _viewModel.Name = "Updated Name";

        Assert.That(_workflow.Name, Is.EqualTo("Updated Name"));
    }

    [Test]
    public void Description_SetValue_UpdatesModel()
    {
        _viewModel.Description = "Updated Description";

        Assert.That(_workflow.Description, Is.EqualTo("Updated Description"));
    }

    [Test]
    public void Actions_InitiallyEmpty()
    {
        Assert.That(_viewModel.Actions, Is.Not.Null);
        Assert.That(_viewModel.Actions, Is.Empty);
    }

    [Test]
    public void Actions_WithExistingActions_CreatesViewModels()
    {
        var workflow = new Workflow();
        workflow.Actions.Add(new WorkflowAction
        {
            Name = "Gong",
            Type = ActionType.Audio,
            Number = 1,
            Audio = new AudioActionPayload { FilePath = "gong.wav" }
        });

        var vm = new WorkflowViewModel(workflow);

        Assert.That(vm.Actions, Has.Count.EqualTo(1));
    }

    [Test]
    public void Actions_WithExecuteScriptAction_CreatesPowerShellViewModel()
    {
        var workflow = new Workflow();
        workflow.Actions.Add(new WorkflowAction
        {
            Name = "Run script",
            Type = ActionType.ExecuteScript,
            Number = 1,
            PowerShell = new PowerShellActionPayload { ScriptPath = "script.ps1" }
        });

        var vm = new WorkflowViewModel(workflow);

        Assert.That(vm.Actions.Single(), Is.TypeOf<PowerShellActionViewModel>());
    }

    [Test]
    public void AddActionCommand_WithTrainDestinationDisplay_CreatesDisplayViewModel()
    {
        var workflow = new Workflow();
        var vm = new WorkflowViewModel(workflow);

        vm.AddActionCommand.Execute(ActionType.TrainDestinationDisplay);

        Assert.That(workflow.Actions.Single().Type, Is.EqualTo(ActionType.TrainDestinationDisplay));
        Assert.That(vm.Actions.Single(), Is.TypeOf<TrainDestinationDisplayViewModel>());
    }

    [Test]
    public void Name_SetValue_RaisesPropertyChanged()
    {
        var propertyChangedRaised = false;
        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(WorkflowViewModel.Name))
                propertyChangedRaised = true;
        };

        _viewModel.Name = "New Name";

        Assert.That(propertyChangedRaised, Is.True);
    }

    [Test]
    public void ChildActionPropertyChanged_UpdatesModelAndRaisesActionsChanged()
    {
        // Arrange
        var action = new WorkflowAction
        {
            Name = "Original action",
            Number = 1,
            Type = ActionType.Command,
            Command = new CommandActionPayload()
        };
        var workflow = new Workflow { Actions = [action] };
        var viewModel = new WorkflowViewModel(workflow);
        var actionsChanged = 0;
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(WorkflowViewModel.Actions))
                actionsChanged++;
        };
        var actionViewModel = (WorkflowActionViewModel)viewModel.Actions.Single();

        // Act
        actionViewModel.Name = "Updated action";

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(action.Name, Is.EqualTo("Updated action"));
            Assert.That(actionsChanged, Is.EqualTo(1));
        });
    }

    [Test]
    public void ProjectViewModel_WorkflowsExposeStableWrappersForProjectModels()
    {
        // Arrange
        var workflow = new Workflow { Name = "Shared workflow" };
        var project = new Project { Workflows = [workflow] };

        // Act
        var projectViewModel = new ProjectViewModel(project);
        var firstAccess = projectViewModel.Workflows.Single();
        var secondAccess = projectViewModel.Workflows.Single();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(firstAccess, Is.SameAs(secondAccess));
            Assert.That(firstAccess.Model, Is.SameAs(workflow));
        });
    }

    [Test]
    public void ProjectViewModel_RefreshPreservesAuthoritativeWorkflowWrapper()
    {
        var workflow = new Workflow { Name = "Shared workflow" };
        var projectViewModel = new ProjectViewModel(new Project { Workflows = [workflow] });
        var original = projectViewModel.Workflows.Single();

        projectViewModel.Refresh();

        Assert.That(projectViewModel.Workflows.Single(), Is.SameAs(original));
    }

    [Test]
    public void AutoLayoutActions_PositionsNodesInGridAndPreservesCustomPositionsUntilReset()
    {
        var workflow = new Workflow();
        var vm = new WorkflowViewModel(workflow);
        for (var i = 0; i < 5; i++)
        {
            vm.AddActionCommand.Execute(ActionType.Audio);
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(vm.Actions[0].CanvasX, Is.EqualTo(WorkflowViewModel.FirstActionCanvasX));
            Assert.That(vm.Actions[0].CanvasY, Is.EqualTo(WorkflowViewModel.FirstActionCanvasY));
            Assert.That(vm.Actions[1].CanvasY, Is.EqualTo(WorkflowViewModel.FirstActionCanvasY + WorkflowViewModel.RowSpacingY));
            Assert.That(vm.Actions[4].CanvasX, Is.EqualTo(WorkflowViewModel.FirstActionCanvasX + WorkflowViewModel.ColumnSpacingX));
            Assert.That(vm.Actions[4].CanvasY, Is.EqualTo(WorkflowViewModel.FirstActionCanvasY));
        }

        var actionsChangedCount = 0;
        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(WorkflowViewModel.Actions))
                actionsChangedCount++;
        };

        vm.Actions[0].SetCanvasPosition(420, 300, isCustomPosition: true);
        Assert.That(actionsChangedCount, Is.Zero);

        vm.AutoLayoutActions(forceResetCustomPositions: false);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(vm.Actions[0].CanvasX, Is.EqualTo(420));
            Assert.That(vm.Actions[0].CanvasY, Is.EqualTo(300));
        }

        vm.ResetActionLayoutCommand.Execute(null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(vm.Actions[0].HasCustomPosition, Is.False);
            Assert.That(vm.Actions[0].CanvasX, Is.EqualTo(WorkflowViewModel.FirstActionCanvasX));
            Assert.That(vm.Actions[0].CanvasY, Is.EqualTo(WorkflowViewModel.FirstActionCanvasY));
        }
    }

    [Test]
    public void ActionNodePresentationProperties_ProvideBadgesAndRaisePropertyChanged()
    {
        var vm = new WorkflowViewModel(new Workflow());
        vm.AddActionCommand.Execute(ActionType.Announcement);
        vm.AddActionCommand.Execute(ActionType.Audio);
        vm.AddActionCommand.Execute(ActionType.Command);
        vm.AddActionCommand.Execute(ActionType.SelectSignalAspect);
        vm.AddActionCommand.Execute(ActionType.ExecuteScript);
        vm.AddActionCommand.Execute(ActionType.TrainDestinationDisplay);
        vm.AddActionCommand.Execute(ActionType.ChangeJourneyStop);

        var announcement = (AnnouncementViewModel)vm.Actions[0];
        var audio = (AudioViewModel)vm.Actions[1];
        var command = (CommandViewModel)vm.Actions[2];
        var signal = (SelectSignalAspectViewModel)vm.Actions[3];
        var script = (PowerShellActionViewModel)vm.Actions[4];
        var display = (TrainDestinationDisplayViewModel)vm.Actions[5];
        var stop = (ChangeJourneyStopViewModel)vm.Actions[6];

        var summaryChanged = false;
        audio.PropertyChanged += (_, e) => summaryChanged |= e.PropertyName == nameof(WorkflowActionViewModel.ParameterSummary);
        audio.FilePath = @"C:\sounds\horn.wav";
        audio.DelayAfterMs = 1500;

        announcement.Message = "Welcome aboard";
        command.Address = 24;
        command.Speed = 60;
        signal.BaseAddress = 12;
        signal.SignalAspect = SignalAspect.Hp0;
        script.ScriptPath = @"C:\scripts\depart.ps1";
        script.Arguments = "-Fast";
        stop.MoveToNextStop = true;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(summaryChanged, Is.True);
            Assert.That(audio.ParameterSummary, Is.EqualTo("horn.wav"));
            Assert.That(audio.HasDelayAfter, Is.True);
            Assert.That(audio.DelayBadgeText, Is.EqualTo("+1500 ms"));
            Assert.That(audio.StepLabel, Is.EqualTo("Step #2"));
            Assert.That(announcement.ParameterSummary, Does.Contain("Welcome aboard"));
            Assert.That(command.ParameterSummary, Is.EqualTo("Addr 24 · Speed 60 · Forward"));
            Assert.That(signal.ParameterSummary, Does.Contain("Addr 12").And.Contain("Hp0"));
            Assert.That(script.ParameterSummary, Is.EqualTo("depart.ps1 -Fast"));
            Assert.That(display.ParameterSummary, Is.EqualTo("No display device selected"));
            Assert.That(stop.ParameterSummary, Is.EqualTo("Move to next stop"));
        }

        audio.FilePath = "/opt/sounds/whistle.wav";
        script.ScriptPath = "/opt/scripts/arrive.ps1";
        using (Assert.EnterMultipleScope())
        {
            Assert.That(audio.ParameterSummary, Is.EqualTo("whistle.wav"));
            Assert.That(script.ParameterSummary, Is.EqualTo("arrive.ps1 -Fast"));
        }
    }
}
