// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging.Abstractions;
using Moba.Backend.Interface;
using Moba.Common.Configuration;
using Moba.Contracts.Runtime;
using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Service;
using Moba.SharedUI.ViewModel;
using Moba.Test.Helpers;
using Moq;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

[TestFixture]
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "NUnit creates this fixture through reflection.")]
internal sealed partial class StationsPageViewModelTests : IDisposable
{
    private SolutionSession _session = null!;
    private Mock<IMobaRuntime> _runtime = null!;
    private Mock<ISolutionIoService> _io = null!;
    private Mock<ISettingsService> _persistence = null!;
    private AppSettings _settings = null!;
    private WorkflowLibraryViewModel _library = null!;
    private StationsPageViewModel _page = null!;
    private string _lastSaved = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _runtime = new Mock<IMobaRuntime>();
        _runtime.SetupGet(value => value.Current).Returns(MobaRuntimeSnapshot.Empty);
        _runtime.Setup(value => value.UpdateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var dispatcher = new Mock<IUiDispatcher>();
        dispatcher.Setup(value => value.InvokeOnUi(It.IsAny<Action>())).Callback<Action>(action => action());
        _io = TestFileServices.CreateMock();
        _io.Setup(value => value.SaveAsync(It.IsAny<Solution>(), It.IsAny<string>()))
            .Callback((Solution solution, string _) => _lastSaved = JsonSerializer.Serialize(solution))
            .ReturnsAsync((Solution _, string path) => (true, (string?)path, (string?)null));
        _session = TestSolutionSessions.Create(new Solution { Projects = [new Project
        {
            Stations = [new Station { Name = "Central", Platforms = [new Platform { Name = "Platform 1", Number = 1 }] }],
            Workflows = [new Workflow { Name = "Departure" }]
        }] }, dispatcher.Object, _runtime.Object, _io.Object);
        _session.CurrentSolutionPath = "stations-test.json";
        _settings = new AppSettings();
        _persistence = new Mock<ISettingsService>();
        _persistence.Setup(value => value.GetSettings()).Returns(_settings);
        _persistence.Setup(value => value.SaveSettingsAsync(_settings)).Returns(Task.CompletedTask);
        _library = new WorkflowLibraryViewModel(_session);
        _page = CreatePage();
        _io.Invocations.Clear();
        _runtime.Invocations.Clear();
    }

    private StationsPageViewModel CreatePage() => new(_session, _runtime.Object, _persistence.Object,
        new LayoutColumnWidthsViewModel(), _library, NullLogger<StationsPageViewModel>.Instance);

    [TearDown]
    public void Dispose()
    {
        _page.Dispose();
        _library.Dispose();
        GC.SuppressFinalize(this);
    }

    [Test]
    public void AddAndDeleteStation_PreserveDefaultsModelWrappersAndPersistence()
    {
        using var assertions = Assert.EnterMultipleScope();
        Assert.That(_page.SelectedProjectStation, Is.Null);
        Assert.That(_page.AddProjectStationCommand.CanExecute(null), Is.True);
        Assert.That(_page.DeleteProjectStationCommand.CanExecute(null), Is.False);
        _page.AddProjectStationCommand.Execute(null);
        var station = _page.SelectedProjectStation!;
        Assert.That(station.Name, Is.EqualTo("New Station"));
        Assert.That(station.Model.Platforms.Single().Number, Is.EqualTo(1));
        Assert.That(station.Platforms.Single().Name, Is.EqualTo("Platform 1"));
        Assert.That(_page.SelectedProject!.Model.Stations.Last(), Is.SameAs(station.Model));
        Assert.That(_page.StationsPageSelectedObject, Is.SameAs(station));
        Assert.That(_page.StationsPagePropertiesTitle, Is.EqualTo("Station Properties"));
        _page.DeleteProjectStationCommand.Execute(null);
        Assert.That(_page.SelectedProject.Model.Stations, Has.Count.EqualTo(1));
        Assert.That(_page.SelectedProject.Stations, Has.Count.EqualTo(1));
        Assert.That(_page.SelectedProjectStation, Is.Null);
        Assert.That(_page.StationsPageSelectedObject, Is.Null);
        Assert.That(_page.StationsPagePropertiesTitle, Is.EqualTo("Properties"));
        Assert.That(JsonSerializer.Deserialize<Solution>(_lastSaved)!.Projects[0].Stations, Has.Count.EqualTo(1));
        _runtime.Verify(value => value.UpdateProjectAsync(_page.SelectedProject.Model, It.IsAny<CancellationToken>()), Times.AtLeast(2));
    }

    [Test]
    public void AddAndDeletePlatform_ClearTheDeletedDefaultAndReturnToStationProperties()
    {
        using var assertions = Assert.EnterMultipleScope();
        SelectFirstStation();
        _page.AddPlatformToProjectStationCommand.Execute(null);
        var platform = _page.SelectedProjectStationPlatform!;
        var station = _page.SelectedProjectStation!;
        Assert.That(platform.Number, Is.EqualTo(2));
        Assert.That(platform.Name, Is.EqualTo("Platform 2"));
        Assert.That(_page.StationsPagePropertiesTitle, Is.EqualTo("Platform Properties"));
        station.PlatformId = platform.Model.Id;
        _page.DeleteProjectStationPlatformCommand.Execute(null);
        Assert.That(station.Model.PlatformId, Is.Null);
        Assert.That(station.Model.Platforms, Has.Count.EqualTo(1));
        Assert.That(_page.SelectedProjectStationPlatform, Is.Null);
        Assert.That(_page.StationsPageSelectedObject, Is.SameAs(station));
        Assert.That(_page.DeleteProjectStationPlatformCommand.CanExecute(null), Is.False);
        Assert.That(JsonSerializer.Deserialize<Solution>(_lastSaved)!.Projects[0].Stations[0].Platforms, Has.Count.EqualTo(1));
    }

    [Test]
    public void StationAndPlatformEdits_AutoSaveAndRefreshTheCurrentProject()
    {
        using var assertions = Assert.EnterMultipleScope();
        SelectFirstStation();
        var station = _page.SelectedProjectStation!;
        _page.SelectedProjectStationPlatform = station.Platforms[0];
        station.Name = "Updated station";
        _page.SelectedProjectStationPlatform.Name = "Updated platform";
        _page.SelectedProjectStationPlatform.InPort = 17;
        var saved = JsonSerializer.Deserialize<Solution>(_lastSaved)!;
        Assert.That(saved.Projects[0].Stations[0].Name, Is.EqualTo("Updated station"));
        Assert.That(saved.Projects[0].Stations[0].Platforms[0].Name, Is.EqualTo("Updated platform"));
        Assert.That(saved.Projects[0].Stations[0].Platforms[0].InPort, Is.EqualTo(17));
        _io.Verify(value => value.SaveAsync(It.IsAny<Solution>(), "stations-test.json"), Times.AtLeast(3));
        _runtime.Verify(value => value.UpdateProjectAsync(_page.SelectedProject!.Model, It.IsAny<CancellationToken>()), Times.AtLeast(3));
    }

    [Test]
    public void WorkflowAssignment_UsesSharedLibraryAndPersistsAssignmentAndRemoval()
    {
        using var assertions = Assert.EnterMultipleScope();
        SelectFirstStation();
        _page.SelectedProjectStationPlatform = _page.SelectedProjectStation!.Platforms[0];
        var platform = _page.SelectedProjectStationPlatform;
        var workflow = _page.WorkflowLibrary.Workflows!.Single();
        platform.AssignWorkflowCommand.Execute(workflow);
        Assert.That(platform.WorkflowName, Is.EqualTo("Departure"));
        Assert.That(JsonSerializer.Deserialize<Solution>(_lastSaved)!.Projects[0].Stations[0].Platforms[0].WorkflowId, Is.EqualTo(workflow.Model.Id));
        platform.RemoveWorkflowCommand.Execute(null);
        Assert.That(platform.HasWorkflow, Is.False);
        Assert.That(platform.RemoveWorkflowCommand.CanExecute(null), Is.False);
        Assert.That(JsonSerializer.Deserialize<Solution>(_lastSaved)!.Projects[0].Stations[0].Platforms[0].WorkflowId, Is.Null);
    }

    [Test]
    public void SelectionChange_DetachesOldStationAndPlatformPropertyTracking()
    {
        using var assertions = Assert.EnterMultipleScope();
        SelectFirstStation();
        var old = _page.SelectedProjectStation!;
        var oldPlatform = old.Platforms[0];
        _page.SelectedProjectStationPlatform = oldPlatform;
        _page.AddProjectStationCommand.Execute(null);
        _io.Invocations.Clear();
        _runtime.Invocations.Clear();
        old.Name = "Old station";
        oldPlatform.Name = "Old platform";
        Assert.That(_page.SelectedProjectStationPlatform, Is.Null);
        Assert.That(_page.StationsPageSelectedObject, Is.SameAs(_page.SelectedProjectStation));
        _io.Verify(value => value.SaveAsync(It.IsAny<Solution>(), It.IsAny<string>()), Times.Never);
        _runtime.Verify(value => value.UpdateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void ProjectSwitch_ClearsSelectionsAndStopsOldEditsFromUpdatingTheNewRuntime()
    {
        using var assertions = Assert.EnterMultipleScope();
        SelectFirstStation();
        var old = _page.SelectedProjectStation!;
        var platform = old.Platforms[0];
        _page.SelectedProjectStationPlatform = platform;
        _session.SelectedProject = new ProjectViewModel(new Project());
        _io.Invocations.Clear();
        _runtime.Invocations.Clear();
        old.Name = "Old";
        platform.InPort = 23;
        Assert.That(_page.SelectedProjectStation, Is.Null);
        Assert.That(_page.SelectedProjectStationPlatform, Is.Null);
        Assert.That(_page.StationsPageSelectedObject, Is.Null);
        Assert.That(_page.AddProjectStationCommand.CanExecute(null), Is.True);
        Assert.That(_page.DeleteProjectStationCommand.CanExecute(null), Is.False);
        Assert.That(_page.AddPlatformToProjectStationCommand.CanExecute(null), Is.False);
        _io.Verify(value => value.SaveAsync(It.IsAny<Solution>(), It.IsAny<string>()), Times.Never);
        _runtime.Verify(value => value.UpdateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void MissingProject_UpdatesCommandAvailabilityAndKeepsCrudSafe()
    {
        using var assertions = Assert.EnterMultipleScope();
        var commandChanges = 0;
        _page.AddProjectStationCommand.CanExecuteChanged += (_, _) => commandChanges++;
        _session.SelectedProject = null;
        _page.AddProjectStationCommand.Execute(null);
        _page.DeleteProjectStationCommand.Execute(null);
        _page.AddPlatformToProjectStationCommand.Execute(null);
        _page.DeleteProjectStationPlatformCommand.Execute(null);
        Assert.That(commandChanges, Is.EqualTo(1));
        Assert.That(_page.AddProjectStationCommand.CanExecute(null), Is.False);
        Assert.That(_page.SelectedProjectStation, Is.Null);
    }

    [Test]
    public void RefreshPlatforms_DetachesDiscardedPlatformWrappers()
    {
        using var assertions = Assert.EnterMultipleScope();
        SelectFirstStation();
        var station = _page.SelectedProjectStation!;
        var old = station.Platforms[0];
        _page.SelectedProjectStationPlatform = old;
        station.RefreshPlatforms();
        _io.Invocations.Clear();
        old.Name = "Stale wrapper";
        Assert.That(_page.SelectedProjectStationPlatform, Is.Null);
        Assert.That(_page.StationsPageSelectedObject, Is.SameAs(station));
        _io.Verify(value => value.SaveAsync(It.IsAny<Solution>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void RemovedStation_ClearsSelectionAndTracking()
    {
        using var assertions = Assert.EnterMultipleScope();
        SelectFirstStation();
        var old = _page.SelectedProjectStation!;
        _page.SelectedProject!.Stations.Clear();
        _io.Invocations.Clear();
        old.Name = "Removed";
        Assert.That(_page.SelectedProjectStation, Is.Null);
        Assert.That(_page.StationsPageSelectedObject, Is.Null);
        _io.Verify(value => value.SaveAsync(It.IsAny<Solution>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void SuppressedAutoSave_DoesNotPersistOrRefreshPropertyEdits()
    {
        SelectFirstStation();
        _page.SelectedProjectStationPlatform = _page.SelectedProjectStation!.Platforms[0];
        using (_session.SuppressAutoSave())
        {
            _page.SelectedProjectStation.Name = "Bulk update";
            _page.SelectedProjectStationPlatform.InPort = 99;
        }
        _io.Verify(value => value.SaveAsync(It.IsAny<Solution>(), It.IsAny<string>()), Times.Never);
        _runtime.Verify(value => value.UpdateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task NewSolution_ClearsStationAndPlatformBeforeReplacement()
    {
        using var assertions = Assert.EnterMultipleScope();
        SelectFirstStation();
        var old = _page.SelectedProjectStation!;
        _page.SelectedProjectStationPlatform = old.Platforms[0];
        await _session.NewSolutionAsync().ConfigureAwait(false);
        _io.Invocations.Clear();
        old.Name = "Replaced";
        Assert.That(_page.SelectedProjectStation, Is.Null);
        Assert.That(_page.SelectedProjectStationPlatform, Is.Null);
        Assert.That(_page.StationsPageSelectedObject, Is.Null);
        _io.Verify(value => value.SaveAsync(It.IsAny<Solution>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void Dispose_DetachesOwnedHandlersAndPreservesSharedWorkflowTracking()
    {
        using var assertions = Assert.EnterMultipleScope();
        SelectFirstStation();
        var station = _page.SelectedProjectStation!;
        var platform = station.Platforms[0];
        _page.SelectedProjectStationPlatform = platform;
        _page.Dispose();
        _page.Dispose();
        var notifications = 0;
        _page.PropertyChanged += (_, _) => notifications++;
        station.Name = "Detached";
        platform.InPort = 31;
        station.Platforms.Clear();
        Assert.That(notifications, Is.Zero);
        Assert.That(_page.SelectedProjectStation, Is.SameAs(station));
        Assert.That(_page.SelectedProjectStationPlatform, Is.SameAs(platform));
        _io.Verify(value => value.SaveAsync(It.IsAny<Solution>(), It.IsAny<string>()), Times.Never);
        _library.Workflows!.Single().Name = "Shared editor still active";
        _io.Verify(value => value.SaveAsync(It.IsAny<Solution>(), It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Test]
    public void Layout_UsesPersistedDefaultsAndSavesOnlyActualChanges()
    {
        using var assertions = Assert.EnterMultipleScope();
        _page.Dispose();
        _settings.Layout.StationsPage.IsStationsListExpanded = false;
        _settings.Layout.StationsPage.IsPlatformsListExpanded = false;
        _settings.Layout.StationsPage.IsPropertiesExpanded = false;
        _settings.Layout.StationsPage.IsWorkflowLibraryExpanded = false;
        _page = CreatePage();
        Assert.That(_page.IsStationsListExpanded, Is.False);
        Assert.That(_page.IsPlatformsListExpanded, Is.False);
        Assert.That(_page.IsStationsPropertiesExpanded, Is.False);
        Assert.That(_page.IsWorkflowLibraryVisible, Is.False);
        _page.IsStationsListExpanded = false;
        _persistence.Verify(value => value.SaveSettingsAsync(_settings), Times.Never);
        _page.IsStationsListExpanded = true;
        _page.IsPlatformsListExpanded = true;
        _page.IsStationsPropertiesExpanded = true;
        _page.IsWorkflowLibraryVisible = true;
        Assert.That(_settings.Layout.StationsPage.IsStationsListExpanded, Is.True);
        Assert.That(_settings.Layout.StationsPage.IsPlatformsListExpanded, Is.True);
        Assert.That(_settings.Layout.StationsPage.IsPropertiesExpanded, Is.True);
        Assert.That(_settings.Layout.StationsPage.IsWorkflowLibraryExpanded, Is.True);
        _persistence.Verify(value => value.SaveSettingsAsync(_settings), Times.Exactly(4));
    }

    private void SelectFirstStation() => _page.SelectedProjectStation = _page.SelectedProject!.Stations[0];
}
